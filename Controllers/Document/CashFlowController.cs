using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Data;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Controllers.Document
{
    /// <summary>
    /// Controller quản lý Sổ Thu Chi (CashFlow) của chi nhánh.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CashFlowController : BaseApiController
    {
        private readonly AppDbContext _context;

        public CashFlowController(AppDbContext context)
        {
            _context = context;
        }

        #region GET api/cashflow?branchId={branchId} — Lấy danh sách thu chi
        /// <summary>
        /// Lấy danh sách phiếu thu chi của chi nhánh (chưa bị xóa mềm), sắp xếp theo BusinessDate giảm dần.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCashFlows([FromQuery] long branchId)
        {
            if (!ValidateUserAndBranchToken(branchId, out _, out var errorResult))
                return errorResult!;

            // Fetch raw entities first (avoid SQL cast of string-stored enum to int)
            var raw = await _context.CashFlows
                .AsNoTracking()
                .Where(c => c.BranchId == branchId && !c.IsDeleted)
                .Include(c => c.Partner)
                .Include(c => c.Document)
                .OrderByDescending(c => c.BusinessDate)
                .ThenByDescending(c => c.PostingSequence)
                .ToListAsync();

            // Project in-memory to avoid EF translating (int)enum as ::int on string column in PostgreSQL
            var list = raw.Select(c => new
            {
                c.Id,
                c.Code,
                c.BranchId,
                c.DocumentId,
                DocumentCode = c.Document?.Code,
                c.PartnerId,
                PartnerName = c.Partner?.Name,
                Direction = c.Direction.ToString(),
                DirectionValue = (int)c.Direction,
                Type = c.Type.ToString(),
                TypeValue = (int)c.Type,
                PaymentMethod = c.PaymentMethod.ToString(),
                Status = c.Status.ToString(),
                StatusValue = (int)c.Status,
                c.TotalAmount,
                c.Note,
                c.BusinessDate,
                c.CreatedAt,
                c.IsDeleted,
            }).ToList();

            return Ok(list);
        }
        #endregion

        #region GET api/cashflow/{id} — Lấy chi tiết 1 phiếu
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
                return errorResult!;

            // Fetch raw entity first to avoid SQL cast error on string-stored enum
            var c = await _context.CashFlows
                .AsNoTracking()
                .Where(cf => cf.Id == id && !cf.IsDeleted)
                .Include(cf => cf.Partner)
                .Include(cf => cf.Document)
                .FirstOrDefaultAsync();

            if (c == null)
                return NotFound(new { message = $"Không tìm thấy phiếu thu chi ID: {id}" });

            return Ok(new
            {
                c.Id,
                c.Code,
                c.BranchId,
                c.DocumentId,
                DocumentCode = c.Document?.Code,
                c.PartnerId,
                PartnerName = c.Partner?.Name,
                Direction = c.Direction.ToString(),
                DirectionValue = (int)c.Direction,
                Type = c.Type.ToString(),
                TypeValue = (int)c.Type,
                PaymentMethod = c.PaymentMethod.ToString(),
                Status = c.Status.ToString(),
                StatusValue = (int)c.Status,
                c.TotalAmount,
                c.Note,
                c.BusinessDate,
                c.CreatedAt,
            });
        }
        #endregion

        #region POST api/cashflow — Tạo phiếu thu hoặc chi thủ công
        /// <summary>
        /// Tạo phiếu thu (Inflow) hoặc phiếu chi (Outflow) thủ công.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCashFlowDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Thông tin phiếu không được để trống." });

            if (!ValidateUserAndBranchToken(dto.BranchId, out long userId, out var errorResult))
                return errorResult!;

            if (dto.TotalAmount <= 0)
                return BadRequest(new { message = "Số tiền phải lớn hơn 0." });

            if (dto.TotalAmount > 10_000_000_000m)
                return BadRequest(new { message = "Số tiền không được vượt quá 10 tỷ VNĐ." });

            // Tự động sinh mã phiếu
            var prefix = dto.Direction == 1 ? "PT" : "PC"; // PT = Phiếu Thu, PC = Phiếu Chi
            var tempCode = $"{prefix}{DateTime.UtcNow.Ticks % 1_000_000:D6}";

            var cashFlow = new CashFlow
            {
                BranchId = dto.BranchId,
                Code = tempCode,
                BusinessDate = dto.BusinessDate == default ? DateTime.UtcNow : dto.BusinessDate,
                Direction = (CashFlowDirection)dto.Direction,
                Type = CashFlowDetailType.Payment,
                PaymentMethod = (PaymentMethod)(dto.PaymentMethod > 0 ? dto.PaymentMethod : 1),
                Status = CashFlowStatus.Completed,
                TotalAmount = dto.TotalAmount,
                PartnerId = dto.PartnerId > 0 ? dto.PartnerId : null,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.CashFlows.Add(cashFlow);
            await _context.SaveChangesAsync();

            // Cập nhật code sau khi có Id
            cashFlow.Code = $"{prefix}{cashFlow.Id:D6}";
            _context.CashFlows.Update(cashFlow);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, id = cashFlow.Id, code = cashFlow.Code, message = $"Tạo {(dto.Direction == 1 ? "phiếu thu" : "phiếu chi")} thành công." });
        }
        #endregion

        #region DELETE api/cashflow/{id}/soft-delete — Xóa mềm phiếu
        [HttpDelete("{id:long}/soft-delete")]
        public async Task<IActionResult> SoftDelete(long id, [FromQuery] string? deleteNote)
        {
            if (!ValidateUserToken(out long userId, out var errorResult))
                return errorResult!;

            var item = await _context.CashFlows.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (item == null)
                return NotFound(new { message = $"Không tìm thấy phiếu thu chi ID: {id}" });

            if (item.DocumentId.HasValue)
            {
                return BadRequest(new { message = "Không thể hủy/xóa phiếu thu chi được sinh ra tự động từ chứng từ gốc (Nhập hàng, Trả hàng NCC, Bán hàng). Hãy hủy hoặc điều chỉnh chứng từ gốc." });
            }

            item.IsDeleted = true;
            item.DeletedBy = userId;
            item.DeletedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(deleteNote))
                item.Note = string.IsNullOrWhiteSpace(item.Note) ? $"[HỦY]: {deleteNote}" : $"{item.Note} | [HỦY]: {deleteNote}";

            _context.CashFlows.Update(item);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã hủy phiếu thu chi thành công." });
        }
        #endregion
    }

    /// <summary>DTO tạo phiếu thu chi thủ công.</summary>
    public class CreateCashFlowDto
    {
        public long BranchId { get; set; }
        /// <summary>1 = Thu (Inflow), 2 = Chi (Outflow)</summary>
        public int Direction { get; set; }
        public decimal TotalAmount { get; set; }
        public long? PartnerId { get; set; }
        /// <summary>1 = Cash, 2 = Card, 3 = Transfer</summary>
        public int PaymentMethod { get; set; } = 1;
        public DateTime BusinessDate { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
    }
}
