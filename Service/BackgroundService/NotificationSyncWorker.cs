using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using MenuGoBE.Interface.Services;
using System.Linq;
using MenuGoBE.Data;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service.BackgroundService
{
    public class NotificationSyncWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationSyncWorker> _logger;

        public NotificationSyncWorker(IServiceProvider serviceProvider, ILogger<NotificationSyncWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationSyncWorker started.");
            
            // Wait 30 seconds before first run to allow the application to fully start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        
                        // Lấy tất cả BranchId đang có trong hệ thống
                        var branchIds = await dbContext.Branches
                            .Select(b => b.Id)
                            .ToArrayAsync(stoppingToken);

                        if (branchIds.Any())
                        {
                            _logger.LogInformation($"Running SyncBranchAlertsAsync for {branchIds.Length} branches...");
                            await notificationService.SyncBranchAlertsAsync(branchIds);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình chạy NotificationSyncWorker.");
                }

                // Cứ 5 phút quét lại 1 lần
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}

