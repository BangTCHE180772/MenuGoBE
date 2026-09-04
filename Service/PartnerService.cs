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

            var cashFlowExpenses = await _context.CashFlows
                .Where(cf => cf.PartnerId == partnerId && !cf.IsDeleted && (int)cf.Direction == 2)
                .SumAsync(cf => cf.TotalAmount);

            decimal totalPaidAmount = totalPaidFromDocs + cashFlowExpenses;
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
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Import && d.Status != DocumentStatus.Cancelled)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return docs.Select(d => new PartnerImportDocumentDto
            {
                Id = d.Id,
                Code = d.Code,
                ImportDate = d.CreatedAt,
                TotalAmount = d.TotalAmount,
                AmountPaid = d.AmountPaid,
                DebtAmount = Math.Max(0m, d.TotalAmount - d.AmountPaid),
                Status = d.Status.ToString()
            }).ToList();
        }
        #endregion

        #region Lấy lịch sử dòng tiền giao dịch tài chính với Nhà cung cấp
        /// <summary>
        /// Tổng hợp lịch sử chứng từ nhập kho và phiếu chi trả nhà cung cấp.
        /// </summary>
        public async Task<List<PartnerTransactionHistoryDto>> GetPartnerCashFlowHistoryAsync(long partnerId)
        {
            var result = new List<PartnerTransactionHistoryDto>();

            var importDocs = await _context.Documents
                .Where(d => d.PartnerId == partnerId && d.Type == DocumentType.Import && d.Status != DocumentStatus.Cancelled)
                .ToListAsync();

            foreach (var doc in importDocs)
            {
                result.Add(new PartnerTransactionHistoryDto
                {
                    Id = $"DOC-{doc.Id}",
                    TransactionDate = doc.CreatedAt,
                    TransactionType = "Nhập hàng",
                    Code = doc.Code,
                    Amount = doc.TotalAmount,
                    Note = "Ghi nhận công nợ nhập kho",
                    PaymentMethod = "Ghi nợ NCC"
                });
            }

            var cashFlows = await _context.CashFlows
                .Where(cf => cf.PartnerId == partnerId && !cf.IsDeleted)
                .ToListAsync();

            foreach (var cf in cashFlows)
            {
                bool isExpense = (int)cf.Direction == 2;
                result.Add(new PartnerTransactionHistoryDto
                {
                    Id = $"CF-{cf.Id}",
                    TransactionDate = cf.BusinessDate,
                    TransactionType = isExpense ? "Thanh toán NCC" : "Thu lại NCC",
                    Code = cf.Code ?? $"CF-{cf.Id}",
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
