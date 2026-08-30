using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service.Document;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.DocumentServiceTest
{
    public class BatchIntegrationTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly Mock<AutoMapper.IMapper> _mockMapper;
        private readonly Mock<IFefoAllocationService> _mockFefoService;
        private readonly DocumentService _service;

        public BatchIntegrationTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockMapper = new Mock<AutoMapper.IMapper>();
            _mockFefoService = new Mock<IFefoAllocationService>();
            _service = new DocumentService(_mockRepo.Object, _mockMapper.Object, _mockFefoService.Object);
        }

        [Fact]
        public async Task Import_CreatesAndIncrements_BInventoryBatch()
        {
            // Arrange
            long branchId = 1;
            long userId = 100;
            var product = new Product { Id = 5, Name = "Bia Heineken", Type = ProductType.Regular };
            var binventory = new BInventory
            {
                Id = 10,
                BranchId = branchId,
                ProductId = 5,
                Product = product,
                Quantity = 10,
                Avg = 100_000,
                LeftOver = 0
            };

            var doc = new Document
            {
                Id = 1,
                Code = "NK000001",
                BranchId = branchId,
                PartnerId = 99,
                Type = DocumentType.Import,
                Status = DocumentStatus.Completed,
                PostedAt = DateTime.UtcNow,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 101,
                        BInventoryId = 10,
                        Quantity = 5,
                        ConversionRate = 1,
                        BaseQuantity = 5,
                        UnitPrice = 120_000,
                        BatchCodeSnapshot = "LOT-IMP-001",
                        ManufactureDateSnapshot = DateTime.UtcNow.AddDays(-2),
                        ExpiryDateSnapshot = DateTime.UtcNow.AddDays(30)
                    }
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(binventory);
            _mockRepo.Setup(r => r.GetBatchesByInventoryIdAsync(10)).ReturnsAsync(new List<BInventoryBatch>());
            _mockRepo.Setup(r => r.GetBatchByInventoryAndCodeAsync(10, "LOT-IMP-001")).ReturnsAsync((BInventoryBatch?)null);

            BInventoryBatch? addedBatch = null;
            _mockRepo.Setup(r => r.AddBatchAsync(It.IsAny<BInventoryBatch>()))
                .Callback<BInventoryBatch>(b => addedBatch = b)
                .Returns(Task.CompletedTask);

            // Act
            await _service.CreateImportCompletedAsync(doc, userId);

            // Assert
            _mockRepo.Verify(r => r.AddBatchAsync(It.IsAny<BInventoryBatch>()), Times.Once);
            Assert.NotNull(addedBatch);
            Assert.Equal("LOT-IMP-001", addedBatch.BatchCode);
            Assert.Equal(5, addedBatch.QuantityRemaining);
            Assert.Equal(120_000, addedBatch.UnitCost);
            Assert.NotNull(addedBatch.ManufactureDate);
            Assert.NotNull(addedBatch.ExpiryDate);
        }

        [Fact]
        public async Task SaleManual_AllocatesBatches_ViaFefo()
        {
            // Arrange
            long branchId = 1;
            long userId = 100;
            var product = new Product { Id = 5, Name = "Bia Heineken", Type = ProductType.Regular };
            var binventory = new BInventory { Id = 10, BranchId = branchId, ProductId = 5, Product = product, Quantity = 20, Avg = 15_000 };

            var doc = new Document
            {
                Id = 2,
                Code = "HD000001",
                BranchId = branchId,
                Type = DocumentType.Sale,
                Status = DocumentStatus.Completed,
                PostedAt = DateTime.UtcNow,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 201,
                        BInventoryId = 10,
                        Quantity = 3,
                        ConversionRate = 1,
                        BaseQuantity = 3,
                        UnitPrice = 25_000
                    }
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(binventory);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(branchId, 5)).ReturnsAsync(binventory);
            _mockRepo.Setup(r => r.GetBatchesByInventoryIdAsync(10)).ReturnsAsync(new List<BInventoryBatch>());

            var fefoResults = new List<FefoAllocationResult>
            {
                new FefoAllocationResult { BatchId = 1, BatchCode = "LOT-01", QuantityAllocated = 3, UnitCost = 15_000 }
            };
            _mockFefoService.Setup(f => f.AllocateAsync(10, 3)).ReturnsAsync(fefoResults);

            BatchAllocation? addedAlloc = null;
            _mockRepo.Setup(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()))
                .Callback<BatchAllocation>(a => addedAlloc = a)
                .Returns(Task.CompletedTask);

            // Act
            await _service.CreateSaleCompletedAsync(doc, userId);

            // Assert
            _mockFefoService.Verify(f => f.AllocateAsync(10, 3), Times.Once);
            _mockRepo.Verify(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()), Times.Once);
            Assert.NotNull(addedAlloc);
            Assert.Equal(BatchAllocationType.Sale, addedAlloc.AllocationType);
            Assert.Equal(3, addedAlloc.QuantityAllocated);
        }

        [Fact]
        public async Task SupplierReturn_DeductsFromOriginalBatch()
        {
            // Arrange
            long branchId = 1;
            long userId = 100;
            var product = new Product { Id = 5, Name = "Bia Heineken", Type = ProductType.Regular };
            var binventory = new BInventory { Id = 10, BranchId = branchId, ProductId = 5, Product = product, Quantity = 10, Avg = 100_000 };
            var batch = new BInventoryBatch
            {
                Id = 1,
                BInventoryId = 10,
                BatchCode = "LOT-IMP-001",
                QuantityOriginal = 10,
                QuantityRemaining = 10,
                UnitCost = 100_000,
                Status = BatchStatus.Active
            };

            var parentImport = new Document
            {
                Id = 10,
                Code = "NK000001",
                Type = DocumentType.Import,
                Status = DocumentStatus.Completed,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail { Id = 101, BInventoryId = 10, Quantity = 10, BaseQuantity = 10, BatchCodeSnapshot = "LOT-IMP-001" }
                }
            };

            var returnDoc = new Document
            {
                Id = 3,
                Code = "TH000001",
                BranchId = branchId,
                Type = DocumentType.Return,
                Status = DocumentStatus.Completed,
                ParentDocumentId = 10,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 301,
                        FatherId = 101,
                        BInventoryId = 10,
                        Quantity = 4,
                        ConversionRate = 1,
                        BaseQuantity = 4,
                        UnitPrice = 100_000
                    }
                }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentImport);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(binventory);
            _mockRepo.Setup(r => r.GetBatchByInventoryAndCodeAsync(10, "LOT-IMP-001")).ReturnsAsync(batch);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10))
                .ReturnsAsync(new Dictionary<long, decimal>());

            BatchAllocation? addedAlloc = null;
            _mockRepo.Setup(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()))
                .Callback<BatchAllocation>(a => addedAlloc = a)
                .Returns(Task.CompletedTask);

            // Act
            await _service.CreateReturnCompletedAsync(returnDoc, userId);

            // Assert
            Assert.Equal(6, batch.QuantityRemaining);
            _mockRepo.Verify(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()), Times.Once);
            Assert.NotNull(addedAlloc);
            Assert.Equal(BatchAllocationType.ReturnToSupplier, addedAlloc.AllocationType);
            Assert.Equal(4, addedAlloc.QuantityAllocated);
        }

        [Fact]
        public async Task Transfer_SendAndReject_AllocatesAndRestoresBatches()
        {
            // Arrange
            long branchId = 1;
            long toBranchId = 2;
            long userId = 100;
            var binventorySend = new BInventory { Id = 10, BranchId = branchId, ProductId = 5, Quantity = 20, Avg = 50_000 };
            var batch = new BInventoryBatch
            {
                Id = 1,
                BInventoryId = 10,
                BatchCode = "LOT-01",
                QuantityOriginal = 20,
                QuantityRemaining = 15,
                UnitCost = 50_000,
                Status = BatchStatus.Active
            };

            var transferDoc = new Document
            {
                Id = 4,
                Code = "CK000001",
                BranchId = branchId,
                ToBranchId = toBranchId,
                Type = DocumentType.Transfer,
                Status = DocumentStatus.Completed,
                TransferStatus = DocumentTransferStatus.InTransit,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 401,
                        BInventoryId = 10,
                        Quantity = 5,
                        ConversionRate = 1,
                        BaseQuantity = 5,
                        UnitPrice = 50_000,
                        SnapshotAvgCost = 50_000
                    }
                }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(transferDoc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(binventorySend);

            var sendAllocations = new List<BatchAllocation>
            {
                new BatchAllocation
                {
                    Id = 1,
                    DocumentDetailId = 401,
                    BatchId = 1,
                    Batch = batch,
                    AllocationType = BatchAllocationType.TransferOut,
                    QuantityAllocated = 5,
                    UnitCost = 50_000
                }
            };
            _mockRepo.Setup(r => r.GetBatchAllocationsByDetailIdsAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(sendAllocations);

            BatchAllocation? rejectAlloc = null;
            _mockRepo.Setup(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()))
                .Callback<BatchAllocation>(a => rejectAlloc = a)
                .Returns(Task.CompletedTask);

            // Act
            await _service.ProcessTransferReceiptAsync(4, DocumentTransferStatus.Rejected, null, "Không đúng chủng loại", userId);

            // Assert
            Assert.Equal(20, batch.QuantityRemaining); // Restored 15 + 5 = 20
            Assert.Equal(DocumentTransferStatus.Rejected, transferDoc.TransferStatus);
            _mockRepo.Verify(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()), Times.Once);
            Assert.NotNull(rejectAlloc);
            Assert.Equal(BatchAllocationType.TransferReturn, rejectAlloc.AllocationType);
        }

        [Fact]
        public async Task Export_AllocatesBatches_ViaFefo()
        {
            // Arrange
            long branchId = 1;
            long userId = 100;
            var product = new Product { Id = 5, Name = "Trứng gà", Type = ProductType.Regular };
            var binventory = new BInventory { Id = 10, BranchId = branchId, ProductId = 5, Product = product, Quantity = 50, Avg = 3_000 };

            var exportDoc = new Document
            {
                Id = 5,
                Code = "XH000001",
                BranchId = branchId,
                Type = DocumentType.ExportDelete,
                Status = DocumentStatus.Completed,
                PostedAt = DateTime.UtcNow,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 501,
                        BInventoryId = 10,
                        Quantity = 10,
                        ConversionRate = 1,
                        BaseQuantity = 10,
                        UnitPrice = 3_000
                    }
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(binventory);
            var fefoResults = new List<FefoAllocationResult>
            {
                new FefoAllocationResult { BatchId = 2, BatchCode = "LOT-EGG-01", QuantityAllocated = 10, UnitCost = 3_000 }
            };
            _mockFefoService.Setup(f => f.AllocateAsync(10, 10)).ReturnsAsync(fefoResults);

            BatchAllocation? addedAlloc = null;
            _mockRepo.Setup(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()))
                .Callback<BatchAllocation>(a => addedAlloc = a)
                .Returns(Task.CompletedTask);

            // Act
            await _service.CreateExportDeleteCompletedAsync(exportDoc, userId);

            // Assert
            _mockFefoService.Verify(f => f.AllocateAsync(10, 10), Times.Once);
            _mockRepo.Verify(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()), Times.Once);
            Assert.NotNull(addedAlloc);
            Assert.Equal(BatchAllocationType.Disposal, addedAlloc.AllocationType);
            Assert.Equal(10, addedAlloc.QuantityAllocated);
        }

        [Fact]
        public async Task Production_AllocatesIngredientsAndCreatesFinishedBatch()
        {
            // Arrange
            long branchId = 1;
            long userId = 100;
            var productTP = new Product { Id = 20, Name = "Bánh Mì Pate", Type = ProductType.Manufactured, ShelfLifeDays = 2 };
            var binventoryTP = new BInventory { Id = 200, BranchId = branchId, ProductId = 20, Product = productTP, Quantity = 0, Avg = 0 };

            var productNVL = new Product { Id = 30, Name = "Pate", Type = ProductType.Regular };
            var binventoryNVL = new BInventory { Id = 300, BranchId = branchId, ProductId = 30, Product = productNVL, Quantity = 100, Avg = 10_000 };

            var recipes = new List<RecipesDetailed>
            {
                new RecipesDetailed { Id = 1, ParentProductId = 20, IngredientProductId = 30, Quantity = 2, IngredientProduct = productNVL }
            };

            var prdDoc = new Document
            {
                Id = 6,
                Code = "SX000001",
                BranchId = branchId,
                Type = DocumentType.Production,
                Status = DocumentStatus.Completed,
                PostedAt = DateTime.UtcNow,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 601,
                        FatherId = null,
                        BInventoryId = 200,
                        Quantity = 10,
                        ConversionRate = 1,
                        BaseQuantity = 10,
                        SnapshotProductName = "Bánh Mì Pate"
                    }
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(200)).ReturnsAsync(binventoryTP);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(300)).ReturnsAsync(binventoryNVL);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(branchId, 30)).ReturnsAsync(binventoryNVL);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(20)).ReturnsAsync(recipes);

            var fefoResults = new List<FefoAllocationResult>
            {
                new FefoAllocationResult { BatchId = 30, BatchCode = "LOT-PATE-01", QuantityAllocated = 20, UnitCost = 10_000 }
            };
            _mockFefoService.Setup(f => f.AllocateAsync(300, 20)).ReturnsAsync(fefoResults);

            BInventoryBatch? prdBatch = null;
            _mockRepo.Setup(r => r.AddBatchAsync(It.IsAny<BInventoryBatch>()))
                .Callback<BInventoryBatch>(b => prdBatch = b)
                .Returns(Task.CompletedTask);

            var addedAllocations = new List<BatchAllocation>();
            _mockRepo.Setup(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()))
                .Callback<BatchAllocation>(a => addedAllocations.Add(a))
                .Returns(Task.CompletedTask);

            // Act
            await _service.CreateProductionCompletedAsync(prdDoc, userId);

            // Assert
            _mockFefoService.Verify(f => f.AllocateAsync(300, 20), Times.Once);
            Assert.Single(addedAllocations);
            Assert.Equal(BatchAllocationType.ProductionConsumption, addedAllocations[0].AllocationType);
            Assert.Equal(20, addedAllocations[0].QuantityAllocated);

            _mockRepo.Verify(r => r.AddBatchAsync(It.IsAny<BInventoryBatch>()), Times.Once);
            Assert.NotNull(prdBatch);
            Assert.Equal(10, prdBatch.QuantityRemaining);
            Assert.NotNull(prdBatch.ExpiryDate);
        }

        [Fact]
        public async Task StockCheck_BalancesBatchQuantity_WhenDiscrepancyExists()
        {
            // Arrange
            long branchId = 1;
            long userId = 100;
            var product = new Product { Id = 5, Name = "Đường Trắng", Type = ProductType.Regular };
            var binventory = new BInventory { Id = 10, BranchId = branchId, ProductId = 5, Product = product, Quantity = 100, Avg = 20_000 };

            var batch = new BInventoryBatch
            {
                Id = 1,
                BInventoryId = 10,
                BatchCode = "LOT-SUGAR-01",
                QuantityOriginal = 100,
                QuantityRemaining = 100,
                UnitCost = 20_000,
                Status = BatchStatus.Active,
                ReceivedDate = DateTime.UtcNow
            };

            var checkDoc = new Document
            {
                Id = 7,
                Code = "KK000001",
                BranchId = branchId,
                Type = DocumentType.Check,
                Status = DocumentStatus.Completed,
                PostedAt = DateTime.UtcNow,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 701,
                        BInventoryId = 10,
                        Quantity = 95, // Giảm 5 so với tồn hệ thống
                        ConversionRate = 1,
                        BaseQuantity = 95,
                        ActualQuantity = 95,
                        SystemQuantity = 100,
                        UnitPrice = 20_000
                    }
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(binventory);
            _mockRepo.Setup(r => r.GetBatchesByInventoryIdAsync(10)).ReturnsAsync(new List<BInventoryBatch> { batch });

            BatchAllocation? addedAlloc = null;
            _mockRepo.Setup(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()))
                .Callback<BatchAllocation>(a => addedAlloc = a)
                .Returns(Task.CompletedTask);

            // Act
            await _service.CreateCheckCompletedAsync(checkDoc, userId);

            // Assert
            Assert.Equal(95, binventory.Quantity);
            Assert.Equal(95, batch.QuantityRemaining);
            _mockRepo.Verify(r => r.AddBatchAllocationAsync(It.IsAny<BatchAllocation>()), Times.Once);
            Assert.NotNull(addedAlloc);
            Assert.Equal(BatchAllocationType.StockCheck, addedAlloc.AllocationType);
            Assert.Equal(-5, addedAlloc.QuantityAllocated);
        }
    }
}
