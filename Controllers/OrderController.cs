using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _service;
        private readonly IOrderDetailService _detailService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ITableService _tableService;
        private readonly IProductService _productService;
        private readonly IOrderAssignmentService _assignmentService;
        private readonly IAccountService _accountService;

        public OrderController(
            IOrderService service,
            IOrderDetailService detailService,
            IHubContext<NotificationHub> hubContext,
            ITableService tableService,
            IProductService productService,
            IOrderAssignmentService assignmentService,
            IAccountService accountService)
        {
            _service = service;
            _detailService = detailService;
            _hubContext = hubContext;
            _tableService = tableService;
            _productService = productService;
            _assignmentService = assignmentService;
            _accountService = accountService;
        }

        /// <summary>
        /// Gửi notification cho assigned waiter hoặc on-duty staff.
        /// Manager LUÔN nhận qua branch group.
        /// </summary>
        private async Task SendOrderNotificationAsync(long orderId, long branchId, object payload)
        {
            var assignedAccountId = await _assignmentService.GetAssignedAccountIdAsync(orderId);

            if (assignedAccountId.HasValue)
            {
                // Targeted: chỉ assigned waiter nhận
                await _hubContext.Clients.Group($"waiter_{assignedAccountId.Value}")
                    .SendAsync("ReceiveNotification", payload);
            }
            else
            {
                // Chưa assign → gửi cho on-duty staff (Waiter + Cashier đang làm)
                await _hubContext.Clients.Group($"on_duty_{branchId}")
                    .SendAsync("ReceiveNotification", payload);
            }

            // Manager/Admin LUÔN nhận (họ join branch group khi login)
            await _hubContext.Clients.Group($"branch_{branchId}")
                .SendAsync("ReceiveNotification", payload);
        }

        private long? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (claim != null && long.TryParse(claim.Value, out var id)) return id;
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] long? branchId)
        {
            var result = await _service.GetAllAsync();

            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                bool isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner") || 
                                      User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                                      User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");
                
                if (!isAdminOrOwner)
                {
                    var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId")).Select(c => c.Value).ToList();
                    var branchIds = branchClaims.Select(b => long.TryParse(b, out var bid) ? bid : 0L).Where(id => id > 0).ToList();
                    
                    if (branchIds.Any())
                    {
                        var filteredResult = result.Where(x => x.BranchId.HasValue && branchIds.Contains(x.BranchId.Value)).ToList();
                        return Ok(filteredResult);
                    }
                }
                else if (branchId.HasValue && branchId.Value > 0)
                {
                    // If Admin/Owner explicitly requests a specific branch
                    var filteredResult = result.Where(x => x.BranchId == branchId.Value).ToList();
                    return Ok(filteredResult);
                }
            }

            return Ok(result);
        }

        [HttpGet("returnable")]
        public async Task<IActionResult> GetReturnableOrders([FromQuery] long branchId)
        {
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                bool isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner") || 
                                      User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                                      User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");
                
                if (!isAdminOrOwner)
                {
                    var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId")).Select(c => c.Value).ToList();
                    var branchIds = branchClaims.Select(b => long.TryParse(b, out var bid) ? bid : 0L).Where(id => id > 0).ToList();
                    
                    if (!branchIds.Contains(branchId))
                    {
                        return Forbid();
                    }
                }
            }

            var result = await _service.GetReturnableOrdersByBranchAsync(branchId);
            return Ok(result);
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices([FromQuery] long branchId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
            {
                bool isAdminOrOwner = User.IsInRole("Admin") || User.IsInRole("Owner") || 
                                      User.HasClaim(ClaimTypes.Role, "Admin") || User.HasClaim(ClaimTypes.Role, "Owner") ||
                                      User.HasClaim("role", "Admin") || User.HasClaim("role", "Owner");
                
                if (!isAdminOrOwner)
                {
                    var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId")).Select(c => c.Value).ToList();
                    var userBranchIds = branchClaims.Select(b => long.TryParse(b, out var bid) ? bid : 0L).Where(id => id > 0).ToList();
                    
                    if (userBranchIds.Any() && !userBranchIds.Contains(branchId))
                    {
                        return Forbid();
                    }
                }
            }

            var result = await _service.GetPaidOrdersByBranchAsync(branchId, startDate, endDate);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy Order" });

            return Ok(result);
        }

        [HttpPost("available-quantities/{branchId}")]
        public async Task<IActionResult> GetAvailableQuantities(long branchId, [FromBody] List<long> productIds)
        {
            try
            {
                var result = await _detailService.GetAvailableQuantitiesAsync(branchId, productIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("menu-available-quantities/{branchId}")]
        public async Task<IActionResult> GetBulkMenuAvailableQuantities(long branchId)
        {
            try
            {
                var result = await _detailService.GetBulkMenuAvailableQuantitiesAsync(branchId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [EnableRateLimiting("CustomerOrderPolicy")]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                dto.CreatedBy = currentUserId.Value;
            }
            
            var result = await _service.CreateAsync(dto);

            // Auto-assign nếu người tạo là staff (có CreatedBy)
            if (currentUserId.HasValue && result != null)
            {
                var table = await _tableService.GetByIdAsync(result.TableId);
                if (table != null)
                {
                    await _assignmentService.TryAssignAsync(result.Id, currentUserId.Value, table.BranchId);
                }
            }

            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] OrderUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Order để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy Order để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }

        [HttpPost("{id}/pay")]
        public async Task<IActionResult> Pay(long id, [FromBody] OrderPayDto? dto)
        {
            var currentUserId = GetCurrentUserId();

            var paidTableIds = await _service.PayOrderAsync(id, dto, "Cash", currentUserId);

            if (paidTableIds == null || !paidTableIds.Any())
                return BadRequest(new { message = "Không thể thanh toán. Hoá đơn không tồn tại hoặc không ở trạng thái Active." });

            // Clear assignment khi thanh toán
            await _assignmentService.DeactivateAssignmentAsync(id, "Paid");

            foreach (var tableId in paidTableIds)
            {
                // Thanh toán: broadcast cho on-duty + branch
                var payload = new
                {
                    TableId = tableId,
                    Type = "Payment",
                    Message = "Thanh toán thành công. Bàn cần dọn dẹp!"
                };
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", payload);
            }

            return Ok(new { message = "Thanh toán thành công" });
        }

        [HttpPost("merge")]
        public async Task<IActionResult> Merge([FromBody] OrderMergeDto dto)
        {
            var result = await _service.MergeAsync(dto);

            if (!result)
                return BadRequest(new { message = "Không thể gộp đơn. Kiểm tra lại OrderId hoặc đơn cha đã bị gộp vào đơn khác." });

            // Xử lý assignment khi merge: xoá assignment bàn con, giữ bàn cha
            await _assignmentService.TransferOnMergeAsync(dto.ChildOrderId, dto.FatherOrderId);

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                Type = "Payment", 
                Message = "Gộp bàn thành công!"
            });

            return Ok(new { message = "Gộp đơn thành công" });
        }

        [HttpDelete("{id}/unmerge")]
        public async Task<IActionResult> Unmerge(long id)
        {
            var result = await _service.UnmergeAsync(id);

            if (!result)
                return BadRequest(new { message = "Không thể tách đơn. Đơn này chưa được gộp." });

            return Ok(new { message = "Tách đơn thành công" });
        }

        [HttpPost("{orderId}/move")]
        public async Task<IActionResult> MoveOrder(long orderId, [FromBody] OrderMoveDto dto)
        {
            var result = await _service.MoveTableAsync(orderId, dto.NewTableId);

            if (!result)
                return BadRequest(new { message = "Không thể chuyển bàn. Vui lòng kiểm tra trạng thái bàn mới và đơn hàng." });

            return Ok(new { message = "Chuyển bàn thành công" });
        }

        [HttpGet("{fatherId}/children")]
        public async Task<IActionResult> GetChildren(long fatherId)
        {
            var result = await _service.GetChildOrdersAsync(fatherId);
            return Ok(result);
        }

        [HttpGet("{fatherId}/merged-summary")]
        public async Task<IActionResult> GetMergedSummary(long fatherId)
        {
            var result = await _service.GetMergedSummaryAsync(fatherId);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng" });

            return Ok(result);
        }

        [HttpGet("{orderId}/details")]
        public async Task<IActionResult> GetDetails(long orderId)
        {
            var result = await _detailService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpGet("{orderId}/details/{id}")]
        public async Task<IActionResult> GetDetailById(long orderId, long id)
        {
            var result = await _detailService.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy OrderDetail" });

            return Ok(result);
        }

        [HttpPost("{orderId}/details")]
        [EnableRateLimiting("CustomerOrderPolicy")]
        public async Task<IActionResult> CreateDetail(long orderId, [FromBody] OrderDetailCreateDto dto)
        {
            dto.OrderId = orderId;

            try
            {
                var result = await _detailService.CreateAsync(dto);

                var order = await _service.GetByIdAsync(orderId);
                if (order != null)
                {
                    var table = await _tableService.GetByIdAsync(order.TableId);
                    if (table != null)
                    {
                        var product = await _productService.GetByIdAsync(dto.ProductId);
                        string productName = product?.Name ?? "Món mới";

                        var payload = new 
                        { 
                            type = dto.Status == "CustomerPending" ? "new-order" : "new-item", 
                            branchId = table.BranchId,
                            tableId = table.Id, 
                            tableName = table.Name,
                            productName = productName,
                            quantity = dto.Quantity,
                            message = $"{table.Name} vừa gọi: {productName} (x{dto.Quantity})",
                            timestamp = System.DateTime.UtcNow
                        };

                        // Mới: thử assign cho user hiện tại (nếu là Waiter)
                        var currentUserId = GetCurrentUserId();
                        bool isAssignedToUser = false;
                        if (currentUserId.HasValue)
                        {
                            isAssignedToUser = await _assignmentService.TryAssignAsync(orderId, currentUserId.Value, table.BranchId);
                            if (isAssignedToUser)
                            {
                                await _assignmentService.TouchAsync(orderId);
                            }
                            else
                            {
                                // Có thể bàn đã được assign rồi, kiểm tra xem có phải gán cho chính mình không để Touch
                                var existingAccountId = await _assignmentService.GetAssignedAccountIdAsync(orderId);
                                if (existingAccountId == currentUserId.Value)
                                {
                                    await _assignmentService.TouchAsync(orderId);
                                    isAssignedToUser = true; // Coi như đã được gán cho user hiện tại
                                }
                                else if (existingAccountId.HasValue)
                                {
                                    await _assignmentService.RecordSupportAsync(orderId, currentUserId.Value, table.BranchId);
                                }
                            }
                        }
                        
                        if (!isAssignedToUser)
                        {
                            // Nếu không thể gán cho người hiện tại (vd: Manager) hoặc khách tự order
                            await _assignmentService.AutoAssignWaiterAsync(orderId, table.Id, table.BranchId);
                        }

                        await SendOrderNotificationAsync(orderId, table.BranchId, payload);
                    }
                }

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{orderId}/details/bulk")]
        [EnableRateLimiting("CustomerOrderPolicy")]
        public async Task<IActionResult> CreateBulkDetail(long orderId, [FromBody] List<OrderDetailCreateDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest(new { message = "Không có món nào được chọn." });

            foreach (var dto in dtos)
            {
                dto.OrderId = orderId;
            }

            try
            {
                var result = await _detailService.CreateMultipleAsync(dtos);
                
                if (!result.Success)
                {
                    return BadRequest(new { message = "Một số món không đủ số lượng.", errors = result.Errors });
                }

                var order = await _service.GetByIdAsync(orderId);
                if (order != null)
                {
                    var table = await _tableService.GetByIdAsync(order.TableId);
                    if (table != null)
                    {
                        var status = dtos.First().Status;
                        string productsSummary = string.Join(", ", result.Details.Select(d => $"{d.ProductName} (x{d.Quantity})"));
                        var payload = new 
                        { 
                            type = status == "CustomerPending" ? "new-order" : "new-item", 
                            branchId = table.BranchId,
                            tableId = table.Id, 
                            tableName = table.Name,
                            productName = "Nhiều món",
                            quantity = dtos.Sum(d => d.Quantity),
                            message = $"{table.Name} vừa gọi: {productsSummary}",
                            timestamp = System.DateTime.UtcNow
                        };

                        // Mới: thử assign cho user hiện tại (nếu là Waiter)
                        var currentUserId = GetCurrentUserId();
                        bool isAssignedToUser = false;
                        if (currentUserId.HasValue)
                        {
                            isAssignedToUser = await _assignmentService.TryAssignAsync(orderId, currentUserId.Value, table.BranchId);
                            if (isAssignedToUser)
                            {
                                await _assignmentService.TouchAsync(orderId);
                            }
                            else
                            {
                                // Có thể bàn đã được assign rồi, kiểm tra xem có phải gán cho chính mình không để Touch
                                var existingAccountId = await _assignmentService.GetAssignedAccountIdAsync(orderId);
                                if (existingAccountId == currentUserId.Value)
                                {
                                    await _assignmentService.TouchAsync(orderId);
                                    isAssignedToUser = true;
                                }
                                else if (existingAccountId.HasValue)
                                {
                                    await _assignmentService.RecordSupportAsync(orderId, currentUserId.Value, table.BranchId);
                                }
                            }
                        }
                        
                        if (!isAssignedToUser)
                        {
                            // Nếu không thể gán cho người hiện tại (vd: Manager) hoặc khách tự order
                            await _assignmentService.AutoAssignWaiterAsync(orderId, table.Id, table.BranchId);
                        }

                        await SendOrderNotificationAsync(orderId, table.BranchId, payload);
                    }
                }

                return Ok(result.Details);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{orderId}/details/{id}")]
        [EnableRateLimiting("CustomerOrderPolicy")]
        public async Task<IActionResult> UpdateDetail(long orderId, long id, [FromBody] OrderDetailUpdateDto dto)
        {
            dto.Id = id;

            var result = await _detailService.UpdateAsync(dto);

            if (!result)
                return NotFound(new { message = "Không tìm thấy OrderDetail để cập nhật" });

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpDelete("{orderId}/details/{id}")]
        public async Task<IActionResult> DeleteDetail(long orderId, long id)
        {
            var result = await _detailService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Không tìm thấy OrderDetail để xóa" });

            return Ok(new { message = "Xóa thành công" });
        }

        [HttpGet("by-table/{tableId}")]
        public async Task<IActionResult> GetTableOrderSummary(long tableId)
        {
            var summary = await _service.GetTableOrderSummaryAsync(tableId);
            return Ok(summary);
        }

        [HttpPut("details/{id}/confirm")]
        public async Task<IActionResult> ConfirmOrderDetail(long id, [FromBody] ConfirmRequestDto? dto = null)
        {
            var quantity = dto?.Quantity;
            var result = await _detailService.ConfirmAsync(id, quantity);
            if (!result)
                return NotFound(new { message = "Không tìm thấy chi tiết đơn hoặc cập nhật thất bại" });
            
            try
            {
                var detail = await _detailService.GetByIdAsync(id);
                if (detail != null)
                {
                    var order = await _service.GetByIdAsync(detail.OrderId);
                    if (order != null)
                    {
                        var table = await _tableService.GetByIdAsync(order.TableId);
                        if (table != null)
                        {
                            var product = await _productService.GetByIdAsync(detail.ProductId);
                            string productName = product?.Name ?? "Món mới";

                            var payload = new
                            {
                                type = "new-item",
                                branchId = table.BranchId,
                                orderDetailId = id,
                                orderId = order.Id,
                                tableId = table.Id,
                                tableName = table.Name,
                                productName = productName,
                                quantity = detail.Quantity,
                                message = $"{table.Name}: Xác nhận món {productName} (x{detail.Quantity})",
                                timestamp = System.DateTime.UtcNow
                            };

                            // Thử assign cho user hiện tại (nếu là Waiter)
                            var currentUserId = GetCurrentUserId();
                            bool isAssignedToUser = false;
                            if (currentUserId.HasValue)
                            {
                                isAssignedToUser = await _assignmentService.TryAssignAsync(order.Id, currentUserId.Value, table.BranchId);
                                if (isAssignedToUser)
                                {
                                    await _assignmentService.TouchAsync(order.Id);
                                }
                                else
                                {
                                    var existingAccountId = await _assignmentService.GetAssignedAccountIdAsync(order.Id);
                                    if (existingAccountId == currentUserId.Value)
                                    {
                                        await _assignmentService.TouchAsync(order.Id);
                                        isAssignedToUser = true;
                                    }
                                    else if (existingAccountId.HasValue)
                                    {
                                        await _assignmentService.RecordSupportAsync(order.Id, currentUserId.Value, table.BranchId);
                                    }
                                }
                            }

                            if (!isAssignedToUser)
                            {
                                await _assignmentService.AutoAssignWaiterAsync(order.Id, table.Id, table.BranchId);
                            }

                            await SendOrderNotificationAsync(order.Id, table.BranchId, payload);
                        }
                    }
                }
            }
            catch
            {
                // Non-blocking SignalR fail-safe
            }

            return Ok(new { message = "Xác nhận món thành công" });
        }

        [HttpPut("details/{id}/reject")]
        public async Task<IActionResult> RejectOrderDetail(long id, [FromBody] RejectRequestDto dto)
        {
            try
            {
                var result = await _detailService.RejectAsync(id, dto.Reason);
                if (!result)
                    return NotFound(new { message = "Không tìm thấy chi tiết đơn hoặc từ chối thất bại" });

                var detail = await _detailService.GetByIdAsync(id);
                if (detail != null)
                {
                    var order = await _service.GetByIdAsync(detail.OrderId);
                    if (order != null)
                    {
                        var table = await _tableService.GetByIdAsync(order.TableId);
                        if (table != null)
                        {
                            var product = await _productService.GetByIdAsync(detail.ProductId);
                            string productName = product?.Name ?? "Món";

                            var payload = new
                            {
                                type = "item-rejected",
                                branchId = table.BranchId,
                                orderDetailId = id,
                                orderId = order.Id,
                                tableId = table.Id,
                                tableName = table.Name,
                                productName = productName,
                                reason = dto.Reason,
                                message = $"{table.Name}: Từ chối món {productName} (Lý do: {dto.Reason})",
                                timestamp = System.DateTime.UtcNow
                            };

                            await _assignmentService.TouchAsync(order.Id);
                            await SendOrderNotificationAsync(order.Id, table.BranchId, payload);
                        }
                    }
                }

                return Ok(new { message = "Từ chối món thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("details/{id}/request-return")]
        public async Task<IActionResult> RequestReturn(long id, [FromBody] ReturnItemRequestDto dto)
        {
            dto.OrderDetailId = id; // Đảm bảo ID khớp với URL
            long confirmedByUserId = 1; // Fallback
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (claim != null && long.TryParse(claim.Value, out var parsedId))
            {
                confirmedByUserId = parsedId;
            }

            try
            {
                var result = await _detailService.RequestReturnItemAsync(dto, confirmedByUserId);
                if (!result)
                    return BadRequest(new { message = "Không thể yêu cầu trả món (Kiểm tra lại số lượng hoặc trạng thái)" });

                return Ok(new { message = "Yêu cầu trả món thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("details/{id}/confirm-return")]
        public async Task<IActionResult> ConfirmReturn(long id)
        {
            long confirmedByUserId = 1; // Fallback
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (claim != null && long.TryParse(claim.Value, out var parsedId))
            {
                confirmedByUserId = parsedId;
            }

            try
            {
                var result = await _detailService.ConfirmReturnItemAsync(id, confirmedByUserId);
                if (!result)
                    return BadRequest(new { message = "Không thể xác nhận trả món" });

                return Ok(new { message = "Xác nhận trả món thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("kitchen-items")]
        public async Task<IActionResult> GetKitchenItems([FromQuery] long? branchId)
        {
            var result = await _detailService.GetKitchenItemsAsync(branchId);
            return Ok(result);
        }

        // GET /api/Order/cancelled-history?branchId=1&fromDate=2026-08-01&toDate=2026-08-31
        [HttpGet("cancelled-history")]
        public async Task<IActionResult> GetCancelledHistory([FromQuery] long branchId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var result = await _detailService.GetCancelledHistoryAsync(branchId, fromDate, toDate);
            return Ok(result);
        }

        [HttpPut("details/{id}/cooking-status")]
        public async Task<IActionResult> UpdateCookingStatus(long id, [FromBody] UpdateCookingStatusDto dto)
        {
            long? tableId = null;
            string tableName = "Không rõ";
            string productName = "Không rõ";

            var detail = await _detailService.GetByIdAsync(id);
            if (detail != null)
            {
                var order = await _service.GetByIdAsync(detail.OrderId);
                if (order != null)
                {
                    tableId = order.TableId;
                    var table = await _tableService.GetByIdAsync(order.TableId);
                    if (table != null) tableName = table.Name;
                }

                var product = await _productService.GetByIdAsync(detail.ProductId);
                if (product != null) productName = product.Name;
            }

            var result = await _detailService.UpdateCookingStatusAsync(id, dto.CookingStatus, dto.Quantity);
            if (!result)
                return NotFound(new { message = "Không tìm thấy chi tiết đơn hoặc cập nhật thất bại" });

            string displayMessage = dto.CookingStatus == "Ready" 
                ? $"Món {productName} ({tableName}) đã nấu xong!" 
                : $"Trạng thái chế biến món {productName} đã đổi sang {dto.CookingStatus}";

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "cooking-status-changed",
                orderDetailId = id,
                cookingStatus = dto.CookingStatus,
                tableId = tableId,
                tableName = tableName,
                productName = productName,
                message = displayMessage,
                timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Cập nhật trạng thái thành công" });
        }

        [HttpPut("details/{id}/reuse-leftover")]
        public async Task<IActionResult> ReuseLeftover(long id, [FromBody] ReuseLeftoverDto dto)
        {
            long? tableId = null;
            string tableName = "Không rõ";
            string productName = "Không rõ";

            var detail = await _detailService.GetByIdAsync(id);
            if (detail != null)
            {
                var order = await _service.GetByIdAsync(detail.OrderId);
                if (order != null)
                {
                    tableId = order.TableId;
                    var table = await _tableService.GetByIdAsync(order.TableId);
                    if (table != null) tableName = table.Name;
                }

                var product = await _productService.GetByIdAsync(detail.ProductId);
                if (product != null) productName = product.Name;
            }

            var result = await _detailService.ApplyLeftoverReuseAsync(id, dto.LeftoverId);
            if (!result)
                return NotFound(new { message = "Không tìm thấy chi tiết đơn." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "cooking-status-changed",
                orderDetailId = id,
                cookingStatus = "Ready",
                tableId = tableId,
                tableName = tableName,
                productName = productName,
                message = $"Món {productName} ({tableName}) đã dùng lại món thừa — dừng chế biến!",
                timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Đã dùng lại món thừa, dừng chế biến." });
        }

        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(long orderId)
        {
            var result = await _service.CancelOrderAsync(orderId);
            if (!result)
                return BadRequest(new { message = "Không thể hủy đơn. Có món đang được chuẩn bị hoặc đã phục vụ." });

            // Clear assignment khi huỷ đơn
            await _assignmentService.DeactivateAssignmentAsync(orderId, "Cancelled");

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "order-cancelled",
                orderId = orderId,
                message = $"Đơn hàng #{orderId} đã bị hủy!",
                timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Hủy đơn hàng thành công" });
        }

        [HttpPut("details/{id}/cancel")]
        public async Task<IActionResult> CancelOrderDetail(long id)
        {
            long? tableId = null;
            string tableName = "Không rõ";
            string productName = "Không rõ";

            var detail = await _detailService.GetByIdAsync(id);
            if (detail != null)
            {
                var order = await _service.GetByIdAsync(detail.OrderId);
                if (order != null)
                {
                    tableId = order.TableId;
                    var table = await _tableService.GetByIdAsync(order.TableId);
                    if (table != null) tableName = table.Name;
                }

                var product = await _productService.GetByIdAsync(detail.ProductId);
                if (product != null) productName = product.Name;
            }

            var currentUserId = GetCurrentUserId();
            var result = await _detailService.CancelOrderDetailAsync(id, currentUserId);
            if (!result)
                return BadRequest(new { message = "Không thể hủy món. Món đã được nhận bởi bếp hoặc đã bị hủy trước đó." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "item-cancelled",
                orderDetailId = id,
                tableId = tableId,
                tableName = tableName,
                productName = productName,
                message = $"Món {productName} ({tableName}) đã bị hủy!",
                timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Hủy món thành công" });
        }
        [HttpPut("batch-cooking-status")]
        public async Task<IActionResult> BatchUpdateCookingStatus([FromBody] BatchUpdateCookingStatusDto dto)
        {
            long[] branchIdsToUse = Array.Empty<long>();
            if (dto.BranchId.HasValue && dto.BranchId.Value > 0)
            {
                branchIdsToUse = new[] { dto.BranchId.Value };
            }
            else
            {
                var branchClaims = User.FindAll("branchId").Concat(User.FindAll("BranchId"));
                branchIdsToUse = branchClaims
                    .Select(c => long.TryParse(c.Value, out var bid) ? bid : 0)
                    .Where(bid => bid > 0)
                    .Distinct()
                    .ToArray();
            }

            if (!branchIdsToUse.Any())
            {
                return Forbid();
            }

            var result = await _detailService.BatchUpdateCookingStatusAsync(dto.ProductId, dto.CookingStatus, branchIdsToUse, dto.Quantity);
            if (result.Started == 0 && result.Errors.Count == 0)
                return NotFound(new { message = "Không tìm thấy món ăn nào đang chờ xác nhận." });

            if (result.Started == 0)
                return BadRequest(new { message = $"Không bắt đầu được mẻ nào. {string.Join(" | ", result.Errors)}", errors = result.Errors });

            var product = await _productService.GetByIdAsync(dto.ProductId);
            string productName = product?.Name ?? "Món ăn";

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "cooking-status-changed",
                cookingStatus = dto.CookingStatus,
                productName = productName,
                message = $"Đã cập nhật mẻ món {productName} sang {dto.CookingStatus}",
                timestamp = DateTime.UtcNow
            });

            if (result.Errors.Count > 0)
            {
                return Ok(new
                {
                    message = $"Đã bắt đầu {result.Started} món, còn {result.Errors.Count} món chưa bắt đầu được. {string.Join(" | ", result.Errors)}",
                    started = result.Started,
                    failed = result.Errors.Count,
                    errors = result.Errors
                });
            }

            return Ok(new { message = "Cập nhật trạng thái mẻ thành công", started = result.Started, failed = 0 });
        }

        // ===== ORDER ASSIGNMENT ENDPOINTS =====

        /// <summary>Lấy danh sách bàn tôi đang phụ trách</summary>
        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetMyAssignments()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var orderIds = await _assignmentService.GetMyOrderIdsAsync(currentUserId.Value);
            var assignments = new List<object>();

            foreach (var orderId in orderIds)
            {
                var order = await _service.GetByIdAsync(orderId);
                if (order != null)
                {
                    var table = await _tableService.GetByIdAsync(order.TableId);
                    assignments.Add(new
                    {
                        orderId,
                        tableId = order.TableId,
                        tableName = table?.Name ?? $"Bàn #{order.TableId}",
                        branchId = table?.BranchId,
                        status = order.Status
                    });
                }
            }

            return Ok(assignments);
        }

        /// <summary>Tự nhận bàn</summary>
        [HttpPost("{orderId}/assign")]
        public async Task<IActionResult> AssignSelf(long orderId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var order = await _service.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            var table = await _tableService.GetByIdAsync(order.TableId);
            if (table == null)
                return NotFound(new { message = "Không tìm thấy bàn." });

            var assigned = await _assignmentService.TryAssignAsync(orderId, currentUserId.Value, table.BranchId);
            if (!assigned)
                return BadRequest(new { message = "Bàn đã có nhân viên phụ trách." });

            return Ok(new { message = $"Đã nhận phụ trách {table.Name}." });
        }

        /// <summary>Bỏ nhận bàn</summary>
        [HttpDelete("{orderId}/unassign")]
        public async Task<IActionResult> Unassign(long orderId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            await _assignmentService.DeactivateAssignmentAsync(orderId, "ManualUnassign");
            return Ok(new { message = "Đã bỏ phụ trách bàn." });
        }

        /// <summary>
        /// Manager chỉ định bắt buộc waiter cho bàn.
        /// Chỉ Manager/Admin mới dùng được.
        /// </summary>
        [HttpPost("{orderId}/force-assign")]
        public async Task<IActionResult> ForceAssign(long orderId, [FromBody] ForceAssignDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            // Validate: chỉ Manager/Admin
            var (isAdmin, isManager, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Chỉ Quản lý mới có quyền chỉ định nhân viên." });

            var order = await _service.GetByIdAsync(orderId);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            var table = await _tableService.GetByIdAsync(order.TableId);
            if (table == null)
                return NotFound(new { message = "Không tìm thấy bàn." });

            await _assignmentService.ForceAssignAsync(orderId, dto.AccountId, table.BranchId);

            // Thông báo cho waiter bị chỉ định
            await _hubContext.Clients.Group($"waiter_{dto.AccountId}")
                .SendAsync("ReceiveNotification", new
                {
                    type = "force-assigned",
                    orderId,
                    tableId = table.Id,
                    tableName = table.Name,
                    message = $"📋 Quản lý đã chỉ định bạn phụ trách {table.Name}.",
                    priority = "Warning",
                    timestamp = DateTime.UtcNow
                });

            return Ok(new { message = $"Đã chỉ định nhân viên cho {table.Name}." });
        }

        /// <summary>
        /// Lấy danh sách nhân viên đang on-duty trong branch (cho Manager chọn khi force-assign).
        /// </summary>
        [HttpGet("on-duty-staff/{branchId}")]
        public async Task<IActionResult> GetOnDutyStaff(long branchId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Không xác định được tài khoản đăng nhập." });

            var (isAdmin, isManager, _) = await _accountService.GetAccountPermissionsAsync(currentUserId.Value);
            if (!isAdmin && !isManager)
                return StatusCode(403, new { message = "Chỉ Quản lý mới có quyền xem danh sách nhân viên đang làm." });

            using var scope = HttpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MenuGoBE.Data.AppDbContext>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

            var onDutyStaff = await db.WorkSchedules
                .Include(ws => ws.Account)
                .Where(ws =>
                    ws.BranchId == branchId &&
                    ws.WorkDate == today &&
                    ws.CheckInAt != null &&
                    ws.CheckOutAt == null &&
                    ws.Status == "Working")
                .Select(ws => new
                {
                    accountId = ws.AccountId,
                    name = ws.Account.Name,
                    shiftId = ws.ShiftId
                })
                .Distinct()
                .ToListAsync();

            return Ok(onDutyStaff);
        }
    }
}
