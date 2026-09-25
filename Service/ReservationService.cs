using AutoMapper;
using MenuGoBE.Dtos.Reservation;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Dtos.Table;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;
using MenuGoBE.Repositories;

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
    private readonly INotificationRepository _notificationRepo;
    private readonly IOrderDetailService _orderDetailService;
    private readonly IPromotionService _promotionService;
    private readonly IAreaRepository _areaRepo;
    private readonly IReservationNotificationQueue _notificationQueue;
    private readonly IBranchRepository _branchRepo;

    public ReservationService(
        IReservationRepository reservationRepo,
        IOrderRepository orderRepo,
        ITableRepository tableRepo,
        IOrderDetailRepository detailRepo,
        IProductRepository productRepo,
        ICustomerRepository customerRepo,
        IMapper mapper,
        IReservationSignalService signalService,
        IHubContext<NotificationHub> hubContext,
        INotificationRepository notificationRepo,
        IOrderDetailService orderDetailService,
        IPromotionService promotionService,
        IAreaRepository areaRepo,
        IReservationNotificationQueue notificationQueue,
        IBranchRepository branchRepo)
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
        _notificationRepo = notificationRepo;
        _orderDetailService = orderDetailService;
        _promotionService = promotionService;
        _areaRepo = areaRepo;
        _notificationQueue = notificationQueue;
        _branchRepo = branchRepo;
    }

    /// <summary>
    /// Lấy tên chi nhánh từ DB, tránh phụ thuộc vào navigation property (có thể null).
    /// </summary>
    private async Task<string> GetBranchNameAsync(long? branchId)
    {
        if (!branchId.HasValue || branchId.Value <= 0) return "Chi nhánh";
        var branch = await _branchRepo.GetByIdAsync(branchId.Value);
        return branch?.Name ?? "Chi nhánh";
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
                BranchId = r.BranchId ?? r.Order?.Table?.Area?.BranchId,
                BranchName = r.Branch?.Name ?? r.Order?.Table?.Area?.Branch?.Name ?? "",
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

    public async Task<List<ReservationViewDto>> GetAllFilteredAsync(List<long>? branchIds, DateTime? fromDate, DateTime? toDate)
    {
        var reservations = await _reservationRepo.GetFilteredWithDetailsAsync(branchIds, fromDate, toDate);
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
                BranchId = r.BranchId ?? r.Order?.Table?.Area?.BranchId,
                BranchName = r.Branch?.Name ?? r.Order?.Table?.Area?.Branch?.Name ?? "",
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

    public async Task<bool> UpdateAsync(long id, ReservationUpdateDto dto)
    {
        if (dto.ReservationTime < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Thời gian đặt bàn phải sau thời gian hiện tại.");
        }

        var reservation = await _reservationRepo.GetByIdAsync(id);
        if (reservation == null) return false;

        if (reservation.Status != "Pending" && reservation.Status != "Confirmed")
        {
            throw new InvalidOperationException("Chỉ có thể sửa đơn đặt bàn chưa đến hoặc đã xác nhận.");
        }

        if (reservation.ReservationTime != dto.ReservationTime)
        {
            if (reservation.OrderId.HasValue)
            {
                var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(reservation.OrderId.Value);
                var tableIds = new List<long>();
                var order = await _orderRepo.GetByIdAsync(reservation.OrderId.Value);
                if (order != null) tableIds.Add(order.TableId);
                tableIds.AddRange(childOrders.Select(c => c.TableId));

                var startTime = dto.ReservationTime.AddMinutes(-59);
                var endTime = dto.ReservationTime.AddMinutes(59);
                var overlappingReservations = await _reservationRepo.GetOverlappingReservationsAsync(startTime, endTime);

                var overlappingTables = overlappingReservations.Where(r => r.Id != reservation.Id && r.OrderId.HasValue && 
                    (r.Order != null && tableIds.Contains(r.Order.TableId) || 
                     _orderRepo.GetChildOrdersWithDetailsAsync(r.OrderId.Value).Result.Any(c => tableIds.Contains(c.TableId))));

                if (overlappingTables.Any())
                {
                    bool has15MinOverlap = false;
                    bool has60MinOverlap = false;

                    foreach (var overlap in overlappingTables)
                    {
                        var diff = Math.Abs((overlap.ReservationTime - dto.ReservationTime).TotalMinutes);
                        if (diff <= 15) has15MinOverlap = true;
                        else if (diff <= 60) has60MinOverlap = true;
                    }

                    if (has15MinOverlap)
                    {
                        throw new InvalidOperationException("Bàn đã chọn có khách đặt trong khung +/- 15 phút.");
                    }
                    else if (has60MinOverlap && !dto.IgnoreWarning)
                    {
                        throw new MenuGoBE.Exceptions.ReservationWarningException("Bàn đã chọn có khách đặt trong vòng 1 tiếng. Bạn có chắc chắn muốn đặt không?");
                    }
                }

                // Check overlap with current occupancy if reservation is soon
                var minutesToReservation = (dto.ReservationTime - DateTime.UtcNow).TotalMinutes;
                if (minutesToReservation <= 60)
                {
                    bool currentOccupancy15 = false;
                    bool currentOccupancy60 = false;
                    foreach (var tableId in tableIds)
                    {
                        var table = await _tableRepo.GetByIdAsync(tableId);
                        if (table != null && (table.Status == "Occupied" || table.Status == "Serving" || table.Status == "Reserved"))
                        {
                            if (minutesToReservation <= 15) currentOccupancy15 = true;
                            else if (minutesToReservation <= 60) currentOccupancy60 = true;
                        }
                    }

                    if (currentOccupancy15)
                    {
                        throw new InvalidOperationException("Bàn đang có khách. Không thể dời lịch đến thời điểm này.");
                    }
                    else if (currentOccupancy60 && !dto.IgnoreWarning)
                    {
                        throw new MenuGoBE.Exceptions.ReservationWarningException("Bàn đang có khách và có thể chưa trống kịp. Bạn có chắc chắn muốn dời lịch không?");
                    }
                }
            }
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
            reservation.Customer = customer;
        }
        else if (customer.Name != dto.CustomerName)
        {
            if (!dto.ConfirmUpdateCustomer)
            {
                throw new MenuGoBE.Exceptions.CustomerConflictException($"Số điện thoại {dto.CustomerPhone} đã đăng ký cho khách hàng: {customer.Name}. Bạn có muốn cập nhật lại tên không?", customer.Name);
            }
            else
            {
                customer.Name = dto.CustomerName;
                await _customerRepo.UpdateAsync(customer);
                reservation.Customer = customer;
            }
        }
        else
        {
             reservation.Customer = customer;
        }

        reservation.ReservationTime = dto.ReservationTime;
        reservation.NumberOfGuests = dto.NumberOfGuests;
        reservation.Note = dto.Note;

        await _reservationRepo.UpdateAsync(reservation);
        await _reservationRepo.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync("ReceiveReservationUpdate");

        return true;
    }

    public async Task<List<TableViewDto>> GetAvailableTablesAsync(long branchId, DateTime reservationTime)
    {
        var allTables = await _tableRepo.GetByBranchIdsAsync(new List<long> { branchId });
        var validTables = allTables.Where(t => 
            t.Status != "Maintenance" && 
            t.Status != "Inactive" && 
            t.Area?.IsActive == true && 
            (t.Area?.Name == null || (!t.Area.Name.ToLower().Contains("internal") && !t.Area.Name.ToLower().Contains("nội bộ")))
        ).ToList();

        var startTime = reservationTime.AddMinutes(-14);
        var endTime = reservationTime.AddMinutes(14);

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

        // Apply current occupancy logic if the reservation is within 30 minutes
        var minutesToRes = (reservationTime - DateTime.UtcNow).TotalMinutes;
        if (minutesToRes <= 30)
        {
            foreach (var t in validTables)
            {
                if (t.Status == "Occupied" || t.Status == "Serving" || t.Status == "Reserved")
                {
                    bookedTableIds.Add(t.Id);
                }
            }
        }

        var availableTables = validTables.Where(t => !bookedTableIds.Contains(t.Id)).ToList();
        return _mapper.Map<List<MenuGoBE.Dtos.Table.TableViewDto>>(availableTables);
    }

    public async Task<ReservationViewDto> CreateAsync(ReservationCreateDto dto)
    {
        if (dto.ReservationTime < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Thời gian đặt bàn phải sau thời gian hiện tại.");
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
            if (!dto.ConfirmUpdateCustomer)
            {
                throw new MenuGoBE.Exceptions.CustomerConflictException($"Số điện thoại {dto.CustomerPhone} đã đăng ký cho khách hàng: {customer.Name}. Bạn có muốn cập nhật lại tên không?", customer.Name);
            }
            else
            {
                customer.Name = dto.CustomerName;
                await _customerRepo.UpdateAsync(customer);
            }
        }

        long? branchId = dto.BranchId;
        if (!branchId.HasValue && dto.TableIds != null && dto.TableIds.Any())
        {
            var firstTable = await _tableRepo.GetByIdAsync(dto.TableIds.First());
            if (firstTable != null && firstTable.Area != null)
            {
                branchId = firstTable.Area.BranchId;
            }
        }

        var reservation = new Reservation
        {
            Customer = customer,
            ReservationTime = dto.ReservationTime,
            NumberOfGuests = dto.NumberOfGuests,
            TableCount = dto.TableCount,
            Note = dto.Note,
            BranchId = branchId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        if (branchId.HasValue)
        {
            // Kiểm tra Overbooking toàn chi nhánh
            var branchTables = await _tableRepo.GetByBranchIdsAsync(new List<long> { branchId.Value });
            var totalTables = branchTables.Count(t => t.Status != "Maintenance" && t.Status != "Inactive");

            var startTime = dto.ReservationTime.AddMinutes(-30);
            var endTime = dto.ReservationTime.AddMinutes(30);
            var overlappingReservations = await _reservationRepo.GetOverlappingReservationsAsync(startTime, endTime);
            
            overlappingReservations = overlappingReservations.Where(r => r.BranchId == branchId.Value || (r.Order != null && r.Order.Table != null && r.Order.Table.Area.BranchId == branchId.Value)).ToList();

            int usedTables15 = overlappingReservations
                .Where(r => Math.Abs((r.ReservationTime - dto.ReservationTime).TotalMinutes) <= 15 && r.Id != reservation.Id)
                .Sum(r => r.TableCount);

            int usedTables30 = overlappingReservations
                .Where(r => Math.Abs((r.ReservationTime - dto.ReservationTime).TotalMinutes) <= 30 && r.Id != reservation.Id)
                .Sum(r => r.TableCount);

            if (usedTables15 + dto.TableCount > totalTables)
            {
                throw new InvalidOperationException("Không thể nhận thêm khách vì số lượng đặt bàn quá lớn vượt quá công suất cho phép (vướng ±15 phút).");
            }
            if (usedTables30 + dto.TableCount > totalTables && !dto.IgnoreWarning)
            {
                throw new MenuGoBE.Exceptions.ReservationWarningException("Cảnh báo: Số lượng đặt bàn trong khoảng thời gian này đang rất đông. Bạn có muốn tiếp tục?");
            }

            // Cross-Area Check: Only if not manually assigning specific tables
            if ((dto.TableIds == null || !dto.TableIds.Any()) && dto.TableCount > 1 && !dto.IgnoreWarning)
            {
                var availableTables = await GetAvailableTablesAsync(branchId.Value, dto.ReservationTime);
                bool hasSingleAreaEnoughTables = false;
                
                var tablesByArea = availableTables.GroupBy(t => t.AreaId);
                foreach (var group in tablesByArea)
                {
                    if (group.Count() >= dto.TableCount)
                    {
                        hasSingleAreaEnoughTables = true;
                        break;
                    }
                }

                if (!hasSingleAreaEnoughTables)
                {
                    throw new MenuGoBE.Exceptions.ReservationWarningException("Lưu ý: Không có đủ số bàn trống trong cùng một khu vực. Hệ thống không thể tự động xếp bàn, bạn sẽ phải tự xếp tay cho đơn này. Bạn có muốn tiếp tục tạo đơn đặt bàn không?");
                }
            }
        }

        if (dto.TableIds != null && dto.TableIds.Any())
        {
            // VALIDATION: Check for overlapping specific tables
            var startTime = dto.ReservationTime.AddMinutes(-59);
            var endTime = dto.ReservationTime.AddMinutes(59);
            var overlappingReservations = await _reservationRepo.GetOverlappingReservationsAsync(startTime, endTime);
            
            var overlap15Tables = new List<string>();
            var overlap60Tables = new List<string>();

            foreach (var r in overlappingReservations)
            {
                if (r.OrderId.HasValue && r.Id != reservation.Id)
                {
                    var tablesInReservation = new HashSet<long>();
                    if (r.Order != null) tablesInReservation.Add(r.Order.TableId);
                    
                    var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(r.OrderId.Value);
                    foreach (var child in childOrders)
                    {
                        tablesInReservation.Add(child.TableId);
                    }

                    var overlappedIds = dto.TableIds.Where(tid => tablesInReservation.Contains(tid)).ToList();
                    if (overlappedIds.Any())
                    {
                        var diff = Math.Abs((r.ReservationTime - dto.ReservationTime).TotalMinutes);
                        foreach (var tid in overlappedIds)
                        {
                            var table = await _tableRepo.GetByIdAsync(tid);
                            if (table != null)
                            {
                                if (diff <= 15) overlap15Tables.Add(table.Name);
                                else if (diff <= 60) overlap60Tables.Add(table.Name);
                            }
                        }
                    }
                }
            }

            var minutesToReservation = (dto.ReservationTime - DateTime.UtcNow).TotalMinutes;
            if (minutesToReservation <= 60)
            {
                foreach (var tableId in dto.TableIds)
                {
                    var table = await _tableRepo.GetByIdAsync(tableId);
                    if (table != null && (table.Status == "Occupied" || table.Status == "Serving" || table.Status == "Reserved"))
                    {
                        if (minutesToReservation <= 15) overlap15Tables.Add(table.Name);
                        else if (minutesToReservation <= 60) overlap60Tables.Add(table.Name);
                    }
                }
            }

            if (overlap15Tables.Any())
            {
                throw new InvalidOperationException($"Không thể xếp bàn. Bàn [{string.Join(", ", overlap15Tables.Distinct())}] đang có khách hoặc bị trùng thời gian đặt.");
            }
            else if (overlap60Tables.Any() && !dto.IgnoreWarning)
            {
                throw new MenuGoBE.Exceptions.ReservationWarningException($"Chú ý: Bàn [{string.Join(", ", overlap60Tables.Distinct())}] đang có khách hoặc có lịch đặt trong vòng 1 tiếng tới. Bạn có chắc chắn muốn chọn?");
            }
        }


            if (dto.PreOrderItems != null && dto.PreOrderItems.Any())
            {
                foreach (var item in dto.PreOrderItems)
                {
                    reservation.ReservationDetails.Add(new ReservationDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Note = item.Note
                    });
                }
            }

        await _reservationRepo.CreateAsync(reservation);
        await _reservationRepo.SaveChangesAsync();

        if (dto.TableIds != null && dto.TableIds.Any())
        {
            await AssignTablesToReservationAsync(reservation.Id, dto.TableIds, dto.IgnoreWarning);
        }

        // Notify Background Task to recalculate the sleep timer
        _signalService.TriggerSignal();

        if (dto.TableIds != null && dto.TableIds.Any() && (dto.ReservationTime - DateTime.UtcNow).TotalMinutes <= 30)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
        }

        // SignalR realtime update for both Branch & Customer
        await _hubContext.Clients.All.SendAsync("ReceiveReservationUpdate");
        if (reservation.BranchId.HasValue)
        {
            await _hubContext.Clients.Group($"branch_{reservation.BranchId.Value}").SendAsync("ReceiveNotification", new
            {
                type = "new-reservation",
                message = $"Khách hàng {customer.Name} đã gửi một yêu cầu đặt bàn mới.",
                reservationId = reservation.Id,
                timestamp = DateTime.UtcNow
            });
        }
        if (reservation.CustomerId > 0)
        {
            await _hubContext.Clients.Group($"customer_{reservation.CustomerId}").SendAsync("ReceiveNotification", new
            {
                type = "new-reservation",
                message = "Yêu cầu đặt bàn của bạn đã được gửi thành công.",
                reservationId = reservation.Id,
                timestamp = DateTime.UtcNow
            });
        }

        // Queue Email notification cho khách
        _ = _notificationQueue.QueueAsync(new Dtos.Reservation.ReservationEmailNotification
        {
            CustomerId = reservation.CustomerId,
            CustomerEmail = customer.Email,
            CustomerName = customer.Name,
            EventType = "Created",
            ReservationTime = reservation.ReservationTime,
            BranchName = await GetBranchNameAsync(reservation.BranchId),
            ReservationId = reservation.Id
        });

        return (await GetAllAsync()).FirstOrDefault(r => r.Id == reservation.Id)!;
    }

    public async Task AssignTablesToReservationAsync(long reservationId, List<long> tableIds, bool ignoreWarning = false)
    {
        var reservation = await _reservationRepo.GetByIdAsync(reservationId);
        if (reservation == null)
        {
            throw new InvalidOperationException("Không tìm thấy đơn đặt bàn.");
        }

        if (tableIds == null || !tableIds.Any())
        {
            throw new InvalidOperationException("Vui lòng chọn ít nhất 1 bàn.");
        }

        // VALIDATION: Check for overlapping specific tables
        var startTime = reservation.ReservationTime.AddMinutes(-59);
        var endTime = reservation.ReservationTime.AddMinutes(59);
        var overlappingReservations = await _reservationRepo.GetOverlappingReservationsAsync(startTime, endTime);
        
        var overlap15Tables = new List<string>();
        var overlap60Tables = new List<string>();

        foreach (var r in overlappingReservations)
        {
            if (r.OrderId.HasValue && r.Id != reservation.Id)
            {
                var tablesInReservation = new HashSet<long>();
                if (r.Order != null) tablesInReservation.Add(r.Order.TableId);
                
                var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(r.OrderId.Value);
                foreach (var child in childOrders)
                {
                    tablesInReservation.Add(child.TableId);
                }

                var overlappedIds = tableIds.Where(tid => tablesInReservation.Contains(tid)).ToList();
                if (overlappedIds.Any())
                {
                    var diff = Math.Abs((r.ReservationTime - reservation.ReservationTime).TotalMinutes);
                    foreach (var tid in overlappedIds)
                    {
                        var table = await _tableRepo.GetByIdAsync(tid);
                        if (table != null)
                        {
                            if (diff <= 15) overlap15Tables.Add(table.Name);
                            else if (diff <= 60) overlap60Tables.Add(table.Name);
                        }
                    }
                }
            }
        }

        var minutesToReservation = (reservation.ReservationTime - DateTime.UtcNow).TotalMinutes;
        if (minutesToReservation <= 60)
        {
            foreach (var tableId in tableIds)
            {
                var table = await _tableRepo.GetByIdAsync(tableId);
                if (table != null && (table.Status == "Occupied" || table.Status == "Serving" || table.Status == "Reserved"))
                {
                    if (minutesToReservation <= 15) overlap15Tables.Add(table.Name);
                    else if (minutesToReservation <= 60) overlap60Tables.Add(table.Name);
                }
            }
        }

        if (overlap15Tables.Any())
        {
            throw new InvalidOperationException($"Khoảng thời gian giữa 2 đơn đặt bàn tại bàn [{string.Join(", ", overlap15Tables.Distinct())}] quá sát nhau (dưới 15 phút). Vui lòng chọn bàn khác để đảm bảo dịch vụ.");
        }
        // For AssignTablesToReservationAsync, it's called from Assign Modal. The Assign Modal does NOT have IgnoreWarning checkbox.
        // Wait, the UI for Assign Modal doesn't have a way to pass "ignoreWarning". 
        // We'll just throw Warning, and the UI catches 422? No, `AssignTableModal` in Frontend needs to handle it. 
        // Let's just throw Warning if it's 60m overlap.
        // Wait, we need to add `ignoreWarning` parameter to AssignTablesToReservationAsync!
        // But the API might not support it.
        // Actually, the Frontend calls `assignTable` which is `PUT /api/reservations/{id}/assign-tables`.
        // We can just add ignoreWarning as query param? Or we just throw Warning, and if they want to ignore, they can't.
        // Let me check what the frontend does. 
        // For now, let's keep it simple: just block if overlap 15m. If overlap 60m, let's just warn? No, just block 60m or allow it?
        // In the requirement: Báo cảnh báo ±60m đích danh Tên bàn.
        // If they can't ignore it in the modal, they would be stuck.
        // Let's just return InvalidOperationException for now, or just throw it.
        if (overlap60Tables.Any() && !ignoreWarning)
        {
            throw new MenuGoBE.Exceptions.ReservationWarningException($"Chú ý: Bàn [{string.Join(", ", overlap60Tables.Distinct())}] đang có khách hoặc có lịch đặt trong vòng 1 tiếng tới. Bạn có chắc chắn muốn chọn?");
        }

        var fatherTableId = tableIds.First();
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

        bool shouldLockImmediately = (reservation.ReservationTime - DateTime.UtcNow).TotalMinutes <= 30;

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
        if (reservation.ReservationDetails != null && reservation.ReservationDetails.Any())
        {
            foreach (var item in reservation.ReservationDetails)
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

        for (int i = 1; i < tableIds.Count; i++)
        {
            var childTableId = tableIds[i];
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
        reservation.Status = "Confirmed";
        reservation.TableCount = tableIds.Count;
        await _reservationRepo.UpdateAsync(reservation);
        await _reservationRepo.SaveChangesAsync();

        // Create Customer Notification
        var branchName = await GetBranchNameAsync(reservation.BranchId);
        var notif = new Notification
        {
            BranchId = reservation.BranchId ?? (fatherOrder.Table?.Area?.BranchId ?? 0),
            CustomerId = reservation.CustomerId,
            Type = "Reservation",
            Title = "Đặt bàn đã được xác nhận",
            Message = $"Đặt bàn của bạn vào lúc {reservation.ReservationTime:dd/MM/yyyy HH:mm} tại {branchName} đã được xác nhận.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            IsImportant = true,
            Priority = "Information",
            ReferenceType = "Reservation",
            ReferenceId = reservationId
        };
        await _notificationRepo.CreateAsync(notif);
        await _notificationRepo.SaveChangesAsync();

        if (reservation.CustomerId > 0)
        {
            await _hubContext.Clients.Group($"customer_{reservation.CustomerId}").SendAsync("ReceiveNotification", new
            {
                type = "reservation-confirmed",
                message = $"Đặt bàn của bạn tại {branchName} đã được xác nhận.",
                timestamp = DateTime.UtcNow
            });
        }

        // Queue Email notification cho khách
        var confirmCustomer = await _customerRepo.GetByIdAsync(reservation.CustomerId);
        var tableTasks = tableIds.Select(async tid => (await _tableRepo.GetByIdAsync(tid))?.Name ?? "");
        var tableNamesArray = await Task.WhenAll(tableTasks);
        
        _ = _notificationQueue.QueueAsync(new Dtos.Reservation.ReservationEmailNotification
        {
            CustomerId = reservation.CustomerId,
            CustomerEmail = confirmCustomer?.Email,
            CustomerName = confirmCustomer?.Name ?? "",
            EventType = "Confirmed",
            ReservationTime = reservation.ReservationTime,
            BranchName = branchName,
            ReservationId = reservation.Id,
            TableNames = string.Join(", ", tableNamesArray)
        });

        _signalService.TriggerSignal();
        await _hubContext.Clients.All.SendAsync("ReceiveReservationUpdate");
        await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
    }

    public async Task<object> CheckInReservationAsync(long id)
    {
        var reservation = await _reservationRepo.GetByIdAsync(id);
        if (reservation == null || (reservation.Status != "Pending" && reservation.Status != "Confirmed")) 
            throw new InvalidOperationException("Không thể check-in đơn đặt bàn này.");

        reservation.Status = "Completed";
        await _reservationRepo.UpdateAsync(reservation);

        var failedItems = new List<string>();

        if (reservation.OrderId.HasValue)
        {
            var fatherOrder = await _orderRepo.GetActiveOrderByIdWithDetailsAsync(reservation.OrderId.Value) 
                              ?? await _orderRepo.GetByIdAsync(reservation.OrderId.Value);
            
            if (fatherOrder != null)
            {
                fatherOrder.Status = "Active";
                fatherOrder.CustomerId = reservation.CustomerId;
                await _orderRepo.UpdateAsync(fatherOrder);

                var fatherTable = await _tableRepo.GetByIdAsync(fatherOrder.TableId);
                if (fatherTable != null)
                {
                    fatherTable.Status = "Occupied";
                    await _tableRepo.UpdateAsync(fatherTable);
                }

                var details = await _detailRepo.GetByOrderIdAsync(fatherOrder.Id);

                // Lấy branchId để áp dụng KM tại thời điểm check-in
                long branchId = 0;
                var fatherTableForPromo = await _tableRepo.GetByIdAsync(fatherOrder.TableId);
                if (fatherTableForPromo != null)
                {
                    var area = await _areaRepo.GetByIdAsync(fatherTableForPromo.AreaId);
                    if (area != null) branchId = area.BranchId;
                }

                // Cập nhật giá KM hiện tại cho pre-order items (giá lock tại thời điểm check-in)
                Dictionary<long, Dtos.Promotion.PromotionDto> promoMap = null;
                if (branchId > 0)
                {
                    promoMap = await _promotionService.GetActivePromotionMapForBranchAsync(branchId);
                }

                foreach (var detail in details)
                {
                    if (detail.Status == "PreOrder" || detail.CookingStatus == "PreOrder")
                    {
                        var product = await _productRepo.GetByIdAsync(detail.ProductId);
                        if (product != null)
                        {
                            // Cập nhật giá theo KM hiện tại (thời điểm khách đến)
                            detail.OriginalPrice = product.SellPrice;
                            if (promoMap != null && promoMap.TryGetValue(detail.ProductId, out var promo))
                            {
                                detail.PromotionId = promo.Id;
                                if (promo.DiscountType == "Percentage")
                                {
                                    var discount = product.SellPrice * (promo.DiscountValue / 100);
                                    if (promo.MaxDiscount > 0 && discount > promo.MaxDiscount) discount = promo.MaxDiscount;
                                    detail.Price = Math.Max(0, product.SellPrice - discount);
                                }
                                else
                                {
                                    detail.Price = Math.Max(0, product.SellPrice - promo.DiscountValue);
                                }
                            }
                            else
                            {
                                detail.Price = product.SellPrice;
                            }
                            await _detailRepo.UpdateAsync(detail);

                            try
                            {
                                await _orderDetailService.ConfirmAsync(detail.Id);
                            }
                            catch (InvalidOperationException)
                            {
                                detail.Status = "Cancelled";
                                detail.Note = (string.IsNullOrEmpty(detail.Note) ? "" : detail.Note + " - ") + "Không đủ nguyên liệu/hàng hóa";
                                await _detailRepo.UpdateAsync(detail);
                                failedItems.Add(product.Name);
                            }
                        }
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
        await _hubContext.Clients.All.SendAsync("ReceiveReservationUpdate");

        if (reservation.CustomerId > 0)
        {
            await _hubContext.Clients.Group($"customer_{reservation.CustomerId}").SendAsync("ReceiveNotification", new
            {
                type = "reservation-updated",
                message = "Bạn đã check-in nhận bàn thành công. Chúc bạn có bữa ăn ngon miệng!",
                reservationId = reservation.Id,
                timestamp = DateTime.UtcNow
            });
        }

        // Queue Email notification cho khách
        var checkInCustomer = await _customerRepo.GetByIdAsync(reservation.CustomerId);
        _ = _notificationQueue.QueueAsync(new Dtos.Reservation.ReservationEmailNotification
        {
            CustomerId = reservation.CustomerId,
            CustomerEmail = checkInCustomer?.Email,
            CustomerName = checkInCustomer?.Name ?? "",
            EventType = "CheckedIn",
            ReservationTime = reservation.ReservationTime,
            BranchName = await GetBranchNameAsync(reservation.BranchId),
            ReservationId = reservation.Id
        });

        return new { Success = true, FailedItems = failedItems };
    }

    public async Task<bool> CancelAsync(long id)
    {
        var reservation = await _reservationRepo.GetByIdAsync(id);
        if (reservation == null || (reservation.Status != "Pending" && reservation.Status != "Confirmed")) return false;

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

        try
        {
            long resolvedBranchId = reservation.BranchId ?? (reservation.Order?.Table?.Area?.BranchId ?? 0);
            if (resolvedBranchId > 0)
            {
                var branchName = await GetBranchNameAsync(reservation.BranchId);
                var notif = new Notification
                {
                    BranchId = resolvedBranchId,
                    CustomerId = reservation.CustomerId,
                    Type = "Reservation",
                    Title = "Đặt bàn đã bị hủy",
                    Message = $"Lịch đặt bàn của bạn vào lúc {reservation.ReservationTime:dd/MM/yyyy HH:mm} tại {branchName} đã bị hủy.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsImportant = false,
                    Priority = "Information",
                    ReferenceType = "Reservation",
                    ReferenceId = id
                };
                await _notificationRepo.CreateAsync(notif);
                await _notificationRepo.SaveChangesAsync();

                if (reservation.CustomerId > 0)
                {
                    await _hubContext.Clients.Group($"customer_{reservation.CustomerId}").SendAsync("ReceiveNotification", new
                    {
                        type = "reservation-cancelled",
                        message = $"Lịch đặt bàn tại {branchName} của bạn đã bị hủy.",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception)
        {
            // Ignore notification errors to not fail the cancellation process
        }

        // Queue Email notification cho khách
        try
        {
            var cancelCustomer = await _customerRepo.GetByIdAsync(reservation.CustomerId);
            _ = _notificationQueue.QueueAsync(new Dtos.Reservation.ReservationEmailNotification
            {
                CustomerId = reservation.CustomerId,
                CustomerEmail = cancelCustomer?.Email,
                CustomerName = cancelCustomer?.Name ?? "",
                EventType = "Cancelled",
                ReservationTime = reservation.ReservationTime,
                BranchName = await GetBranchNameAsync(reservation.BranchId),
                ReservationId = reservation.Id
            });
        }
        catch { /* Bỏ qua lỗi email */ }

        _signalService.TriggerSignal();
        await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
        await _hubContext.Clients.All.SendAsync("ReceiveReservationUpdate");
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
        await _hubContext.Clients.All.SendAsync("ReceiveReservationUpdate");
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
        else
        {
            // Create father order for the newly assigned table
            var fatherOrder = new Order
            {
                TableId = newTableId,
                FatherId = null,
                Status = "Reserved",
                TotalAmount = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _orderRepo.CreateAsync(fatherOrder);
            await _orderRepo.SaveChangesAsync();

            bool shouldLockImmediately = DateTime.UtcNow >= reservation.ReservationTime.AddMinutes(-30) && DateTime.UtcNow < reservation.ReservationTime.AddMinutes(30);
            if (shouldLockImmediately)
            {
                newTable.Status = "Reserved";
                await _tableRepo.UpdateAsync(newTable);
                await _tableRepo.SaveChangesAsync();
            }

            reservation.OrderId = fatherOrder.Id;
            reservation.Status = "Confirmed";
            await _reservationRepo.UpdateAsync(reservation);

            // Create Customer Notification
            var branchName = await GetBranchNameAsync(reservation.BranchId);
            var notif = new Notification
            {
                BranchId = reservation.BranchId ?? (fatherOrder.Table?.Area?.BranchId ?? 0),
                CustomerId = reservation.CustomerId,
                Type = "Reservation",
                Title = "Đặt bàn đã được xác nhận",
                Message = $"Đặt bàn của bạn vào lúc {reservation.ReservationTime:dd/MM/yyyy HH:mm} tại {branchName} đã được xác nhận.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsImportant = true,
                Priority = "Information",
                ReferenceType = "Reservation",
                ReferenceId = reservationId
            };
            await _notificationRepo.CreateAsync(notif);
            await _notificationRepo.SaveChangesAsync();

            if (reservation.CustomerId > 0)
            {
                await _hubContext.Clients.Group($"customer_{reservation.CustomerId}").SendAsync("ReceiveNotification", new
                {
                    type = "reservation-confirmed",
                    message = $"Đặt bàn của bạn tại {branchName} đã được xác nhận.",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // Lấy danh sách tất cả các bàn của reservation
        var allTableNames = new List<string>();
        if (reservation.OrderId.HasValue)
        {
            var rOrder = await _orderRepo.GetActiveOrderByIdWithDetailsAsync(reservation.OrderId.Value) 
                         ?? await _orderRepo.GetByIdAsync(reservation.OrderId.Value);
            if (rOrder != null)
            {
                var mainTable = await _tableRepo.GetByIdAsync(rOrder.TableId);
                if (mainTable != null) allTableNames.Add(mainTable.Name);

                var childOrders = await _orderRepo.GetChildOrdersWithDetailsAsync(rOrder.Id);
                foreach (var child in childOrders)
                {
                    var childTable = await _tableRepo.GetByIdAsync(child.TableId);
                    if (childTable != null) allTableNames.Add(childTable.Name);
                }
            }
        }
        else
        {
            allTableNames.Add(newTable.Name);
        }
        var tableNamesStr = string.Join(", ", allTableNames);

        // Queue Email notification cho khách
        var confirmCustomer = await _customerRepo.GetByIdAsync(reservation.CustomerId);
        _ = _notificationQueue.QueueAsync(new Dtos.Reservation.ReservationEmailNotification
        {
            CustomerId = reservation.CustomerId,
            CustomerEmail = confirmCustomer?.Email,
            CustomerName = confirmCustomer?.Name ?? "",
            EventType = "Confirmed",
            ReservationTime = reservation.ReservationTime,
            BranchName = await GetBranchNameAsync(reservation.BranchId),
            ReservationId = reservation.Id,
            TableNames = tableNamesStr
        });

        _signalService.TriggerSignal();
        await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
        await _hubContext.Clients.All.SendAsync("ReceiveReservationUpdate");
        return true;
    }

    public async Task<bool> RejectAsync(long id)
    {
        var reservation = await _reservationRepo.GetByIdAsync(id);
        if (reservation == null || reservation.Status != "Pending") return false;

        reservation.Status = "Rejected";
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

        // Create Customer Notification
        var branchName = await GetBranchNameAsync(reservation.BranchId);
        var notif = new Notification
        {
            BranchId = reservation.BranchId ?? 0,
            CustomerId = reservation.CustomerId,
            Type = "Reservation",
            Title = "Yêu cầu đặt bàn bị từ chối",
            Message = $"Yêu cầu đặt bàn của bạn vào lúc {reservation.ReservationTime:dd/MM/yyyy HH:mm} tại {branchName} đã bị từ chối.",
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            IsImportant = true,
            Priority = "Warning",
            ReferenceType = "Reservation",
            ReferenceId = id
        };
        await _notificationRepo.CreateAsync(notif);
        await _notificationRepo.SaveChangesAsync();

        if (reservation.CustomerId > 0)
        {
            await _hubContext.Clients.Group($"customer_{reservation.CustomerId}").SendAsync("ReceiveNotification", new
            {
                type = "reservation-rejected",
                message = $"Đặt bàn của bạn tại {branchName} đã bị từ chối.",
                timestamp = DateTime.UtcNow
            });
        }

        // Queue Email notification cho khách
        var rejectCustomer = await _customerRepo.GetByIdAsync(reservation.CustomerId);
        _ = _notificationQueue.QueueAsync(new Dtos.Reservation.ReservationEmailNotification
        {
            CustomerId = reservation.CustomerId,
            CustomerEmail = rejectCustomer?.Email,
            CustomerName = rejectCustomer?.Name ?? "",
            EventType = "Rejected",
            ReservationTime = reservation.ReservationTime,
            BranchName = branchName,
            ReservationId = reservation.Id
        });

        _signalService.TriggerSignal();
        await _hubContext.Clients.All.SendAsync("ReceiveTableListUpdate");
        await _hubContext.Clients.All.SendAsync("ReceiveReservationUpdate");
        return true;
    }
}
