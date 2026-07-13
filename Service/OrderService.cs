using AutoMapper;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly ITableRepository _tableRepo;
        private readonly IMapper _mapper;

        public OrderService(IOrderRepository repo, ITableRepository tableRepo, IMapper mapper)
        {
            _repo = repo;
            _tableRepo = tableRepo;
            _mapper = mapper;
        }

        public async Task<List<OrderViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<OrderViewDto>>(data);
        }

        public async Task<OrderViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;

            return _mapper.Map<OrderViewDto>(data);
        }

        public async Task<OrderViewDto> CreateAsync(OrderCreateDto dto)
        {
            var entity = _mapper.Map<Order>(dto);

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            return _mapper.Map<OrderViewDto>(entity);
        }

        public async Task<bool> UpdateAsync(OrderUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MergeAsync(OrderMergeDto dto)
        {
            var childOrder = await _repo.GetByIdAsync(dto.ChildOrderId);
            if (childOrder == null) return false;

            var fatherOrder = await _repo.GetByIdAsync(dto.FatherOrderId);
            if (fatherOrder == null) return false;

            if (dto.ChildOrderId == dto.FatherOrderId) return false;

            if (fatherOrder.FatherId != null) return false;

            childOrder.FatherId = dto.FatherOrderId;

            await _repo.UpdateAsync(childOrder);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnmergeAsync(long childOrderId)
        {
            var childOrder = await _repo.GetByIdAsync(childOrderId);
            if (childOrder == null || childOrder.FatherId == null) return false;

            childOrder.FatherId = null;

            await _repo.UpdateAsync(childOrder);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<List<OrderViewDto>> GetChildOrdersAsync(long fatherOrderId)
        {
            var data = await _repo.GetChildOrdersAsync(fatherOrderId);
            return _mapper.Map<List<OrderViewDto>>(data);
        }

        public async Task<OrderMergedSummaryDto?> GetMergedSummaryAsync(long fatherOrderId)
        {
            var fatherOrder = await _repo.GetByIdAsync(fatherOrderId);
            if (fatherOrder == null) return null;

            var childOrders = await _repo.GetChildOrdersAsync(fatherOrderId);

            var grandTotal = fatherOrder.TotalAmount + childOrders.Sum(o => o.TotalAmount);

            return new OrderMergedSummaryDto
            {
                FatherOrder = _mapper.Map<OrderViewDto>(fatherOrder),
                ChildOrders = _mapper.Map<List<OrderViewDto>>(childOrders),
                GrandTotal = grandTotal
            };
        }

        public async Task<TableOrderSummaryDto> GetTableOrderSummaryAsync(long tableId)
        {
            var table = await _tableRepo.GetByIdAsync(tableId);
            var order = await _repo.GetActiveOrderByTableIdAsync(tableId);

            var summary = new TableOrderSummaryDto
            {
                TableId = tableId,
                TableName = table?.Name ?? string.Empty,
                TableStatus = table?.Status ?? string.Empty,
                AreaName = table?.Area?.Name ?? string.Empty,
            };

            if (order == null)
            {
                return summary;
            }

            summary.ActiveOrderId = order.Id;

            var items = order.OrderDetails
                .Where(od => od.Status != "Cancelled")
                .Select(od => new OrderItemDto
                {
                    OrderDetailId = od.Id,
                    OrderId = od.OrderId,
                    ProductName = od.Product?.Name ?? "Không rõ",
                    Quantity = od.Quantity,
                    Price = od.Price,
                    Note = od.Note,
                    Status = od.Status,
                    CookingStatus = od.CookingStatus,
                    CreatedAt = od.CreatedAt,
                })
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            summary.Items = items;
            summary.PendingConfirmCount = items.Count(i => i.Status == "CustomerPending");
            summary.ReadyToServeCount = items.Count(i => i.CookingStatus == "Ready");

            return summary;
        }
    }
}
