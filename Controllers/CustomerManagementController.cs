using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Customer;
using MenuGoBE.Dtos.Partner;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerManagementController : BaseApiController
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub>? _hubContext;

        public CustomerManagementController(AppDbContext context, IHubContext<NotificationHub>? hubContext = null)
        {
            _context = context;
            _hubContext = hubContext;
        }

        #region Helper phát thông báo SignalR cập nhật khách hàng
        private async Task NotifyCustomerUpdatedAsync(string action, long customerId)
        {
            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveCustomerUpdate", new { action, customerId });
                }
                catch
                {
                    // Tránh lỗi SignalR ảnh hưởng luồng xử lý chính
                }
            }
        }
        #endregion

        #region Lấy danh sách khách hàng (Có phân trang và tìm kiếm)
        [HttpGet]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var query = _context.Customers.AsNoTracking().AsQueryable();

            // Lọc theo từ khóa tìm kiếm (tên, số điện thoại, email)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(s) ||
                    c.Phone.Contains(s) ||
                    (c.Email != null && c.Email.ToLower().Contains(s)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerViewDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Email = c.Email,
                    Point = c.Point,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                items,
                totalCount,
                page,
                pageSize
            });
        }
        #endregion

        #region Lấy chi tiết thông tin một khách hàng
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var customer = await _context.Customers.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CustomerViewDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Email = c.Email,
                    Point = c.Point,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { message = "Không tìm thấy khách hàng yêu cầu." });
            }

            return Ok(customer);
        }
        #endregion

        #region Thêm mới khách hàng
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto dto)
        {
            if (!ValidateUserToken(out var currentUserId, out var errorResult))
            {
                return errorResult!;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Kiểm tra trùng lặp số điện thoại
            var phoneExists = await _context.Customers.AnyAsync(c => c.Phone == dto.Phone.Trim());
            if (phoneExists)
            {
                return BadRequest(new { message = "Số điện thoại này đã được đăng ký cho khách hàng khác." });
            }

            var customer = new Customer
            {
                Name = dto.Name.Trim(),
                Phone = dto.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Point = Math.Max(0, dto.InitialPoint),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            // Nếu có điểm khởi tạo lớn hơn 0, ghi nhận lịch sử giao dịch ban đầu
            if (customer.Point > 0)
            {
                long? validCreatedBy = null;
                if (currentUserId > 0 && await _context.Accounts.AnyAsync(a => a.Id == currentUserId))
                {
                    validCreatedBy = currentUserId;
                }

                var trans = new CustomerPointTransaction
                {
                    CustomerId = customer.Id,
                    Type = PointTransactionType.ManualAdjustment,
                    PointChange = customer.Point,
                    PointBefore = 0,
                    PointAfter = customer.Point,
                    Reason = "Khởi tạo điểm ban đầu khi tạo khách hàng",
                    IsManual = true,
                    CreatedBy = validCreatedBy,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.CustomerPointTransactions.AddAsync(trans);
                await _context.SaveChangesAsync();
            }

            await NotifyCustomerUpdatedAsync("create", customer.Id);

            return Ok(new CustomerViewDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                Point = customer.Point,
                CreatedAt = customer.CreatedAt
            });
        }
        #endregion

        #region Cập nhật thông tin profile khách hàng (Không sửa mật khẩu)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(long id, [FromBody] CustomerProfileUpdateDto dto)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = "Không tìm thấy khách hàng." });
            }

            // Kiểm tra trùng lặp số điện thoại với khách hàng khác
            var cleanPhone = dto.Phone.Trim();
            var phoneDuplicate = await _context.Customers.AnyAsync(c => c.Phone == cleanPhone && c.Id != id);
            if (phoneDuplicate)
            {
                return BadRequest(new { message = "Số điện thoại này đã được sử dụng bởi khách hàng khác." });
            }

            // Cập nhật thông tin cá nhân - tuyệt đối không thay đổi PasswordHash
            customer.Name = dto.Name.Trim();
            customer.Phone = cleanPhone;
            customer.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

            // Đồng bộ thông tin cập nhật sang bảng Partner (ở tất cả các chi nhánh)
            var linkedPartners = await _context.Partners.Where(p => p.CustomerId == customer.Id).ToListAsync();
            foreach (var partner in linkedPartners)
            {
                partner.Name = customer.Name;
                partner.Phone = customer.Phone;
                if (customer.Email != null)
                {
                    partner.Email = customer.Email;
                }
            }

            await _context.SaveChangesAsync();

            await NotifyCustomerUpdatedAsync("update", customer.Id);

            return Ok(new CustomerViewDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                Point = customer.Point,
                CreatedAt = customer.CreatedAt
            });
        }
        #endregion

        #region Xóa khách hàng
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = "Không tìm thấy khách hàng." });
            }

            // Kiểm tra nếu khách hàng đã có đơn hàng thì không xóa cứng
            var hasOrders = await _context.Orders.AnyAsync(o => o.CustomerId == id);
            if (hasOrders)
            {
                return BadRequest(new { message = "Không thể xóa khách hàng đã có lịch sử đơn hàng trong hệ thống." });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await NotifyCustomerUpdatedAsync("delete", id);

            return Ok(new { success = true, message = "Xóa khách hàng thành công." });
        }
        #endregion

        #region Lấy lịch sử mua hàng của khách hàng
        [HttpGet("{id}/purchases")]
        public async Task<IActionResult> GetCustomerPurchases(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            // Lấy danh sách đơn hàng đã thanh toán hoặc hoàn thành của khách
            var orders = await _context.Orders
                .Where(o => o.CustomerId == id && o.Status != "Cancelled")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orderIds = orders.Select(o => o.Id).ToList();

            // Lấy các giao dịch tích điểm thực tế tương ứng với từng hóa đơn
            var earnPointTransactions = await _context.CustomerPointTransactions
                .Where(t => t.CustomerId == id
                         && t.OrderId.HasValue
                         && orderIds.Contains(t.OrderId.Value)
                         && t.Type == PointTransactionType.Earn
                         && !t.IsReversed)
                .ToListAsync();

            var result = orders.Select(o =>
            {
                decimal finalAmt = Math.Max(0m, o.TotalAmount - o.DiscountAmount);

                // Lấy điểm nhận thực tế từ lịch sử biến động điểm đã ghi nhận cho hóa đơn này
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

            return Ok(result);
        }
        #endregion

        #region Lấy lịch sử biến động điểm tích lũy của khách hàng
        [HttpGet("{id}/points")]
        public async Task<IActionResult> GetCustomerPoints(long id)
        {
            if (!ValidateUserToken(out _, out var errorResult))
            {
                return errorResult!;
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = "Không tìm thấy khách hàng." });
            }

            // 1. Lấy tất cả giao dịch điểm đã lưu trong bảng CustomerPointTransactions
            var savedTransactions = await _context.CustomerPointTransactions
                .Where(t => t.CustomerId == id)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new CustomerPointTransactionDto
                {
                    Id = t.Id,
                    Time = t.CreatedAt,
                    Type = t.Type == PointTransactionType.Earn ? "Tích điểm"
                         : t.Type == PointTransactionType.Redeem ? "Sử dụng điểm"
                         : t.Type == PointTransactionType.Reversal ? "Hoàn tác điều chỉnh"
                         : "Điều chỉnh thủ công",
                    TypeEnum = t.Type,
                    RelatedCode = t.OrderId.HasValue ? $"HD{t.OrderId.Value}" : t.RelatedCode,
                    OrderId = t.OrderId,
                    PointChange = t.PointChange,
                    PointBefore = t.PointBefore,
                    PointAfter = t.PointAfter,
                    Reason = t.Reason,
                    IsManual = t.IsManual,
                    IsReversed = t.IsReversed
                })
                .ToListAsync();

            // 2. Để tương thích ngược với dữ liệu cũ: nếu có Order quá khứ chưa được ghi vào bảng ledger, ta quét bổ sung
            var existingOrderIds = savedTransactions
                .Where(t => t.OrderId.HasValue)
                .Select(t => t.OrderId!.Value)
                .ToHashSet();

            var historicalOrders = await _context.Orders
                .Where(o => o.CustomerId == id && (o.Status == "Paid" || o.Status == "Completed"))
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            var combinedList = new List<CustomerPointTransactionDto>(savedTransactions);

            // Bổ sung các đơn hàng lịch sử chưa có trong CustomerPointTransactions
            int syntheticRunningPoint = 0;
            foreach (var ord in historicalOrders)
            {
                if (existingOrderIds.Contains(ord.Id)) continue;

                string code = $"HD{ord.Id}";
                decimal finalAmt = Math.Max(0m, ord.TotalAmount - ord.DiscountAmount);

                if (ord.PointsUsed > 0)
                {
                    int before = syntheticRunningPoint;
                    syntheticRunningPoint = Math.Max(0, syntheticRunningPoint - ord.PointsUsed);
                    combinedList.Add(new CustomerPointTransactionDto
                    {
                        Id = ord.Id * 1000 + 1,
                        Time = ord.CreatedAt,
                        Type = "Sử dụng điểm",
                        TypeEnum = PointTransactionType.Redeem,
                        RelatedCode = code,
                        OrderId = ord.Id,
                        PointChange = -ord.PointsUsed,
                        PointBefore = before,
                        PointAfter = syntheticRunningPoint,
                        Reason = $"Thanh toán hóa đơn {code}",
                        IsManual = false,
                        IsReversed = false
                    });
                }

                int earned = (int)Math.Floor(finalAmt / 10000m);
                if (earned > 0)
                {
                    int before = syntheticRunningPoint;
                    syntheticRunningPoint += earned;
                    combinedList.Add(new CustomerPointTransactionDto
                    {
                        Id = ord.Id * 1000 + 2,
                        Time = ord.CreatedAt,
                        Type = "Tích điểm",
                        TypeEnum = PointTransactionType.Earn,
                        RelatedCode = code,
                        OrderId = ord.Id,
                        PointChange = earned,
                        PointBefore = before,
                        PointAfter = syntheticRunningPoint,
                        Reason = $"Tích điểm đơn hàng {code}",
                        IsManual = false,
                        IsReversed = false
                    });
                }
            }

            // Sắp xếp thời gian mới nhất lên đầu
            var finalResult = combinedList.OrderByDescending(t => t.Time).ToList();
            return Ok(finalResult);
        }
        #endregion

        #region Điều chỉnh điểm tích lũy thủ công
        [HttpPost("{id}/adjust-points")]
        public async Task<IActionResult> AdjustCustomerPoints(long id, [FromBody] PointAdjustmentDto dto)
        {
            if (!ValidateUserToken(out var currentUserId, out var errorResult))
            {
                return errorResult!;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = "Không tìm thấy khách hàng." });
            }

            // Tính toán chênh lệch điểm
            int pointBefore = customer.Point;
            int pointAfter = dto.NewPoints;
            int delta = pointAfter - pointBefore;

            if (delta == 0)
            {
                return BadRequest(new { message = "Số điểm mới trùng khớp với số điểm hiện tại. Không có thay đổi." });
            }

            try
            {
                // Cập nhật số dư điểm của khách hàng
                customer.Point = pointAfter;

                long? validCreatedBy = null;
                if (currentUserId > 0 && await _context.Accounts.AnyAsync(a => a.Id == currentUserId))
                {
                    validCreatedBy = currentUserId;
                }

                // Ghi nhận bản ghi lịch sử điều chỉnh thủ công
                var trans = new CustomerPointTransaction
                {
                    CustomerId = customer.Id,
                    Type = PointTransactionType.ManualAdjustment,
                    PointChange = delta,
                    PointBefore = pointBefore,
                    PointAfter = pointAfter,
                    Reason = dto.Reason.Trim(),
                    IsManual = true,
                    IsReversed = false,
                    CreatedBy = validCreatedBy,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.CustomerPointTransactions.AddAsync(trans);
                await _context.SaveChangesAsync();

                await NotifyCustomerUpdatedAsync("point_change", customer.Id);

                return Ok(new
                {
                    success = true,
                    message = $"Đã điều chỉnh điểm của khách hàng thành {pointAfter} điểm.",
                    currentPoint = customer.Point,
                    transaction = new CustomerPointTransactionDto
                    {
                        Id = trans.Id,
                        Time = trans.CreatedAt,
                        Type = "Điều chỉnh thủ công",
                        TypeEnum = PointTransactionType.ManualAdjustment,
                        RelatedCode = null,
                        PointChange = delta,
                        PointBefore = pointBefore,
                        PointAfter = pointAfter,
                        Reason = trans.Reason,
                        IsManual = true,
                        IsReversed = false
                    }
                });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { message = $"Lỗi khi lưu điều chỉnh điểm: {innerMsg}" });
            }
        }
        #endregion

        #region Chỉnh sửa giao dịch điểm thủ công
        [HttpPut("point-transactions/{transactionId}")]
        public async Task<IActionResult> EditManualPointTransaction(long transactionId, [FromBody] PointTransactionEditDto dto)
        {
            if (!ValidateUserToken(out var currentUserId, out var errorResult))
            {
                return errorResult!;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var trans = await _context.CustomerPointTransactions.FindAsync(transactionId);
            if (trans == null)
            {
                return NotFound(new { message = "Không tìm thấy giao dịch điểm yêu cầu." });
            }

            if (!trans.IsManual)
            {
                return BadRequest(new { message = "Giao dịch phát sinh từ hóa đơn không được phép chỉnh sửa trực tiếp." });
            }

            if (trans.IsReversed)
            {
                return BadRequest(new { message = "Giao dịch này đã bị hoàn tác, không thể chỉnh sửa." });
            }

            var customer = await _context.Customers.FindAsync(trans.CustomerId);
            if (customer == null)
            {
                return NotFound(new { message = "Khách hàng không tồn tại." });
            }

            // Tính toán mức chênh lệch mới
            int oldDelta = trans.PointChange;
            int newTargetAfter = dto.NewPoints;
            int newDelta = newTargetAfter - trans.PointBefore;
            int adjustmentDiff = newDelta - oldDelta;

            // Cập nhật điểm hiện tại của khách hàng theo phần chênh lệch điều chỉnh
            customer.Point = Math.Max(0, customer.Point + adjustmentDiff);

            // Cập nhật bản ghi giao dịch
            trans.PointChange = newDelta;
            trans.PointAfter = newTargetAfter;
            trans.Reason = dto.Reason.Trim();
            trans.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await NotifyCustomerUpdatedAsync("point_change", customer.Id);

            return Ok(new
            {
                success = true,
                message = "Cập nhật giao dịch điều chỉnh điểm thành công.",
                currentPoint = customer.Point
            });
        }
        #endregion

        #region Xóa (hoàn tác) giao dịch điểm thủ công
        [HttpDelete("point-transactions/{transactionId}")]
        public async Task<IActionResult> DeleteManualPointTransaction(long transactionId, [FromQuery] string? reason)
        {
            if (!ValidateUserToken(out var currentUserId, out var errorResult))
            {
                return errorResult!;
            }

            var trans = await _context.CustomerPointTransactions.FindAsync(transactionId);
            if (trans == null)
            {
                return NotFound(new { message = "Không tìm thấy giao dịch điểm." });
            }

            if (!trans.IsManual)
            {
                return BadRequest(new { message = "Giao dịch phát sinh từ hóa đơn không được phép xóa trực tiếp." });
            }

            if (trans.IsReversed)
            {
                return BadRequest(new { message = "Giao dịch này đã được hoàn tác trước đó." });
            }

            var customer = await _context.Customers.FindAsync(trans.CustomerId);
            if (customer == null)
            {
                return NotFound(new { message = "Khách hàng không tồn tại." });
            }

            try
            {
                // Đánh dấu giao dịch cũ đã bị hoàn tác
                trans.IsReversed = true;
                trans.UpdatedAt = DateTime.UtcNow;

                // Hoàn lại điểm cho khách hàng (trừ đi phần đã cộng hoặc cộng lại phần đã trừ)
                int pointBefore = customer.Point;
                int reversalDelta = -trans.PointChange;
                int pointAfter = Math.Max(0, customer.Point + reversalDelta);
                customer.Point = pointAfter;

                long? validCreatedBy = null;
                if (currentUserId > 0 && await _context.Accounts.AnyAsync(a => a.Id == currentUserId))
                {
                    validCreatedBy = currentUserId;
                }

                // Tạo bản ghi hoàn tác mới (Reversal) để lưu vết kiểm toán đầy đủ
                var reversalTrans = new CustomerPointTransaction
                {
                    CustomerId = customer.Id,
                    Type = PointTransactionType.Reversal,
                    PointChange = reversalDelta,
                    PointBefore = pointBefore,
                    PointAfter = pointAfter,
                    Reason = !string.IsNullOrWhiteSpace(reason) 
                        ? reason.Trim() 
                        : $"Hoàn tác giao dịch điều chỉnh #{trans.Id} ({trans.Reason})",
                    IsManual = true,
                    IsReversed = false,
                    ReversedTransactionId = trans.Id,
                    CreatedBy = validCreatedBy,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.CustomerPointTransactions.AddAsync(reversalTrans);
                await _context.SaveChangesAsync();

                await NotifyCustomerUpdatedAsync("point_change", customer.Id);

                return Ok(new
                {
                    success = true,
                    message = "Đã hoàn tác giao dịch điều chỉnh điểm thành công.",
                    currentPoint = customer.Point
                });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest(new { message = $"Lỗi khi hoàn tác giao dịch: {innerMsg}" });
            }
        }
        #endregion
    }
}
