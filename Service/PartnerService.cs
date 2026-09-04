using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Base;
using MenuGoBE.Dtos.Partner;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Dtos.Address;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class PartnerService : IPartnerService
    {
        private readonly IPartnerRepository _partnerRepo;
        private readonly IBranchRepository _branchRepo;
        private readonly AppDbContext _context;

        public PartnerService(IPartnerRepository partnerRepo, IBranchRepository branchRepo, AppDbContext context)
        {
            _partnerRepo = partnerRepo;
            _branchRepo = branchRepo;
            _context = context;
        }

        #region Lấy danh sách đối tác phân trang
        /// <summary>
        /// Lấy danh sách đối tác có phân trang và lọc theo mode.
        /// </summary>
        public async Task<PagedResult<PartnerViewDto>> GetPagedPartnersAsync(PartnerQueryDto query)
        {
            var (partners, totalCount) = await _partnerRepo.GetPagedPartnersAsync(query);

            var items = partners.Select(MapToViewDto).ToList();

            return new PagedResult<PartnerViewDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
        #endregion

        #region Lấy chi tiết đối tác theo ID
        /// <summary>
        /// Lấy thông tin chi tiết đối tác theo ID và kiểm tra mode phù hợp.
        /// </summary>
        public async Task<PartnerViewDto> GetPartnerByIdAsync(long id, int? mode = null)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(id);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại");
            }

            if (mode.HasValue)
            {
                bool matches = mode.Value switch
                {
                    1 => partner.Type == PartnerType.Supplier,
                    2 => partner.Type == PartnerType.Transporter || partner.Type == PartnerType.Other,
                    3 => partner.Type == PartnerType.Customer,
                    _ => true
                };

                if (!matches)
                {
                    throw new Exception($"Đối tác với Id {id} không phù hợp với mode {mode.Value}");
                }
            }

            return MapToViewDto(partner);
        }
        #endregion

        #region Lấy danh sách đối tác theo chi nhánh
        /// <summary>
        /// Lấy toàn bộ danh sách đối tác thuộc chi nhánh.
        /// </summary>
        public async Task<List<PartnerViewDto>> GetPartnersByBranchAsync(long branchId, string? search = null, int? mode = null)
        {
            var query = new PartnerQueryDto
            {
                BranchId = branchId,
                Search = search,
                Mode = mode,
                Page = 1,
                PageSize = int.MaxValue
            };

            var (partners, _) = await _partnerRepo.GetPagedPartnersAsync(query);
            return partners.Select(MapToViewDto).ToList();
        }
        #endregion

        #region Tạo mới đối tác
        /// <summary>
        /// Tạo thông tin đối tác mới.
        /// </summary>
        public async Task<PartnerViewDto> CreatePartnerAsync(PartnerCreateDto dto, long? createdBy = null)
        {
            var branch = await _branchRepo.GetByIdAsync(dto.BranchId);
            if (branch == null)
            {
                throw new MenuGoException(ErrorCodes.DocumentBranchNotFound);
            }

            var partner = new Partner
            {
                BranchId = dto.BranchId,
                Type = dto.Type,
                Name = dto.Name,
                AddressId = dto.AddressId,
                Phone = dto.Phone,
                Email = dto.Email,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _partnerRepo.CreatePartnerAsync(partner);
            return MapToViewDto(created);
        }
        #endregion

        #region Cập nhật đối tác
        /// <summary>
        /// Cập nhật thông tin đối tác.
        /// </summary>
        public async Task<PartnerViewDto> UpdatePartnerAsync(long id, PartnerUpdateDto dto)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(id);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại");
            }

            partner.Name = dto.Name;
            partner.AddressId = dto.AddressId;
            partner.Phone = dto.Phone;
            partner.Email = dto.Email;

            var updated = await _partnerRepo.UpdatePartnerAsync(partner);
            return MapToViewDto(updated);
        }
        #endregion

        #region Xóa đối tác
        /// <summary>
        /// Xóa thông tin đối tác nếu chưa phát sinh giao dịch.
        /// </summary>
        public async Task<bool> DeletePartnerAsync(long id)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(id);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại");
            }

            bool hasTransactions = await _partnerRepo.CheckPartnerInTransactionsAsync(id);
            if (hasTransactions)
            {
                throw new Exception("Không thể xóa Đối tác vì đã có phát sinh giao dịch.");
            }

            return await _partnerRepo.DeletePartnerAsync(partner);
        }
        #endregion

        #region Lấy tổng quan tài chính công nợ của Nhà cung cấp / Đối tác
        /// <summary>
        /// Tính tổng tiền nhập hàng, tổng tiền đã thanh toán và nợ tồn của nhà cung cấp.
        /// </summary>
        public async Task<PartnerFinancialSummaryDto> GetPartnerFinancialSummaryAsync(long partnerId)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(partnerId);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại.");
            }

            var importDocs = await _context.Documents
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Import && d.Status != DocumentStatus.Cancelled)
                .ToListAsync();

            decimal totalImportAmount = importDocs.Sum(d => d.TotalAmount);
            decimal totalPaidFromDocs = importDocs.Sum(d => d.AmountPaid);

            // Chỉ cộng thêm các phiếu chi không gắn với DocumentId để tránh tính trùng 2 lần
            var unlinkedCashFlowExpenses = await _context.CashFlows
                .Where(cf => cf.PartnerId == partnerId && !cf.IsDeleted && (int)cf.Direction == 2 && cf.DocumentId == null)
                .SumAsync(cf => (decimal?)cf.TotalAmount) ?? 0m;

            decimal totalPaidAmount = totalPaidFromDocs + unlinkedCashFlowExpenses;
            decimal remainingDebt = Math.Max(0m, totalImportAmount - totalPaidAmount);

            return new PartnerFinancialSummaryDto
            {
                PartnerId = partnerId,
                PartnerName = partner.Name,
                TotalImportAmount = totalImportAmount,
                TotalPaidAmount = totalPaidAmount,
                RemainingDebt = remainingDebt
            };
        }
        #endregion

        #region Lấy danh sách phiếu nhập hàng của Nhà cung cấp
        /// <summary>
        /// Lấy danh sách chứng từ nhập kho gắn với nhà cung cấp.
        /// </summary>
        public async Task<List<PartnerImportDocumentDto>> GetPartnerImportDocumentsAsync(long partnerId)
        {
            var docs = await _context.Documents
                .Include(d => d.CashFlows.Where(cf => !cf.IsDeleted))
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Import && d.Status != DocumentStatus.Cancelled)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return docs.Select(d =>
            {
                var txList = d.CashFlows.Where(cf => !cf.IsDeleted).Select(cf => new PartnerTransactionHistoryDto
                {
                    Id = $"CF-{cf.Id}",
                    TransactionDate = cf.BusinessDate,
                    TransactionType = (int)cf.Direction == 2 ? "Thanh toán NCC" : "Thu lại NCC",
                    Code = cf.Code ?? $"CF-{cf.Id}",
                    ImportCode = d.Code,
                    Amount = (int)cf.Direction == 2 ? -cf.TotalAmount : cf.TotalAmount,
                    Note = cf.Note ?? "Thanh toán cho nhà cung cấp",
                    PaymentMethod = cf.PaymentMethod.ToString()
                }).OrderByDescending(t => t.TransactionDate).ToList();

                // Fallback nếu có AmountPaid từ lúc tạo phiếu nhưng chưa có CashFlow riêng
                if (txList.Count == 0 && d.AmountPaid > 0)
                {
                    txList.Add(new PartnerTransactionHistoryDto
                    {
                        Id = $"DOC-PAID-{d.Id}",
                        TransactionDate = d.CreatedAt,
                        TransactionType = "Thanh toán NCC",
                        Code = $"TT-{d.Code}",
                        ImportCode = d.Code,
                        Amount = -d.AmountPaid,
                        Note = $"Thanh toán khi lập phiếu nhập {d.Code}",
                        PaymentMethod = "Tiền mặt"
                    });
                }

                return new PartnerImportDocumentDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    ImportDate = d.CreatedAt,
                    TotalAmount = d.TotalAmount,
                    AmountPaid = d.AmountPaid,
                    DebtAmount = Math.Max(0m, d.TotalAmount - d.AmountPaid),
                    Status = d.Status.ToString(),
                    Transactions = txList
                };
            }).ToList();
        }
        #endregion

        #region Thanh toán tiền cho Nhà cung cấp theo phiếu nhập kho
        /// <summary>
        /// Thanh toán tiền cho nhà cung cấp gắn với phiếu nhập kho, cập nhật công nợ và sinh phiếu chi CashFlow.
        /// </summary>
        public async Task<PartnerImportDocumentDto> PayPartnerImportDocumentAsync(long partnerId, long documentId, PartnerPayDocumentDto dto, long? userId = null)
        {
            var doc = await _context.Documents
                .Include(d => d.CashFlows.Where(cf => !cf.IsDeleted))
                .FirstOrDefaultAsync(d => d.Id == documentId && d.PartnerId == partnerId && d.Type == DocumentType.Import);

            if (doc == null)
            {
                throw new Exception("Không tìm thấy phiếu nhập kho của đối tác.");
            }

            if (doc.Status == DocumentStatus.Cancelled)
            {
                throw new Exception("Không thể thanh toán cho phiếu nhập kho đã bị hủy.");
            }

            decimal remainingDebt = Math.Max(0m, doc.TotalAmount - doc.AmountPaid);
            if (dto.Amount <= 0)
            {
                throw new Exception("Số tiền thanh toán phải lớn hơn 0.");
            }

            if (dto.Amount > remainingDebt)
            {
                throw new Exception($"Số tiền thanh toán ({dto.Amount:N0} VNĐ) không được vượt quá số nợ còn lại ({remainingDebt:N0} VNĐ).");
            }

            // Cập nhật số tiền đã trả trên chứng từ
            doc.AmountPaid += dto.Amount;
            doc.AmountDue = Math.Max(0m, doc.TotalAmount - doc.AmountPaid);

            // Sinh phiếu chi CashFlow thanh toán cho NCC
            var prefix = "PC";
            var tempCode = $"{prefix}{DateTime.UtcNow.Ticks % 1_000_000:D6}";
            var cashFlow = new CashFlow
            {
                BranchId = doc.BranchId,
                DocumentId = doc.Id,
                PartnerId = doc.PartnerId ?? partnerId,
                Code = tempCode,
                BusinessDate = dto.PaymentDate == default ? DateTime.UtcNow : dto.PaymentDate,
                Direction = CashFlowDirection.Outflow,
                Type = CashFlowDetailType.Payment,
                PaymentMethod = (PaymentMethod)(dto.PaymentMethod > 0 ? dto.PaymentMethod : 1),
                Status = CashFlowStatus.Completed,
                TotalAmount = dto.Amount,
                Note = !string.IsNullOrWhiteSpace(dto.Note) ? dto.Note : $"Thanh toán cho NCC phiếu nhập {doc.Code}",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.CashFlows.Add(cashFlow);
            await _context.SaveChangesAsync();

            // Cập nhật mã phiếu chính thức sau khi có Id tự tăng
            cashFlow.Code = $"{prefix}{cashFlow.Id:D6}";
            await _context.SaveChangesAsync();

            // Nạp lại danh sách giao dịch của phiếu
            var txList = await _context.CashFlows
                .Where(cf => cf.DocumentId == doc.Id && !cf.IsDeleted)
                .OrderByDescending(cf => cf.BusinessDate)
                .Select(cf => new PartnerTransactionHistoryDto
                {
                    Id = $"CF-{cf.Id}",
                    TransactionDate = cf.BusinessDate,
                    TransactionType = (int)cf.Direction == 2 ? "Thanh toán NCC" : "Thu lại NCC",
                    Code = cf.Code ?? $"CF-{cf.Id}",
                    ImportCode = doc.Code,
                    Amount = (int)cf.Direction == 2 ? -cf.TotalAmount : cf.TotalAmount,
                    Note = cf.Note ?? "Thanh toán cho nhà cung cấp",
                    PaymentMethod = cf.PaymentMethod.ToString()
                }).ToListAsync();

            return new PartnerImportDocumentDto
            {
                Id = doc.Id,
                Code = doc.Code,
                ImportDate = doc.CreatedAt,
                TotalAmount = doc.TotalAmount,
                AmountPaid = doc.AmountPaid,
                DebtAmount = Math.Max(0m, doc.TotalAmount - doc.AmountPaid),
                Status = doc.Status.ToString(),
                Transactions = txList
            };
        }
        #endregion

        #region Lấy lịch sử dòng tiền giao dịch tài chính với Nhà cung cấp
        /// <summary>
        /// Tổng hợp lịch sử chứng từ nhập kho và phiếu chi trả nhà cung cấp.
        /// </summary>
        public async Task<List<PartnerTransactionHistoryDto>> GetPartnerCashFlowHistoryAsync(long partnerId)
        {
            var result = new List<PartnerTransactionHistoryDto>();

            // Lấy danh sách chứng từ nhập kho của đối tác
            var importDocs = await _context.Documents
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Import && d.Status != DocumentStatus.Cancelled)
                .ToListAsync();

            var importDocIds = importDocs.Select(d => d.Id).ToHashSet();

            // Lấy các dòng tiền chi/thu liên kết trực tiếp bằng PartnerId hoặc thông qua DocumentId của đối tác
            var cashFlows = await _context.CashFlows
                .Include(cf => cf.Document)
                .Where(cf => !cf.IsDeleted && (cf.PartnerId == partnerId || (cf.DocumentId != null && importDocIds.Contains(cf.DocumentId.Value))))
                .ToListAsync();

            var recordedDocIdsWithPayment = cashFlows
                .Where(cf => cf.DocumentId != null && (int)cf.Direction == 2)
                .Select(cf => cf.DocumentId!.Value)
                .ToHashSet();

            foreach (var doc in importDocs)
            {
                result.Add(new PartnerTransactionHistoryDto
                {
                    Id = $"DOC-{doc.Id}",
                    TransactionDate = doc.CreatedAt,
                    TransactionType = "Nhập hàng",
                    Code = string.Empty,
                    ImportCode = doc.Code,
                    Amount = doc.TotalAmount,
                    Note = "Ghi nhận công nợ nhập kho",
                    PaymentMethod = "Ghi nợ NCC"
                });

                // Fallback nếu phiếu nhập có thanh toán ngay lúc tạo nhưng chưa có phiếu chi CashFlow riêng
                if (doc.AmountPaid > 0 && !recordedDocIdsWithPayment.Contains(doc.Id))
                {
                    result.Add(new PartnerTransactionHistoryDto
                    {
                        Id = $"DOC-PAID-{doc.Id}",
                        TransactionDate = doc.CreatedAt,
                        TransactionType = "Thanh toán NCC",
                        Code = $"TT-{doc.Code}",
                        ImportCode = doc.Code,
                        Amount = -doc.AmountPaid,
                        Note = $"Thanh toán khi lập phiếu nhập {doc.Code}",
                        PaymentMethod = "Tiền mặt"
                    });
                }
            }

            foreach (var cf in cashFlows)
            {
                bool isExpense = (int)cf.Direction == 2;
                result.Add(new PartnerTransactionHistoryDto
                {
                    Id = $"CF-{cf.Id}",
                    TransactionDate = cf.BusinessDate,
                    TransactionType = isExpense ? "Thanh toán NCC" : "Thu lại NCC",
                    Code = cf.Code ?? $"CF-{cf.Id}",
                    ImportCode = cf.Document != null ? cf.Document.Code : (importDocs.FirstOrDefault(d => d.Id == cf.DocumentId)?.Code),
                    Amount = isExpense ? -cf.TotalAmount : cf.TotalAmount,
                    Note = cf.Note ?? "Thanh toán cho nhà cung cấp",
                    PaymentMethod = cf.PaymentMethod.ToString()
                });
            }

            return result.OrderByDescending(t => t.TransactionDate).ToList();
        }
        #endregion

        #region Lấy lịch sử mua hàng của Khách hàng
        /// <summary>
        /// Lấy danh sách hóa đơn/đơn hàng đã mua của khách hàng.
        /// </summary>
        public async Task<List<CustomerPurchaseHistoryDto>> GetCustomerPurchaseHistoryAsync(long customerId)
        {
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId && o.Status != "Cancelled")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o =>
            {
                decimal finalAmt = Math.Max(0m, o.TotalAmount - o.DiscountAmount);
                return new CustomerPurchaseHistoryDto
                {
                    OrderId = o.Id,
                    InvoiceCode = $"HD{o.Id}",
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    FinalAmount = finalAmt,
                    PointsEarned = (int)Math.Floor(finalAmt / 10000m),
                    PointsUsed = o.PointsUsed,
                    Status = o.Status
                };
            }).ToList();
        }
        #endregion

        #region Lấy lịch sử tăng/giảm điểm của Khách hàng
        /// <summary>
        /// Tổng hợp lịch sử thay đổi điểm tích lũy của khách hàng từ các đơn hàng.
        /// </summary>
        public async Task<List<CustomerPointHistoryDto>> GetCustomerPointHistoryAsync(long customerId)
        {
            var result = new List<CustomerPointHistoryDto>();

            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId && o.Status == "Completed")
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            int runningPoint = 0;
            foreach (var order in orders)
            {
                string code = $"HD{order.Id}";
                decimal finalAmt = Math.Max(0m, order.TotalAmount - order.DiscountAmount);

                if (order.PointsUsed > 0)
                {
                    int pointBefore = runningPoint;
                    runningPoint = Math.Max(0, runningPoint - order.PointsUsed);
                    result.Add(new CustomerPointHistoryDto
                    {
                        Id = order.Id * 10 + 1,
                        Time = order.CreatedAt,
                        Type = "Sử dụng điểm",
                        RelatedCode = code,
                        PointChange = -order.PointsUsed,
                        PointBefore = pointBefore,
                        PointAfter = runningPoint,
                        Reason = $"Thanh toán đơn hàng {code}"
                    });
                }

                int pointsEarned = (int)Math.Floor(finalAmt / 10000m);
                if (pointsEarned > 0)
                {
                    int pointBefore = runningPoint;
                    runningPoint += pointsEarned;
                    result.Add(new CustomerPointHistoryDto
                    {
                        Id = order.Id * 10 + 2,
                        Time = order.CreatedAt,
                        Type = "Tích điểm",
                        RelatedCode = code,
                        PointChange = pointsEarned,
                        PointBefore = pointBefore,
                        PointAfter = runningPoint,
                        Reason = $"Tích điểm mua hàng {code}"
                    });
                }
            }

            return result.OrderByDescending(p => p.Time).ToList();
        }
        #endregion

        #region Helper MapToViewDto
        private PartnerViewDto MapToViewDto(Partner partner)
        {
            return new PartnerViewDto
            {
                Id = partner.Id,
                BranchId = partner.BranchId,
                Type = partner.Type,
                Name = partner.Name,
                AddressId = partner.AddressId,
                Phone = partner.Phone,
                Email = partner.Email,
                CreatedBy = partner.CreatedBy,
                CreatedAt = partner.CreatedAt
            };
        }
        #endregion
    }
}
