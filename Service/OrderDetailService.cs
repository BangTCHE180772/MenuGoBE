using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;

namespace MenuGoBE.Service
{
    public class OrderDetailService : IOrderDetailService
    {
        private readonly IOrderDetailRepository _detailRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IMapper _mapper;
        private readonly ITableRepository _tableRepo;
        private readonly IAreaRepository _areaRepo;
        private readonly IProductRepository _productRepo;
        private readonly IKitchenService _kitchenService;
        private readonly IBInventoryRepository _bInventoryRepo;
        private readonly IRecipesDetailedRepository _recipesRepo;
        private readonly IHubContext<NotificationHub> _hubContext;

        public OrderDetailService(
            IOrderDetailRepository detailRepo, 
            IOrderRepository orderRepo, 
            IMapper mapper,
            ITableRepository tableRepo,
            IProductRepository productRepo,
            IAreaRepository areaRepo,
            IKitchenService kitchenService,
            IBInventoryRepository bInventoryRepo,
            IRecipesDetailedRepository recipesRepo,
            IHubContext<NotificationHub> hubContext)
        {
            _detailRepo = detailRepo;
            _orderRepo = orderRepo;
            _mapper = mapper;
            _tableRepo = tableRepo;
            _productRepo = productRepo;
            _areaRepo = areaRepo;
            _kitchenService = kitchenService;
            _bInventoryRepo = bInventoryRepo;
            _recipesRepo = recipesRepo;
            _hubContext = hubContext;
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
                                if (binv.Quantity < entity.Quantity)
                                {
                                    await SendStockAlertAsync(branchId, entity.ProductId, "Sản phẩm đã hết hàng trong kho.");
                                    throw new InvalidOperationException("Sản phẩm đã hết hàng trong kho.");
                                }
                                binv.Quantity -= entity.Quantity;
                                await _bInventoryRepo.UpdateAsync(binv);
                                await _bInventoryRepo.SaveChangesAsync();
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
                                    if (binv.Quantity < quantityDiff)
                                    {
                                        await SendStockAlertAsync(branchId, entity.ProductId, "Sản phẩm đã hết hàng trong kho.");
                                        throw new InvalidOperationException("Sản phẩm đã hết hàng trong kho.");
                                    }
                                    binv.Quantity -= quantityDiff;
                                    await _bInventoryRepo.UpdateAsync(binv);
                                    await _bInventoryRepo.SaveChangesAsync();
                                }
                            }
                        }
                    }
                }
            }
            else if (oldStatus == "Confirmed" && entity.Status == "Confirmed" && oldQuantity > entity.Quantity)
            {
                int quantityDiff = oldQuantity - entity.Quantity; // positive diff to restore
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
                                    binv.Quantity += quantityDiff;
                                    await _bInventoryRepo.UpdateAsync(binv);
                                    await _bInventoryRepo.SaveChangesAsync();
                                }
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

            // Cộng lại kho nếu đã trừ khi Confirmed (Regular/Manufactured)
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
                                var binv = await _bInventoryRepo.GetByProductAndBranchAsync(productId, branchId);
                                if (binv != null && binv.IsManageQuantity)
                                {
                                    binv.Quantity += quantity;
                                    await _bInventoryRepo.UpdateAsync(binv);
                                    await _bInventoryRepo.SaveChangesAsync();
                                }
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
                // CHECK & DEDUCT kho trực tiếp cho Regular/Manufactured
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
                                if (binv.Quantity < detail.Quantity)
                                {
                                    await SendStockAlertAsync(branchId, detail.ProductId, "Sản phẩm đã hết hàng trong kho.");
                                    throw new InvalidOperationException("Sản phẩm đã hết hàng trong kho.");
                                }
                                binv.Quantity -= detail.Quantity;
                                await _bInventoryRepo.UpdateAsync(binv);
                                await _bInventoryRepo.SaveChangesAsync();
                            }
                        }
                    }
                }

                // Set CookingStatus = Ready cho Regular/Manufactured (sẵn sàng bê ra)
                await _detailRepo.UpdateCookingStatusAsync(orderDetailId, "Ready");
            }
            else if (productType == "Processed")
            {
                // Set CookingStatus = Waiting cho Processed (chờ bếp nhận nấu)
                await _detailRepo.UpdateCookingStatusAsync(orderDetailId, "Waiting");
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
            // Xử lý trừ nguyên liệu khi Processed bắt đầu nấu
            if (cookingStatus == "Cooking")
            {
                var detail = await _detailRepo.GetByIdAsync(orderDetailId);
                if (detail != null)
                {
                    var productType = await _productRepo.GetProductTypeAsync(detail.ProductId);
                    if (productType == "Processed")
                    {
                        var order = await _orderRepo.GetByIdAsync(detail.OrderId);
                        if (order != null)
                        {
                            var table = await _tableRepo.GetByIdAsync(order.TableId);
                            if (table != null)
                            {
                                var area = await _areaRepo.GetByIdAsync(table.AreaId);
                                if (area != null)
                                {
                                    long branchId = area.BranchId;
                                    var recipes = await _recipesRepo.GetRecipesByProductIdAsync(detail.ProductId);

                                    if (recipes.Any())
                                    {
                                        // CHECK toàn bộ nguyên liệu trước khi trừ
                                        var insufficientIngredients = new List<string>();
                                        var ingredientUpdates = new List<(BInventory binv, decimal needed)>();

                                        foreach (var recipe in recipes)
                                        {
                                            decimal neededQty = recipe.Quantity * detail.Quantity;
                                            var ingredientBinv = await _bInventoryRepo.GetByProductAndBranchAsync(recipe.IngredientProductId, branchId);

                                            if (ingredientBinv == null || (ingredientBinv.IsManageQuantity && ingredientBinv.Quantity < neededQty))
                                            {
                                                var ingredientProduct = await _productRepo.GetByIdAsync(recipe.IngredientProductId);
                                                string ingredientName = ingredientProduct?.Name ?? $"SP #{recipe.IngredientProductId}";
                                                insufficientIngredients.Add(ingredientName);
                                            }
                                            else if (ingredientBinv.IsManageQuantity)
                                            {
                                                ingredientUpdates.Add((ingredientBinv, neededQty));
                                            }
                                        }

                                        if (insufficientIngredients.Any())
                                        {
                                            var product = await _productRepo.GetByIdAsync(detail.ProductId);
                                            string productName = product?.Name ?? "Món ăn";
                                            string missingList = string.Join(", ", insufficientIngredients);

                                            await _hubContext.Clients.Group($"branch_{branchId}").SendAsync("ReceiveStockAlert", new
                                            {
                                                type = "stock-alert",
                                                branchId = branchId,
                                                productName = productName,
                                                message = $"⚠️ Món \"{productName}\" không thể nấu - thiếu nguyên liệu: {missingList}",
                                                missingItems = insufficientIngredients,
                                                timestamp = DateTime.UtcNow
                                            });

                                            throw new InvalidOperationException($"Không đủ nguyên liệu để nấu: {missingList}");
                                        }

                                        // Tất cả đủ → Trừ nguyên liệu
                                        foreach (var (binv, needed) in ingredientUpdates)
                                        {
                                            binv.Quantity -= needed;
                                            await _bInventoryRepo.UpdateAsync(binv);
                                        }
                                        await _bInventoryRepo.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                    }
                }
            }

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

                    // Cộng lại kho nếu đã trừ khi Confirmed (Regular/Manufactured)
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
                                    var binv = await _bInventoryRepo.GetByProductAndBranchAsync(entity.ProductId, branchId);
                                    if (binv != null && binv.IsManageQuantity)
                                    {
                                        binv.Quantity += entity.Quantity;
                                        await _bInventoryRepo.UpdateAsync(binv);
                                        await _bInventoryRepo.SaveChangesAsync();
                                    }
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
            // Tính tổng: (Số lượng đặt - Số lượng trả) * Đơn giá
            order.TotalAmount = details
                .Where(d => d.Status != "Cancelled")
                .Sum(d => (d.Quantity - d.ReturnedQuantity) * d.Price);

            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveChangesAsync();
        }

        private async Task SendStockAlertAsync(long branchId, long productId, string message)
        {
            try
            {
                var product = await _productRepo.GetByIdAsync(productId);
                string productName = product?.Name ?? $"SP #{productId}";

                await _hubContext.Clients.Group($"branch_{branchId}").SendAsync("ReceiveStockAlert", new
                {
                    type = "stock-alert",
                    branchId = branchId,
                    productName = productName,
                    message = $"⚠️ Cảnh báo: Món \"{productName}\" đã hết hàng, không thể phục vụ!",
                    timestamp = DateTime.UtcNow
                });
            }
            catch
            {
                // Non-blocking SignalR fail-safe
            }
        }

        public async Task<bool> RequestReturnItemAsync(ReturnItemRequestDto dto, long confirmedByUserId)
        {
            var entity = await _detailRepo.GetByIdAsync(dto.OrderDetailId);
            if (entity == null)
                throw new KeyNotFoundException($"Không tìm thấy OrderDetail với Id = {dto.OrderDetailId}");

            // Validate: chỉ trả món đã Confirmed
            if (entity.Status != "Confirmed")
                throw new InvalidOperationException("Chỉ có thể trả món ở trạng thái Confirmed.");

            // Validate: số lượng trả hợp lệ
            if (dto.ReturnQuantity < 1)
                throw new InvalidOperationException("Số lượng trả phải >= 1.");

            int alreadyReturned = entity.ReturnedQuantity;
            int maxReturnable = entity.Quantity - alreadyReturned;

            if (dto.ReturnQuantity > maxReturnable)
                throw new InvalidOperationException($"Số lượng trả ({dto.ReturnQuantity}) vượt quá số lượng có thể trả ({maxReturnable}).");

            // Validate: phải có lý do
            if (string.IsNullOrWhiteSpace(dto.ReturnReason))
                throw new InvalidOperationException("Phải cung cấp lý do trả món.");

            // Update return fields
            entity.ReturnedQuantity += dto.ReturnQuantity;
            entity.ReturnReason = dto.ReturnReason;
            entity.ReturnIsIntact = dto.IsIntact;
            entity.ReturnedAt = DateTime.UtcNow;
            entity.ReturnConfirmedBy = confirmedByUserId;

            // Re-stock logic: CHỈ Regular + còn nguyên vẹn mới cộng lại kho
            var productType = await _productRepo.GetProductTypeAsync(entity.ProductId);
            if (productType == "Regular" && dto.IsIntact)
            {
                var order = await _orderRepo.GetByIdAsync(entity.OrderId);
                if (order != null)
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
                                binv.Quantity += dto.ReturnQuantity;
                                await _bInventoryRepo.UpdateAsync(binv);
                                await _bInventoryRepo.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            // Processed/Manufactured: KHÔNG cộng lại kho (nguyên liệu đã tiêu hao)

            await _detailRepo.UpdateAsync(entity);
            await _detailRepo.SaveChangesAsync();

            // Recalculate order total (trừ phần trả)
            var orderForRecalc = await _orderRepo.GetByIdAsync(entity.OrderId);
            if (orderForRecalc != null)
                await RecalculateTotalAmountAsync(orderForRecalc);

            return true;
        }
    }
}
