using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly ITableRepository _tableRepo;
        private readonly IMapper _mapper;
        private readonly ICustomerRepository _customerRepo;

        public OrderService(IOrderRepository repo, ITableRepository tableRepo, IMapper mapper, ICustomerRepository customerRepo)
        {
            _repo = repo;
            _tableRepo = tableRepo;
            _mapper = mapper;
            _customerRepo = customerRepo;
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
            while (fatherOrder.FatherId != null)
            {
                fatherOrder = await _repo.GetByIdAsync(fatherOrder.FatherId.Value);
                if (fatherOrder == null) return false;
            }

            if (childOrder.Id == fatherOrder.Id) return false;

            var childrenOfChild = await _repo.GetChildOrdersAsync(childOrder.Id);
            foreach (var c in childrenOfChild)
            {
                c.FatherId = fatherOrder.Id;
                await _repo.UpdateAsync(c);
            }

            childOrder.FatherId = fatherOrder.Id;

            await _repo.UpdateAsync(childOrder);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UnmergeAsync(long orderId)
        {
            var order = await _repo.GetByIdAsync(orderId);
            if (order == null) return false;

            if (order.FatherId != null) 
            {
                order.FatherId = null;
                await _repo.UpdateAsync(order);
            } 
            else 
            {
                var childOrders = await _repo.GetChildOrdersAsync(order.Id);
                if (!childOrders.Any()) return false;

                var newRoot = childOrders.First();
                newRoot.FatherId = null;
                await _repo.UpdateAsync(newRoot);

                foreach (var child in childOrders.Skip(1))
                {
                    child.FatherId = newRoot.Id;
                    await _repo.UpdateAsync(child);
                }
            }

            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveTableAsync(long orderId, long newTableId)
        {
            var order = await _repo.GetByIdAsync(orderId);
            if (order == null || order.Status != "Active") return false;

            var newTable = await _tableRepo.GetByIdAsync(newTableId);
            if (newTable == null || newTable.Status != "Empty") return false;

            var oldTable = await _tableRepo.GetByIdAsync(order.TableId);

            order.TableId = newTableId;
            await _repo.UpdateAsync(order);

            if (oldTable != null)
            {
                oldTable.Status = "Empty";
            }
            newTable.Status = "Occupied";

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
            var summary = new TableOrderSummaryDto
            {
                TableId = tableId,
                TableName = table?.Name ?? string.Empty,
                TableStatus = table?.Status ?? string.Empty,
                AreaName = table?.Area?.Name ?? string.Empty,
                Items = new List<OrderItemDto>()
            };

            var order = await _repo.GetActiveOrderByTableIdAsync(tableId);
            if (order == null)
            {
                return summary;
            }

            summary.OriginalOrderId = order.Id;
            bool isChild = order.FatherId != null;

            while (order.FatherId != null)
            {
                var fatherOrder = await _repo.GetActiveOrderByIdWithDetailsAsync(order.FatherId.Value);
                if (fatherOrder == null) break;
                order = fatherOrder;
            }

            summary.ActiveOrderId = order.Id;

            var childOrders = await _repo.GetChildOrdersWithDetailsAsync(order.Id);
            bool isMerged = childOrders.Any();
            summary.IsMerged = isChild || isMerged;

            var items = order.OrderDetails
                .Where(od => od.Status != "Cancelled")
                .Select(od => new OrderItemDto
                {
                    OrderDetailId = od.Id,
                    OrderId = od.OrderId,
                    ProductName = isMerged ? $"{od.Product?.Name ?? "Không rõ"} ({order.Table?.Name})" : (od.Product?.Name ?? "Không rõ"),
                    Quantity = od.Quantity,
                    Price = od.Price,
                    Note = od.Note,
                    Status = od.Status,
                    CookingStatus = od.CookingStatus,
                    CreatedAt = od.CreatedAt,
                }).ToList();
            foreach (var child in childOrders)
            {
                var childItems = child.OrderDetails
                    .Where(od => od.Status != "Cancelled")
                    .Select(od => new OrderItemDto
                    {
                        OrderDetailId = od.Id,
                        OrderId = od.OrderId,
                        ProductName = $"{od.Product?.Name ?? "Không rõ"} ({child.Table?.Name})",
                        Quantity = od.Quantity,
                        Price = od.Price,
                        Note = od.Note,
                        Status = od.Status,
                        CookingStatus = od.CookingStatus,
                        CreatedAt = od.CreatedAt,
                    });
                items.AddRange(childItems);
            }

            items = items.OrderByDescending(i => i.CreatedAt).ToList();

            summary.Items = items;
            summary.PendingConfirmCount = items.Count(i => i.Status == "CustomerPending");
            summary.ReadyToServeCount = items.Count(i => i.CookingStatus == "Ready");

            return summary;
        }

        public async Task<List<long>> PayOrderAsync(long orderId, OrderPayDto? dto = null)
        {
            var paidTableIds = new List<long>();
            var orderWithDetails = await _repo.GetActiveOrderByIdWithDetailsAsync(orderId);
            if (orderWithDetails == null) return paidTableIds;

            var order = orderWithDetails;
            
            var childOrders = await _repo.GetChildOrdersWithDetailsAsync(order.Id);

            decimal totalAmount = order.OrderDetails.Where(od => od.Status != "Cancelled").Sum(od => od.Price * od.Quantity);
            foreach (var child in childOrders)
            {
                totalAmount += child.OrderDetails.Where(od => od.Status != "Cancelled").Sum(od => od.Price * od.Quantity);
            }
            var grandTotal = totalAmount + Math.Round(totalAmount * 0.08m);
            int pointsEarned = (int)(grandTotal / 100000) * 100;
            if (dto != null && !string.IsNullOrEmpty(dto.CustomerPhone))
            {
                var customer = await _customerRepo.GetByPhoneAsync(dto.CustomerPhone);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = !string.IsNullOrEmpty(dto.CustomerName) ? dto.CustomerName : "Khách lẻ",
                        Phone = dto.CustomerPhone,
                        Point = pointsEarned,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _customerRepo.CreateAsync(customer);
                    await _repo.SaveChangesAsync(); 
                }
                else
                {
                    customer.Point += pointsEarned;
                    await _customerRepo.UpdateAsync(customer);
                }
                order.CustomerId = customer.Id;
            }

            order.Status = "Paid";
            
            if (order.TableId > 0)
            {
                var table = await _tableRepo.GetByIdAsync(order.TableId);
                if (table != null)
                {
                    table.Status = "Cleaning";
                    paidTableIds.Add(table.Id);
                }
            }

            foreach (var child in childOrders)
            {
                if (child.Status == "Active")
                {
                    child.Status = "Paid";
                    if (child.TableId > 0)
                    {
                        var childTable = await _tableRepo.GetByIdAsync(child.TableId);
                        if (childTable != null)
                        {
                            childTable.Status = "Cleaning";
                            paidTableIds.Add(childTable.Id);
                        }
                    }
                }
            }

            await _repo.SaveChangesAsync();
            return paidTableIds;
        }
    }
}
