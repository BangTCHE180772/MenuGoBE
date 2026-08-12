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
    public class CheckDocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly DocumentService _service;

        public CheckDocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfiles>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            _service = new DocumentService(_mockRepo.Object, _mapper);
        }

        private Document CreateValidCheckDocument()
        {
            return new Document
            {
                Id = 80,
                BranchId = 1,
                Code = "KK000080",
                Type = DocumentType.Check,
                Status = DocumentStatus.Pending,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 801,
                        BInventoryId = 5,
                        Quantity = 80, // Tồn thực tế kiểm kê = 80
                        ConversionRate = 1,
                        BaseQuantity = 80,
                        UnitPrice = 15000,
                        SnapshotProductName = "Bột năng"
                    }
                }
            };
        }

        private BInventory CreateValidBInventory(long id = 5, decimal qty = 100)
        {
            return new BInventory
            {
                Id = id,
                BranchId = 1,
                ProductId = 50,
                Quantity = qty, // Tồn hệ thống trước kiểm kê = 100
                Avg = 15000,
                LeftOver = 200,
                Product = new Product
                {
                    Id = 50,
                    Name = "Bột năng",
                    Type = ProductType.Ingredient
                }
            };
        }

        #region 1. Check Pending & Base Rules Tests (5 Cases)
        [Fact]
        public async Task CreateCheckPendingAsync_ValidDocument_CreatesSuccessfully()
        {
            var doc = CreateValidCheckDocument();
            var bInv = CreateValidBInventory();

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateCheckPendingAsync(doc, 1);

            Assert.NotNull(result);
            Assert.Equal(DocumentType.Check, result.Type);
        }

        [Fact]
        public async Task CreateCheckPendingAsync_ExceedsNoteLength_ThrowsArgumentException()
        {
            var doc = CreateValidCheckDocument();
            doc.Note = new string('C', 256);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateCheckPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateCheckPendingAsync_NegativeActualQuantity_ThrowsArgumentException()
        {
            var doc = CreateValidCheckDocument();
            doc.DocumentDetails.First().Quantity = -5; // Negative stock not allowed

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(CreateValidBInventory(5));

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateCheckPendingAsync(doc, 1));
        }

        [Fact]
        public async Task UpdateCheckPendingAsync_ValidDocument_UpdatesSuccessfully()
        {
            var doc = CreateValidCheckDocument();
            var bInv = CreateValidBInventory();

            _mockRepo.Setup(r => r.GetByIdAsync(80)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.UpdateCheckPendingAsync(80, doc, 1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SoftDeleteCheckPendingAsync_ValidPendingDocument_ReturnsTrue()
        {
            var doc = CreateValidCheckDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(80)).ReturnsAsync(doc);

            var success = await _service.SoftDeleteCheckPendingAsync(80, 1, "Hủy kiểm kho");
            Assert.True(success);
        }
        #endregion

        #region 2. Check Complete & Inventory Adjustment Tests (5 Cases)
        [Fact]
        public async Task CompleteCheckAsync_NegativeVariance_ReducesStockAndPostsDeltaLedger()
        {
            var doc = CreateValidCheckDocument(); // Actual = 80
            var bInv = CreateValidBInventory(5, qty: 100); // System = 100 -> Variance = 80 - 100 = -20

            _mockRepo.Setup(r => r.GetByIdAsync(80)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteCheckAsync(80, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(80, bInv.Quantity); // Adjusted to actual 80
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.Is<InventoryLedger>(l =>
                l.QuantityDelta == -20 &&
                l.RunningQuantity == 80
            )), Times.Once);
        }

        [Fact]
        public async Task CompleteCheckAsync_PositiveVariance_IncreasesStockAndPostsDeltaLedger()
        {
            var doc = CreateValidCheckDocument();
            doc.DocumentDetails.First().Quantity = 120; // Actual = 120
            var bInv = CreateValidBInventory(5, qty: 100); // System = 100 -> Variance = +20

            _mockRepo.Setup(r => r.GetByIdAsync(80)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteCheckAsync(80, 1);

            Assert.Equal(120, bInv.Quantity); // Adjusted to actual 120
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.Is<InventoryLedger>(l =>
                l.QuantityDelta == 20 &&
                l.RunningQuantity == 120
            )), Times.Once);
        }

        [Fact]
        public async Task CompleteCheckAsync_ActualQuantityZero_ZeroesLeftOver()
        {
            var doc = CreateValidCheckDocument();
            doc.DocumentDetails.First().Quantity = 0; // Actual = 0
            var bInv = CreateValidBInventory(5, qty: 50);
            bInv.LeftOver = 400;

            _mockRepo.Setup(r => r.GetByIdAsync(80)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(bInv);

            await _service.CompleteCheckAsync(80, 1);

            Assert.Equal(0, bInv.Quantity);
            Assert.Equal(0, bInv.LeftOver); // LeftOver zeroed
        }

        [Fact]
        public async Task CreateCheckCompletedAsync_DirectPostsSuccessfully()
        {
            var doc = CreateValidCheckDocument();
            var bInv = CreateValidBInventory(5, qty: 100);

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateCheckCompletedAsync(doc, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(80, bInv.Quantity);
        }

        [Fact]
        public async Task GetCheckListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidCheckDocument() };
            _mockRepo.Setup(r => r.GetCheckDocumentsAsync(1)).ReturnsAsync(list);

            var result = await _service.GetCheckListAsync(1);
            Assert.Single(result);
        }

        [Fact]
        public async Task CreateCheckCompletedAsync_WithUnitConversion_PositiveVariance_CalculatesBaseActualAndTotalVarianceAmount()
        {
            var doc = CreateValidCheckDocument();
            doc.DocumentDetails.First().UnitConversionId = 10;
            doc.DocumentDetails.First().Quantity = 12; // 12 thùng

            var bInv = CreateValidBInventory(5, qty: 100); // System = 100 chai
            bInv.Avg = 15000;

            var unitConversion = new UnitConversion
            {
                Id = 10,
                ProductId = 50,
                ConversionPoint = 10 // 10 chai/thùng
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(10)).ReturnsAsync(unitConversion);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateCheckCompletedAsync(doc, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            // 12 thùng * 10 chai/thùng = 120 chai thực tế
            Assert.Equal(120, result.DocumentDetails.First().BaseQuantity);
            Assert.Equal(120, result.DocumentDetails.First().ActualQuantity);
            Assert.Equal(120, bInv.Quantity);

            // Variance = 120 - 100 = +20 chai
            // Total amount = +20 * 15,000 = 300,000
            Assert.Equal(300000m, result.TotalAmount);

            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.Is<InventoryLedger>(l =>
                l.QuantityDelta == 20 &&
                l.InventoryValueDelta == 300000m &&
                l.RunningQuantity == 120
            )), Times.Once);
        }

        [Fact]
        public async Task CompleteCheckAsync_WithUnitConversion_NegativeVariance_CalculatesBaseActualAndTotalVarianceAmount()
        {
            var doc = CreateValidCheckDocument();
            doc.DocumentDetails.First().UnitConversionId = 10;
            doc.DocumentDetails.First().Quantity = 8; // 8 thùng

            var bInv = CreateValidBInventory(5, qty: 100); // System = 100 chai
            bInv.Avg = 15000;

            var unitConversion = new UnitConversion
            {
                Id = 10,
                ProductId = 50,
                ConversionPoint = 10 // 10 chai/thùng
            };

            _mockRepo.Setup(r => r.GetByIdAsync(80)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(5)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(10)).ReturnsAsync(unitConversion);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteCheckAsync(80, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            // 8 thùng * 10 chai/thùng = 80 chai thực tế
            Assert.Equal(80, result.DocumentDetails.First().BaseQuantity);
            Assert.Equal(80, result.DocumentDetails.First().ActualQuantity);
            Assert.Equal(80, bInv.Quantity);

            // Variance = 80 - 100 = -20 chai
            // Total amount = -20 * 15,000 = -300,000
            Assert.Equal(-300000m, result.TotalAmount);

            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.Is<InventoryLedger>(l =>
                l.QuantityDelta == -20 &&
                l.InventoryValueDelta == -300000m &&
                l.RunningQuantity == 80
            )), Times.Once);
        }
        #endregion
    }
}
