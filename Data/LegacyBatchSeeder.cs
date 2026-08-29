using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Data
{
    #region Seeder Khởi tạo Lô Kế Thừa (Legacy Batch Seeder)
    /// <summary>
    /// Seeder tự động khởi tạo bản ghi Lô LEGACY-INIT cho tất cả các bản ghi BInventory hiện có số lượng tồn > 0.
    /// Đảm bảo tính toàn vẹn bất biến: BInventory.Quantity == SUM(BInventoryBatch.QuantityRemaining).
    /// </summary>
    public static class LegacyBatchSeeder
    {
        public static async Task SeedLegacyBatchesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetService<ILogger<AppDbContext>>();

            try
            {
                var positiveInventories = await context.BInventories
                    .Include(b => b.Batches)
                    .Where(b => b.Quantity > 0)
                    .ToListAsync();

                int seededCount = 0;
                foreach (var inventory in positiveInventories)
                {
                    // Nếu BInventory chưa có bất kỳ Lô nào
                    if (!inventory.Batches.Any())
                    {
                        var legacyBatch = new BInventoryBatch
                        {
                            BInventoryId = inventory.Id,
                            BatchCode = "LEGACY-INIT",
                            QuantityOriginal = inventory.Quantity,
                            QuantityRemaining = inventory.Quantity,
                            UnitCost = inventory.Avg,
                            ManufactureDate = null,
                            ExpiryDate = null,
                            ReceivedDate = inventory.CreatedAt,
                            Status = BatchStatus.Active,
                            CreatedAt = DateTime.UtcNow
                        };

                        await context.BInventoryBatches.AddAsync(legacyBatch);
                        seededCount++;
                    }
                }

                if (seededCount > 0)
                {
                    await context.SaveChangesAsync();
                    logger?.LogInformation($"[LegacyBatchSeeder] Đã khởi tạo thành công {seededCount} Lô LEGACY-INIT cho các kho có tồn dương.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[LegacyBatchSeeder] Lỗi trong quá trình khởi tạo Lô kế thừa LEGACY-INIT.");
            }
        }
    }
    #endregion
}
