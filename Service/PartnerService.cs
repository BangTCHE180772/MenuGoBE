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
        /// Lấy danh sách đối tác có phân trang và lọc theo mode, kèm tính toán dư nợ.
        /// </summary>
        public async Task<PagedResult<PartnerViewDto>> GetPagedPartnersAsync(PartnerQueryDto query)
        {
            var (partners, totalCount) = await _partnerRepo.GetPagedPartnersAsync(query);

            var items = await PopulatePartnersRemainingDebtAsync(partners);

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

            // Tính toán số dư công nợ cho đối tác
            var populated = await PopulatePartnersRemainingDebtAsync(new List<Partner> { partner });
            return populated.FirstOrDefault() ?? MapToViewDto(partner);
        }
        #endregion

        #region Lấy danh sách đối tác theo chi nhánh
        /// <summary>
        /// Lấy toàn bộ danh sách đối tác thuộc chi nhánh, kèm tính toán dư nợ.
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
            return await PopulatePartnersRemainingDebtAsync(partners);
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
                ImageUrls = dto.ImageUrls != null && dto.ImageUrls.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(dto.ImageUrls) : null,
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
            if (dto.ImageUrls != null)
            {
                partner.ImageUrls = dto.ImageUrls.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(dto.ImageUrls) : null;
            }

            var updated = await _partnerRepo.UpdatePartnerAsync(partner);
            return MapToViewDto(updated);
        }
        #endregion

        #region Cập nhật hình ảnh hợp đồng đối tác
        /// <summary>
        /// Cập nhật danh sách hình ảnh hợp đồng cho đối tác/nhà cung cấp.
        /// </summary>
        public async Task<PartnerViewDto> UpdatePartnerImagesAsync(long id, List<string>? imageUrls)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(id);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại");
            }

            partner.ImageUrls = imageUrls != null && imageUrls.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(imageUrls) : null;

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
        /// Tính tổng quan tài chính công nợ đối tác theo công thức bù trừ âm/dương:
        /// Dư Nợ = (Tổng tiền hàng Nhập - Tổng tiền hàng Trả) - (Tổng tiền mình Đã Chi - Tổng tiền mình Đã Thu).
        /// Cho phép giá trị âm biểu thị NCC đang nợ lại cửa hàng.
        /// </summary>
        public async Task<PartnerFinancialSummaryDto> GetPartnerFinancialSummaryAsync(long partnerId)
        {
            var partner = await _partnerRepo.GetPartnerByIdAsync(partnerId);
            if (partner == null)
            {
                throw new Exception("Đối tác không tồn tại.");
            }

            // Lấy danh sách phiếu nhập kho hợp lệ
            var importDocs = await _context.Documents
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Import && d.Status != DocumentStatus.Cancelled)
                .ToListAsync();

            // Lấy danh sách phiếu trả hàng hợp lệ
            var returnDocs = await _context.Documents
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Return && d.Status != DocumentStatus.Cancelled)
                .ToListAsync();

            // Tổng tiền hàng Nhập và tiền đã thanh toán trực tiếp qua phiếu nhập
            decimal totalImportAmount = importDocs.Sum(d => d.TotalAmount);
            decimal totalPaidFromDocs = importDocs.Sum(d => d.AmountPaid);

            // Tổng tiền hàng Trả và tiền NCC hoàn trả trực tiếp qua phiếu trả
            decimal totalReturnAmount = returnDocs.Sum(d => d.TotalAmount);
            decimal totalRefundFromDocs = returnDocs.Sum(d => d.AmountPaid);

            // Phiếu chi độc lập không gắn DocumentId (tránh tính trùng)
            var unlinkedExpenses = await _context.CashFlows
                .Where(cf => cf.PartnerId == partnerId && !cf.IsDeleted && (int)cf.Direction == 2 && cf.DocumentId == null)
                .SumAsync(cf => (decimal?)cf.TotalAmount) ?? 0m;

            // Phiếu thu độc lập không gắn DocumentId (tránh tính trùng)
            var unlinkedInflows = await _context.CashFlows
                .Where(cf => cf.PartnerId == partnerId && !cf.IsDeleted && (int)cf.Direction == 1 && cf.DocumentId == null)
                .SumAsync(cf => (decimal?)cf.TotalAmount) ?? 0m;

            // Tổng tiền mình Đã Chi = Đã trả qua phiếu nhập + Phiếu chi rời
            decimal totalPaidAmount = totalPaidFromDocs + unlinkedExpenses;

            // Tổng tiền mình Đã Thu = NCC hoàn trả qua phiếu trả + Phiếu thu rời
            decimal totalReceivedAmount = totalRefundFromDocs + unlinkedInflows;

            // Dư Nợ = (Tổng tiền hàng Nhập - Tổng tiền hàng Trả) - (Tổng tiền mình Đã Chi - Tổng tiền mình Đã Thu)
            decimal remainingDebt = (totalImportAmount - totalReturnAmount) - (totalPaidAmount - totalReceivedAmount);
            // Loại bỏ sai số lẻ dưới 1 đồng và làm tròn theo chuẩn VNĐ
            remainingDebt = Math.Abs(remainingDebt) < 1m ? 0m : Math.Round(remainingDebt, 0, MidpointRounding.AwayFromZero);

            return new PartnerFinancialSummaryDto
            {
                PartnerId = partnerId,
                PartnerName = partner.Name,
                TotalImportAmount = totalImportAmount,
                TotalReturnAmount = totalReturnAmount,
                TotalPaidAmount = totalPaidAmount,
                TotalReceivedAmount = totalReceivedAmount,
                RemainingDebt = remainingDebt
            };
        }
        #endregion

        #region Lấy danh sách phiếu nhập hàng của Nhà cung cấp
        /// <summary>
        /// Lấy danh sách chứng từ nhập kho gắn với nhà cung cấp, hỗ trợ cấn trừ công nợ và trừ hàng xuất trả.
        /// </summary>
        public async Task<List<PartnerImportDocumentDto>> GetPartnerImportDocumentsAsync(long partnerId)
        {
            var docs = await _context.Documents
                .Include(d => d.CashFlows.Where(cf => !cf.IsDeleted))
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Import && d.Status != DocumentStatus.Cancelled)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            // Lấy danh sách các phiếu xuất trả hàng liên kết với các phiếu nhập
            var returnDocs = await _context.Documents
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Return && d.Status != DocumentStatus.Cancelled && d.ParentDocumentId != null)
                .ToListAsync();

            var returnDocsByParent = returnDocs
                .GroupBy(r => r.ParentDocumentId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            return docs.Select(d =>
            {
                var childReturns = returnDocsByParent.GetValueOrDefault(d.Id, new List<Models.Document>());
                decimal returnedAmount = childReturns.Sum(r => r.TotalAmount);
                decimal effectivePayableAmount = Math.Max(0m, d.TotalAmount - returnedAmount);

                decimal debtDeduction = ExtractDebtDeduction(d.Note);

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

                // Bổ sung các bản ghi xuất trả hàng vào lịch sử giao dịch của phiếu nhập
                foreach (var ret in childReturns)
                {
                    txList.Add(new PartnerTransactionHistoryDto
                    {
                        Id = $"DOC-RET-{ret.Id}",
                        TransactionDate = ret.CreatedAt,
                        TransactionType = "Xuất trả hàng",
                        Code = ret.Code,
                        ImportCode = d.Code,
                        Amount = -ret.TotalAmount,
                        Note = !string.IsNullOrWhiteSpace(ret.Note) ? ret.Note : $"Xuất trả hàng cho NCC #{ret.Code}",
                        PaymentMethod = "Trừ giá trị hàng trả"
                    });
                }

                // Ghi nhận giao dịch cấn trừ công nợ nếu có
                if (debtDeduction > 0)
                {
                    txList.Add(new PartnerTransactionHistoryDto
                    {
                        Id = $"DOC-DED-{d.Id}",
                        TransactionDate = d.CreatedAt,
                        TransactionType = "Trừ tiền NCC nợ",
                        Code = $"DED-{d.Code}",
                        ImportCode = d.Code,
                        Amount = -debtDeduction,
                        Note = $"Cấn trừ công nợ NCC khi lập phiếu nhập {d.Code}",
                        PaymentMethod = "Cấn trừ nợ NCC"
                    });
                }

                // Fallback nếu có AmountPaid từ lúc tạo phiếu nhưng chưa có CashFlow riêng
                if (txList.Count(t => t.TransactionType == "Thanh toán NCC") == 0 && d.AmountPaid > 0)
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

                decimal debtAmount = Math.Max(0m, (effectivePayableAmount - debtDeduction) - d.AmountPaid);
                // Loại bỏ sai số lẻ dưới 1 đồng và làm tròn
                debtAmount = debtAmount < 1m ? 0m : Math.Round(debtAmount, 0, MidpointRounding.AwayFromZero);
                decimal totalSettled = d.AmountPaid + debtDeduction;

                string paymentStatus;
                if (returnedAmount >= d.TotalAmount && d.TotalAmount > 0)
                {
                    paymentStatus = "Returned";
                }
                else if (debtAmount == 0)
                {
                    paymentStatus = "Paid";
                }
                else if (totalSettled > 0 || returnedAmount > 0)
                {
                    paymentStatus = "Partial";
                }
                else
                {
                    paymentStatus = "Unpaid";
                }

                return new PartnerImportDocumentDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    ImportDate = d.CreatedAt,
                    TotalAmount = d.TotalAmount,
                    AmountPaid = totalSettled,
                    ReturnedAmount = returnedAmount,
                    EffectivePayableAmount = effectivePayableAmount,
                    DebtAmount = debtAmount,
                    Status = d.Status.ToString(),
                    PaymentStatus = paymentStatus,
                    Transactions = txList.OrderByDescending(t => t.TransactionDate).ToList()
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

            // Tính tổng tiền hàng đã xuất trả cho phiếu nhập này
            var returnedAmount = await _context.Documents
                .Where(d => d.ParentDocumentId == doc.Id && d.Type == DocumentType.Return && d.Status != DocumentStatus.Cancelled)
                .SumAsync(d => d.TotalAmount);

            decimal debtDeduction = ExtractDebtDeduction(doc.Note);
            decimal effectivePayable = Math.Max(0m, doc.TotalAmount - returnedAmount);
            decimal remainingDebt = Math.Max(0m, (effectivePayable - debtDeduction) - doc.AmountPaid);

            if (remainingDebt <= 0)
            {
                throw new Exception("Phiếu nhập này đã hoàn tất thanh toán hoặc đã xuất trả lại hàng, không còn dư nợ để thanh toán.");
            }

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
            doc.AmountDue = Math.Max(0m, (effectivePayable - debtDeduction) - doc.AmountPaid);

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

            // Lấy danh sách chứng từ trả hàng của đối tác
            var returnDocs = await _context.Documents
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Return && d.Status != DocumentStatus.Cancelled)
                .ToListAsync();

            var allDocIds = importDocs.Select(d => d.Id).Concat(returnDocs.Select(d => d.Id)).ToHashSet();

            // Lấy các dòng tiền chi/thu liên kết trực tiếp bằng PartnerId hoặc thông qua DocumentId của đối tác
            var cashFlows = await _context.CashFlows
                .Include(cf => cf.Document)
                .Where(cf => !cf.IsDeleted && (cf.PartnerId == partnerId || (cf.DocumentId != null && allDocIds.Contains(cf.DocumentId.Value))))
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

            foreach (var doc in returnDocs)
            {
                result.Add(new PartnerTransactionHistoryDto
                {
                    Id = $"DOC-RET-{doc.Id}",
                    TransactionDate = doc.CreatedAt,
                    TransactionType = "Trả hàng NCC",
                    Code = doc.Code,
                    ImportCode = doc.ParentDocumentId.HasValue ? (importDocs.FirstOrDefault(d => d.Id == doc.ParentDocumentId.Value)?.Code ?? string.Empty) : string.Empty,
                    Amount = -doc.TotalAmount,
                    Note = !string.IsNullOrWhiteSpace(doc.Note) ? doc.Note : "Xuất trả hàng cho NCC",
                    PaymentMethod = "Giảm trừ công nợ"
                });
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
                    ImportCode = cf.Document != null ? cf.Document.Code : (importDocs.FirstOrDefault(d => d.Id == cf.DocumentId)?.Code ?? returnDocs.FirstOrDefault(d => d.Id == cf.DocumentId)?.Code),
                    Amount = isExpense ? -cf.TotalAmount : cf.TotalAmount,
                    Note = cf.Note ?? (isExpense ? "Thanh toán cho nhà cung cấp" : "Thu lại từ nhà cung cấp"),
                    PaymentMethod = cf.PaymentMethod.ToString()
                });
            }

            return result.OrderByDescending(t => t.TransactionDate).ToList();
        }
        #endregion

        #region Lấy lịch sử mua hàng của khách hàng
        public async Task<List<CustomerPurchaseHistoryDto>> GetCustomerPurchaseHistoryAsync(long customerId)
        {
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId && o.Status != "Cancelled")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderIds = orders.Select(o => o.Id).ToList();

            // Lấy các giao dịch tích điểm thực tế tương ứng với từng hóa đơn
            var earnPointTransactions = await _context.CustomerPointTransactions
                .Where(t => t.CustomerId == customerId
                         && t.OrderId.HasValue
                         && orderIds.Contains(t.OrderId.Value)
                         && t.Type == PointTransactionType.Earn
                         && !t.IsReversed)
                .ToListAsync();

            return orders.Select(o =>
            {
                decimal finalAmt = Math.Max(0m, o.TotalAmount - o.DiscountAmount);
                var matchedEarnTrans = earnPointTransactions.Where(t => t.OrderId == o.Id).ToList();
                int actualPointsEarned = matchedEarnTrans.Any()
                    ? matchedEarnTrans.Sum(t => t.PointChange)
                    : (int)(finalAmt / 100000m) * 1000;

                return new CustomerPurchaseHistoryDto
                {
                    OrderId = o.Id,
                    InvoiceCode = $"HD{o.Id}",
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    FinalAmount = finalAmt,
                    PointsEarned = actualPointsEarned,
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
            var imgs = new List<string>();
            if (!string.IsNullOrWhiteSpace(partner.ImageUrls))
            {
                try
                {
                    imgs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(partner.ImageUrls) ?? new List<string>();
                }
                catch
                {
                    imgs = new List<string> { partner.ImageUrls };
                }
            }

            return new PartnerViewDto
            {
                Id = partner.Id,
                BranchId = partner.BranchId,
                Type = partner.Type,
                Name = partner.Name,
                AddressId = partner.AddressId,
                Phone = partner.Phone,
                Email = partner.Email,
                ImageUrls = imgs,
                CreatedBy = partner.CreatedBy,
                CreatedAt = partner.CreatedAt
            };
        }
        #endregion

        #region Helper trích xuất số tiền trừ nợ NCC từ Note
        /// <summary>
        /// Trích xuất số tiền đã trừ nợ NCC từ ghi chú phiếu nhập kho.
        /// </summary>
        private static decimal ExtractDebtDeduction(string? note)
        {
            if (string.IsNullOrWhiteSpace(note)) return 0m;
            var match = System.Text.RegularExpressions.Regex.Match(note, @"Trừ tiền NCC nợ:\s*([0-9.,]+)");
            if (match.Success)
            {
                var rawNum = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(rawNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
                {
                    return val;
                }
            }
            return 0m;
        }
        #endregion

        #region Helper tính Dư nợ cho danh sách đối tác
        /// <summary>
        /// Tính toán số dư công nợ tổng hợp cho danh sách đối tác.
        /// </summary>
        private async Task<List<PartnerViewDto>> PopulatePartnersRemainingDebtAsync(List<Partner> partners)
        {
            if (partners.Count == 0) return new List<PartnerViewDto>();

            var partnerIds = partners.Select(p => p.Id).ToList();

            var importDocs = await _context.Documents
                .Where(d => d.PartnerId.HasValue && partnerIds.Contains(d.PartnerId.Value) && d.Type == DocumentType.Import && d.Status != DocumentStatus.Cancelled)
                .Select(d => new { PartnerId = d.PartnerId!.Value, d.TotalAmount, d.AmountPaid })
                .ToListAsync();

            var returnDocs = await _context.Documents
                .Where(d => d.PartnerId.HasValue && partnerIds.Contains(d.PartnerId.Value) && d.Type == DocumentType.Return && d.Status != DocumentStatus.Cancelled)
                .Select(d => new { PartnerId = d.PartnerId!.Value, d.TotalAmount, d.AmountPaid })
                .ToListAsync();

            var unlinkedCashFlows = await _context.CashFlows
                .Where(cf => cf.PartnerId.HasValue && partnerIds.Contains(cf.PartnerId.Value) && !cf.IsDeleted && cf.DocumentId == null)
                .Select(cf => new { PartnerId = cf.PartnerId!.Value, cf.Direction, cf.TotalAmount })
                .ToListAsync();

            return partners.Select(p =>
            {
                var dto = MapToViewDto(p);
                var pImports = importDocs.Where(d => d.PartnerId == p.Id).ToList();
                var pReturns = returnDocs.Where(d => d.PartnerId == p.Id).ToList();
                var pCFs = unlinkedCashFlows.Where(cf => cf.PartnerId == p.Id).ToList();

                decimal totalImport = pImports.Sum(d => d.TotalAmount);
                decimal totalPaid = pImports.Sum(d => d.AmountPaid) + pCFs.Where(cf => (int)cf.Direction == 2).Sum(cf => cf.TotalAmount);
                decimal totalReturn = pReturns.Sum(d => d.TotalAmount);
                decimal totalRefund = pReturns.Sum(d => d.AmountPaid) + pCFs.Where(cf => (int)cf.Direction == 1).Sum(cf => cf.TotalAmount);

                decimal remaining = (totalImport - totalReturn) - (totalPaid - totalRefund);
                // Loại bỏ sai số lẻ dưới 1 đồng và làm tròn theo chuẩn VNĐ
                dto.RemainingDebt = Math.Abs(remaining) < 1m ? 0m : Math.Round(remaining, 0, MidpointRounding.AwayFromZero);
                return dto;
            }).ToList();
        }
        #endregion
    }
}
