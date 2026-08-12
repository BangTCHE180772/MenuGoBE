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
    public class ReturnDocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly DocumentService _service;

        public ReturnDocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfiles>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            _service = new DocumentService(_mockRepo.Object, _mapper);
        }

        private Document CreateValidImportDocument()
        {
            return new Document
            {
                Id = 10,
                BranchId = 1,
                PartnerId = 100,
                Code = "NH000010",
                Type = DocumentType.Import,
                Status = DocumentStatus.Completed,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 1001,
                        DocumentId = 10,
                        BInventoryId = 50,
                        Quantity = 100,
                        ConversionRate = 1,
                        BaseQuantity = 100,
                        UnitPrice = 10000,
                        SnapshotProductName = "Nước ngọt Cốc",
                        SnapshotProductCode = "COCA",
                        SnapshotUnitName = "Lon",
                        SnapshotBaseUnitName = "Lon"
                    }
                }
            };
        }

        private Document CreateValidReturnDocument(long parentId = 10)
        {
            return new Document
            {
                Id = 20,
                BranchId = 1,
                ParentDocumentId = parentId,
                Code = "TH000020",
                Type = DocumentType.Return,
                Status = DocumentStatus.Completed,
                AmountPaid = 200000,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        FatherId = 1001,
                        Quantity = 20,
                        UnitPrice = 10000
                    }
                }
            };
        }

        private BInventory CreateValidBInventory(long id = 50, decimal qty = 100)
        {
            return new BInventory
            {
                Id = id,
                BranchId = 1,
                ProductId = 5,
                Quantity = qty,
                Avg = 10000,
                LeftOver = 0
            };
        }

        #region 1. Return Validation & Parent Dependency Tests (5 Cases)
        [Fact]
        public async Task CreateReturnCompletedAsync_NullDocument_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(null!, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_MissingParentDocumentId_ThrowsArgumentException()
        {
            var returnDoc = CreateValidReturnDocument();
            returnDoc.ParentDocumentId = null; // Missing

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ParentNotFound_ThrowsKeyNotFoundException()
        {
            var returnDoc = CreateValidReturnDocument(parentId: 999);
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Document?)null);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ParentNotImportType_ThrowsKeyNotFoundException()
        {
            var parentDoc = CreateValidImportDocument();
            parentDoc.Type = DocumentType.Sale; // Not Import

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);

            var returnDoc = CreateValidReturnDocument();
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ParentNotCompleted_ThrowsInvalidOperationException()
        {
            var parentDoc = CreateValidImportDocument();
            parentDoc.Status = DocumentStatus.Pending; // Not Completed

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);

            var returnDoc = CreateValidReturnDocument();
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }
        #endregion

        #region 2. Return Details & FatherId Binding Tests (5 Cases)
        [Fact]
        public async Task CreateReturnCompletedAsync_EmptyDetails_ThrowsArgumentException()
        {
            var parentDoc = CreateValidImportDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails = new List<DocumentDetail>();

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_AllDetailsZeroQuantity_ThrowsArgumentException()
        {
            var parentDoc = CreateValidImportDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().Quantity = 0; // All <= 0

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_MissingFatherId_ThrowsArgumentException()
        {
            var parentDoc = CreateValidImportDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().FatherId = null;

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_FatherIdNotFoundInParent_ThrowsKeyNotFoundException()
        {
            var parentDoc = CreateValidImportDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().FatherId = 8888; // Non-existent

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ExceedsMaxUnitPrice_ThrowsArgumentException()
        {
            var parentDoc = CreateValidImportDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(new Dictionary<long, decimal>());

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().UnitPrice = 20_000_000_000m; // > 10 billion

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }
        #endregion

        #region 3. Return Quantity Limits & Stock Checks (5 Cases)
        [Fact]
        public async Task CreateReturnCompletedAsync_ExceedsSingleReturnImportQty_ThrowsArgumentException()
        {
            var parentDoc = CreateValidImportDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(new Dictionary<long, decimal>());

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().Quantity = 150; // > 100 import qty

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ExceedsCumulativeReturnedQty_ThrowsArgumentException()
        {
            var parentDoc = CreateValidImportDocument();
            var prevReturned = new Dictionary<long, decimal> { { 1001, 80m } }; // 80 returned previously out of 100

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(prevReturned);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().Quantity = 30; // 80 + 30 = 110 > 100

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ParentFullyReturned_ThrowsInvalidOperationException()
        {
            var parentDoc = CreateValidImportDocument();
            var prevReturned = new Dictionary<long, decimal> { { 1001, 100m } }; // Fully returned (100/100)

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(prevReturned);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().Quantity = 5;

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_InsufficientBranchInventory_ThrowsInvalidOperationException()
        {
            var parentDoc = CreateValidImportDocument();
            var bInv = CreateValidBInventory(qty: 10); // Only 10 in stock

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(new Dictionary<long, decimal>());
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(50)).ReturnsAsync(bInv);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().Quantity = 20; // Wants to return 20, but stock is 10

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateReturnCompletedAsync(returnDoc, 1));
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ValidReturn_DeductsStockAndZeroesLeftOverWhenEmpty()
        {
            var parentDoc = CreateValidImportDocument();
            var bInv = CreateValidBInventory(qty: 20); // Exactly 20 in stock
            bInv.LeftOver = 500;

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(new Dictionary<long, decimal>());
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(50)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            InventoryLedger? capturedLedger = null;
            _mockRepo.Setup(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()))
                .Callback<InventoryLedger>(l => capturedLedger = l)
                .Returns(Task.CompletedTask);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().Quantity = 20;

            var result = await _service.CreateReturnCompletedAsync(returnDoc, 1);

            Assert.Equal(0, bInv.Quantity);
            Assert.Equal(0, bInv.LeftOver); // LeftOver zeroed
            Assert.NotNull(capturedLedger);
            Assert.Equal(-200500m, capturedLedger!.InventoryValueDelta); // 20 * 10000 + 500 = 200500
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
        }
        #endregion

        #region 4. Return CashFlow & Custom UnitPrice Tests (5 Cases)
        [Fact]
        public async Task CreateReturnCompletedAsync_UserSpecifiedCustomUnitPrice_UsesCustomUnitPrice()
        {
            var parentDoc = CreateValidImportDocument();
            var bInv = CreateValidBInventory(qty: 100);

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(new Dictionary<long, decimal>());
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(50)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().UnitPrice = 12000; // Custom UnitPrice higher than import 10000

            var result = await _service.CreateReturnCompletedAsync(returnDoc, 1);

            Assert.Equal(12000, result.DocumentDetails.First().UnitPrice);
            Assert.Equal(240000, result.TotalAmount); // 20 * 12000
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ZeroUnitPrice_DefaultsToFatherUnitPrice()
        {
            var parentDoc = CreateValidImportDocument();
            var bInv = CreateValidBInventory(qty: 100);

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(new Dictionary<long, decimal>());
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(50)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.DocumentDetails.First().UnitPrice = 0; // Zero -> default to father (10000)

            var result = await _service.CreateReturnCompletedAsync(returnDoc, 1);

            Assert.Equal(10000, result.DocumentDetails.First().UnitPrice);
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_AmountPaidGreaterThanZero_CreatesInflowRefundCashFlow()
        {
            var parentDoc = CreateValidImportDocument();
            var bInv = CreateValidBInventory(qty: 100);

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(new Dictionary<long, decimal>());
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(50)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.AmountPaid = 200000;

            await _service.CreateReturnCompletedAsync(returnDoc, 1);

            _mockRepo.Verify(r => r.AddCashFlowAsync(It.Is<CashFlow>(cf =>
                cf.Direction == CashFlowDirection.Inflow &&
                cf.Type == CashFlowDetailType.Refund &&
                cf.TotalAmount == 200000
            )), Times.Once);
        }

        [Fact]
        public async Task CreateReturnCompletedAsync_ZeroAmountPaid_DoesNotCreateCashFlow()
        {
            var parentDoc = CreateValidImportDocument();
            var bInv = CreateValidBInventory(qty: 100);

            _mockRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(parentDoc);
            _mockRepo.Setup(r => r.GetPreviouslyReturnedQuantitiesAsync(10)).ReturnsAsync(new Dictionary<long, decimal>());
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(50)).ReturnsAsync(bInv);

            var returnDoc = CreateValidReturnDocument();
            returnDoc.AmountPaid = 0; // No CashFlow

            await _service.CreateReturnCompletedAsync(returnDoc, 1);

            _mockRepo.Verify(r => r.AddCashFlowAsync(It.IsAny<CashFlow>()), Times.Never);
        }

        [Fact]
        public async Task GetReturnListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidReturnDocument() };
            _mockRepo.Setup(r => r.GetReturnDocumentsAsync(1)).ReturnsAsync(list);

            var result = await _service.GetReturnListAsync(1);

            Assert.Single(result);
        }
        #endregion
    }
}
