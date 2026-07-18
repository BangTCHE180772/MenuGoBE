using AutoMapper;
using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;

namespace MenuGoBE.Service;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ITableRepository _tableRepo;
    private readonly IOrderDetailRepository _detailRepo;
    private readonly IProductRepository _productRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IMapper _mapper;
    private readonly IReservationSignalService _signalService;

    public ReservationService(
        IReservationRepository reservationRepo,
        IOrderRepository orderRepo,
        ITableRepository tableRepo,
        IOrderDetailRepository detailRepo,
        IProductRepository productRepo,
        ICustomerRepository customerRepo,
        IMapper mapper,
        IReservationSignalService signalService)
    {
        _reservationRepo = reservationRepo;
        _orderRepo = orderRepo;
        _tableRepo = tableRepo;
        _detailRepo = detailRepo;
        _productRepo = productRepo;
        _customerRepo = customerRepo;
        _mapper = mapper;
        _signalService = signalService;
    }

    public async Task<List<ReservationViewDto>> GetAllAsync()
    {
        var reservations = await _reservationRepo.GetAllWithDetailsAsync();
        var dtos = new List<ReservationViewDto>();

        foreach (var r in reservations)
        {
            var dto = new ReservationViewDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer?.Name ?? "",
                CustomerPhone = r.Customer?.Phone ?? "",
                ReservationTime = r.ReservationTime,
                NumberOfGuests = r.NumberOfGuests,
                Note = r.Note,
                Status = r.Status,
                OrderId = r.OrderId,
                CreatedAt = r.CreatedAt
            };

            if (r.OrderId.HasValue && r.Order != null)
            {
                dto.TableNames.Add(r.Order.Table?.Name ?? "");
                dto.PreOrderItemsCount = r.Order.OrderDetails.Count(od => od.Status == "PreOrder" || od.CookingStatus == "PreOrder");

                var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(r.OrderId.Value);
                foreach (var child in childOrders)
                {
                    dto.TableNames.Add(child.Table?.Name ?? "");
                }
            }

            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<ReservationViewDto> CreateAsync(ReservationCreateDto dto)
    {
        var customer = await _customerRepo.GetByPhoneAsync(dto.CustomerPhone);
        if (customer == null)
        {
            customer = new Customer
            {
                Name = dto.CustomerName,
                Phone = dto.CustomerPhone,
                CreatedAt = DateTime.UtcNow
            };
            await _customerRepo.CreateAsync(customer);
        }
        else if (customer.Name != dto.CustomerName)
        {
            customer.Name = dto.CustomerName;
            await _customerRepo.UpdateAsync(customer);
        }

        var reservation = new Reservation
        {
            Customer = customer,
            ReservationTime = dto.ReservationTime,
            NumberOfGuests = dto.NumberOfGuests,
            Note = dto.Note,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        if (dto.TableIds != null && dto.TableIds.Any())
        {
            var fatherTableId = dto.TableIds.First();
            var fatherOrder = new Order
            {
                TableId = fatherTableId,
                FatherId = null,
                Status = "Reserved",
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _orderRepo.CreateAsync(fatherOrder);
            await _orderRepo.SaveChangesAsync();

            // Do NOT forcefully lock the table status here.
            // Let the ReservationMonitorService smart task handle the lock 2 hours prior!

            decimal totalAmount = 0;
            if (dto.PreOrderItems != null && dto.PreOrderItems.Any())
            {
                foreach (var item in dto.PreOrderItems)
                {
                    var product = await _productRepo.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        var od = new OrderDetail
                        {
                            OrderId = fatherOrder.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = product.SellPrice,
                            Note = item.Note,
                            Status = "PreOrder",
                            CookingStatus = "PreOrder",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _detailRepo.CreateAsync(od);
                        totalAmount += od.Price * od.Quantity;
                    }
                }
                fatherOrder.TotalAmount = totalAmount;
                await _orderRepo.UpdateAsync(fatherOrder);
            }

            for (int i = 1; i < dto.TableIds.Count; i++)
            {
                var childTableId = dto.TableIds[i];
                var childOrder = new Order
                {
                    TableId = childTableId,
                    FatherId = fatherOrder.Id,
                    Status = "Reserved",
                    TotalAmount = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await _orderRepo.CreateAsync(childOrder);
            }

            await _orderRepo.SaveChangesAsync();
            await _detailRepo.SaveChangesAsync();

            reservation.OrderId = fatherOrder.Id;
        }

        await _reservationRepo.CreateAsync(reservation);
        await _reservationRepo.SaveChangesAsync();

        // Notify Background Task to recalculate the sleep timer
        _signalService.TriggerSignal();

        return (await GetAllAsync()).FirstOrDefault(r => r.Id == reservation.Id)!;
    }

    public async Task<bool> CheckInAsync(long id)
    {
        var reservation = await _reservationRepo.GetByIdAsync(id);
        if (reservation == null || reservation.Status != "Pending") return false;

        reservation.Status = "Completed";
        await _reservationRepo.UpdateAsync(reservation);

        if (reservation.OrderId.HasValue)
        {
            var fatherOrder = await _orderRepo.GetActiveOrderByIdWithDetailsAsync(reservation.OrderId.Value) 
                              ?? await _orderRepo.GetByIdAsync(reservation.OrderId.Value);
            
            if (fatherOrder != null)
            {
                var fatherTable = await _tableRepo.GetByIdAsync(fatherOrder.TableId);
                if (fatherTable != null && fatherTable.Status == "Occupied")
                {
                    return false;
                }

                fatherOrder.Status = "Active";
                await _orderRepo.UpdateAsync(fatherOrder);

                if (fatherTable != null)
                {
                    fatherTable.Status = "Occupied";
                    await _tableRepo.UpdateAsync(fatherTable);
                }

                var details = await _detailRepo.GetByOrderIdAsync(fatherOrder.Id);
                foreach (var detail in details)
                {
                    if (detail.Status == "PreOrder" || detail.CookingStatus == "PreOrder")
                    {
                        await _detailRepo.UpdateStatusAsync(detail.Id, "Confirmed");
                        await _detailRepo.UpdateCookingStatusAsync(detail.Id, "Waiting");
                    }
                }

                var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(fatherOrder.Id);
                foreach (var child in childOrders)
                {
                    child.Status = "Active";
                    await _orderRepo.UpdateAsync(child);

                    var childTable = await _tableRepo.GetByIdAsync(child.TableId);
                    if (childTable != null)
                    {
                        childTable.Status = "Occupied";
                        await _tableRepo.UpdateAsync(childTable);
                    }
                }

                await _orderRepo.SaveChangesAsync();
                await _tableRepo.SaveChangesAsync();
                await _detailRepo.SaveChangesAsync();
            }
        }

        await _reservationRepo.SaveChangesAsync();
        _signalService.TriggerSignal();
        return true;
    }

    public async Task<bool> CancelAsync(long id)
    {
        var reservation = await _reservationRepo.GetByIdAsync(id);
        if (reservation == null || reservation.Status != "Pending") return false;

        reservation.Status = "Cancelled";
        await _reservationRepo.UpdateAsync(reservation);

        if (reservation.OrderId.HasValue)
        {
            var fatherOrder = await _orderRepo.GetActiveOrderByIdWithDetailsAsync(reservation.OrderId.Value)
                              ?? await _orderRepo.GetByIdAsync(reservation.OrderId.Value);
            
            if (fatherOrder != null)
            {
                fatherOrder.Status = "Cancelled";
                await _orderRepo.UpdateAsync(fatherOrder);

                var fatherTable = await _tableRepo.GetByIdAsync(fatherOrder.TableId);
                if (fatherTable != null)
                {
                    if (fatherTable.Status == "Reserved")
                    {
                        fatherTable.Status = "Empty";
                        await _tableRepo.UpdateAsync(fatherTable);
                    }
                }

                var details = await _detailRepo.GetByOrderIdAsync(fatherOrder.Id);
                foreach (var detail in details)
                {
                    await _detailRepo.UpdateStatusAsync(detail.Id, "Cancelled");
                }

                var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(fatherOrder.Id);
                foreach (var child in childOrders)
                {
                    child.Status = "Cancelled";
                    await _orderRepo.UpdateAsync(child);

                    var childTable = await _tableRepo.GetByIdAsync(child.TableId);
                    if (childTable != null)
                    {
                        if (childTable.Status == "Reserved")
                        {
                            childTable.Status = "Empty";
                            await _tableRepo.UpdateAsync(childTable);
                        }
                    }
                }

                await _orderRepo.SaveChangesAsync();
                await _tableRepo.SaveChangesAsync();
                await _detailRepo.SaveChangesAsync();
            }
        }

        await _reservationRepo.SaveChangesAsync();
        _signalService.TriggerSignal();
        return true;
    }

    public async Task<bool> ChangeTableAsync(long reservationId, long newTableId)
    {
        var reservation = await _reservationRepo.GetByIdAsync(reservationId);
        if (reservation == null || reservation.Status != "Pending") return false;

        var newTable = await _tableRepo.GetByIdAsync(newTableId);
        if (newTable == null || (newTable.Status != "Empty" && newTable.Status != "Available")) return false;

        if (reservation.OrderId.HasValue)
        {
            var order = await _orderRepo.GetActiveOrderByIdWithDetailsAsync(reservation.OrderId.Value)
                        ?? await _orderRepo.GetByIdAsync(reservation.OrderId.Value);
            
            if (order != null)
            {
                var oldTable = await _tableRepo.GetByIdAsync(order.TableId);
                if (oldTable != null && oldTable.Status == "Reserved")
                {
                    oldTable.Status = "Empty";
                    await _tableRepo.UpdateAsync(oldTable);
                }

                order.TableId = newTableId;
                await _orderRepo.UpdateAsync(order);

                // Lock new table if it's within 2 hours
                if (DateTime.UtcNow >= reservation.ReservationTime.AddHours(-2) && DateTime.UtcNow < reservation.ReservationTime.AddMinutes(30))
                {
                    newTable.Status = "Reserved";
                }
                await _tableRepo.UpdateAsync(newTable);

                await _orderRepo.SaveChangesAsync();
                await _tableRepo.SaveChangesAsync();
            }
        }

        _signalService.TriggerSignal();
        return true;
    }
}
