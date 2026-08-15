using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Mapper;
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
        private readonly IMapper _mapper;
        private readonly DocumentService _service;

        public CostAdjustmentDocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfiles>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            _service = new DocumentService(_mockRepo.Object, _mapper);
        }

        private Document CreateValidCostAdjustmentDocument(decimal newAvgCost = 250000)
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
                        NewAvgCost = newAvgCost, // Giá vốn mới (250,000 VNĐ)
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
            var doc = CreateValidCostAdjustmentDocument(newAvgCost: 250000);
            var bInv = CreateValidBInventory(qty: 100, avg: 200000);

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateCostAdjustmentPendingAsync(doc, 1);

            Assert.NotNull(result);
            Assert.Equal(DocumentType.CostAdjustment, result.Type);
        }

        [Fact]
        public async Task CreateCostAdjustmentPendingAsync_NegativeNewAvgCost_ThrowsArgumentException()
        {
            var doc = CreateValidCostAdjustmentDocument(newAvgCost: -10000); // Negative new avg cost

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(CreateValidBInventory());

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateCostAdjustmentPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateCostAdjustmentPendingAsync_ZeroOrNegativeStockQuantity_ThrowsInvalidOperationException()
        {
            var doc = CreateValidCostAdjustmentDocument(newAvgCost: 250000);
            var zeroStockInventory = CreateValidBInventory(qty: 0, avg: 200000);

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(zeroStockInventory);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateCostAdjustmentPendingAsync(doc, 1));
        }

        [Fact]
        public async Task UpdateCostAdjustmentPendingAsync_ValidDocument_UpdatesSuccessfully()
        {
            var doc = CreateValidCostAdjustmentDocument(newAvgCost: 230000);
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
            var doc = CreateValidCostAdjustmentDocument(newAvgCost: 250000); // 200k -> 250k (+50k * 100 = +5,000,000)
            var bInv = CreateValidBInventory(3, qty: 100, avg: 200000, leftOver: 0);

            _mockRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteCostAdjustmentAsync(90, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(250000, bInv.Avg); // New avg cost = 250,000 VNĐ
            Assert.Equal(0, bInv.LeftOver);
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.Is<InventoryLedger>(l =>
                l.QuantityDelta == 0 &&
                l.InventoryValueDelta == 5000000 &&
                l.RunningAverageCost == 250000
            )), Times.Once);
        }

        [Fact]
        public async Task CompleteCostAdjustmentAsync_NegativeAdjustment_ReducesAvgCostAndLeftOver()
        {
            var doc = CreateValidCostAdjustmentDocument(newAvgCost: 150000); // 200k -> 150k (-50k * 100 = -5,000,000)
            var bInv = CreateValidBInventory(3, qty: 100, avg: 200000, leftOver: 0);

            _mockRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteCostAdjustmentAsync(90, 1);

            Assert.Equal(150000, bInv.Avg);
            Assert.Equal(0, bInv.LeftOver);
        }

        [Fact]
        public async Task CompleteCostAdjustmentAsync_ZeroStockQuantity_ThrowsInvalidOperationException()
        {
            var doc = CreateValidCostAdjustmentDocument(newAvgCost: 150000);
            var bInv = CreateValidBInventory(3, qty: 0, avg: 200000, leftOver: 0);

            _mockRepo.Setup(r => r.GetByIdAsync(90)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3)).ReturnsAsync(bInv);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CompleteCostAdjustmentAsync(90, 1));
        }

        [Fact]
        public async Task CreateCostAdjustmentCompletedAsync_DirectPostsSuccessfully()
        {
            var doc = CreateValidCostAdjustmentDocument(newAvgCost: 120000);
            var bInv = CreateValidBInventory(3, qty: 50, avg: 100000); // 100k -> 120k (+20k * 50 = +1,000,000)

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
