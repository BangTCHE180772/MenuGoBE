using MenuGoBE.Data;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;
using MenuGoBE.Models;

namespace MenuGoBE.Service
{
    public class AutoCheckoutService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoCheckoutService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AutoCheckoutService(IServiceScopeFactory scopeFactory, ILogger<AutoCheckoutService> logger, IHubContext<NotificationHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoCheckoutService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Dọn dẹp tự động check-out cho các ca đã hết hạn
                    await ProcessAutoCheckoutsAsync();
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during AutoCheckoutService execution.");
                }
            }
        }

        private async Task ProcessAutoCheckoutsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var _repo = scope.ServiceProvider.GetRequiredService<IWorkScheduleRepository>();

            var utcNow = DateTime.UtcNow;
            var localNow = utcNow.AddHours(7);
            var today = DateOnly.FromDateTime(localNow);

            bool hasChanges = false;

            // 1. Xử lý các ca đã quá hạn giờ/ngày kết thúc mà nhân viên chưa Check-in -> Tự động chuyển thành "ABSENT"
            var expiredUncheckedSchedules = (await _repo.GetExpiredUncheckedInSchedulesAsync()) ?? new List<WorkSchedule>();
            foreach (var ws in expiredUncheckedSchedules)
            {
                if (ws.Shift == null) continue;

                var shiftEndLocal = ws.WorkDate.ToDateTime(ws.Shift.EndTime);
                if (ws.Shift.EndTime < ws.Shift.StartTime)
                {
                    shiftEndLocal = shiftEndLocal.AddDays(1);
                }

                if (shiftEndLocal <= localNow)
                {
                    ws.Status = "ABSENT";
                    await _repo.UpdateAsync(ws);
                    _logger.LogInformation($"Auto-Absent: Marked ScheduleId: {ws.Id}, AccountId: {ws.AccountId} as ABSENT (expired at {shiftEndLocal})");
                    hasChanges = true;
                }
            }

            // 2. Lấy TẤT CẢ các ca đang làm (kể cả những ca vắt qua đêm của ngày hôm trước) để Auto-Checkout
            var openSchedules = await _repo.GetOpenSchedulesAsync();

            if (openSchedules.Any())
            {
                var accountIds = openSchedules.Select(ws => ws.AccountId).Distinct().ToList();
                
                // Lấy các ca đang chờ để Auto-Transition
                var pendingSchedules = await _repo.GetPendingSchedulesByAccountIdsAsync(accountIds);

                foreach (var ws in openSchedules)
                {
                    // Tính giờ kết thúc local của ca làm việc
                    var shiftEndLocal = ws.WorkDate.ToDateTime(ws.Shift.EndTime);
                    if (ws.Shift.EndTime < ws.Shift.StartTime)
                    {
                        // Ca kết thúc vào sáng hôm sau
                        shiftEndLocal = shiftEndLocal.AddDays(1);
                    }
                    
                    // Tìm ca nối tiếp (nếu có)
                    var nextSchedule = pendingSchedules.FirstOrDefault(s => 
                    {
                        if (s.AccountId != ws.AccountId || s.BranchId != ws.BranchId) return false;
                        var nextStartLocal = s.WorkDate.ToDateTime(s.Shift.StartTime);
                        return Math.Abs((nextStartLocal - shiftEndLocal).TotalMinutes) <= 30;
                    });

                    if (nextSchedule != null)
                    {
                        // Tình huống A: Có ca nối tiếp -> Transition NGAY LẬP TỨC khi hết ca
                        if (shiftEndLocal <= localNow)
                        {
                            var checkOutTimeUtc = DateTime.SpecifyKind(shiftEndLocal.AddHours(-7), DateTimeKind.Utc); // Convert local EndTime to UTC
                            ws.CheckOutAt = checkOutTimeUtc;
                            ws.Status = "Completed";
                            if (ws.CheckInAt.HasValue)
                            {
                                ws.ActualHours = WorkScheduleService.CalculateBoundedActualHours(ws);
                            }
                            
                            await _repo.UpdateAsync(ws);
                            _logger.LogInformation($"Auto-Transition: Checked-out AccountId: {ws.AccountId}, ScheduleId: {ws.Id} at {checkOutTimeUtc}");

                            // Auto-unassign tất cả bàn của nhân viên khi chuyển ca
                            var assignmentService = scope.ServiceProvider.GetRequiredService<IOrderAssignmentService>();
                            await assignmentService.DeactivateAllByAccountAsync(ws.AccountId, "ShiftEnded");
                            _logger.LogInformation($"Auto-unassigned all tables for AccountId: {ws.AccountId} (auto-transition)");

                            var nextShiftStartLocal = nextSchedule.WorkDate.ToDateTime(nextSchedule.Shift.StartTime);
                            var nextShiftStartUtc = DateTime.SpecifyKind(nextShiftStartLocal.AddHours(-7), DateTimeKind.Utc);
                            
                            nextSchedule.CheckInAt = nextShiftStartUtc;
                            nextSchedule.Status = "Working";
                            
                            await _repo.UpdateAsync(nextSchedule);
                            _logger.LogInformation($"Auto-Transition: Checked-in AccountId: {nextSchedule.AccountId}, ScheduleId: {nextSchedule.Id} at {nextShiftStartUtc}");
                            
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        // Tình huống B: KHÔNG có ca nối tiếp (tan làm) -> Chờ 2 tiếng mới ép check-out (Grace period)
                        if (shiftEndLocal.AddHours(2) <= localNow)
                        {
                            var forcedCheckOutTimeUtc = DateTime.SpecifyKind(shiftEndLocal.AddHours(-7), DateTimeKind.Utc); // Giữ nguyên giờ chốt ca là đúng EndTime theo UTC
                            ws.CheckOutAt = forcedCheckOutTimeUtc;
                            ws.Status = "Completed";
                            if (ws.CheckInAt.HasValue)
                            {
                                ws.ActualHours = WorkScheduleService.CalculateBoundedActualHours(ws);
                            }
                            
                            await _repo.UpdateAsync(ws);
                            _logger.LogInformation($"Auto-ForcedCheckout: Checked-out AccountId: {ws.AccountId}, ScheduleId: {ws.Id} at {forcedCheckOutTimeUtc}");

                            // Auto-unassign tất cả bàn của nhân viên khi ép checkout
                            var assignmentService2 = scope.ServiceProvider.GetRequiredService<IOrderAssignmentService>();
                            await assignmentService2.DeactivateAllByAccountAsync(ws.AccountId, "ShiftEnded");
                            _logger.LogInformation($"Auto-unassigned all tables for AccountId: {ws.AccountId} (auto-forced-checkout)");
                            hasChanges = true;
                        }
                    }
                }
            }

            if (hasChanges)
            {
                await _repo.SaveChangesAsync();
                
                // Gửi thông báo SignalR cho Client để tự động refresh màn hình
                if (_hubContext?.Clients != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveScheduleUpdate");
                }
            }
        }

    }
}
