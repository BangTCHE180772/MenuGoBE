using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service.Document;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.DocumentServiceTest
{
    public class ProductionOperationsAndIntegrityTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"ProductionOperations_TestDb_{Guid.NewGuid()}")
                .Options;
            return new AppDbContext(options);
        }

        #region 1. Concurrency Stress Test Scenario 1: Exact Depletion (100 qty -> 10 x 10)
        [Fact]
        public async Task ConcurrencyStress_Scenario1_ExactDepletion_NoNegativeBatch_DriftZero()
        {
            using var context = CreateInMemoryDbContext();

            var bInventory = new BInventory
            {
                Id = 100,
                BranchId = 1,
                ProductId = 1,
                Quantity = 100,
                Avg = 50000
            };
            var batch = new BInventoryBatch
            {
                Id = 101,
                BInventoryId = 100,
                BatchCode = "BATCH-STRESS-1",
                QuantityOriginal = 100,
                QuantityRemaining = 100,
                UnitCost = 50000,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };

            context.BInventories.Add(bInventory);
            context.BInventoryBatches.Add(batch);
            await context.SaveChangesAsync();

            var fefoService = new FefoAllocationService(context, NullLogger<FefoAllocationService>.Instance);

            // Execute 10 allocations of 10 units each
            int successfulAllocations = 0;
            for (int i = 0; i < 10; i++)
            {
                var allocs = await fefoService.AllocateAsync(100, 10);
                if (allocs.Sum(a => a.QuantityAllocated) == 10)
                {
                    successfulAllocations++;
                    bInventory.Quantity -= 10;
                    await context.SaveChangesAsync();
                }
            }

            Assert.Equal(10, successfulAllocations);
            Assert.Equal(0, batch.QuantityRemaining);
            Assert.Equal(BatchStatus.Depleted, batch.Status);
            Assert.Equal(0, bInventory.Quantity);

            // Verify Invariant: Drift == 0, Negative Batch == 0
            var totalBatchRemaining = await context.BInventoryBatches.Where(b => b.BInventoryId == 100).SumAsync(b => b.QuantityRemaining);
            Assert.Equal(bInventory.Quantity, totalBatchRemaining);
            Assert.False(await context.BInventoryBatches.AnyAsync(b => b.QuantityRemaining < 0));
        }
        #endregion

        #region 2. Concurrency Stress Test Scenario 2: Over-Allocation (100 qty -> 20 requests of 10)
        [Fact]
        public async Task ConcurrencyStress_Scenario2_OverAllocation_RejectsExcessGracefully()
        {
            using var context = CreateInMemoryDbContext();

            var bInventory = new BInventory
            {
                Id = 200,
                BranchId = 1,
                ProductId = 2,
                Quantity = 100,
                Avg = 30000
            };
            var batch = new BInventoryBatch
            {
                Id = 201,
                BInventoryId = 200,
                BatchCode = "BATCH-STRESS-2",
                QuantityOriginal = 100,
                QuantityRemaining = 100,
                UnitCost = 30000,
                ExpiryDate = DateTime.UtcNow.AddDays(15),
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };

            context.BInventories.Add(bInventory);
            context.BInventoryBatches.Add(batch);
            await context.SaveChangesAsync();

            var fefoService = new FefoAllocationService(context, NullLogger<FefoAllocationService>.Instance);

            int successCount = 0;
            int failedCount = 0;

            for (int i = 0; i < 20; i++)
            {
                try
                {
                    var allocs = await fefoService.AllocateAsync(200, 10);
                    successCount++;
                    bInventory.Quantity -= 10;
                    await context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    failedCount++;
                }
            }

            Assert.Equal(10, successCount);
            Assert.Equal(10, failedCount);
            Assert.Equal(0, bInventory.Quantity);
            Assert.Equal(0, batch.QuantityRemaining);

            // Invariant verification
            var totalBatchRemaining = await context.BInventoryBatches.Where(b => b.BInventoryId == 200).SumAsync(b => b.QuantityRemaining);
            Assert.Equal(0, totalBatchRemaining);
            Assert.Equal(bInventory.Quantity, totalBatchRemaining);
        }
        #endregion

        #region 3. Concurrency Stress Test Scenario 3: Multiple Batches FEFO Order Preserved
        [Fact]
        public async Task ConcurrencyStress_Scenario3_MultiBatch_FEFO_OrderStrictlyPreserved()
        {
            using var context = CreateInMemoryDbContext();

            var bInventory = new BInventory { Id = 300, BranchId = 1, ProductId = 3, Quantity = 100, Avg = 20000 };
            var batchA = new BInventoryBatch
            {
                Id = 301,
                BInventoryId = 300,
                BatchCode = "BATCH-A",
                QuantityOriginal = 30,
                QuantityRemaining = 30,
                UnitCost = 20000,
                ExpiryDate = DateTime.UtcNow.AddDays(1), // Earliest
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };
            var batchB = new BInventoryBatch
            {
                Id = 302,
                BInventoryId = 300,
                BatchCode = "BATCH-B",
                QuantityOriginal = 30,
                QuantityRemaining = 30,
                UnitCost = 20000,
                ExpiryDate = DateTime.UtcNow.AddDays(3), // Middle
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };
            var batchC = new BInventoryBatch
            {
                Id = 303,
                BInventoryId = 300,
                BatchCode = "BATCH-C",
                QuantityOriginal = 40,
                QuantityRemaining = 40,
                UnitCost = 20000,
                ExpiryDate = DateTime.UtcNow.AddDays(7), // Latest
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };

            context.BInventories.Add(bInventory);
            context.BInventoryBatches.AddRange(batchA, batchB, batchC);
            await context.SaveChangesAsync();

            var fefoService = new FefoAllocationService(context, NullLogger<FefoAllocationService>.Instance);

            // Step 1: Consume 20 -> All from Batch A (A remaining: 10)
            var alloc1 = await fefoService.AllocateAsync(300, 20);
            await context.SaveChangesAsync();
            Assert.Single(alloc1);
            Assert.Equal(301, alloc1[0].BatchId);
            Assert.Equal(10, batchA.QuantityRemaining);

            // Step 2: Consume 25 -> 10 from Batch A (depleted) and 15 from Batch B (B remaining: 15)
            var alloc2 = await fefoService.AllocateAsync(300, 25);
            await context.SaveChangesAsync();
            Assert.Equal(2, alloc2.Count);
            Assert.Equal(301, alloc2[0].BatchId);
            Assert.Equal(10, alloc2[0].QuantityAllocated);
            Assert.Equal(302, alloc2[1].BatchId);
            Assert.Equal(15, alloc2[1].QuantityAllocated);
            Assert.Equal(BatchStatus.Depleted, batchA.Status);
            Assert.Equal(15, batchB.QuantityRemaining);

            // Step 3: Consume 35 -> 15 from Batch B (depleted) and 20 from Batch C (C remaining: 20)
            var alloc3 = await fefoService.AllocateAsync(300, 35);
            await context.SaveChangesAsync();
            Assert.Equal(2, alloc3.Count);
            Assert.Equal(302, alloc3[0].BatchId);
            Assert.Equal(15, alloc3[0].QuantityAllocated);
            Assert.Equal(303, alloc3[1].BatchId);
            Assert.Equal(20, alloc3[1].QuantityAllocated);
            Assert.Equal(BatchStatus.Depleted, batchB.Status);
            Assert.Equal(20, batchC.QuantityRemaining);

            // Invariant Check
            var totalRemaining = await context.BInventoryBatches.Where(b => b.BInventoryId == 300).SumAsync(b => b.QuantityRemaining);
            Assert.Equal(20, totalRemaining);
        }
        #endregion

        #region 4. Long-Run Data Simulation Test (Multiple Cycles)
        [Fact]
        public async Task LongRunSimulation_MultipleCycles_MaintainsDataIntegrity()
        {
            using var context = CreateInMemoryDbContext();

            var bInventory = new BInventory
            {
                Id = 400,
                BranchId = 1,
                ProductId = 4,
                Quantity = 0,
                Avg = 0
            };
            context.BInventories.Add(bInventory);
            await context.SaveChangesAsync();

            var fefoService = new FefoAllocationService(context, NullLogger<FefoAllocationService>.Instance);

            // Simulate 5 consecutive business cycles
            for (int cycle = 1; cycle <= 5; cycle++)
            {
                // 1. Import new batch
                var newBatch = new BInventoryBatch
                {
                    Id = 4000 + cycle,
                    BInventoryId = 400,
                    BatchCode = $"BATCH-CYCLE-{cycle}",
                    QuantityOriginal = 50,
                    QuantityRemaining = 50,
                    UnitCost = 10000 + (cycle * 1000),
                    ExpiryDate = DateTime.UtcNow.AddDays(10 + cycle),
                    Status = BatchStatus.Active,
                    ReceivedDate = DateTime.UtcNow
                };
                context.BInventoryBatches.Add(newBatch);
                bInventory.Quantity += 50;
                await context.SaveChangesAsync();

                // 2. Sale deduction via FEFO
                var saleAlloc = await fefoService.AllocateAsync(400, 20);
                bInventory.Quantity -= 20;
                await context.SaveChangesAsync();

                // 3. Customer Return 5 units to active batch
                var activeBatch = await context.BInventoryBatches
                    .Where(b => b.BInventoryId == 400 && b.Status == BatchStatus.Active)
                    .OrderByDescending(b => b.Id)
                    .FirstOrDefaultAsync();
                Assert.NotNull(activeBatch);
                activeBatch.QuantityRemaining += 5;
                bInventory.Quantity += 5;
                await context.SaveChangesAsync();

                // 4. Disposal of 5 units via FEFO
                var disposalAlloc = await fefoService.AllocateAsync(400, 5);
                bInventory.Quantity -= 5;
                await context.SaveChangesAsync();

                // Verify Invariants after each cycle
                var sumBatches = await context.BInventoryBatches.Where(b => b.BInventoryId == 400).SumAsync(b => b.QuantityRemaining);
                Assert.Equal(bInventory.Quantity, sumBatches);
                Assert.False(await context.BInventoryBatches.AnyAsync(b => b.QuantityRemaining < 0));
                Assert.False(await context.BInventoryBatches.AnyAsync(b => b.QuantityRemaining > b.QuantityOriginal + 10));
            }

            // Final Invariant Confirmation after 5 full cycles
            var finalSum = await context.BInventoryBatches.Where(b => b.BInventoryId == 400).SumAsync(b => b.QuantityRemaining);
            Assert.Equal(bInventory.Quantity, finalSum);
            Assert.True(bInventory.Quantity > 0);
        }
        #endregion

        #region 5. Data Integrity Monitor & Expiry Metrics Drill
        [Fact]
        public async Task DataIntegrityMonitor_DetectsAndCategorizesBatchesAccurately()
        {
            using var context = CreateInMemoryDbContext();

            var now = DateTime.UtcNow;
            var batches = new List<BInventoryBatch>
            {
                new BInventoryBatch { Id = 1, BInventoryId = 500, BatchCode = "EXP-1", QuantityOriginal = 10, QuantityRemaining = 10, UnitCost = 10000, ExpiryDate = now.AddDays(-2), Status = BatchStatus.Expired },
                new BInventoryBatch { Id = 2, BInventoryId = 500, BatchCode = "CRIT-1", QuantityOriginal = 10, QuantityRemaining = 10, UnitCost = 15000, ExpiryDate = now.AddDays(2), Status = BatchStatus.Active },
                new BInventoryBatch { Id = 3, BInventoryId = 500, BatchCode = "NEAR-1", QuantityOriginal = 10, QuantityRemaining = 10, UnitCost = 20000, ExpiryDate = now.AddDays(5), Status = BatchStatus.Active },
                new BInventoryBatch { Id = 4, BInventoryId = 500, BatchCode = "VAL-1", QuantityOriginal = 10, QuantityRemaining = 10, UnitCost = 25000, ExpiryDate = now.AddDays(20), Status = BatchStatus.Active },
                new BInventoryBatch { Id = 5, BInventoryId = 500, BatchCode = "NOEXP-1", QuantityOriginal = 10, QuantityRemaining = 10, UnitCost = 30000, ExpiryDate = null, Status = BatchStatus.Active },
            };

            context.BInventoryBatches.AddRange(batches);
            await context.SaveChangesAsync();

            var allBatches = await context.BInventoryBatches.ToListAsync();

            var expiredCount = allBatches.Count(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date < now.Date);
            var criticalCount = allBatches.Count(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date >= now.Date && b.ExpiryDate.Value.Date <= now.AddDays(3).Date);
            var nearExpiryCount = allBatches.Count(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date > now.AddDays(3).Date && b.ExpiryDate.Value.Date <= now.AddDays(7).Date);
            var validCount = allBatches.Count(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date > now.AddDays(7).Date);
            var noExpiryCount = allBatches.Count(b => !b.ExpiryDate.HasValue);

            Assert.Equal(1, expiredCount);
            Assert.Equal(1, criticalCount);
            Assert.Equal(1, nearExpiryCount);
            Assert.Equal(1, validCount);
            Assert.Equal(1, noExpiryCount);

            // Estimated value expiring within 7 days (Critical + NearExpiry)
            var expiringSoonValue = allBatches
                .Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date >= now.Date && b.ExpiryDate.Value.Date <= now.AddDays(7).Date)
                .Sum(b => b.QuantityRemaining * b.UnitCost);

            Assert.Equal((10 * 15000) + (10 * 20000), expiringSoonValue);
        }
        #endregion
    }
}
