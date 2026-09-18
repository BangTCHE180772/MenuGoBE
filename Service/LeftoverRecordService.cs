using MenuGoBE.Dtos.Leftover;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Exceptions;
using MenuGoBE.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MenuGoBE.Service
{
    public class LeftoverRecordService : ILeftoverRecordService
    {
        private readonly ILeftoverRecordRepository _repo;
        private readonly IHubContext<NotificationHub> _hubContext;

        public LeftoverRecordService(ILeftoverRecordRepository repo, IHubContext<NotificationHub> hubContext)
        {
            _repo = repo;
            _hubContext = hubContext;
        }

        public async Task<List<LeftoverRecordViewDto>> GetByBranchAsync(long branchId, LeftoverType? type, DateOnly? fromDate, DateOnly? toDate)
        {
            var data = await _repo.GetByBranchAsync(branchId, type, fromDate, toDate);

            return data.Select(l => new LeftoverRecordViewDto
            {
                Id = l.Id,
                BranchId = l.BranchId,
                Type = l.Type,
                ProductId = l.ProductId,
                ProductName = l.Product?.Name ?? string.Empty,
                Quantity = l.Quantity,
                Reason = l.Reason,
                OrderDetailId = l.OrderDetailId,
                TableName = l.OrderDetail?.Order?.Table?.Name,
                HandlingAction = l.HandlingAction,
                AtFaultAccountId = l.AtFaultAccountId,
                AtFaultAccountName = l.AtFaultAccount?.Name,
                UsedAt = l.UsedAt,
                UsedByOrderDetailId = l.UsedByOrderDetailId,
                ShiftId = l.ShiftId,
                ShiftName = l.Shift?.Name,
                RecordDate = l.RecordDate,
                CreatedBy = l.CreatedBy,
                CreatedByName = l.Creator?.Name,
                CreatedAt = l.CreatedAt,
            }).ToList();
        }

        // Cửa sổ gợi ý dùng lại: 30 phút kể từ lúc món bị trả.
        private static readonly TimeSpan ReuseWindow = TimeSpan.FromMinutes(30);

        public async Task<List<LeftoverReusableDto>> GetReusableAsync(long branchId, long? productId)
        {
            var sinceUtc = DateTime.UtcNow - ReuseWindow;
            var data = await _repo.GetReusableAsync(branchId, sinceUtc, productId);

            return data.Select(l => new LeftoverReusableDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductName = l.Product?.Name ?? string.Empty,
                Quantity = l.Quantity,
                TableName = l.OrderDetail?.Order?.Table?.Name,
                CreatedAt = l.CreatedAt,
                ExpiresAt = l.CreatedAt + ReuseWindow,
            }).ToList();
        }

        public async Task MarkUsedAsync(long id, long productId, int quantity, long orderDetailId)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null)
            {
                throw new MenuGoException(new ErrorResult(404, "Món thừa không tồn tại hoặc đã bị xoá.", 404));
            }

            if (entity.Type != LeftoverType.Return || entity.UsedAt.HasValue)
            {
                throw new MenuGoException(new ErrorResult(400, "Món thừa này đã được dùng lại hoặc không thể dùng lại.", 400));
            }

            if (entity.ProductId != productId || entity.Quantity != quantity)
            {
                throw new MenuGoException(new ErrorResult(400, "Món hoặc số lượng không khớp với món thừa đã chọn.", 400));
            }

            if (entity.CreatedAt < DateTime.UtcNow - ReuseWindow)
            {
                throw new MenuGoException(new ErrorResult(400, "Món thừa đã quá 30 phút, không thể dùng lại.", 400));
            }

            entity.UsedAt = DateTime.UtcNow;
            entity.UsedByOrderDetailId = orderDetailId;
            await _repo.SaveChangesAsync();
        }

        public async Task<LeftoverRecordViewDto> CreateAsync(LeftoverRecordCreateDto dto, long? createdBy)
        {
            if (dto.Quantity <= 0)
            {
                throw new MenuGoException(new ErrorResult(400, "Số lượng phải lớn hơn 0.", 400));
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                throw new MenuGoException(new ErrorResult(400, "Phải nhập lý do.", 400));
            }

            OrderDetail? orderDetail = null;

            if (dto.Type == LeftoverType.Return)
            {
                if (!dto.OrderDetailId.HasValue)
                {
                    throw new MenuGoException(new ErrorResult(400, "Khách trả món phải gắn với một chi tiết đơn hàng (OrderDetailId).", 400));
                }
                if (!dto.HandlingAction.HasValue)
                {
                    throw new MenuGoException(new ErrorResult(400, "Phải chọn cách xử lý (huỷ / nhân viên dùng / tái sử dụng).", 400));
                }

                orderDetail = await _repo.GetOrderDetailWithTableAsync(dto.OrderDetailId.Value);
                if (orderDetail == null)
                {
                    throw new MenuGoException(new ErrorResult(404, "Không tìm thấy chi tiết đơn hàng.", 404));
                }
            }
            else // Extra: không gắn đơn khách, không đụng tổng tiền order, không cộng trả kho
            {
                if (dto.OrderDetailId.HasValue)
                {
                    throw new MenuGoException(new ErrorResult(400, "Bếp làm dư không được gắn với đơn hàng của khách.", 400));
                }
                if (dto.AtFaultAccountId.HasValue)
                {
                    throw new MenuGoException(new ErrorResult(400, "Bếp làm dư không gắn tài khoản làm sai.", 400));
                }
            }

            var entity = new LeftoverRecord
            {
                BranchId = dto.BranchId,
                Type = dto.Type,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                Reason = dto.Reason.Trim(),
                OrderDetailId = dto.OrderDetailId,
                HandlingAction = dto.HandlingAction,
                AtFaultAccountId = dto.AtFaultAccountId,
                ShiftId = dto.ShiftId,
                RecordDate = dto.RecordDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
            };

            // Chủ đích KHÔNG chạm vào Order.TotalAmount hay BInventory.Quantity ở đây —
            // đây là bảng ghi nhận (log) thuần túy cho cả 2 nghiệp vụ Return/Extra.
            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync();

            var saved = await _repo.GetByIdAsync(entity.Id);

            // Báo cho bếp biết ngay khi khách trả món — chỉ để xem, bếp không thao tác
            // gì trên thông báo này (nếu khớp món đang chờ nấu thì bếp tự bấm nút gợi ý
            // dùng lại món thừa ở màn hình KDS như bình thường).
            if (entity.Type == LeftoverType.Return)
            {
                string tableName = orderDetail?.Order?.Table?.Name ?? "?";
                string productName = saved?.Product?.Name ?? "?";
                string notifMessage = $"Bàn {tableName} vừa trả món \"{productName}\" x{entity.Quantity}. Lý do: {entity.Reason}";

                await _hubContext.Clients.Group($"branch_{entity.BranchId}").SendAsync("ReceiveNotification", new
                {
                    type = "leftover-return",
                    branchId = entity.BranchId,
                    leftoverId = entity.Id,
                    productId = entity.ProductId,
                    productName,
                    quantity = entity.Quantity,
                    tableName,
                    reason = entity.Reason,
                    message = notifMessage,
                    timestamp = DateTime.UtcNow
                });
            }

            return new LeftoverRecordViewDto
            {
                Id = entity.Id,
                BranchId = entity.BranchId,
                Type = entity.Type,
                ProductId = entity.ProductId,
                ProductName = saved?.Product?.Name ?? string.Empty,
                Quantity = entity.Quantity,
                Reason = entity.Reason,
                OrderDetailId = entity.OrderDetailId,
                TableName = orderDetail?.Order?.Table?.Name,
                HandlingAction = entity.HandlingAction,
                AtFaultAccountId = entity.AtFaultAccountId,
                ShiftId = entity.ShiftId,
                RecordDate = entity.RecordDate,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
            };
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            await _repo.DeleteAsync(id);
            await _repo.SaveChangesAsync();

            return true;
        }
    }
}
