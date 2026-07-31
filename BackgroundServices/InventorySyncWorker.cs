using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MenuGoBE.BackgroundServices
{
    public class InventorySyncWorker : BackgroundService
    {
        private readonly ILogger<InventorySyncWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public InventorySyncWorker(ILogger<InventorySyncWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("InventorySyncWorker is starting.");

            // Chu kỳ chạy: Mỗi 30 giây (có thể cấu hình trong appsettings nếu muốn)
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncReservedQuantitiesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình đồng bộ Inventory từ Redis về PostgreSQL.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("InventorySyncWorker is stopping.");
        }

        private async Task SyncReservedQuantitiesAsync()
        {
            // Do BackgroundService là Singleton, ta phải tạo một Scope mới để gọi các Scoped Services
            using var scope = _serviceProvider.CreateScope();
            var redisService = scope.ServiceProvider.GetRequiredService<IRedisInventoryService>();
            var binvRepo = scope.ServiceProvider.GetRequiredService<IBInventoryRepository>();

            // Lấy toàn bộ ReservedQuantity từ Redis
            var reservedData = await redisService.GetAllReservedQuantitiesAsync();
            if (reservedData.Count == 0)
            {
                return;
            }

            int updatedCount = 0;
            foreach (var kvp in reservedData)
            {
                var parts = kvp.Key.Split(':');
                if (parts.Length == 2 && long.TryParse(parts[0], out long branchId) && long.TryParse(parts[1], out long productId))
                {
                    long redisQuantity = kvp.Value;

                    // Đồng bộ xuống PostgreSQL
                    var binv = await binvRepo.GetByProductAndBranchAsync(productId, branchId);
                    if (binv != null)
                    {
                        if (binv.ReservedQuantity != redisQuantity)
                        {
                            binv.ReservedQuantity = redisQuantity;
                            await binvRepo.UpdateAsync(binv);
                            updatedCount++;
                        }
                    }
                }
            }

            if (updatedCount > 0)
            {
                await binvRepo.SaveChangesAsync();
                _logger.LogInformation("InventorySyncWorker: Đồng bộ thành công {Count} sản phẩm từ Redis về PostgreSQL.", updatedCount);
            }
        }
    }
}
