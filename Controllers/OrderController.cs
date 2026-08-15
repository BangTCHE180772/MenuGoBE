using MenuGoBE.Dtos.Order;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;
using Microsoft.AspNetCore.RateLimiting;

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

        public OrderController(IOrderService service, IOrderDetailService detailService, IHubContext<NotificationHub> hubContext, ITableService tableService, IProductService productService)
        {
            _service = service;
            _detailService = detailService;
            _hubContext = hubContext;
            _tableService = tableService;
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices([FromQuery] long branchId)
        {
            var result = await _service.GetPaidOrdersByBranchAsync(branchId);
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

        [HttpPost]
        [EnableRateLimiting("CustomerOrderPolicy")]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (claim != null && long.TryParse(claim.Value, out var parsedId))
            {
                dto.CreatedBy = parsedId;
            }
            
            var result = await _service.CreateAsync(dto);
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
            long? currentUserId = null; // Fallback
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("id");
            if (claim != null && long.TryParse(claim.Value, out var parsedId))
            {
                currentUserId = parsedId;
            }

            var paidTableIds = await _service.PayOrderAsync(id, dto, "Cash", currentUserId);

            if (paidTableIds == null || !paidTableIds.Any())
                return BadRequest(new { message = "Không thể thanh toán. Hoá đơn không tồn tại hoặc không ở trạng thái Active." });

            foreach (var tableId in paidTableIds)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    TableId = tableId,
                    Type = "Payment",
                    Message = "Thanh toán thành công. Bàn cần dọn dẹp!"
                });
            }

            return Ok(new { message = "Thanh toán thành công" });
        }

        [HttpPost("merge")]
        public async Task<IActionResult> Merge([FromBody] OrderMergeDto dto)
        {
            var result = await _service.MergeAsync(dto);

            if (!result)
                return BadRequest(new { message = "Không thể gộp đơn. Kiểm tra lại OrderId hoặc đơn cha đã bị gộp vào đơn khác." });

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

                        await _hubContext.Clients.Group($"branch_{table.BranchId}").SendAsync("ReceiveNotification", payload);
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification", payload);
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

                        await _hubContext.Clients.Group($"branch_{table.BranchId}").SendAsync("ReceiveNotification", payload);
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification", payload);
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

                            await _hubContext.Clients.Group($"branch_{table.BranchId}").SendAsync("ReceiveNotification", payload);
                            await _hubContext.Clients.All.SendAsync("ReceiveNotification", payload);
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

                            await _hubContext.Clients.Group($"branch_{table.BranchId}").SendAsync("ReceiveNotification", payload);
                            await _hubContext.Clients.All.SendAsync("ReceiveNotification", payload);
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
        public async Task<IActionResult> GetKitchenItems()
        {
            var result = await _detailService.GetKitchenItemsAsync();
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

            var result = await _detailService.UpdateCookingStatusAsync(id, dto.CookingStatus);
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

        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(long orderId)
        {
            var result = await _service.CancelOrderAsync(orderId);
            if (!result)
                return BadRequest(new { message = "Không thể hủy đơn. Có món đang được chuẩn bị hoặc đã phục vụ." });

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
            var result = await _detailService.CancelOrderDetailAsync(id);
            if (!result)
                return BadRequest(new { message = "Không thể hủy món. Món đã được nhận bởi bếp hoặc đã bị hủy trước đó." });

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                type = "item-cancelled",
                orderDetailId = id,
                message = $"Món #{id} đã bị hủy!",
                timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Hủy món thành công" });
        }
    }
}
