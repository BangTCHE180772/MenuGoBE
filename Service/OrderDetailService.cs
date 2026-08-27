using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
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
        private readonly IDocumentService _documentService;

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
            IHubContext<NotificationHub> hubContext,
            IDocumentService documentService)
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
            _documentService = documentService;
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

        public async Task<int> GetAvailableQuantityAsync(long productId, long branchId)
        {
            var productType = await _productRepo.GetProductTypeAsync(productId);
            if (productType == "Processed")
            {
                var recipes = await _recipesRepo.GetRecipesByProductIdAsync(productId);
                if (recipes == null || !recipes.Any())
                {
                    var fallbackBinv = await _bInventoryRepo.GetByProductAndBranchAsync(productId, branchId);
                    if (fallbackBinv != null && !fallbackBinv.IsManageQuantity) return 999999;
                    return 0;
                }

                int maxProcessable = int.MaxValue;
                bool hasValidRecipe = false;
                foreach (var recipe in recipes)
                {
                    if (recipe.Quantity > 0)
                    {
                        hasValidRecipe = true;
                        int ingredientQty = await GetAvailableQuantityAsync(recipe.IngredientProductId, branchId);
                        int possibleQty = (int)(ingredientQty / recipe.Quantity);
                        if (possibleQty < maxProcessable)
                        {
                            maxProcessable = possibleQty;
                        }
                    }
                }
                return hasValidRecipe ? maxProcessable : 0;
            }

            var binv = await _bInventoryRepo.GetByProductAndBranchAsync(productId, branchId);
            if (binv == null)
            {
                return 0; // Chưa có thông tin kho, coi như hết hàng
            }

            if (!binv.IsManageQuantity)
            {
                return 999999; // Món không quản lý số lượng (Unlimited)
            }

            var pendingQty = await _detailRepo.GetPendingQuantityAsync(productId, branchId);
            return (int)binv.Quantity - pendingQty;
        }

        public async Task<Dictionary<long, int>> GetAvailableQuantitiesAsync(long branchId, List<long> productIds)
        {
            var result = new Dictionary<long, int>();
            if (productIds == null || !productIds.Any()) return result;

            foreach (var productId in productIds.Distinct())
            {
                result[productId] = await GetAvailableQuantityAsync(productId, branchId);
            }

            return result;
        }

        public async Task<OrderDetailViewDto> CreateAsync(OrderDetailCreateDto dto)
        {
            var order = await _orderRepo.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                throw new KeyNotFoundException("Không tìm thấy Order.");
            }

            var table = await _tableRepo.GetByIdAsync(order.TableId);
            long branchId = 0;
            if (table != null)
            {
                var area = await _areaRepo.GetByIdAsync(table.AreaId);
                if (area != null) branchId = area.BranchId;
            }

            if (branchId > 0)
            {
                int availableQty = await GetAvailableQuantityAsync(dto.ProductId, branchId);
                if (dto.Quantity > availableQty)
                {
                    throw new InvalidOperationException($"Số lượng món không đủ. Chỉ còn {availableQty} phần.");
                }
            }

            var entity = _mapper.Map<OrderDetail>(dto);
            var productType = await _productRepo.GetProductTypeAsync(entity.ProductId);
            
            if (entity.Status == "Confirmed" && (string.IsNullOrEmpty(entity.CookingStatus) || entity.CookingStatus == "Waiting" || entity.CookingStatus == "CustomerPending" || entity.CookingStatus == "PreOrder"))
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
                    if (table != null)
                    {
                        var area = await _areaRepo.GetByIdAsync(table.AreaId);
                        if (area != null)
                        {
                            branchId = area.BranchId;
                            try
                            {
                                var product = await _productRepo.GetByIdAsync(entity.ProductId);
                                if (product != null)
                                {
                                    var saleDoc = await _documentService.GetOrCreatePendingDocumentAsync(branchId, order.Id, MenuGoBE.Models.Enums.DocumentType.Sale, order.CreatedBy ?? 1);
                                    await _documentService.AppendItemToSaleDocumentAsync(saleDoc.Id, product, entity.Quantity, entity.Id, order.CreatedBy ?? 1);
                                }
                            }
                            catch (Exception ex)
                            {
                                await SendStockAlertAsync(branchId, entity.ProductId, ex.Message);
                                throw new InvalidOperationException(ex.Message);
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

        public async Task<BulkOrderResultDto> CreateMultipleAsync(List<OrderDetailCreateDto> dtos)
        {
            var result = new BulkOrderResultDto { Success = true };
            if (dtos == null || !dtos.Any()) return result;

            long orderId = dtos.First().OrderId;
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new KeyNotFoundException("Không tìm thấy Order.");

            var table = await _tableRepo.GetByIdAsync(order.TableId);
            long branchId = 0;
            if (table != null)
            {
                var area = await _areaRepo.GetByIdAsync(table.AreaId);
                if (area != null) branchId = area.BranchId;
            }

            if (branchId > 0)
            {
                var groupedProducts = dtos.GroupBy(d => d.ProductId)
                                          .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(d => d.Quantity) })
                                          .ToList();

                foreach (var item in groupedProducts)
                {
                    int availableQty = await GetAvailableQuantityAsync(item.ProductId, branchId);
                    if (item.TotalQuantity > availableQty)
                    {
                        var product = await _productRepo.GetByIdAsync(item.ProductId);
                        result.Success = false;
                        result.Errors.Add(new InsufficientStockDto
                        {
                            ProductId = item.ProductId,
                            ProductName = product?.Name ?? "Món không xác định",
                            RequestedQuantity = item.TotalQuantity,
                            AvailableQuantity = availableQty
                        });
                    }
                }
            }

            if (!result.Success)
            {
                return result; // Khong tao Detail nao, bao loi luon
            }

            // Neu du so luong, tao tung mon
            var strategy = _orderRepo.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                result.Details.Clear();
                using var transaction = await _orderRepo.BeginTransactionAsync();
                try
                {
                    var createdEntities = new List<OrderDetail>();
                    foreach (var dto in dtos)
                    {
                        var entity = _mapper.Map<OrderDetail>(dto);
                        var productType = await _productRepo.GetProductTypeAsync(entity.ProductId);
                        
                        if (entity.Status == "Confirmed" && (string.IsNullOrEmpty(entity.CookingStatus) || entity.CookingStatus == "Waiting" || entity.CookingStatus == "CustomerPending" || entity.CookingStatus == "PreOrder"))
                        {
                            if (productType == "Regular" || productType == "Manufactured")
                                entity.CookingStatus = "Ready";
                            else
                                entity.CookingStatus = "Waiting";
                        }

                        entity.CreatedAt = DateTime.UtcNow;
                        await _detailRepo.CreateAsync(entity);
                        createdEntities.Add(entity);

                        if (entity.Status == "Confirmed")
                        {
                            if (productType == "Regular" || productType == "Manufactured")
                            {
                                if (table != null)
                                {
                                    var area = await _areaRepo.GetByIdAsync(table.AreaId);
                                    if (area != null)
                                    {
                                        try
                                        {
                                            var product = await _productRepo.GetByIdAsync(entity.ProductId);
                                            if (product != null)
                                            {
                                                var saleDoc = await _documentService.GetOrCreatePendingDocumentAsync(branchId, order.Id, MenuGoBE.Models.Enums.DocumentType.Sale, order.CreatedBy ?? 1);
                                                await _documentService.AppendItemToSaleDocumentAsync(saleDoc.Id, product, entity.Quantity, entity.Id, order.CreatedBy ?? 1);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            await SendStockAlertAsync(branchId, entity.ProductId, ex.Message);
                                            throw new InvalidOperationException(ex.Message);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    await _detailRepo.SaveChangesAsync();
                    await RecalculateTotalAmountAsync(order);
                    await transaction.CommitAsync();

                    foreach (var entity in createdEntities)
                    {
                        var viewDto = _mapper.Map<OrderDetailViewDto>(entity);
                        var product = await _productRepo.GetByIdAsync(entity.ProductId);
                        if (product != null)
                        {
                            viewDto.ProductName = product.Name;
                        }
                        result.Details.Add(viewDto);
                    }
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            return result;
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
                            int availableQty = await GetAvailableQuantityAsync(entity.ProductId, branchId);
                            if (quantityDiff > availableQty)
                            {
                                throw new InvalidOperationException($"Số lượng khả dụng không đủ. Chỉ còn {availableQty} phần.");
                            }
                            
                            var productType = await _productRepo.GetProductTypeAsync(entity.ProductId);
                            if (productType == "Regular" || productType == "Manufactured" && oldStatus == "Confirmed")
                            {
                                var product = await _productRepo.GetByIdAsync(entity.ProductId);
                                if (product != null)
                                {
                                    var saleDoc = await _documentService.GetOrCreatePendingDocumentAsync(branchId, entity.OrderId, MenuGoBE.Models.Enums.DocumentType.Sale, order_check.CreatedBy ?? 1);
                                    await _documentService.AppendItemToSaleDocumentAsync(saleDoc.Id, product, quantityDiff, entity.Id, order_check.CreatedBy ?? 1);
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
                                var product = await _productRepo.GetByIdAsync(entity.ProductId);
                                if (product != null)
                                {
                                    var saleDoc = await _documentService.GetOrCreatePendingDocumentAsync(branchId, entity.OrderId, MenuGoBE.Models.Enums.DocumentType.Sale, order_check.CreatedBy ?? 1);
                                    await _documentService.AppendItemToSaleDocumentAsync(saleDoc.Id, product, -quantityDiff, entity.Id, order_check.CreatedBy ?? 1);
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
                                var product = await _productRepo.GetByIdAsync(productId);
                                if (product != null)
                                {
                                    var saleDoc = await _documentService.GetOrCreatePendingDocumentAsync(branchId, orderId, MenuGoBE.Models.Enums.DocumentType.Sale, order?.CreatedBy ?? 1);
                                    await _documentService.AppendItemToSaleDocumentAsync(saleDoc.Id, product, -quantity, entity.Id, order?.CreatedBy ?? 1);
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public async Task<bool> ConfirmAsync(long orderDetailId, int? finalQuantity = null)
        {
            var detail = await _detailRepo.GetByIdAsync(orderDetailId);
            if (detail == null) return false;

            // Ngăn chặn trừ kho nhiều lần do click đúp (Race Condition)
            if (detail.Status == "Confirmed") return true;

            if (finalQuantity.HasValue && finalQuantity.Value > 0)
            {
                detail.Quantity = finalQuantity.Value;
                await _detailRepo.UpdateAsync(detail);
                await _detailRepo.SaveChangesAsync();
            }

            var productType = await _productRepo.GetProductTypeAsync(detail.ProductId);
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
                        int availableQty = await GetAvailableQuantityAsync(detail.ProductId, branchId);
                        // Khi confirm, món này ĐÃ NẰM trong pendingQty (nếu là CustomerPending). 
                        // Do đó availableQty hiện tại đã bị trừ đi số lượng của chính detail này.
                        // Nếu finalQuantity lớn hơn detail.Quantity ban đầu, ta phải kiểm tra phần chênh lệch.
                        // Hệ thống sử dụng GetPendingQuantityAsync để giữ hàng ảo, 
                        // và chỉ trừ thật sự khi trạng thái là Served.
                    }
                }
            }

            if (productType == "Regular" || productType == "Manufactured")
            {
                // CHECK & DEDUCT kho trực tiếp cho Regular/Manufactured qua DocumentService
                if (order != null)
                {
                    var table = await _tableRepo.GetByIdAsync(order.TableId);
                    if (table != null)
                    {
                        var area = await _areaRepo.GetByIdAsync(table.AreaId);
                        if (area != null)
                        {
                            long branchId = area.BranchId;
                            try
                            {
                                var product = await _productRepo.GetByIdAsync(detail.ProductId);
                                if (product != null)
                                {
                                    var saleDoc = await _documentService.GetOrCreatePendingDocumentAsync(branchId, order.Id, MenuGoBE.Models.Enums.DocumentType.Sale, order.CreatedBy ?? 1);
                                    await _documentService.AppendItemToSaleDocumentAsync(saleDoc.Id, product, detail.Quantity, detail.Id, order.CreatedBy ?? 1);
                                }
                            }
                            catch (Exception ex)
                            {
                                await SendStockAlertAsync(branchId, detail.ProductId, ex.Message);
                                throw new InvalidOperationException(ex.Message);
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

                if (finalQuantity.HasValue && order != null)
                {
                    await RecalculateTotalAmountAsync(order);
                }
            }
            return result;
        }

        public async Task<bool> RejectAsync(long orderDetailId, string reason)
        {
            var detail = await _detailRepo.GetByIdAsync(orderDetailId);
            if (detail == null) return false;

            if (detail.Status != "CustomerPending")
            {
                throw new InvalidOperationException("Chỉ có thể từ chối món đang chờ xác nhận (CustomerPending).");
            }

            if (!string.IsNullOrEmpty(reason))
            {
                detail.ReturnReason = reason;
                detail.Note = (string.IsNullOrEmpty(detail.Note) ? "" : detail.Note + " - ") + $"Từ chối: {reason}";
            }

            var result = await _detailRepo.UpdateStatusAsync(orderDetailId, "Cancelled");
            if (result)
            {
                await _detailRepo.UpdateAsync(detail);
                await _detailRepo.SaveChangesAsync();
                
                var order = await _orderRepo.GetByIdAsync(detail.OrderId);
                if (order != null)
                {
                    await RecalculateTotalAmountAsync(order);
                }
            }
            return result;
        }

        public async Task<bool> UpdateCookingStatusAsync(long orderDetailId, string cookingStatus, int? quantity = null)
        {
            var detail = await _detailRepo.GetByIdAsync(orderDetailId);
            if (detail == null) return false;

            // Handle splitting if quantity is less than detail.Quantity
            if (quantity.HasValue && quantity.Value > 0 && quantity.Value < detail.Quantity)
            {
                var newDetail = new OrderDetail
                {
                    OrderId = detail.OrderId,
                    ProductId = detail.ProductId,
                    Quantity = detail.Quantity - quantity.Value,
                    Price = detail.Price,
                    Note = detail.Note,
                    Status = detail.Status,
                    CookingStatus = detail.CookingStatus,
                    BatchId = detail.BatchId,
                    CreatedAt = DateTime.UtcNow
                };
                detail.Quantity = quantity.Value;
                
                await _detailRepo.UpdateAsync(detail);
                await _detailRepo.CreateAsync(newDetail);
                await _detailRepo.SaveChangesAsync();
            }

            // Xử lý trừ nguyên liệu khi Processed bắt đầu nấu
            if (cookingStatus == "Cooking")
            {
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
                                    try
                                    {
                                        var product = await _productRepo.GetByIdAsync(detail.ProductId);
                                        if (product != null)
                                        {
                                            var saleDoc = await _documentService.GetOrCreatePendingDocumentAsync(branchId, order.Id, MenuGoBE.Models.Enums.DocumentType.Sale, order.CreatedBy ?? 1);
                                            await _documentService.AppendItemToSaleDocumentAsync(saleDoc.Id, product, detail.Quantity, detail.Id, order.CreatedBy ?? 1);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        var product = await _productRepo.GetByIdAsync(detail.ProductId);
                                        string productName = product?.Name ?? "Món ăn";
                                        
                                        await _hubContext.Clients.Group($"branch_{branchId}").SendAsync("ReceiveStockAlert", new
                                        {
                                            type = "stock-alert",
                                            branchId = branchId,
                                            productName = productName,
                                            message = $"⚠️ Món \"{productName}\" không thể nấu - {ex.Message}",
                                            timestamp = DateTime.UtcNow
                                        });

                                        throw new InvalidOperationException($"Không đủ nguyên liệu để nấu: {ex.Message}");
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

        public async Task<List<KitchenItemDto>> GetKitchenItemsAsync(long? branchId = null)
        {
            var activeOrders = await _orderRepo.GetActiveKitchenOrdersWithDetailsAsync(branchId);
            var result = new List<KitchenItemDto>();

            foreach (var order in activeOrders)
            {
                var tableName = order.Table?.Name ?? $"Bàn #{order.TableId}";
                var items = order.OrderDetails
                    .Where(od => od.Product != null && od.Product.Type == MenuGoBE.Models.Enums.ProductType.Processed && 
                                 (string.Equals(od.Status, "Confirmed", StringComparison.OrdinalIgnoreCase) || 
                                  string.Equals(od.Status, "PartialReturned", StringComparison.OrdinalIgnoreCase)) &&
                                 !string.Equals(od.CookingStatus, "Served", StringComparison.OrdinalIgnoreCase) && 
                                 !string.Equals(od.CookingStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
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
                        BatchId = od.BatchId,
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

            // Validate: phải có lý do (Bỏ qua vì trả hàng nguyên vẹn không cần lý do)
            /*
            if (string.IsNullOrWhiteSpace(dto.ReturnReason))
                throw new InvalidOperationException("Phải cung cấp lý do trả món.");
            */

            // Update return fields
            entity.ReturnedQuantity += dto.ReturnQuantity;
            entity.ReturnReason = dto.ReturnReason;
            entity.ReturnIsIntact = dto.IsIntact;
            entity.ReturnedAt = DateTime.UtcNow;
            entity.ReturnConfirmedBy = confirmedByUserId;

            var product = await _productRepo.GetByIdAsync(entity.ProductId);
            if (product == null)
                throw new InvalidOperationException("Không tìm thấy thông tin sản phẩm.");

            // --- NEW LOGIC: Bắt buộc hàng phải nguyên vẹn (IsIntact = true) và là loại Regular ---
            if (product.Type != MenuGoBE.Models.Enums.ProductType.Regular)
                throw new InvalidOperationException("Chỉ cho phép trả hàng đối với sản phẩm loại Thường (Regular).");

            if (dto.IsIntact == false)
                throw new InvalidOperationException("Chỉ cho phép trả hàng khi hàng còn nguyên vẹn (chưa qua sử dụng/bóc vỏ).");
            
            // Re-stock logic: CHỈ Regular + còn nguyên vẹn mới cộng lại kho
            // Logic trừ/cộng kho và ghi sổ kho được quản lý tập trung ở DocumentService
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
                        var returnDoc = await _documentService.GetOrCreatePendingDocumentAsync(branchId, order.Id, MenuGoBE.Models.Enums.DocumentType.CustomerReturn, confirmedByUserId);
                        await _documentService.AppendItemToReturnDocumentAsync(returnDoc.Id, product, dto.ReturnQuantity, dto.IsIntact, entity.Id, confirmedByUserId);
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

        public async Task<bool> ConfirmReturnItemAsync(long orderDetailId, long confirmedByUserId)
        {
            // Trả món đã được thực hiện trực tiếp ở RequestReturnItemAsync.
            // Endpoint này được giữ lại để tương thích ngược nếu client gọi.
            return await Task.FromResult(true);
        }

        public async Task<bool> BatchUpdateCookingStatusAsync(long productId, string cookingStatus, long[] branchIds)
        {
            var pendingItems = await _detailRepo.GetPendingByProductIdAsync(productId, branchIds);
            if (!pendingItems.Any()) return false;

            string batchId = Guid.NewGuid().ToString();
            bool result = true;

            foreach (var item in pendingItems)
            {
                item.BatchId = batchId;
                await _detailRepo.UpdateAsync(item);
                await _detailRepo.SaveChangesAsync();
                
                var success = await UpdateCookingStatusAsync(item.Id, cookingStatus);
                if (!success) result = false;
            }

            return result;
        }
    }
}
