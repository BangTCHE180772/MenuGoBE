using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Models.Enums;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Services.Document;
using Microsoft.Extensions.Options;
using MenuGoBE.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MenuGoBE.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly ITableRepository _tableRepo;
        private readonly IMapper _mapper;
        private readonly ICustomerRepository _customerRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IVoucherRepository _voucherRepo;
        private readonly IReservationRepository _reservationRepo;
        private readonly IProductRepository _productRepo;
        private readonly IAreaRepository _areaRepo;
        private readonly IBInventoryRepository _bInventoryRepo;
        private readonly IDocumentRepository _documentRepo;
        private readonly IDocumentService _documentService;
        private readonly IPartnerRepository _partnerRepo;
        private readonly AppDbContext _context;
        private readonly PointSystemConfig _pointConfig;
        private readonly IHubContext<NotificationHub>? _hubContext;

        public OrderService(
            IOrderRepository repo, 
            IMapper mapper, 
            ITableRepository tableRepo, 
            ICustomerRepository customerRepo, 
            IPaymentRepository paymentRepo, 
            IVoucherRepository voucherRepo, 
            IReservationRepository reservationRepo,
            IProductRepository productRepo,
            IAreaRepository areaRepo,
            IBInventoryRepository bInventoryRepo,
            IDocumentRepository documentRepo,
            IPartnerRepository partnerRepo,
            AppDbContext context,
            IDocumentService documentService,
            IOptions<PointSystemConfig> pointConfig,
            IHubContext<NotificationHub>? hubContext = null)
        {
            _repo = repo;
            _mapper = mapper;
            _tableRepo = tableRepo;
            _customerRepo = customerRepo;
            _paymentRepo = paymentRepo;
            _voucherRepo = voucherRepo;
            _reservationRepo = reservationRepo;
            _productRepo = productRepo;
            _areaRepo = areaRepo;
            _bInventoryRepo = bInventoryRepo;
            _documentRepo = documentRepo;
            _documentService = documentService;
            _partnerRepo = partnerRepo;
            _context = context;
            _pointConfig = pointConfig.Value;
            _hubContext = hubContext;
        }

        public async Task<List<OrderViewDto>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return _mapper.Map<List<OrderViewDto>>(data);
        }

        public async Task<OrderViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetOrderByIdWithDetailsAsync(id);
            if (data == null) return null;

            var dto = _mapper.Map<OrderViewDto>(data);
            if (data.CreatedBy.HasValue)
            {
                var account = await _context.Accounts.FindAsync(data.CreatedBy.Value);
                if (account != null)
                {
                    dto.CreatedByName = account.Name;
                }
            }
            return dto;
        }

        public async Task<OrderViewDto> CreateAsync(OrderCreateDto dto)
        {
            if (dto.TableId > 0)
            {
                var table = await _tableRepo.GetByIdAsync(dto.TableId);
                if (table != null && (table.Status == "Internal" || !table.IsActive))
                {
                    throw new MenuGoException(new ErrorResult(400, "Không thể tạo hóa đơn mới trên bàn hệ thống nội bộ.", 400));
                }

                var existingActiveOrder = await _repo.GetActiveOrderByTableIdAsync(dto.TableId);
                if (existingActiveOrder != null)
                {
                    throw new MenuGoException(new ErrorResult(400, "Bàn này đã có hóa đơn chưa thanh toán. Không thể tạo hóa đơn mới.", 400));
                }
            }

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

            if (childOrder.Status == "Reserved" || fatherOrder.Status == "Reserved")
            {
                throw new InvalidOperationException("Không thể gộp bàn đang chờ khách đặt trước. Vui lòng Xác nhận khách đến trước khi gộp.");
            }
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
                c.Status = fatherOrder.Status;
                await _repo.UpdateAsync(c);
            }

            childOrder.FatherId = fatherOrder.Id;
            childOrder.Status = fatherOrder.Status;
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

            bool isProtected = await _reservationRepo.IsTableProtectedAsync(newTableId, 60);
            if (isProtected)
            {
                throw new InvalidOperationException("Bàn mới đã được đặt trước trong thời gian tới. Vui lòng chọn bàn khác.");
            }

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
                BranchId = table?.Area?.BranchId ?? 0,
                Items = new List<OrderItemDto>()
            };

            var order = await _repo.GetActiveOrderByTableIdAsync(tableId);
            if (order == null)
            {
                if (table?.Status == "Reserved")
                {
                    var reservation = await _reservationRepo.GetUpcomingByTableIdAsync(tableId);
                    if (reservation != null)
                    {
                        summary.ReservationId = reservation.Id;
                        summary.CustomerName = reservation.Customer?.Name;
                        summary.CustomerPhone = reservation.Customer?.Phone;
                        summary.ReservationTime = reservation.ReservationTime;
                    }
                }
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

            if (isMerged || isChild)
            {
                var allTableNames = new List<string>();
                if (order.Table != null) allTableNames.Add(order.Table.Name);
                allTableNames.AddRange(childOrders.Where(c => c.Table != null).Select(c => c.Table.Name));
                summary.TableName = string.Join(", ", allTableNames);
            }

            var items = order.OrderDetails
                .Where(od => od.Status != "Cancelled")
                .Select(od => new OrderItemDto
                {
                    OrderDetailId = od.Id,
                    OrderId = od.OrderId,
                    ProductId = od.ProductId,
                    ProductName = isMerged ? $"{od.Product?.Name ?? "Không rõ"} ({order.Table?.Name})" : (od.Product?.Name ?? "Không rõ"),
                    Quantity = od.Quantity,
                    Price = od.Price,
                    Note = od.Note,
                    Status = od.Status,
                    CookingStatus = od.CookingStatus,
                    CreatedAt = od.CreatedAt,
                    ReturnedQuantity = od.ReturnedQuantity,
                    ReturnReason = od.ReturnReason,
                    ProductType = od.Product != null ? od.Product.Type.ToString() : string.Empty
                }).ToList();
            foreach (var child in childOrders)
            {
                var childItems = child.OrderDetails
                    .Where(od => od.Status != "Cancelled")
                    .Select(od => new OrderItemDto
                    {
                        OrderDetailId = od.Id,
                        OrderId = od.OrderId,
                        ProductId = od.ProductId,
                        ProductName = $"{od.Product?.Name ?? "Không rõ"} ({child.Table?.Name})",
                        Quantity = od.Quantity,
                        Price = od.Price,
                        Note = od.Note,
                        Status = od.Status,
                        CookingStatus = od.CookingStatus,
                        CreatedAt = od.CreatedAt,
                        ReturnedQuantity = od.ReturnedQuantity,
                        ReturnReason = od.ReturnReason,
                        ProductType = od.Product != null ? od.Product.Type.ToString() : string.Empty
                    });
                items.AddRange(childItems);
            }

            items = items.OrderByDescending(i => i.CreatedAt).ToList();

            summary.Items = items;
            summary.PendingConfirmCount = items.Count(i => i.Status == "CustomerPending");
            summary.ReadyToServeCount = items.Count(i => i.CookingStatus == "Ready");

            if (table?.Status == "Reserved")
            {
                var reservation = await _reservationRepo.GetByOrderIdAsync(summary.ActiveOrderId.Value);
                if (reservation != null)
                {
                    summary.ReservationId = reservation.Id;
                    summary.CustomerName = reservation.Customer?.Name;
                    summary.CustomerPhone = reservation.Customer?.Phone;
                    summary.ReservationTime = reservation.ReservationTime;
                }
            }

            return summary;
        }

        public async Task<decimal> PreparePaymentAsync(long orderId, OrderPayDto? dto, string paymentMethod = "Cash")
        {
            var orderWithDetails = await _repo.GetActiveOrderByIdWithDetailsAsync(orderId);
            if (orderWithDetails == null) throw new MenuGoException(new ErrorResult(404, "Không tìm thấy đơn hàng hoặc đơn hàng đã thanh toán", 404));
            var order = orderWithDetails;
            var childOrders = await _repo.GetChildOrdersWithDetailsAsync(order.Id);

            decimal totalAmount = order.OrderDetails.Where(od => od.Status != "Cancelled").Sum(od => od.Price * (od.Quantity - od.ReturnedQuantity));
            foreach (var child in childOrders)
            {
                totalAmount += child.OrderDetails.Where(od => od.Status != "Cancelled").Sum(od => od.Price * (od.Quantity - od.ReturnedQuantity));
            }
            var grandTotal = totalAmount + Math.Round(totalAmount * 0.08m);

            if (dto != null && !string.IsNullOrEmpty(dto.CustomerPhone))
            {
                var customer = await _customerRepo.GetByPhoneAsync(dto.CustomerPhone);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = !string.IsNullOrEmpty(dto.CustomerName) ? dto.CustomerName : "Khách lẻ",
                        Phone = dto.CustomerPhone,
                        Point = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _customerRepo.CreateAsync(customer);
                    await _repo.SaveChangesAsync();
                }
                else
                {
                    // Cập nhật tên mới nếu thu ngân có nhập tên khác
                    if (!string.IsNullOrEmpty(dto.CustomerName) && customer.Name != dto.CustomerName)
                    {
                        customer.Name = dto.CustomerName;
                        await _customerRepo.UpdateAsync(customer);
                        await _repo.SaveChangesAsync();
                    }
                }
                order.CustomerId = customer.Id;
            }

            decimal discountAmount = 0;
            order.VoucherId = null;
            if (dto != null && !string.IsNullOrEmpty(dto.VoucherCode))
            {
                var voucher = await _voucherRepo.GetByCodeAsync(dto.VoucherCode);
                if (voucher != null && voucher.IsActive && voucher.StartDate <= DateTime.UtcNow && voucher.EndDate >= DateTime.UtcNow && voucher.UsedCount < voucher.Quantity && grandTotal >= voucher.MinOrderValue)
                {
                    if (voucher.DiscountType == "Fixed")
                        discountAmount = voucher.DiscountValue;
                    else
                    {
                        discountAmount = grandTotal * (voucher.DiscountValue / 100);
                        if (voucher.MaxDiscount > 0 && discountAmount > voucher.MaxDiscount)
                            discountAmount = voucher.MaxDiscount;
                    }
                    order.VoucherId = voucher.Id;
                }
            }

            int pointsUsed = 0;
            if (dto != null && dto.PointsUsed > 0 && order.CustomerId.HasValue)
            {
                var customer = await _customerRepo.GetByIdAsync(order.CustomerId.Value);
                if (customer != null && customer.Point >= _pointConfig.MinPointsToUse)
                {
                    int requestedPoints = Math.Min(dto.PointsUsed, customer.Point);
                    
                    decimal remainingAfterVoucher = grandTotal - discountAmount;
                    decimal maxPointsAllowed = remainingAfterVoucher * (_pointConfig.MaxDiscountPercentage / 100m);
                    
                    pointsUsed = (int)Math.Min(requestedPoints, maxPointsAllowed);
                }
            }

            decimal totalDiscount = discountAmount + pointsUsed;
            if (totalDiscount > grandTotal)
            {
                if (discountAmount > grandTotal) 
                {
                    discountAmount = grandTotal;
                    pointsUsed = 0;
                }
                else
                {
                    pointsUsed = (int)(grandTotal - discountAmount);
                }
            }

            decimal amountBeforeRounding = grandTotal - discountAmount - pointsUsed;
            decimal roundedTotal = amountBeforeRounding;
            decimal roundingDiscount = 0;

            if (paymentMethod == "Cash")
            {
                roundedTotal = Math.Round(amountBeforeRounding / 1000m, MidpointRounding.AwayFromZero) * 1000m;
                roundingDiscount = amountBeforeRounding - roundedTotal;
            }

            order.DiscountAmount = discountAmount + roundingDiscount;
            order.PointsUsed = pointsUsed;
            order.TotalAmount = roundedTotal;

            await _repo.UpdateAsync(order);
            await _repo.SaveChangesAsync();

            return order.TotalAmount;
        }

        public async Task<List<long>> PayOrderAsync(long orderId, OrderPayDto? dto = null, string paymentMethod = "Cash", long? userId = null)
        {
            var strategy = _repo.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _repo.BeginTransactionAsync();
                try
                {
                var paidTableIds = new List<long>();
                var orderWithDetails = await _repo.GetActiveOrderByIdWithDetailsAsync(orderId);
                if (orderWithDetails == null) return paidTableIds;

                var order = orderWithDetails;

                var childOrdersCheck = await _repo.GetChildOrdersWithDetailsAsync(order.Id);
                int effectiveItemsCount = order.OrderDetails.Count(od => od.Status != "Cancelled" && (od.Quantity - od.ReturnedQuantity) > 0);
                foreach (var child in childOrdersCheck)
                {
                    effectiveItemsCount += child.OrderDetails.Count(od => od.Status != "Cancelled" && (od.Quantity - od.ReturnedQuantity) > 0);
                }

                if (effectiveItemsCount == 0)
                {
                    order.Status = "Cancelled";
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Status != "Cancelled") detail.Status = "Cancelled";
                    }

                    if (order.TableId > 0)
                    {
                        var table = await _tableRepo.GetByIdAsync(order.TableId);
                        if (table != null)
                        {
                            table.Status = "Cleaning";
                            paidTableIds.Add(table.Id);
                        }
                    }

                    var childTableIdsForCancel = childOrdersCheck.Where(c => c.TableId > 0).Select(c => c.TableId).Distinct().ToList();
                    var childTablesForCancel = await _tableRepo.GetByIdsAsync(childTableIdsForCancel);
                    var childTableCancelDict = childTablesForCancel.ToDictionary(t => t.Id);

                    foreach (var child in childOrdersCheck)
                    {
                        if (child.Status == "Active")
                        {
                            child.Status = "Cancelled";
                            foreach (var detail in child.OrderDetails)
                            {
                                if (detail.Status != "Cancelled") detail.Status = "Cancelled";
                            }
                            if (child.TableId > 0 && childTableCancelDict.TryGetValue(child.TableId, out var childTable))
                            {
                                if (!paidTableIds.Contains(childTable.Id))
                                {
                                    childTable.Status = "Cleaning";
                                    paidTableIds.Add(childTable.Id);
                                }
                            }
                        }
                    }

                    await _repo.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return paidTableIds;
                }

                if (paymentMethod == "Cash" || dto != null)
                {
                    await PreparePaymentAsync(orderId, dto, paymentMethod);
                    _repo.ClearChangeTracker();
                    order = await _repo.GetActiveOrderByIdWithDetailsAsync(orderId);
                    if (order == null) return paidTableIds;
                }

                var finalAmount = order.TotalAmount;
                int pointsEarned = 0;
                if (_pointConfig.EarnRate_SpendAmount > 0)
                {
                    pointsEarned = (int)(finalAmount / _pointConfig.EarnRate_SpendAmount) * _pointConfig.EarnRate_PointReward;
                }

                // Bù tích điểm cho khách khi bếp làm chậm: món đã nấu xong nhưng khách
                // không nhận nữa (bị huỷ khi đã Ready) bị ghi nhận là LeftoverRecord với
                // lý do cố định KitchenDelayReason — cộng thêm điểm theo đúng tỉ lệ tích
                // điểm thường (100 điểm / 100,000đ) trên giá trị món đó, thay cho hoàn tiền.
                var orderDetailIdsForBonus = order.OrderDetails.Select(od => od.Id).ToList();
                var childOrdersForBonus = await _repo.GetChildOrdersWithDetailsAsync(order.Id);
                foreach (var child in childOrdersForBonus)
                {
                    orderDetailIdsForBonus.AddRange(child.OrderDetails.Select(od => od.Id));
                }

                int compensationBonusPoints = 0;
                if (orderDetailIdsForBonus.Count > 0 && _pointConfig.EarnRate_SpendAmount > 0)
                {
                    var delayedRecords = await _context.LeftoverRecords
                        .AsNoTracking()
                        .Include(l => l.OrderDetail)
                        .ThenInclude(od => od.Product)
                        .Where(l => l.Type == LeftoverType.Return
                                 && l.OrderDetail != null
                                 && l.OrderDetail.Product != null
                                 && l.OrderDetail.Product.Type == MenuGoBE.Models.Enums.ProductType.Processed
                                 && l.OrderDetailId.HasValue
                                 && orderDetailIdsForBonus.Contains(l.OrderDetailId.Value))
                        .ToListAsync();

                    compensationBonusPoints = delayedRecords.Sum(l =>
                        (int)(((l.OrderDetail?.Price ?? 0) * l.Quantity) / _pointConfig.EarnRate_SpendAmount) * _pointConfig.EarnRate_PointReward);
                }

                if (order.CustomerId.HasValue)
                {
                    var customer = await _customerRepo.GetByIdAsync(order.CustomerId.Value);
                    if (customer != null)
                    {
                        int currentPoint = customer.Point;
                        string orderCode = $"HD{order.Id}";

                        // 1. Trừ điểm nếu khách có sử dụng điểm
                        if (order.PointsUsed > 0)
                        {
                            int before = currentPoint;
                            currentPoint = Math.Max(0, currentPoint - order.PointsUsed);
                            var redeemTrans = new CustomerPointTransaction
                            {
                                CustomerId = customer.Id,
                                OrderId = order.Id,
                                RelatedCode = orderCode,
                                Type = MenuGoBE.Models.Enums.PointTransactionType.Redeem,
                                PointChange = -order.PointsUsed,
                                PointBefore = before,
                                PointAfter = currentPoint,
                                Reason = $"Sử dụng điểm thanh toán hóa đơn {orderCode}",
                                IsManual = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _context.CustomerPointTransactions.AddAsync(redeemTrans);
                        }

                        // 2. Tích điểm theo số tiền thanh toán
                        if (pointsEarned > 0)
                        {
                            int before = currentPoint;
                            currentPoint += pointsEarned;
                            var earnTrans = new CustomerPointTransaction
                            {
                                CustomerId = customer.Id,
                                OrderId = order.Id,
                                RelatedCode = orderCode,
                                Type = MenuGoBE.Models.Enums.PointTransactionType.Earn,
                                PointChange = pointsEarned,
                                PointBefore = before,
                                PointAfter = currentPoint,
                                Reason = $"Tích điểm đơn hàng {orderCode}",
                                IsManual = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _context.CustomerPointTransactions.AddAsync(earnTrans);
                        }

                        // 3. Bù điểm khi trả món đã chế biến — thay cho hoàn tiền.
                        if (compensationBonusPoints > 0)
                        {
                            int before = currentPoint;
                            currentPoint += compensationBonusPoints;
                            var bonusTrans = new CustomerPointTransaction
                            {
                                CustomerId = customer.Id,
                                OrderId = order.Id,
                                RelatedCode = orderCode,
                                Type = MenuGoBE.Models.Enums.PointTransactionType.Earn,
                                PointChange = compensationBonusPoints,
                                PointBefore = before,
                                PointAfter = currentPoint,
                                Reason = $"Bù điểm do lỗi của nhà hàng khi làm món ăn - {orderCode}",
                                IsManual = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _context.CustomerPointTransactions.AddAsync(bonusTrans);
                        }

                        customer.Point = currentPoint;
                        await _customerRepo.UpdateAsync(customer);
                        await _context.SaveChangesAsync();

                        if (_hubContext != null)
                        {
                            try
                            {
                                await _hubContext.Clients.All.SendAsync("ReceiveCustomerUpdate", new { action = "point_change", customerId = customer.Id });
                            }
                            catch { }
                        }
                    }
                }

                if (order.VoucherId.HasValue)
                {
                    var voucher = await _voucherRepo.GetByIdAsync(order.VoucherId.Value);
                    if (voucher != null)
                    {
                        voucher.UsedCount += 1;
                        await _voucherRepo.UpdateAsync(voucher);
                    }
                }

                order.Status = "Paid";

                var hasSuccessPayment = await _paymentRepo.HasSuccessPaymentAsync(order.Id);
                if (!hasSuccessPayment && paymentMethod == "Cash")
                {
                    var payment = new Payment
                    {
                        OrderId = order.Id,
                        Amount = finalAmount,
                        Method = "Cash",
                        Status = "Success",
                        CreatedAt = DateTime.UtcNow,
                        CompletedAt = DateTime.UtcNow
                    };
                    await _paymentRepo.CreateAsync(payment);
                }

                if (order.TableId > 0)
                {
                    var table = await _tableRepo.GetByIdAsync(order.TableId);
                    if (table != null)
                    {
                        table.Status = "Cleaning";
                        paidTableIds.Add(table.Id);
                    }
                }

                var childOrders = await _repo.GetChildOrdersWithDetailsAsync(order.Id);
                var childTableIds = childOrders.Where(c => c.TableId > 0).Select(c => c.TableId).Distinct().ToList();
                var childTables = await _tableRepo.GetByIdsAsync(childTableIds);
                var childTableDict = childTables.ToDictionary(t => t.Id);

                foreach (var child in childOrders)
                {
                    if (child.Status == "Active")
                    {
                        child.Status = "Paid";
                        if (child.TableId > 0 && childTableDict.TryGetValue(child.TableId, out var childTable))
                        {
                            childTable.Status = "Cleaning";
                            paidTableIds.Add(childTable.Id);
                        }
                    }
                }

                // Tích hợp Redis & PostgreSQL: Trừ kho thực tế và giải phóng tạm giữ khi thanh toán
                var tableForInventory = await _tableRepo.GetByIdAsync(order.TableId);
                long? branchId = null;
                if (tableForInventory != null)
                {
                    var area = await _areaRepo.GetByIdAsync(tableForInventory.AreaId);
                    if (area != null) branchId = area.BranchId;
                }
                if (!branchId.HasValue)
                {
                    var firstBranch = await _context.Branches.Select(b => b.Id).FirstOrDefaultAsync();
                    if (firstBranch > 0) branchId = firstBranch;
                }

                if (branchId.HasValue)
                {
                    var allDetails = order.OrderDetails.Where(od => od.Status != "Cancelled").ToList();
                    foreach (var child in childOrders)
                    {
                        allDetails.AddRange(child.OrderDetails.Where(od => od.Status != "Cancelled"));
                    }

                    var productGroups = allDetails.GroupBy(d => d.ProductId)
                                               .Select(g => new { ProductId = g.Key, Quantity = g.Sum(d => d.Quantity), Price = g.First().Price })
                                               .ToList();

                    // 1. Tìm hoặc tạo Partner
                    Partner? targetPartner = null;
                    string searchPhone = "";
                    string partnerName = "Khách vãng lai";

                    if (order.CustomerId.HasValue)
                    {
                        var customer = await _customerRepo.GetByIdAsync(order.CustomerId.Value);
                        if (customer != null)
                        {
                            searchPhone = customer.Phone;
                            partnerName = customer.Name;
                        }
                    }

                    var partnerQuery = new MenuGoBE.Dtos.Partner.PartnerQueryDto
                    {
                        BranchId = branchId.Value,
                        Type = MenuGoBE.Models.Enums.PartnerType.Customer,
                        Search = string.IsNullOrEmpty(searchPhone) ? partnerName : searchPhone,
                        PageSize = 50
                    };

                    var (partners, _) = await _partnerRepo.GetPagedPartnersAsync(partnerQuery);

                    if (!string.IsNullOrEmpty(searchPhone))
                    {
                        targetPartner = partners.FirstOrDefault(p => p.Phone == searchPhone);
                    }
                    else
                    {
                        targetPartner = partners.FirstOrDefault(p => p.Name == partnerName);
                    }

                    if (targetPartner == null)
                    {
                        long validCreatedBy = order.CreatedBy ?? 1;
                        if (userId.HasValue)
                        {
                            var userExists = await _context.Accounts.AnyAsync(a => a.Id == userId.Value);
                            if (userExists) validCreatedBy = userId.Value;
                        }

                        if (!await _context.Accounts.AnyAsync(a => a.Id == validCreatedBy))
                        {
                            var firstAccount = await _context.Accounts.Select(a => a.Id).FirstOrDefaultAsync();
                            if (firstAccount > 0) validCreatedBy = firstAccount;
                        }

                        targetPartner = new Partner
                        {
                            BranchId = branchId.Value,
                            Type = MenuGoBE.Models.Enums.PartnerType.Customer,
                            Name = partnerName,
                            Phone = searchPhone,
                            Email = string.Empty,
                            CreatedBy = validCreatedBy,
                            CreatedAt = DateTime.UtcNow,
                            CustomerId = order.CustomerId // Gắn CustomerId vào Partner mới
                        };
                        targetPartner = await _partnerRepo.CreatePartnerAsync(targetPartner);
                    }
                    else
                    {
                        bool partnerUpdated = false;
                        if (!string.IsNullOrEmpty(partnerName) && targetPartner.Name != partnerName)
                        {
                            targetPartner.Name = partnerName;
                            partnerUpdated = true;
                        }
                        
                        if (order.CustomerId.HasValue && targetPartner.CustomerId != order.CustomerId.Value)
                        {
                            targetPartner.CustomerId = order.CustomerId.Value;
                            partnerUpdated = true;
                        }

                        if (partnerUpdated)
                        {
                            await _partnerRepo.UpdatePartnerAsync(targetPartner);
                        }
                    }

                    // 2. Chốt các chứng từ Pending của Order và tự động sinh CashFlow
                    var methodEnum = MenuGoBE.Models.Enums.PaymentMethod.Cash;
                    if (Enum.TryParse<MenuGoBE.Models.Enums.PaymentMethod>(paymentMethod, true, out var parsedMethod))
                    {
                        methodEnum = parsedMethod;
                    }

                    await _documentService.FinalizeOrderDocumentsAsync(order.Id, finalAmount, methodEnum, targetPartner?.Id, userId);
                }

                await _repo.SaveChangesAsync();
                await transaction.CommitAsync();

                return paidTableIds;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            });
        }




        public async Task<bool> CancelOrderAsync(long orderId)
        {
            var orderWithDetails = await _repo.GetActiveOrderByIdWithDetailsAsync(orderId);
            if (orderWithDetails == null) return false;

            if (orderWithDetails.OrderDetails.Any(od => od.Status != "Cancelled" && od.CookingStatus != "Waiting"))
            {
                return false;
            }

            orderWithDetails.Status = "Cancelled";
            foreach (var detail in orderWithDetails.OrderDetails)
            {
                if (detail.Status != "Cancelled")
                {
                    detail.Status = "Cancelled";
                }
            }

            if (orderWithDetails.TableId > 0)
            {
                var table = await _tableRepo.GetByIdAsync(orderWithDetails.TableId);
                if (table != null)
                {
                    table.Status = "Empty";
                }
            }

            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task<List<OrderViewDto>> GetReturnableOrdersByBranchAsync(long branchId)
        {
            var orders = await _repo.GetReturnableOrdersByBranchAsync(branchId);
            return _mapper.Map<List<OrderViewDto>>(orders);
        }

        public async Task<List<OrderViewDto>> GetPaidOrdersByBranchAsync(long branchId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var orders = await _repo.GetPaidOrdersByBranchAsync(branchId, startDate, endDate);
            var dtos = _mapper.Map<List<OrderViewDto>>(orders);

            var creatorIds = orders.Where(o => o.CreatedBy.HasValue).Select(o => o.CreatedBy!.Value).Distinct().ToList();
            var accounts = new Dictionary<long, string>();
            if (creatorIds.Any())
            {
                accounts = await _context.Accounts.Where(a => creatorIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name);
            }



            foreach (var dto in dtos)
            {
                if (dto.CreatedBy.HasValue && accounts.TryGetValue(dto.CreatedBy.Value, out var name))
                {
                    dto.CreatedByName = name;
                }
            }

            return dtos;
        }
    }
}
