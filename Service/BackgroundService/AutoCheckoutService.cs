using MenuGoBE.Data;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Interface.Repository;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Hubs;

namespace MenuGoBE.Service
{
    public class AutoCheckoutService : BackgroundService
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
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during AutoCheckoutService execution.");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        private async Task ProcessAutoCheckoutsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var _repo = scope.ServiceProvider.GetRequiredService<IWorkScheduleRepository>();

            var utcNow = DateTime.UtcNow;
            var localNow = utcNow.AddHours(7);
            var today = DateOnly.FromDateTime(localNow);

            // Lấy TẤT CẢ các ca đang làm (kể cả những ca vắt qua đêm của ngày hôm trước)
            var openSchedules = await _repo.GetOpenSchedulesAsync();

            if (!openSchedules.Any()) return;

            var accountIds = openSchedules.Select(ws => ws.AccountId).Distinct().ToList();
            
            // Lấy các ca đang chờ để Auto-Transition
            var pendingSchedules = await _repo.GetPendingSchedulesByAccountIdsAsync(accountIds);
            
            bool hasChanges = false;

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
                            ws.ActualHours = (decimal)Math.Max(0, (checkOutTimeUtc - ws.CheckInAt.Value).TotalHours);
                        }
                        
                        await _repo.UpdateAsync(ws);
                        _logger.LogInformation($"Auto-Transition: Checked-out AccountId: {ws.AccountId}, ScheduleId: {ws.Id} at {checkOutTimeUtc}");

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
                            ws.ActualHours = (decimal)Math.Max(0, (forcedCheckOutTimeUtc - ws.CheckInAt.Value).TotalHours);
                        }
                        
                        await _repo.UpdateAsync(ws);
                        _logger.LogInformation($"Auto-ForcedCheckout: Checked-out AccountId: {ws.AccountId}, ScheduleId: {ws.Id} at {forcedCheckOutTimeUtc}");
                        hasChanges = true;
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
