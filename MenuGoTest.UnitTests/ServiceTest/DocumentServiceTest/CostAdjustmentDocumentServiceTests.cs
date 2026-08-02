using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service.Document;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.DocumentServiceTest
{
    public class CostAdjustmentDocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly DocumentService _service;

        public CostAdjustmentDocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
            _service = new DocumentService(_mockRepo.Object);
        }

        private Document CreateValidCostAdjustmentDocument(decimal unitPriceAdjustment = 5000000)
        {
            return new Document
            {
                Id = 90,
                BranchId = 1,
                Code = "DC000090",
                Type = DocumentType.CostAdjustment,
                Status = DocumentStatus.Pending,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 901,
                        BInventoryId = 3,
                        Quantity = 1,
                        UnitPrice = unitPriceAdjustment, // V_adj (+5 triệu hoặc -2 triệu)
                        ConversionRate = 1,
                        BaseQuantity = 1,
                        SnapshotProductName = "Rượu vang Pháp"
                    }
                }
            };
        }

        private BInventory CreateValidBInventory(long id = 3, decimal qty = 100, decimal avg = 200000, decimal leftOver = 0)
        {
            return new BInventory
            {
                Id = id,
                BranchId = 1,
                ProductId = 30,
                Quantity = qty, // Total stock value = 100 * 200,000 = 20,000,000 VNĐ
                Avg = avg,
                LeftOver = leftOver,
                Product = new Product
                {
                    Id = 30,
                    Name = "Rượu vang Pháp",
                    Type = ProductType.Regular
                }
            };
        }

        #region 1. CostAdjustment Pending & Limits Validation Tests (5 Cases)
        [Fact]
        public async Task CreateCostAdjustmentPendingAsync_ValidDocument_CreatesSuccessfully()
        {
            var doc = CreateValidCostAdjustmentDocument();
            var bInv = CreateValidBInventory();

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateCostAdjustmentPendingAsync(doc, 1);

            Assert.NotNull(result);
            Assert.Equal(DocumentType.CostAdjustment, result.Type);
        }

        [Fact]
        public async Task CreateCostAdjustmentPendingAsync_Exceeds10BillionPositiveLimit_ThrowsArgumentException()
        {
            var doc = CreateValidCostAdjustmentDocument(unitPriceAdjustment: 15_000_000_000m); // > +10 billion

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(CreateValidBInventory());

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCostAdjustmentPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateCostAdjustmentPendingAsync_Exceeds10BillionNegativeLimit_ThrowsArgumentException()
        {
            var doc = CreateValidCostAdjustmentDocument(unitPriceAdjustment: -15_000_000_000m); // < -10 billion

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(CreateValidBInventory());

            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateCostAdjustmentPendingAsync(doc, 1));
        }

        [Fact]
        public async Task UpdateCostAdjustmentPendingAsync_ValidDocument_UpdatesSuccessfully()
        {
            var doc = CreateValidCostAdjustmentDocument();
            var bInv = CreateValidBInventory();

            _mockRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.UpdateCostAdjustmentPendingAsync(90, doc, 1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SoftDeleteCostAdjustmentPendingAsync_ValidPendingDocument_ReturnsTrue()
        {
            var doc = CreateValidCostAdjustmentDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(doc);

            var success = await _service.SoftDeleteCostAdjustmentPendingAsync(90, 1, "Hủy điều chỉnh");
            Assert.True(success);
        }
        #endregion

        #region 2. CostAdjustment Complete & Negative Stock Value Guard Tests (5 Cases)
        [Fact]
        public async Task CompleteCostAdjustmentAsync_PositiveAdjustment_UpdatesAvgCostAndLeftOver()
        {
            var doc = CreateValidCostAdjustmentDocument(unitPriceAdjustment: 5000000); // +5,000,000 VNĐ
            var bInv = CreateValidBInventory(3, qty: 100, avg: 200000, leftOver: 0); // Initial value = 20M. New value = 25M

            _mockRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteCostAdjustmentAsync(90, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(250000, bInv.Avg); // 25,000,000 / 100 = 250,000 VNĐ
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.Is<InventoryLedger>(l =>
                l.QuantityDelta == 0 &&
                l.InventoryValueDelta == 5000000 &&
                l.RunningAverageCost == 250000
            )), Times.Once);
        }

        [Fact]
        public async Task CompleteCostAdjustmentAsync_NegativeAdjustment_ReducesAvgCostAndLeftOver()
        {
            var doc = CreateValidCostAdjustmentDocument(unitPriceAdjustment: -5000000); // -5,000,000 VNĐ
            var bInv = CreateValidBInventory(3, qty: 100, avg: 200000, leftOver: 0); // Initial 20M -> New 15M

            _mockRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteCostAdjustmentAsync(90, 1);

            Assert.Equal(150000, bInv.Avg); // 15,000,000 / 100 = 150,000 VNĐ
        }

        [Fact]
        public async Task CompleteCostAdjustmentAsync_NegativeAdjustmentCausingNegativeStockValue_ThrowsInvalidOperationException()
        {
            var doc = CreateValidCostAdjustmentDocument(unitPriceAdjustment: -25_000_000m); // -25M on a 20M stock -> Negative value (-5M)
            var bInv = CreateValidBInventory(3, qty: 100, avg: 200000, leftOver: 0); // Stock value = 20M

            _mockRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteCostAdjustmentAsync(90, 1));
        }

        [Fact]
        public async Task CreateCostAdjustmentCompletedAsync_DirectPostsSuccessfully()
        {
            var doc = CreateValidCostAdjustmentDocument(unitPriceAdjustment: 1000000);
            var bInv = CreateValidBInventory(3, qty: 50, avg: 100000); // Total 5M -> New 6M -> Avg 120,000

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateCostAdjustmentCompletedAsync(doc, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(120000, bInv.Avg);
        }

        [Fact]
        public async Task GetCostAdjustmentListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidCostAdjustmentDocument() };
            _mockRepo.Setup(r => r.GetCostAdjustmentDocumentsAsync(1)).ReturnsAsync(list);

            var result = await _service.GetCostAdjustmentListAsync(1);
            Assert.Single(result);
        }
        #endregion
    }
}
