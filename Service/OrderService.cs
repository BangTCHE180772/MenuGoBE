using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Models.Enums;

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
        private readonly IRedisInventoryService _redisInventoryService;
        private readonly IBInventoryRepository _bInventoryRepo;
        private readonly IDocumentRepository _documentRepo;
        private readonly IPartnerRepository _partnerRepo;

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
            IRedisInventoryService redisInventoryService,
            IBInventoryRepository bInventoryRepo,
            IDocumentRepository documentRepo,
            IPartnerRepository partnerRepo)
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
            _redisInventoryService = redisInventoryService;
            _bInventoryRepo = bInventoryRepo;
            _documentRepo = documentRepo;
            _partnerRepo = partnerRepo;
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
            if (dto.TableId > 0)
            {
                var table = await _tableRepo.GetByIdAsync(dto.TableId);
                if (table != null && (table.Status == "Internal" || table.Status == "Deleted"))
                {
                    throw new Exception("Không thể tạo hóa đơn mới trên bàn hệ thống nội bộ.");
                }

                var existingActiveOrder = await _repo.GetActiveOrderByTableIdAsync(dto.TableId);
                if (existingActiveOrder != null)
                {
                    throw new Exception("Bàn này đã có hóa đơn chưa thanh toán. Không thể tạo hóa đơn mới.");
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

        public async Task<decimal> PreparePaymentAsync(long orderId, OrderPayDto? dto)
        {
            var orderWithDetails = await _repo.GetActiveOrderByIdWithDetailsAsync(orderId);
            if (orderWithDetails == null) throw new Exception("Không tìm thấy đơn hàng hoặc đơn hàng đã thanh toán");
            var order = orderWithDetails;
            var childOrders = await _repo.GetChildOrdersWithDetailsAsync(order.Id);

            decimal totalAmount = order.OrderDetails.Where(od => od.Status != "Cancelled").Sum(od => od.Price * od.Quantity);
            foreach (var child in childOrders)
            {
                totalAmount += child.OrderDetails.Where(od => od.Status != "Cancelled").Sum(od => od.Price * od.Quantity);
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
                if (customer != null && customer.Point >= dto.PointsUsed)
                {
                    pointsUsed = dto.PointsUsed;
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

            order.DiscountAmount = discountAmount;
            order.PointsUsed = pointsUsed;
            order.TotalAmount = grandTotal - discountAmount - pointsUsed;

            await _repo.UpdateAsync(order);
            await _repo.SaveChangesAsync();

            return order.TotalAmount;
        }

        public async Task<List<long>> PayOrderAsync(long orderId, OrderPayDto? dto = null, string paymentMethod = "Cash")
        {
            var paidTableIds = new List<long>();
            var orderWithDetails = await _repo.GetActiveOrderByIdWithDetailsAsync(orderId);
            if (orderWithDetails == null) return paidTableIds;

            var order = orderWithDetails;
            
            if (paymentMethod == "Cash" || dto != null)
            {
                await PreparePaymentAsync(orderId, dto);
                order = await _repo.GetActiveOrderByIdWithDetailsAsync(orderId);
                if (order == null) return paidTableIds;
            }

            var finalAmount = order.TotalAmount;
            int pointsEarned = (int)(finalAmount / 100000) * 100;

            if (order.CustomerId.HasValue)
            {
                var customer = await _customerRepo.GetByIdAsync(order.CustomerId.Value);
                if (customer != null)
                {
                    customer.Point = customer.Point - order.PointsUsed + pointsEarned;
                    await _customerRepo.UpdateAsync(customer);
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

            // Tích hợp Redis & PostgreSQL: Trừ kho thực tế và giải phóng tạm giữ khi thanh toán
            var tableForInventory = await _tableRepo.GetByIdAsync(order.TableId);
            long? branchId = null;
            if (tableForInventory != null)
            {
                var area = await _areaRepo.GetByIdAsync(tableForInventory.AreaId);
                if (area != null) branchId = area.BranchId;
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

#if FALSE
                var document = new MenuGoBE.Models.Document
                {
                    BranchId = branchId.Value,
                    BusinessDate = DateTime.UtcNow,
                    Code = "PXBH-" + order.Id, // Phiếu Xuất Bán Hàng
                    Type = DocumentType.Sales,
                    TransferStatus = DocumentTransferStatus.None,
                    TotalAmount = finalAmount,
                    CreatedAt = DateTime.UtcNow
                };

                var documentDetails = new List<DocumentDetail>();
                var bInvsToUpdate = new List<BInventory>();
                var ledgersToCreate = new List<InventoryLedger>();
                var docPartners = new List<DocumentPartner>();

                // Đồng bộ Customer -> Partner
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

                // Tìm Partner có sẵn
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
                    // Tạo mới
                    targetPartner = new Partner
                    {
                        BranchId = branchId.Value,
                        Type = MenuGoBE.Models.Enums.PartnerType.Customer,
                        Name = partnerName,
                        Phone = searchPhone,
                        Email = string.Empty,
                        CreatedBy = order.CreatedBy ?? 1,
                        CreatedAt = DateTime.UtcNow
                    };
                    targetPartner = await _partnerRepo.CreatePartnerAsync(targetPartner);
                }

                if (targetPartner != null)
                {
                    docPartners.Add(new DocumentPartner
                    {
                        PartnerId = targetPartner.Id,
                        Cost = finalAmount,
                        Note = $"Giao dịch đơn hàng {order.Id}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                foreach (var group in productGroups)
                {
                    var product = await _productRepo.GetByIdAsync(group.ProductId);
                    if (product == null) continue;

                    var productTypeStr = product.Type.ToString();
                    if (productTypeStr == "Regular" || productTypeStr == "Manufactured")
                    {
                        var binv = await _bInventoryRepo.GetByProductAndBranchAsync(group.ProductId, branchId.Value);
                        if (binv != null)
                        {
                            binv.Quantity -= group.Quantity;
                            bInvsToUpdate.Add(binv);

                            documentDetails.Add(new DocumentDetail
                            {
                                BInventoryId = binv.Id,
                                Quantity = group.Quantity,
                                UnitPrice = group.Price,
                                CreatedAt = DateTime.UtcNow
                            });

                            ledgersToCreate.Add(new InventoryLedger
                            {
                                BInventoryId = binv.Id,
                                DocumentType = DocumentType.Sales,
                                SnapshotProductName = product.Name,
                                SnapshotUnitName = product.Name,
                                ConversionRateSnapshot = 1m,
                                QuantityDelta = -group.Quantity,
                                RunningQuantity = binv.Quantity,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                        await _redisInventoryService.DecrementReservedQuantityAsync((int)branchId.Value, (int)group.ProductId, group.Quantity);
                    }
                    else if (productTypeStr == "Processed")
                    {
                        if (product.RecipeItems != null)
                        {
                            foreach (var recipeItem in product.RecipeItems)
                            {
                                var ingredientQty = recipeItem.Quantity * group.Quantity;
                                var binv = await _bInventoryRepo.GetByProductAndBranchAsync(recipeItem.IngredientProductId, branchId.Value);
                                if (binv != null)
                                {
                                    binv.Quantity -= ingredientQty;
                                    bInvsToUpdate.Add(binv);

                                    documentDetails.Add(new DocumentDetail
                                    {
                                        BInventoryId = binv.Id,
                                        Quantity = ingredientQty,
                                        UnitPrice = 0,
                                        CreatedAt = DateTime.UtcNow
                                    });

                                    ledgersToCreate.Add(new InventoryLedger
                                    {
                                        BInventoryId = binv.Id,
                                        DocumentType = DocumentType.Sales,
                                        SnapshotProductName = recipeItem.IngredientProduct?.Name ?? string.Empty,
                                        SnapshotUnitName = recipeItem.IngredientProduct?.Name ?? string.Empty,
                                        ConversionRateSnapshot = 1m,
                                        QuantityDelta = -ingredientQty,
                                        RunningQuantity = binv.Quantity,
                                        CreatedAt = DateTime.UtcNow
                                    });
                                }
                            }
                        }
                    }
                }

                var cashFlows = new List<CashFlow>();
                var cashFlowDetails = new List<CashFlowDetail>();

                if (finalAmount > 0)
                {
                    var cashFlow = new CashFlow
                    {
                        BranchId = branchId.Value,
                        Code = "PTBH-" + order.Id, // Phiếu Thu Bán Hàng
                        BusinessDate = DateTime.UtcNow,
                        Direction = MenuGoBE.Models.Enums.CashFlowDirection.Inflow,
                        Status = MenuGoBE.Models.Enums.CashFlowStatus.Completed,
                        TotalAmount = finalAmount,
                        CreatedAt = DateTime.UtcNow
                    };
                    cashFlows.Add(cashFlow);

                    var methodEnum = MenuGoBE.Models.Enums.PaymentMethod.Cash;
                    if (Enum.TryParse<MenuGoBE.Models.Enums.PaymentMethod>(paymentMethod, true, out var parsedMethod))
                    {
                        methodEnum = parsedMethod;
                    }

                    cashFlowDetails.Add(new CashFlowDetail
                    {
                        Type = MenuGoBE.Models.Enums.CashFlowDetailType.Payment,
                        PaymentMethod = methodEnum,
                        Amount = finalAmount,
                        Note = $"Thanh toán đơn {order.Id}"
                    });
                }

                if (bInvsToUpdate.Any() || cashFlows.Any())
                {
                    await _documentRepo.ExecuteDocumentTransactionAsync(
                        document,
                        documentDetails,
                        docPartners,
                        bInvsToUpdate,
                        ledgersToCreate,
                        new List<InventoryLedger>(),
                        cashFlows,
                        cashFlowDetails
                    );
                }
#endif
            }

            await _repo.SaveChangesAsync();
            return paidTableIds;
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

            await _repo.UpdateAsync(orderWithDetails);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}
