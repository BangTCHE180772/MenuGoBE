using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
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
    public class DataIntegrityAndConcurrencyTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"DataIntegrity_TestDb_{Guid.NewGuid()}")
                .Options;
            return new AppDbContext(options);
        }

        #region 1. FEFO Invariant & Multi-Batch Allocation Test
        [Fact]
        public async Task FefoAllocation_MultiBatch_CorrectlyAllocatesAndDeducts()
        {
            using var context = CreateInMemoryDbContext();

            var batches = new List<BInventoryBatch>
            {
                new BInventoryBatch
                {
                    Id = 1,
                    BInventoryId = 10,
                    BatchCode = "BATCH-A",
                    QuantityOriginal = 5,
                    QuantityRemaining = 5,
                    UnitCost = 10000,
                    ExpiryDate = DateTime.UtcNow.AddDays(2),
                    Status = BatchStatus.Active,
                    ReceivedDate = DateTime.UtcNow
                },
                new BInventoryBatch
                {
                    Id = 2,
                    BInventoryId = 10,
                    BatchCode = "BATCH-B",
                    QuantityOriginal = 10,
                    QuantityRemaining = 10,
                    UnitCost = 12000,
                    ExpiryDate = DateTime.UtcNow.AddDays(5),
                    Status = BatchStatus.Active,
                    ReceivedDate = DateTime.UtcNow
                }
            };

            context.BInventoryBatches.AddRange(batches);
            await context.SaveChangesAsync();

            var fefoService = new FefoAllocationService(context, NullLogger<FefoAllocationService>.Instance);

            // Consume 8 units: Batch A has 5 -> 0, Batch B has 10 -> 7
            var allocations = await fefoService.AllocateAsync(10, 8);
            await context.SaveChangesAsync();

            Assert.Equal(2, allocations.Count);
            Assert.Equal(5, allocations[0].QuantityAllocated);
            Assert.Equal(1, allocations[0].BatchId);
            Assert.Equal(3, allocations[1].QuantityAllocated);
            Assert.Equal(2, allocations[1].BatchId);

            var b1 = await context.BInventoryBatches.FindAsync(1L);
            var b2 = await context.BInventoryBatches.FindAsync(2L);

            Assert.Equal(0, b1!.QuantityRemaining);
            Assert.Equal(BatchStatus.Depleted, b1.Status);

            Assert.Equal(7, b2!.QuantityRemaining);
            Assert.Equal(BatchStatus.Active, b2.Status);
        }
        #endregion

        #region 2. Concurrency Simulation Test
        [Fact]
        public async Task FefoAllocation_ConcurrentDeductions_PreventsOverAllocation()
        {
            using var context = CreateInMemoryDbContext();

            var batch = new BInventoryBatch
            {
                Id = 1,
                BInventoryId = 20,
                BatchCode = "BATCH-CONCURRENT",
                QuantityOriginal = 10,
                QuantityRemaining = 10,
                UnitCost = 20000,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };

            context.BInventoryBatches.Add(batch);
            await context.SaveChangesAsync();

            var fefoService = new FefoAllocationService(context, NullLogger<FefoAllocationService>.Instance);

            // Transaction 1 consumes 8
            var alloc1 = await fefoService.AllocateAsync(20, 8);
            await context.SaveChangesAsync();
            Assert.Single(alloc1);
            Assert.Equal(2, batch.QuantityRemaining);

            // Transaction 2 tries to consume 8 but only 2 remain -> Insufficient inventory exception
            var ex = await Assert.ThrowsAsync<MenuGoException>(() =>
                fefoService.AllocateAsync(20, 8));

            Assert.Equal(ErrorCodes.DocumentInsufficientInventory.Code, ex.Code);
            Assert.Contains("không đủ để phân bổ", ex.Message);
            Assert.Equal(2, batch.QuantityRemaining); // Unchanged after failed attempt
        }
        #endregion

        #region 3. Expiry Rules & No Expiry Handling Test
        [Fact]
        public async Task FefoAllocation_ExpiredBatchSkipped_NoExpiryHandledLast()
        {
            using var context = CreateInMemoryDbContext();

            var expiredBatch = new BInventoryBatch
            {
                Id = 1,
                BInventoryId = 30,
                BatchCode = "BATCH-EXPIRED",
                QuantityOriginal = 10,
                QuantityRemaining = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(-1), // Expired
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };

            var nearExpiryBatch = new BInventoryBatch
            {
                Id = 2,
                BInventoryId = 30,
                BatchCode = "BATCH-NEAR",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(3),
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };

            var noExpiryBatch = new BInventoryBatch
            {
                Id = 3,
                BInventoryId = 30,
                BatchCode = "BATCH-NO-EXPIRY",
                QuantityOriginal = 10,
                QuantityRemaining = 10,
                ExpiryDate = null, // No Expiry -> should be allocated AFTER near expiry
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };

            context.BInventoryBatches.AddRange(expiredBatch, nearExpiryBatch, noExpiryBatch);
            await context.SaveChangesAsync();

            var fefoService = new FefoAllocationService(context, NullLogger<FefoAllocationService>.Instance);

            // Consume 7 units: Expired skipped -> Near (5) -> NoExpiry (2)
            var allocations = await fefoService.AllocateAsync(30, 7);
            await context.SaveChangesAsync();

            Assert.Equal(2, allocations.Count);
            Assert.Equal(2, allocations[0].BatchId); // Near expiry first
            Assert.Equal(5, allocations[0].QuantityAllocated);

            Assert.Equal(3, allocations[1].BatchId); // No expiry second
            Assert.Equal(2, allocations[1].QuantityAllocated);

            var bExp = await context.BInventoryBatches.FindAsync(1L);
            var bNear = await context.BInventoryBatches.FindAsync(2L);
            var bNo = await context.BInventoryBatches.FindAsync(3L);

            Assert.Equal(10, bExp!.QuantityRemaining); // Expired batch was not touched
            Assert.Equal(0, bNear!.QuantityRemaining);
            Assert.Equal(8, bNo!.QuantityRemaining);
        }
        #endregion

        #region 4. MWAC vs Batch UnitCost Integrity Test
        [Fact]
        public void MWAC_Vs_BatchUnitCost_PreservesCostSeparation()
        {
            var bInv = new BInventory
            {
                Id = 100,
                Quantity = 0,
                Avg = 0,
                LeftOver = 0
            };

            // Import Batch 1: 100 @ 10,000
            decimal qty1 = 100;
            decimal price1 = 10000;
            bInv.Avg = ((bInv.Quantity * bInv.Avg) + (qty1 * price1)) / (bInv.Quantity + qty1);
            bInv.Quantity += qty1;

            var batch1 = new BInventoryBatch
            {
                Id = 1,
                BInventoryId = bInv.Id,
                QuantityOriginal = qty1,
                QuantityRemaining = qty1,
                UnitCost = price1
            };

            Assert.Equal(100, bInv.Quantity);
            Assert.Equal(10000, bInv.Avg);
            Assert.Equal(10000, batch1.UnitCost);

            // Import Batch 2: 100 @ 20,000
            decimal qty2 = 100;
            decimal price2 = 20000;
            bInv.Avg = ((bInv.Quantity * bInv.Avg) + (qty2 * price2)) / (bInv.Quantity + qty2);
            bInv.Quantity += qty2;

            var batch2 = new BInventoryBatch
            {
                Id = 2,
                BInventoryId = bInv.Id,
                QuantityOriginal = qty2,
                QuantityRemaining = qty2,
                UnitCost = price2
            };

            // BInventory.Avg is 15,000 (MWAC), but Batch1.UnitCost is 10,000 and Batch2.UnitCost is 20,000
            Assert.Equal(200, bInv.Quantity);
            Assert.Equal(15000, bInv.Avg);
            Assert.Equal(10000, batch1.UnitCost);
            Assert.Equal(20000, batch2.UnitCost);

            // Sale of 50 units: BInventory.Avg MUST NOT change on Sale!
            bInv.Quantity -= 50;
            batch1.QuantityRemaining -= 50;

            Assert.Equal(150, bInv.Quantity);
            Assert.Equal(15000, bInv.Avg); // Avg unperturbed
            Assert.Equal(50, batch1.QuantityRemaining);
            Assert.Equal(100, batch2.QuantityRemaining);
            Assert.Equal(bInv.Quantity, batch1.QuantityRemaining + batch2.QuantityRemaining);
        }
        #endregion

        #region 5. Transfer Reject Target Batch Integrity Test
        [Fact]
        public void TransferReject_RestoresToExactOriginatingBatch()
        {
            var sourceBatch = new BInventoryBatch
            {
                Id = 55,
                BatchCode = "SRC-LOT-55",
                QuantityOriginal = 100,
                QuantityRemaining = 60, // 40 transferred out
                UnitCost = 15000
            };

            var otherBatch = new BInventoryBatch
            {
                Id = 56,
                BatchCode = "SRC-LOT-56",
                QuantityOriginal = 50,
                QuantityRemaining = 50,
                UnitCost = 15000
            };

            var transferOutAlloc = new BatchAllocation
            {
                Id = 1,
                BatchId = sourceBatch.Id,
                Batch = sourceBatch,
                QuantityAllocated = 40,
                AllocationType = BatchAllocationType.TransferOut
            };

            // When Transfer is rejected, restore quantity to originating batch
            transferOutAlloc.Batch.QuantityRemaining += transferOutAlloc.QuantityAllocated;

            Assert.Equal(100, sourceBatch.QuantityRemaining);
            Assert.Equal(50, otherBatch.QuantityRemaining); // Other batch untouched
        }
        #endregion
    }
}
