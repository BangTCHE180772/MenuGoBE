using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class OrderDetailService : IOrderDetailService
    {
        private readonly IOrderDetailRepository _detailRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IMapper _mapper;
        private readonly IRedisInventoryService _redisInventoryService;
        private readonly ITableRepository _tableRepo;
        private readonly IAreaRepository _areaRepo;
        private readonly IProductRepository _productRepo;
        private readonly IKitchenService _kitchenService;
        private readonly IBInventoryRepository _bInventoryRepo;

        public OrderDetailService(
            IOrderDetailRepository detailRepo, 
            IOrderRepository orderRepo, 
            IMapper mapper,
            IRedisInventoryService redisInventoryService,
            ITableRepository tableRepo,
            IProductRepository productRepo,
            IAreaRepository areaRepo,
            IKitchenService kitchenService,
            IBInventoryRepository bInventoryRepo)
        {
            _detailRepo = detailRepo;
            _orderRepo = orderRepo;
            _mapper = mapper;
            _redisInventoryService = redisInventoryService;
            _tableRepo = tableRepo;
            _productRepo = productRepo;
            _areaRepo = areaRepo;
            _kitchenService = kitchenService;
            _bInventoryRepo = bInventoryRepo;
        }

        public async Task<List<OrderDetailViewDto>> GetByOrderIdAsync(long orderId)
        {
            var data = await _detailRepo.GetByOrderIdAsync(orderId);
            return _mapper.Map<List<OrderDetailViewDto>>(data);
        }

        public async Task<OrderDetailViewDto?> GetByIdAsync(long id)
        {
            var data = await _detailRepo.GetByIdAsync(id);
            if (data == null) return null;

            return _mapper.Map<OrderDetailViewDto>(data);
        }

        public async Task<OrderDetailViewDto> CreateAsync(OrderDetailCreateDto dto)
        {
            var order = await _orderRepo.GetByIdAsync(dto.OrderId);
            if (order == null)
                throw new KeyNotFoundException($"Không tìm thấy Order với Id = {dto.OrderId}");

            var entity = _mapper.Map<OrderDetail>(dto);
            var productType = await _productRepo.GetProductTypeAsync(entity.ProductId);
            
            if (entity.Status == "Confirmed" && (string.IsNullOrEmpty(entity.CookingStatus) || entity.CookingStatus == "CustomerPending" || entity.CookingStatus == "PreOrder"))
            {
                if (productType == "Regular" || productType == "Manufactured")
                {
                    entity.CookingStatus = "Ready";
                }
                else
                {
                    entity.CookingStatus = "Waiting";
                }
            }

            if (entity.Status == "Confirmed")
            {
                if (productType == "Regular" || productType == "Manufactured")
                {
                    var table = await _tableRepo.GetByIdAsync(order.TableId);
                    if (table != null)
                    {
                        var area = await _areaRepo.GetByIdAsync(table.AreaId);
                        if (area != null)
                        {
                            long branchId = area.BranchId;
                            var binv = await _bInventoryRepo.GetByProductAndBranchAsync(entity.ProductId, branchId);
                            if (binv != null && binv.IsManageQuantity)
                            {
                                bool success = await _redisInventoryService.TryReserveInventoryAsync((int)branchId, (int)entity.ProductId, entity.Quantity, binv.Quantity);
                                if (!success)
                                {
                                    throw new InvalidOperationException("Sản phẩm đã hết hàng trong kho.");
                                }
                            }
                            else
                            {
                                await _redisInventoryService.IncrementReservedQuantityAsync((int)branchId, (int)entity.ProductId, entity.Quantity);
                            }
                        }
                    }
                }
            }

            await _detailRepo.CreateAsync(entity);
            await _detailRepo.SaveChangesAsync();

            await RecalculateTotalAmountAsync(order);

            return _mapper.Map<OrderDetailViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(OrderDetailUpdateDto dto)
        {
            var entity = await _detailRepo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            int oldQuantity = entity.Quantity;
            string oldStatus = entity.Status;

            _mapper.Map(dto, entity);

            if (oldStatus == "Confirmed" && entity.Status == "Confirmed" && oldQuantity < entity.Quantity)
            {
                int quantityDiff = entity.Quantity - oldQuantity;
                var productType = await _productRepo.GetProductTypeAsync(entity.ProductId);
                if (productType == "Regular" || productType == "Manufactured")
                {
                    var order_check = await _orderRepo.GetByIdAsync(entity.OrderId);
                    if (order_check != null)
                    {
                        var table = await _tableRepo.GetByIdAsync(order_check.TableId);
                        if (table != null)
                        {
                            var area = await _areaRepo.GetByIdAsync(table.AreaId);
                            if (area != null)
                            {
                                long branchId = area.BranchId;
                                var binv = await _bInventoryRepo.GetByProductAndBranchAsync(entity.ProductId, branchId);
                                if (binv != null && binv.IsManageQuantity)
                                {
                                    bool success = await _redisInventoryService.TryReserveInventoryAsync((int)branchId, (int)entity.ProductId, quantityDiff, binv.Quantity);
                                    if (!success)
                                    {
                                        throw new InvalidOperationException("Sản phẩm đã hết hàng trong kho.");
                                    }
                                }
                                else
                                {
                                    await _redisInventoryService.IncrementReservedQuantityAsync((int)branchId, (int)entity.ProductId, quantityDiff);
                                }
                            }
                        }
                    }
                }
            }
            else if (oldStatus == "Confirmed" && entity.Status == "Confirmed" && oldQuantity > entity.Quantity)
            {
                int quantityDiff = entity.Quantity - oldQuantity; // this is negative
                var productType = await _productRepo.GetProductTypeAsync(entity.ProductId);
                if (productType == "Regular" || productType == "Manufactured")
                {
                    var order_check = await _orderRepo.GetByIdAsync(entity.OrderId);
                    if (order_check != null)
                    {
                        var table = await _tableRepo.GetByIdAsync(order_check.TableId);
                        if (table != null)
                        {
                            var area = await _areaRepo.GetByIdAsync(table.AreaId);
                            if (area != null)
                            {
                                long branchId = area.BranchId;
                                await _redisInventoryService.IncrementReservedQuantityAsync((int)branchId, (int)entity.ProductId, quantityDiff);
                            }
                        }
                    }
                }
            }

            await _detailRepo.UpdateAsync(entity);
            await _detailRepo.SaveChangesAsync();

            var order = await _orderRepo.GetByIdAsync(entity.OrderId);
            if (order != null)
                await RecalculateTotalAmountAsync(order);

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _detailRepo.GetByIdAsync(id);
            if (entity == null) return false;

            var orderId = entity.OrderId;
            var quantity = entity.Quantity;
            var status = entity.Status;
            var productId = entity.ProductId;

            await _detailRepo.DeleteAsync(id);
            await _detailRepo.SaveChangesAsync();

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null)
                await RecalculateTotalAmountAsync(order);

            if (status == "Confirmed")
            {
                var productType = await _productRepo.GetProductTypeAsync(productId);
                if (productType == "Regular" || productType == "Manufactured")
                {
                    if (order != null)
                    {
                        var table = await _tableRepo.GetByIdAsync(order.TableId);
                        if (table != null)
                        {
                            var area = await _areaRepo.GetByIdAsync(table.AreaId);
                            if (area != null)
                            {
                                long branchId = area.BranchId;
                                await _redisInventoryService.DecrementReservedQuantityAsync((int)branchId, (int)productId, quantity);
                            }
                        }
                    }
                }
            }

            return true;
        }

        public async Task<bool> ConfirmAsync(long orderDetailId)
        {
            var detail = await _detailRepo.GetByIdAsync(orderDetailId);
            if (detail == null) return false;

            var productType = await _productRepo.GetProductTypeAsync(detail.ProductId);
            var order = await _orderRepo.GetByIdAsync(detail.OrderId);

            if (productType == "Regular" || productType == "Manufactured")
            {
                if (order != null)
                {
                    var table = await _tableRepo.GetByIdAsync(order.TableId);
                    if (table != null)
                    {
                        var area = await _areaRepo.GetByIdAsync(table.AreaId);
                        if (area != null)
                        {
                            long branchId = area.BranchId;
                            var binv = await _bInventoryRepo.GetByProductAndBranchAsync(detail.ProductId, branchId);
                            if (binv != null && binv.IsManageQuantity)
                            {
                                bool success = await _redisInventoryService.TryReserveInventoryAsync((int)branchId, (int)detail.ProductId, detail.Quantity, binv.Quantity);
                                if (!success)
                                {
                                    throw new InvalidOperationException("Sản phẩm đã hết hàng trong kho.");
                                }
                            }
                            else
                            {
                                await _redisInventoryService.IncrementReservedQuantityAsync((int)branchId, (int)detail.ProductId, detail.Quantity);
                            }
                        }
                    }
                }
            }

            var result = await _detailRepo.UpdateStatusAsync(orderDetailId, "Confirmed");
            if (result)
            {
                if (order != null && (order.Status == "Reserved" || order.Status == "CustomerPending"))
                {
                    order.Status = "Active";
                    await _orderRepo.UpdateAsync(order);
                }
                await _detailRepo.SaveChangesAsync();
            }
            return result;
        }

        public async Task<bool> UpdateCookingStatusAsync(long orderDetailId, string cookingStatus)
        {
            var result = await _detailRepo.UpdateCookingStatusAsync(orderDetailId, cookingStatus);
            if (result)
            {
                if (cookingStatus == "Served")
                {
                    await _kitchenService.HandleInternalRestockCompletionAsync(orderDetailId);
                }

                await _detailRepo.SaveChangesAsync();
            }
            return result;
        }

        public async Task<bool> CancelOrderDetailAsync(long orderDetailId)
        {
            var entity = await _detailRepo.GetByIdAsync(orderDetailId);
            if (entity == null) return false;

            if (entity.CookingStatus != "Waiting" || entity.Status == "Cancelled")
            {
                return false;
            }

            string oldStatus = entity.Status;

            var result = await _detailRepo.UpdateStatusAsync(orderDetailId, "Cancelled");
            if (result)
            {
                await _detailRepo.SaveChangesAsync();
                
                var order = await _orderRepo.GetByIdAsync(entity.OrderId);
                if (order != null)
                {
                    await RecalculateTotalAmountAsync(order);

                    if (oldStatus == "Confirmed")
                    {
                        var productType = await _productRepo.GetProductTypeAsync(entity.ProductId);
                        if (productType == "Regular" || productType == "Manufactured")
                        {
                            var table = await _tableRepo.GetByIdAsync(order.TableId);
                            if (table != null)
                            {
                                var area = await _areaRepo.GetByIdAsync(table.AreaId);
                                if (area != null)
                                {
                                    long branchId = area.BranchId;
                                    await _redisInventoryService.DecrementReservedQuantityAsync((int)branchId, (int)entity.ProductId, entity.Quantity);
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public async Task<List<KitchenItemDto>> GetKitchenItemsAsync()
        {
            var activeOrders = await _orderRepo.GetActiveKitchenOrdersWithDetailsAsync();
            var result = new List<KitchenItemDto>();

            foreach (var order in activeOrders)
            {
                var tableName = order.Table?.Name ?? $"Bàn #{order.TableId}";
                var items = order.OrderDetails
                    .Where(od => string.Equals(od.Status, "Confirmed", StringComparison.OrdinalIgnoreCase) || 
                                 (!string.Equals(od.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) && 
                                  !string.IsNullOrEmpty(od.CookingStatus) && 
                                  !string.Equals(od.CookingStatus, "Served", StringComparison.OrdinalIgnoreCase) && 
                                  !string.Equals(od.CookingStatus, "CustomerPending", StringComparison.OrdinalIgnoreCase)))
                    .Select(od => new KitchenItemDto
                    {
                        OrderDetailId = od.Id,
                        OrderId = od.OrderId,
                        TableId = order.TableId,
                        TableName = tableName,
                        ProductId = od.ProductId,
                        ProductName = od.Product?.Name ?? "Mon ăn",
                        ProductImage = od.Product?.Image?.ImageLink,
                        Quantity = od.Quantity,
                        Price = od.Price,
                        Note = od.Note ?? string.Empty,
                        Status = od.Status ?? "Confirmed",
                        CookingStatus = string.IsNullOrEmpty(od.CookingStatus) ? "Waiting" : od.CookingStatus,
                        CreatedAt = od.CreatedAt
                    });
                result.AddRange(items);
            }

            return result.OrderBy(x => x.CreatedAt).ToList();
        }

        private async Task RecalculateTotalAmountAsync(Order order)
        {
            var details = await _detailRepo.GetByOrderIdAsync(order.Id);
            order.TotalAmount = details.Where(d => d.Status != "Cancelled").Sum(d => d.Quantity * d.Price);

            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveChangesAsync();
        }
    }
}
