using MenuGoBE.Data;
using Microsoft.EntityFrameworkCore;
using MenuGoBE.Interface.Repository;

namespace MenuGoBE.Service
{
    public class AutoCheckoutService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoCheckoutService> _logger;

        public AutoCheckoutService(IServiceScopeFactory scopeFactory, ILogger<AutoCheckoutService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoCheckoutService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan delay = TimeSpan.FromMinutes(30);

                try
                {
                    // 1. Dọn dẹp tự động check-out cho các ca đã hết hạn
                    await ProcessAutoCheckoutsAsync();

                    // 2. Tính toán thời gian ngủ cho tới mốc hết ca tiếp theo, nhưng tối đa là 30 phút
                    var nextCutoffDelay = await CalculateNextShiftCutoffDelayAsync();
                    if (nextCutoffDelay < delay)
                    {
                        delay = nextCutoffDelay;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during AutoCheckoutService execution.");
                }

                _logger.LogInformation($"AutoCheckoutService sleeping for {delay.TotalMinutes:F1} minutes until next shift cutoff.");
                await Task.Delay(delay, stoppingToken);
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
                
                // Đợt 1: Đúng giờ EndTime -> kiểm tra xem có ca nối tiếp để transition không
                if (shiftEndLocal <= localNow)
                {
                    // Tìm ca nối tiếp
                    var nextSchedule = pendingSchedules.FirstOrDefault(s => s.AccountId == ws.AccountId
                                                                       && s.BranchId == ws.BranchId
                                                                       && s.Shift.StartTime <= ws.Shift.EndTime.AddMinutes(30)
                                                                       && s.Shift.StartTime >= ws.Shift.EndTime.AddMinutes(-30));
                    
                    if (nextSchedule != null)
                    {
                        var checkOutTimeUtc = shiftEndLocal.AddHours(-7); // Convert local EndTime to UTC
                        ws.CheckOutAt = checkOutTimeUtc;
                        ws.Status = "Completed";
                        if (ws.CheckInAt.HasValue)
                        {
                            ws.ActualHours = (decimal)Math.Max(0, (checkOutTimeUtc - ws.CheckInAt.Value).TotalHours);
                        }
                        
                        await _repo.UpdateAsync(ws);
                        _logger.LogInformation($"Auto-Transition: Checked-out AccountId: {ws.AccountId}, ScheduleId: {ws.Id} at {checkOutTimeUtc}");

                        var nextShiftStartLocal = nextSchedule.WorkDate.ToDateTime(nextSchedule.Shift.StartTime);
                        var nextShiftStartUtc = nextShiftStartLocal.AddHours(-7);
                        
                        nextSchedule.CheckInAt = nextShiftStartUtc;
                        nextSchedule.Status = "Working";
                        
                        await _repo.UpdateAsync(nextSchedule);
                        _logger.LogInformation($"Auto-Transition: Checked-in AccountId: {nextSchedule.AccountId}, ScheduleId: {nextSchedule.Id} at {nextShiftStartUtc}");
                        
                        hasChanges = true;
                        continue; // Đã check-out thành công qua Auto-Transition, bỏ qua đợt 2
                    }
                }

                // Đợt 2: Đã quá 30 phút sau EndTime -> Ép check-out (dành cho những ai không có ca nối tiếp)
                if (shiftEndLocal.AddMinutes(30) <= localNow)
                {
                    var checkOutTimeUtc = shiftEndLocal.AddHours(-7); // Giữ nguyên giờ chốt ca là đúng EndTime theo UTC
                    ws.CheckOutAt = checkOutTimeUtc;
                    ws.Status = "Completed";
                    if (ws.CheckInAt.HasValue)
                    {
                        ws.ActualHours = (decimal)Math.Max(0, (checkOutTimeUtc - ws.CheckInAt.Value).TotalHours);
                    }
                    
                    await _repo.UpdateAsync(ws);
                    _logger.LogInformation($"Auto-ForcedCheckout: Checked-out AccountId: {ws.AccountId}, ScheduleId: {ws.Id} at {checkOutTimeUtc}");
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _repo.SaveChangesAsync();
            }
        }

        private async Task<TimeSpan> CalculateNextShiftCutoffDelayAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var _repo = scope.ServiceProvider.GetRequiredService<IWorkScheduleRepository>();

            var localNow = DateTime.UtcNow.AddHours(7);

            var openSchedules = await _repo.GetOpenSchedulesAsync();

            if (openSchedules.Any())
            {
                // Tính danh sách các mốc giờ kết thúc thực tế của các ca đang làm (bao gồm cả mốc EndTime và EndTime + 30m)
                var futureCutoffs = openSchedules
                    .SelectMany(ws => 
                    {
                        var endLocal = ws.WorkDate.ToDateTime(ws.Shift.EndTime);
                        if (ws.Shift.EndTime < ws.Shift.StartTime)
                        {
                            endLocal = endLocal.AddDays(1);
                        }
                        return new[] { endLocal, endLocal.AddMinutes(30) };
                    })
                    .Where(cutoff => cutoff > localNow)
                    .OrderBy(cutoff => cutoff)
                    .ToList();

                if (futureCutoffs.Any())
                {
                    var nextCutoff = futureCutoffs.First();
                    var delay = nextCutoff - localNow;
                    return delay < TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : delay;
                }
            }

            // Nếu không có ca nào, hoặc không có mốc tương lai nào, trả về mức delay mặc định
            return TimeSpan.FromMinutes(30);
        }
    }
}
