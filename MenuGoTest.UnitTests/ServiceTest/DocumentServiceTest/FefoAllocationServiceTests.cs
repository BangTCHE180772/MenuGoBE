using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MenuGoBE.Data;
using MenuGoBE.Exceptions;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service.Document;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.DocumentServiceTest
{
    public class FefoAllocationServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly FefoAllocationService _service;

        public FefoAllocationServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"MenuGo_FefoTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _service = new FefoAllocationService(_context, NullLogger<FefoAllocationService>.Instance);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region TC-01: Một Batch đủ tồn kho
        [Fact]
        public async Task TC01_SingleBatchSufficient_AllocatesCorrectly()
        {
            // Arrange
            long inventoryId = 1;
            var batch = new BInventoryBatch
            {
                Id = 101,
                BInventoryId = inventoryId,
                BatchCode = "LOT-01",
                QuantityOriginal = 10,
                QuantityRemaining = 10,
                UnitCost = 50000,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                ReceivedDate = DateTime.UtcNow.AddDays(-2),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddAsync(batch);
            await _context.SaveChangesAsync();

            // Act
            var allocations = await _service.AllocateAsync(inventoryId, 5);

            // Assert
            Assert.Single(allocations);
            Assert.Equal(101, allocations[0].BatchId);
            Assert.Equal("LOT-01", allocations[0].BatchCode);
            Assert.Equal(5, allocations[0].QuantityAllocated);
            Assert.Equal(50000, allocations[0].UnitCost);
            Assert.Equal(5, batch.QuantityRemaining);
            Assert.Equal(BatchStatus.Active, batch.Status);
        }
        #endregion

        #region TC-02: Phân bổ từ nhiều Lô (Multi-Batch)
        [Fact]
        public async Task TC02_MultiBatch_AllocatesAcrossMultipleBatches()
        {
            // Arrange
            long inventoryId = 2;
            var batchA = new BInventoryBatch
            {
                Id = 201,
                BInventoryId = inventoryId,
                BatchCode = "LOT-A",
                QuantityOriginal = 2,
                QuantityRemaining = 2,
                UnitCost = 10000,
                ExpiryDate = DateTime.UtcNow.AddDays(5),
                ReceivedDate = DateTime.UtcNow.AddDays(-5),
                Status = BatchStatus.Active
            };
            var batchB = new BInventoryBatch
            {
                Id = 202,
                BInventoryId = inventoryId,
                BatchCode = "LOT-B",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                UnitCost = 12000,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                ReceivedDate = DateTime.UtcNow.AddDays(-2),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddRangeAsync(batchA, batchB);
            await _context.SaveChangesAsync();

            // Act: Cần xuất 6 đơn vị (2 từ A, 4 từ B)
            var allocations = await _service.AllocateAsync(inventoryId, 6);

            // Assert
            Assert.Equal(2, allocations.Count);
            
            Assert.Equal(201, allocations[0].BatchId);
            Assert.Equal(2, allocations[0].QuantityAllocated);
            Assert.Equal(10000, allocations[0].UnitCost);
            Assert.Equal(0, batchA.QuantityRemaining);
            Assert.Equal(BatchStatus.Depleted, batchA.Status);

            Assert.Equal(202, allocations[1].BatchId);
            Assert.Equal(4, allocations[1].QuantityAllocated);
            Assert.Equal(12000, allocations[1].UnitCost);
            Assert.Equal(1, batchB.QuantityRemaining);
            Assert.Equal(BatchStatus.Active, batchB.Status);
        }
        #endregion

        #region TC-03: Thứ tự ưu tiên FEFO theo ExpiryDate
        [Fact]
        public async Task TC03_FefoOrder_PrioritizesEarliestExpiryDate()
        {
            // Arrange: A: 10/09, B: 01/09, C: NULL
            long inventoryId = 3;
            var batchA = new BInventoryBatch
            {
                Id = 301,
                BInventoryId = inventoryId,
                BatchCode = "LOT-A",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(20),
                ReceivedDate = DateTime.UtcNow.AddDays(-10),
                Status = BatchStatus.Active
            };
            var batchB = new BInventoryBatch
            {
                Id = 302,
                BInventoryId = inventoryId,
                BatchCode = "LOT-B",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(5),
                ReceivedDate = DateTime.UtcNow.AddDays(-2),
                Status = BatchStatus.Active
            };
            var batchC = new BInventoryBatch
            {
                Id = 303,
                BInventoryId = inventoryId,
                BatchCode = "LOT-C",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = null,
                ReceivedDate = DateTime.UtcNow.AddDays(-20),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddRangeAsync(batchA, batchB, batchC);
            await _context.SaveChangesAsync();

            // Act: Xuất 12 đơn vị (5 từ B, 5 từ A, 2 từ C)
            var allocations = await _service.AllocateAsync(inventoryId, 12);

            // Assert: Thứ tự B -> A -> C
            Assert.Equal(3, allocations.Count);
            Assert.Equal(302, allocations[0].BatchId); // B
            Assert.Equal(301, allocations[1].BatchId); // A
            Assert.Equal(303, allocations[2].BatchId); // C
        }
        #endregion

        #region TC-04: Cùng HSD -> Ưu tiên ReceivedDate sớm hơn
        [Fact]
        public async Task TC04_SameExpiry_PrioritizesEarlierReceivedDate()
        {
            // Arrange
            long inventoryId = 4;
            var expiry = DateTime.UtcNow.AddDays(15);
            var batchA = new BInventoryBatch
            {
                Id = 401,
                BInventoryId = inventoryId,
                BatchCode = "LOT-A",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = expiry,
                ReceivedDate = DateTime.UtcNow.AddDays(-10), // Nhập sớm hơn
                Status = BatchStatus.Active
            };
            var batchB = new BInventoryBatch
            {
                Id = 402,
                BInventoryId = inventoryId,
                BatchCode = "LOT-B",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = expiry,
                ReceivedDate = DateTime.UtcNow.AddDays(-2),  // Nhập muộn hơn
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddRangeAsync(batchB, batchA);
            await _context.SaveChangesAsync();

            // Act
            var allocations = await _service.AllocateAsync(inventoryId, 3);

            // Assert: A nhập trước -> xuất từ A trước
            Assert.Single(allocations);
            Assert.Equal(401, allocations[0].BatchId);
            Assert.Equal(3, allocations[0].QuantityAllocated);
        }
        #endregion

        #region TC-05: Cùng HSD và cùng ReceivedDate -> Id ASC (Deterministic tie-breaker)
        [Fact]
        public async Task TC05_SameExpiryAndReceived_PrioritizesSmallerId()
        {
            // Arrange
            long inventoryId = 5;
            var expiry = DateTime.UtcNow.AddDays(15);
            var received = DateTime.UtcNow.AddDays(-5);
            var batch1 = new BInventoryBatch
            {
                Id = 501,
                BInventoryId = inventoryId,
                BatchCode = "LOT-1",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = expiry,
                ReceivedDate = received,
                Status = BatchStatus.Active
            };
            var batch2 = new BInventoryBatch
            {
                Id = 502,
                BInventoryId = inventoryId,
                BatchCode = "LOT-2",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = expiry,
                ReceivedDate = received,
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddRangeAsync(batch2, batch1);
            await _context.SaveChangesAsync();

            // Act
            var allocations = await _service.AllocateAsync(inventoryId, 2);

            // Assert
            Assert.Single(allocations);
            Assert.Equal(501, allocations[0].BatchId);
        }
        #endregion

        #region TC-06: Loại trừ Lô đã hết hạn (Expired Exclusion)
        [Fact]
        public async Task TC06_ExpiredBatch_IsExcludedFromAllocation()
        {
            // Arrange
            long inventoryId = 6;
            var expiredBatch = new BInventoryBatch
            {
                Id = 601,
                BInventoryId = inventoryId,
                BatchCode = "LOT-EXPIRED",
                QuantityOriginal = 10,
                QuantityRemaining = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(-1), // Đã quá hạn
                ReceivedDate = DateTime.UtcNow.AddDays(-30),
                Status = BatchStatus.Active
            };
            var validBatch = new BInventoryBatch
            {
                Id = 602,
                BInventoryId = inventoryId,
                BatchCode = "LOT-VALID",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(5),
                ReceivedDate = DateTime.UtcNow.AddDays(-5),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddRangeAsync(expiredBatch, validBatch);
            await _context.SaveChangesAsync();

            // Act: Xuất 3 đơn vị
            var allocations = await _service.AllocateAsync(inventoryId, 3);

            // Assert: Chỉ xuất từ LOT-VALID, không đụng tới LOT-EXPIRED
            Assert.Single(allocations);
            Assert.Equal(602, allocations[0].BatchId);
            Assert.Equal(3, allocations[0].QuantityAllocated);
            Assert.Equal(10, expiredBatch.QuantityRemaining); // Không đổi
        }
        #endregion

        #region TC-07: Lô có ExpiryDate NULL luôn xếp sau cùng
        [Fact]
        public async Task TC07_NullExpiry_IsAlwaysSortedLast()
        {
            // Arrange
            long inventoryId = 7;
            var nullExpiryBatch = new BInventoryBatch
            {
                Id = 701,
                BInventoryId = inventoryId,
                BatchCode = "LOT-NULL-EXPIRY",
                QuantityOriginal = 10,
                QuantityRemaining = 10,
                ExpiryDate = null,
                ReceivedDate = DateTime.UtcNow.AddDays(-100), // Nhập rất lâu trước đó
                Status = BatchStatus.Active
            };
            var validExpiryBatch = new BInventoryBatch
            {
                Id = 702,
                BInventoryId = inventoryId,
                BatchCode = "LOT-VALID-EXPIRY",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                ReceivedDate = DateTime.UtcNow.AddDays(-1),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddRangeAsync(nullExpiryBatch, validExpiryBatch);
            await _context.SaveChangesAsync();

            // Act: Xuất 8 đơn vị (5 từ VALID, 3 từ NULL)
            var allocations = await _service.AllocateAsync(inventoryId, 8);

            // Assert
            Assert.Equal(2, allocations.Count);
            Assert.Equal(702, allocations[0].BatchId); // VALID trước
            Assert.Equal(5, allocations[0].QuantityAllocated);

            Assert.Equal(701, allocations[1].BatchId); // NULL sau
            Assert.Equal(3, allocations[1].QuantityAllocated);
        }
        #endregion

        #region TC-08: Không đủ tồn kho khả dụng -> Ném MenuGoException
        [Fact]
        public async Task TC08_InsufficientInventory_ThrowsMenuGoException()
        {
            // Arrange
            long inventoryId = 8;
            var batch = new BInventoryBatch
            {
                Id = 801,
                BInventoryId = inventoryId,
                BatchCode = "LOT-01",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddAsync(batch);
            await _context.SaveChangesAsync();

            // Act & Assert: Yêu cầu 6 trong khi chỉ có 5
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.AllocateAsync(inventoryId, 6));
            Assert.Equal(ErrorCodes.DocumentInsufficientInventory.Code, ex.Code);
            Assert.Equal(5, batch.QuantityRemaining); // Tồn kho không bị thay đổi
        }
        #endregion

        #region TC-09: Yêu cầu số lượng = 0 -> Ném ArgumentException
        [Fact]
        public async Task TC09_ZeroQuantityRequest_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AllocateAsync(1, 0));
        }
        #endregion

        #region TC-10: Yêu cầu số lượng âm -> Ném ArgumentException
        [Fact]
        public async Task TC10_NegativeQuantityRequest_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AllocateAsync(1, -5));
        }
        #endregion

        #region TC-11: Lô xuất hết về 0 -> Chuyển Status thành Depleted
        [Fact]
        public async Task TC11_BatchReachesZero_StatusBecomesDepleted()
        {
            // Arrange
            long inventoryId = 11;
            var batch = new BInventoryBatch
            {
                Id = 1101,
                BInventoryId = inventoryId,
                BatchCode = "LOT-EXACT",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddAsync(batch);
            await _context.SaveChangesAsync();

            // Act: Xuất đúng 5 đơn vị
            var allocations = await _service.AllocateAsync(inventoryId, 5);

            // Assert
            Assert.Single(allocations);
            Assert.Equal(0, batch.QuantityRemaining);
            Assert.Equal(BatchStatus.Depleted, batch.Status);
        }
        #endregion

        #region TC-12: Các Lô Inactive / Locked / Depleted không bao giờ được cấp phát
        [Fact]
        public async Task TC12_NonActiveBatches_AreNeverAllocated()
        {
            // Arrange
            long inventoryId = 12;
            var depletedBatch = new BInventoryBatch
            {
                Id = 1201,
                BInventoryId = inventoryId,
                BatchCode = "LOT-DEPLETED",
                QuantityOriginal = 5,
                QuantityRemaining = 0,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                Status = BatchStatus.Depleted
            };
            var lockedBatch = new BInventoryBatch
            {
                Id = 1202,
                BInventoryId = inventoryId,
                BatchCode = "LOT-LOCKED",
                QuantityOriginal = 10,
                QuantityRemaining = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                Status = BatchStatus.Locked
            };
            var activeBatch = new BInventoryBatch
            {
                Id = 1203,
                BInventoryId = inventoryId,
                BatchCode = "LOT-ACTIVE",
                QuantityOriginal = 5,
                QuantityRemaining = 5,
                ExpiryDate = DateTime.UtcNow.AddDays(10),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddRangeAsync(depletedBatch, lockedBatch, activeBatch);
            await _context.SaveChangesAsync();

            // Act: Xuất 4 đơn vị
            var allocations = await _service.AllocateAsync(inventoryId, 4);

            // Assert: Chỉ lấy từ Active
            Assert.Single(allocations);
            Assert.Equal(1203, allocations[0].BatchId);
            Assert.Equal(4, allocations[0].QuantityAllocated);
        }
        #endregion

        #region TC-13: Chỉ có Lô hết hạn trong kho -> Ném DocumentInsufficientInventory
        [Fact]
        public async Task TC13_OnlyExpiredBatchesExist_ThrowsDocumentInsufficientInventory()
        {
            // Arrange
            long inventoryId = 13;
            var expiredBatch = new BInventoryBatch
            {
                Id = 1301,
                BInventoryId = inventoryId,
                BatchCode = "LOT-EXPIRED-ONLY",
                QuantityOriginal = 20,
                QuantityRemaining = 20,
                ExpiryDate = DateTime.UtcNow.AddDays(-5),
                Status = BatchStatus.Active
            };
            await _context.BInventoryBatches.AddAsync(expiredBatch);
            await _context.SaveChangesAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<MenuGoException>(() => _service.AllocateAsync(inventoryId, 1));
            Assert.Equal(ErrorCodes.DocumentInsufficientInventory.Code, ex.Code);
        }
        #endregion

        #region TC-14: Kết hợp hỗn hợp Valid + Expired + NULL
        [Fact]
        public async Task TC14_MixedBatches_CorrectlyAllocatesValidBatchesOnly()
        {
            // Arrange
            long inventoryId = 14;
            var expired = new BInventoryBatch { Id = 1401, BInventoryId = inventoryId, BatchCode = "EXP", QuantityRemaining = 10, ExpiryDate = DateTime.UtcNow.AddDays(-2), Status = BatchStatus.Active };
            var nearExpiry = new BInventoryBatch { Id = 1402, BInventoryId = inventoryId, BatchCode = "NEAR", QuantityRemaining = 4, ExpiryDate = DateTime.UtcNow.AddDays(3), Status = BatchStatus.Active };
            var farExpiry = new BInventoryBatch { Id = 1403, BInventoryId = inventoryId, BatchCode = "FAR", QuantityRemaining = 6, ExpiryDate = DateTime.UtcNow.AddDays(30), Status = BatchStatus.Active };
            var nullExpiry = new BInventoryBatch { Id = 1404, BInventoryId = inventoryId, BatchCode = "NULL", QuantityRemaining = 5, ExpiryDate = null, Status = BatchStatus.Active };

            await _context.BInventoryBatches.AddRangeAsync(expired, nearExpiry, farExpiry, nullExpiry);
            await _context.SaveChangesAsync();

            // Act: Xuất 12 đơn vị (4 từ NEAR, 6 từ FAR, 2 từ NULL)
            var allocations = await _service.AllocateAsync(inventoryId, 12);

            // Assert
            Assert.Equal(3, allocations.Count);
            Assert.Equal(1402, allocations[0].BatchId); // NEAR (4)
            Assert.Equal(1403, allocations[1].BatchId); // FAR (6)
            Assert.Equal(1404, allocations[2].BatchId); // NULL (2)
            Assert.Equal(10, expired.QuantityRemaining); // EXP untouched
        }
        #endregion

        #region TC-15: Tính tất định (Deterministic Ordering)
        [Fact]
        public async Task TC15_DeterministicOrdering_ProducesIdenticalOrderEveryTime()
        {
            // Arrange
            long inventoryId = 15;
            var now = DateTime.UtcNow;
            for (int i = 1; i <= 10; i++)
            {
                await _context.BInventoryBatches.AddAsync(new BInventoryBatch
                {
                    Id = 1500 + i,
                    BInventoryId = inventoryId,
                    BatchCode = $"LOT-{i}",
                    QuantityOriginal = 2,
                    QuantityRemaining = 2,
                    ExpiryDate = now.AddDays(i % 3),
                    ReceivedDate = now.AddDays(-i),
                    Status = BatchStatus.Active
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var allocations = await _service.AllocateAsync(inventoryId, 10);

            // Assert: Lô có ExpiryDate sớm nhất phải luôn nằm ở vị trí đầu
            Assert.True(allocations.Count > 0);
            for (int i = 0; i < allocations.Count - 1; i++)
            {
                if (allocations[i].ExpiryDate.HasValue && allocations[i + 1].ExpiryDate.HasValue)
                {
                    Assert.True(allocations[i].ExpiryDate <= allocations[i + 1].ExpiryDate);
                }
            }
        }
        #endregion

        #region TC-16: Đối soát tính toàn vẹn (Reconciliation Check)
        [Fact]
        public async Task TC16_CheckReconciliation_DetectsConsistentAndDriftCorrectly()
        {
            // Case Consistent
            long inventoryId1 = 161;
            var inventory1 = new BInventory { Id = inventoryId1, BranchId = 1, ProductId = 1, Quantity = 15, Avg = 10000 };
            var batch1A = new BInventoryBatch { Id = 1601, BInventoryId = inventoryId1, BatchCode = "A", QuantityOriginal = 10, QuantityRemaining = 10, Status = BatchStatus.Active };
            var batch1B = new BInventoryBatch { Id = 1602, BInventoryId = inventoryId1, BatchCode = "B", QuantityOriginal = 5, QuantityRemaining = 5, Status = BatchStatus.Active };

            // Case Drift
            long inventoryId2 = 162;
            var inventory2 = new BInventory { Id = inventoryId2, BranchId = 1, ProductId = 2, Quantity = 20, Avg = 10000 }; // Lệch 5
            var batch2A = new BInventoryBatch { Id = 1603, BInventoryId = inventoryId2, BatchCode = "C", QuantityOriginal = 15, QuantityRemaining = 15, Status = BatchStatus.Active };

            await _context.BInventories.AddRangeAsync(inventory1, inventory2);
            await _context.BInventoryBatches.AddRangeAsync(batch1A, batch1B, batch2A);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _service.CheckReconciliationAsync(inventoryId1);
            var result2 = await _service.CheckReconciliationAsync(inventoryId2);

            // Assert
            Assert.True(result1.IsConsistent);
            Assert.Equal(0, result1.DriftQuantity);

            Assert.False(result2.IsConsistent);
            Assert.Equal(5, result2.DriftQuantity);
        }
        #endregion
    }
}
