using AutoMapper;
using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Dtos.Table;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;

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
    private readonly IHubContext<NotificationHub> _hubContext;

    public ReservationService(
        IReservationRepository reservationRepo,
        IOrderRepository orderRepo,
        ITableRepository tableRepo,
        IOrderDetailRepository detailRepo,
        IProductRepository productRepo,
        ICustomerRepository customerRepo,
        IMapper mapper,
        IReservationSignalService signalService,
        IHubContext<NotificationHub> hubContext)
    {
        _reservationRepo = reservationRepo;
        _orderRepo = orderRepo;
        _tableRepo = tableRepo;
        _detailRepo = detailRepo;
        _productRepo = productRepo;
        _customerRepo = customerRepo;
        _mapper = mapper;
        _signalService = signalService;
        _hubContext = hubContext;
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

    public async Task<List<TableViewDto>> GetAvailableTablesAsync(long branchId, DateTime reservationTime)
    {
        var allTables = await _tableRepo.GetByBranchIdsAsync(new List<long> { branchId });
        var validTables = allTables.Where(t => t.Status == "Empty" || t.Status == "Available").ToList();

        var startTime = reservationTime.AddMinutes(-89);
        var endTime = reservationTime.AddMinutes(89);

        var overlappingReservations = await _reservationRepo.GetOverlappingReservationsAsync(startTime, endTime);
        
        var bookedTableIds = overlappingReservations
            .Where(r => r.OrderId.HasValue && r.Order != null)
            .Select(r => r.Order.TableId)
            .ToHashSet();

        // Include child tables from reservations
        foreach (var r in overlappingReservations)
        {
            if (r.OrderId.HasValue)
            {
                var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(r.OrderId.Value);
                foreach (var child in childOrders)
                {
                    bookedTableIds.Add(child.TableId);
                }
            }
        }

        var availableTables = validTables.Where(t => !bookedTableIds.Contains(t.Id)).ToList();
        return _mapper.Map<List<MenuGoBE.Dtos.Table.TableViewDto>>(availableTables);
    }

    public async Task<ReservationViewDto> CreateAsync(ReservationCreateDto dto)
    {
        if (dto.ReservationTime < DateTime.UtcNow.AddMinutes(10))
        {
            throw new InvalidOperationException("Thời gian đặt bàn phải cách thời gian hiện tại ít nhất 10 phút.");
        }

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
            // VALIDATION: Check for overlapping tables
            var startTime = dto.ReservationTime.AddMinutes(-89);
            var endTime = dto.ReservationTime.AddMinutes(89);
            var overlappingReservations = await _reservationRepo.GetOverlappingReservationsAsync(startTime, endTime);
            
            var bookedTableIds = overlappingReservations
                .Where(r => r.OrderId.HasValue && r.Order != null)
                .Select(r => r.Order.TableId)
                .ToHashSet();

            foreach (var r in overlappingReservations)
            {
                if (r.OrderId.HasValue)
                {
                    var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(r.OrderId.Value);
                    foreach (var child in childOrders)
                    {
                        bookedTableIds.Add(child.TableId);
                    }
                }
            }

            foreach (var tableId in dto.TableIds)
            {
                if (bookedTableIds.Contains(tableId))
                {
                    throw new InvalidOperationException("Bàn đã được đặt trong khoảng thời gian này.");
                }
            }

            var minutesToReservation = (dto.ReservationTime - DateTime.UtcNow).TotalMinutes;
            if (minutesToReservation <= 60)
            {
                foreach (var tableId in dto.TableIds)
                {
                    var table = await _tableRepo.GetByIdAsync(tableId);
                    if (table != null && (table.Status == "Occupied" || table.Status == "Serving"))
                    {
                        throw new InvalidOperationException($"Bàn {table.Name} đang có khách. Vui lòng chọn bàn khác hoặc dời thời gian đặt bàn.");
                    }
                }
            }

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

            // Do NOT forcefully lock the table status here unless it's within 30 minutes.
            bool shouldLockImmediately = (dto.ReservationTime - DateTime.UtcNow).TotalMinutes <= 30;

            if (shouldLockImmediately)
            {
                var fatherTable = await _tableRepo.GetByIdAsync(fatherTableId);
                if (fatherTable != null && (fatherTable.Status == "Empty" || fatherTable.Status == "Available"))
                {
                    fatherTable.Status = "Reserved";
                    await _tableRepo.UpdateAsync(fatherTable);
                }
            }

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
                if (shouldLockImmediately)
                {
                    var childTable = await _tableRepo.GetByIdAsync(childTableId);
                    if (childTable != null && (childTable.Status == "Empty" || childTable.Status == "Available"))
                    {
                        childTable.Status = "Reserved";
                        await _tableRepo.UpdateAsync(childTable);
                    }
                }
            }

            await _orderRepo.SaveChangesAsync();
            await _tableRepo.SaveChangesAsync();
            await _detailRepo.SaveChangesAsync();

            reservation.OrderId = fatherOrder.Id;
        }

        await _reservationRepo.CreateAsync(reservation);
        await _reservationRepo.SaveChangesAsync();

        // Notify Background Task to recalculate the sleep timer
        _signalService.TriggerSignal();

        if (dto.TableIds != null && dto.TableIds.Any() && (dto.ReservationTime - DateTime.UtcNow).TotalMinutes <= 30)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
        }

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
        await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
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
        await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
        return true;
    }

    public async Task<bool> ExtendAsync(long id, int minutes)
    {
        var reservation = await _reservationRepo.GetByIdAsync(id);
        if (reservation == null || reservation.Status != "Pending") return false;

        reservation.ReservationTime = reservation.ReservationTime.AddMinutes(minutes);
        await _reservationRepo.UpdateAsync(reservation);
        await _reservationRepo.SaveChangesAsync();

        _signalService.TriggerSignal();
        await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
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

                // Lock new table if it's within 30 minutes
                if (DateTime.UtcNow >= reservation.ReservationTime.AddMinutes(-30) && DateTime.UtcNow < reservation.ReservationTime.AddMinutes(30))
                {
                    newTable.Status = "Reserved";
                }
                await _tableRepo.UpdateAsync(newTable);

                await _orderRepo.SaveChangesAsync();
                await _tableRepo.SaveChangesAsync();
            }
        }

        _signalService.TriggerSignal();
        await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
        return true;
    }
}
