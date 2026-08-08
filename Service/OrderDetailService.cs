using AutoMapper;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class OrderDetailService : IOrderDetailService
    {
        private readonly IOrderDetailRepository _detailRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly ITableService _tableService;
        private readonly IProductService _productService;
        private readonly IInvoiceDocumentService _invoiceService;
        private readonly IMapper _mapper;

        public OrderDetailService(
            IOrderDetailRepository detailRepo,
            IOrderRepository orderRepo,
            ITableService tableService,
            IProductService productService,
            IInvoiceDocumentService invoiceService,
            IMapper mapper)
        {
            _detailRepo = detailRepo;
            _orderRepo = orderRepo;
            _tableService = tableService;
            _productService = productService;
            _invoiceService = invoiceService;
            _mapper = mapper;
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
            if (entity.Status == "Confirmed" && (string.IsNullOrEmpty(entity.CookingStatus) || entity.CookingStatus == "CustomerPending" || entity.CookingStatus == "PreOrder"))
            {
                entity.CookingStatus = "Waiting";
            }

            await _detailRepo.CreateAsync(entity);
            await _detailRepo.SaveChangesAsync();

            if (entity.Status == "Confirmed")
            {
                await DeductInventoryForConfirmedDetailAsync(entity);
            }

            await RecalculateTotalAmountAsync(order);

            return _mapper.Map<OrderDetailViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(OrderDetailUpdateDto dto)
        {
            var entity = await _detailRepo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);

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

            await _detailRepo.DeleteAsync(id);
            await _detailRepo.SaveChangesAsync();

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order != null)
                await RecalculateTotalAmountAsync(order);

            return true;
        }

        public async Task<bool> ConfirmAsync(long orderDetailId)
        {
            var existing = await _detailRepo.GetByIdAsync(orderDetailId);
            if (existing == null) return false;

            bool wasAlreadyConfirmed = existing.Status == "Confirmed";

            var result = await _detailRepo.UpdateStatusAsync(orderDetailId, "Confirmed");
            if (result)
            {
                var detail = await _detailRepo.GetByIdAsync(orderDetailId);
                if (detail != null)
                {
                    var order = await _orderRepo.GetByIdAsync(detail.OrderId);
                    if (order != null && (order.Status == "Reserved" || order.Status == "CustomerPending"))
                    {
                        order.Status = "Active";
                        await _orderRepo.UpdateAsync(order);
                    }

                    if (!wasAlreadyConfirmed)
                    {
                        await DeductInventoryForConfirmedDetailAsync(detail);
                    }
                }
                await _detailRepo.SaveChangesAsync();
            }
            return result;
        }

        /// <summary>
        /// Trừ kho khi một OrderDetail được xác nhận (ấn confirm): trừ nguyên liệu
        /// theo công thức nếu sản phẩm thuộc loại Processed, ngược lại trừ trực tiếp
        /// số lượng của chính sản phẩm đó.
        /// </summary>
        private async Task DeductInventoryForConfirmedDetailAsync(OrderDetail entity)
        {
            var order = await _orderRepo.GetByIdAsync(entity.OrderId);
            if (order == null) return;

            var table = await _tableService.GetByIdAsync(order.TableId);
            if (table == null) return;

            var product = await _productService.GetByIdAsync(entity.ProductId);
            if (product == null) return;

            var lines = new List<InvoiceOutflowLineDto>();

            if (product.Type == ProductType.Processed && product.RecipeItems != null && product.RecipeItems.Count > 0)
            {
                foreach (var ingredient in product.RecipeItems)
                {
                    lines.Add(new InvoiceOutflowLineDto
                    {
                        ProductId = ingredient.IngredientProductId,
                        Quantity = ingredient.Quantity * entity.Quantity,
                        Note = $"Trừ nguyên liệu [{ingredient.IngredientProductName}] cho món {product.Name} x{entity.Quantity} (OrderDetail #{entity.Id})"
                    });
                }
            }
            else
            {
                lines.Add(new InvoiceOutflowLineDto
                {
                    ProductId = entity.ProductId,
                    Quantity = entity.Quantity,
                    Note = $"Xuất bán {product.Name} x{entity.Quantity} (OrderDetail #{entity.Id})"
                });
            }

            await _invoiceService.CreateInvoiceAsync(table.BranchId, lines);
        }

        public async Task<bool> UpdateCookingStatusAsync(long orderDetailId, string cookingStatus)
        {
            var result = await _detailRepo.UpdateCookingStatusAsync(orderDetailId, cookingStatus);
            if (result)
            {
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

            var result = await _detailRepo.UpdateStatusAsync(orderDetailId, "Cancelled");
            if (result)
            {
                await _detailRepo.SaveChangesAsync();
                
                var order = await _orderRepo.GetByIdAsync(entity.OrderId);
                if (order != null)
                {
                    await RecalculateTotalAmountAsync(order);
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
                        CookingStatus = string.IsNullOrEmpty(od.CookingStatus) || od.CookingStatus == "Accepted" ? "Waiting" : od.CookingStatus,
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
