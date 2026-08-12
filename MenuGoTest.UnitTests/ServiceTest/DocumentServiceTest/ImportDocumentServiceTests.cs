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
    public class ImportDocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly DocumentService _service;

        public ImportDocumentServiceTests()
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
                Id = 1,
                BranchId = 10,
                PartnerId = 20,
                Code = "NH000001",
                Type = DocumentType.Import,
                Status = DocumentStatus.Pending,
                Note = "Ghi chú hợp lệ",
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 101,
                        BInventoryId = 1,
                        Quantity = 10,
                        UnitPrice = 50000,
                        UnitConversionId = 1,
                        ConversionRate = 1,
                        SnapshotProductName = "Thịt bò"
                    }
                }
            };
        }

        private BInventory CreateValidBInventory(long id = 1, decimal qty = 50, decimal avg = 40000, decimal leftOver = 0)
        {
            return new BInventory
            {
                Id = id,
                BranchId = 10,
                ProductId = 100,
                Quantity = qty,
                Avg = avg,
                LeftOver = leftOver,
                Product = new Product
                {
                    Id = 100,
                    Name = "Thịt bò",
                    Type = ProductType.Regular
                }
            };
        }

        #region 1. Import Pending Creation & Base Validation Tests (5 Cases)
        [Fact]
        public async Task CreateImportPendingAsync_NullDocument_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(null!, 1));
        }

        [Fact]
        public async Task CreateImportPendingAsync_ExceedsNoteLength_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            doc.Note = new string('A', 256);
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateImportPendingAsync_ExceedsDeleteNoteLength_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            doc.DeleteNote = new string('B', 256);
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateImportPendingAsync_EmptyDetails_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            doc.DocumentDetails = new List<DocumentDetail>();
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateImportPendingAsync_DuplicateBInventoryDetails_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            doc.DocumentDetails.Add(new DocumentDetail
            {
                BInventoryId = 1,
                Quantity = 5,
                UnitPrice = 10000
            });
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }
        #endregion

        #region 2. Import Validation Rules & Product Types (5 Cases)
        [Fact]
        public async Task CreateImportPendingAsync_InvalidQuantity_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            doc.DocumentDetails.First().Quantity = 0; // Less than 0.001

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(CreateValidBInventory());
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 });

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateImportPendingAsync_QuantityExceedsMax_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            doc.DocumentDetails.First().Quantity = 100_000_000_000m; // > 10 billion

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(CreateValidBInventory());
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 });

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateImportPendingAsync_UnitPriceExceedsMax_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            doc.DocumentDetails.First().UnitPrice = 20_000_000_000m; // > 10 billion

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(CreateValidBInventory());
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 });

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateImportPendingAsync_InvalidProductType_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            var bInv = CreateValidBInventory();
            bInv.Product.Type = ProductType.Processed; // Processed product cannot be imported

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateImportPendingAsync_InvalidUnitConversionId_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            var bInv = CreateValidBInventory();
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync((UnitConversion?)null);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }
        #endregion

        #region 3. Import Code Generation & Pending Operations (5 Cases)
        [Fact]
        public async Task CreateImportPendingAsync_ValidDocument_SetsPendingStateAndGeneratesCode()
        {
            var doc = CreateValidImportDocument();
            doc.Code = "";
            var bInv = CreateValidBInventory();
            var unitConv = new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(unitConv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateImportPendingAsync(doc, 1);

            Assert.NotNull(result);
            Assert.Equal(DocumentStatus.Pending, result.Status);
            Assert.Equal(DocumentType.Import, result.Type);
        }

        [Fact]
        public async Task CreateImportPendingAsync_ExistingDuplicateCode_ThrowsInvalidOperationException()
        {
            var doc = CreateValidImportDocument();
            var bInv = CreateValidBInventory();
            var unitConv = new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(unitConv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync("NH000001", doc.Id)).ReturnsAsync(true);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateImportPendingAsync(doc, 1));
        }

        [Fact]
        public async Task UpdateImportPendingAsync_ValidDocument_UpdatesSuccessfully()
        {
            var doc = CreateValidImportDocument();
            var bInv = CreateValidBInventory();
            var unitConv = new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(unitConv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.UpdateImportPendingAsync(1, doc, 1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SoftDeleteImportPendingAsync_ValidPendingDocument_ReturnsTrue()
        {
            var doc = CreateValidImportDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);

            var success = await _service.SoftDeleteImportPendingAsync(1, 1, "Hủy thử nghiệm");
            Assert.True(success);
        }

        [Fact]
        public async Task GetImportListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidImportDocument() };
            _mockRepo.Setup(r => r.GetImportDocumentsAsync(10)).ReturnsAsync(list);

            var result = await _service.GetImportListAsync(10);
            Assert.Single(result);
        }
        #endregion

        #region 4. Import Complete & Average Cost Calculation Tests (5 Cases)
        [Fact]
        public async Task CompleteImportAsync_MissingSupplier_ThrowsArgumentException()
        {
            var doc = CreateValidImportDocument();
            doc.PartnerId = null; // Missing supplier

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(CreateValidBInventory());
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 });

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CompleteImportAsync(1, 1));
        }

        [Fact]
        public async Task CompleteImportAsync_LockedCompletedDocument_ThrowsInvalidOperationException()
        {
            var doc = CreateValidImportDocument();
            doc.Status = DocumentStatus.Completed; // Locked

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CompleteImportAsync(1, 1));
        }

        [Fact]
        public async Task CompleteImportAsync_ValidPendingDocument_CalculatesAverageCostAndPosts()
        {
            var doc = CreateValidImportDocument();
            doc.AmountPaid = 500000;
            var bInv = CreateValidBInventory(qty: 10, avg: 40000, leftOver: 0);
            var unitConv = new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(unitConv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteImportAsync(1, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
            _mockRepo.Verify(r => r.AddCashFlowAsync(It.IsAny<CashFlow>()), Times.Once);
        }

        [Fact]
        public async Task CompleteImportAsync_ZeroPriorInventory_RefreshesAverageCostToNewImportBatch()
        {
            var doc = CreateValidImportDocument();
            // Quantity = 10, UnitPrice = 50000 -> UnitCost = 50000
            doc.DocumentDetails.First().Quantity = 10;
            doc.DocumentDetails.First().UnitPrice = 50000;
            
            // Prior inventory is 0, old Avg was 100000, old LeftOver was 0.5
            var bInv = CreateValidBInventory(qty: 0, avg: 100000, leftOver: 0.5m);
            var unitConv = new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(unitConv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteImportAsync(1, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(50000m, bInv.Avg); // Avg refreshed to 50,000 (new import unit cost)
            Assert.Equal(0m, bInv.LeftOver); // LeftOver reset to 0
            Assert.Equal(10m, bInv.Quantity); // Quantity = 10
        }

        [Fact]
        public async Task CreateImportCompletedAsync_ValidDocument_DirectPostsSuccessfully()
        {
            var doc = CreateValidImportDocument();
            doc.AmountPaid = 0; // No CashFlow
            var bInv = CreateValidBInventory();
            var unitConv = new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(unitConv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateImportCompletedAsync(doc, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
            _mockRepo.Verify(r => r.AddCashFlowAsync(It.IsAny<CashFlow>()), Times.Never);
        }

        [Fact]
        public async Task CompleteImportAsync_ZeroExistingQty_RecalculatesAvgCorrectly()
        {
            var doc = CreateValidImportDocument();
            var bInv = CreateValidBInventory(qty: 0, avg: 0, leftOver: 0);
            var unitConv = new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(unitConv);

            var result = await _service.CompleteImportAsync(1, 1);

            Assert.Equal(50000, bInv.Avg);
            Assert.Equal(10, bInv.Quantity);
        }
        #endregion

        #region 5. Import CashFlow & Snapshot Freeze Edge Cases (5 Cases)
        [Fact]
        public async Task CreateImportCashFlow_AmountPaidGreaterThanZero_CreatesCashFlowOutflow()
        {
            var doc = CreateValidImportDocument();
            doc.AmountPaid = 100000;
            var bInv = CreateValidBInventory();
            var unitConv = new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(unitConv);

            await _service.CreateImportCompletedAsync(doc, 1);

            _mockRepo.Verify(r => r.AddCashFlowAsync(It.Is<CashFlow>(cf =>
                cf.Direction == CashFlowDirection.Outflow &&
                cf.TotalAmount == 100000 &&
                cf.Type == CashFlowDetailType.Payment
            )), Times.Once);
        }

        [Fact]
        public async Task FreezeDocumentSnapshotsAsync_FillsAllSnapshotFields()
        {
            var doc = CreateValidImportDocument();
            var branch = new Branch { Id = 10, Name = "Chi nhánh 1" };
            var partner = new Partner { Id = 20, Name = "Nhà cung cấp A" };
            var account = new Account { Id = 1, Name = "Admin Nguyễn" };
            var bInv = CreateValidBInventory();

            _mockRepo.Setup(r => r.GetBranchByIdAsync(10)).ReturnsAsync(branch);
            _mockRepo.Setup(r => r.GetPartnerByIdAsync(20)).ReturnsAsync(partner);
            _mockRepo.Setup(r => r.GetAccountByIdAsync(1)).ReturnsAsync(account);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 });

            await _service.CreateImportCompletedAsync(doc, 1);

            Assert.Equal("Chi nhánh 1", doc.SnapshotBranchName);
            Assert.Equal("Nhà cung cấp A", doc.SnapshotPartnerName);
            Assert.Equal("Admin Nguyễn", doc.SnapshotCreatedByName);
        }

        [Fact]
        public async Task CreateImportCompletedAsync_CalculatesBaseQuantityWithConversionRate()
        {
            var doc = CreateValidImportDocument();
            doc.DocumentDetails.First().Quantity = 2; // 2 Thùng
            doc.DocumentDetails.First().UnitConversionId = 2;
            var bInv = CreateValidBInventory();
            var unitConv = new UnitConversion { Id = 2, ProductId = 100, ConversionPoint = 24 }; // 1 Thùng = 24 Chai

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(2)).ReturnsAsync(unitConv);

            await _service.CreateImportCompletedAsync(doc, 1);

            Assert.Equal(48, doc.DocumentDetails.First().BaseQuantity); // 2 * 24 = 48
        }

        [Fact]
        public async Task CompleteImportAsync_NonExistentBInventory_ThrowsKeyNotFoundException()
        {
            var doc = CreateValidImportDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync((BInventory?)null);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CompleteImportAsync(1, 1));
        }

        [Fact]
        public async Task CreateImportCompletedAsync_ZeroAmountPaid_DoesNotCreatePaymentCashFlow()
        {
            var doc = CreateValidImportDocument();
            doc.AmountPaid = 0;
            var bInv = CreateValidBInventory();
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 });

            await _service.CreateImportCompletedAsync(doc, 1);

            _mockRepo.Verify(r => r.AddCashFlowAsync(It.Is<CashFlow>(cf => cf.Type == CashFlowDetailType.Payment)), Times.Never);
        }

        [Theory]
        [InlineData(100.4, 100.0, 0.4)]
        [InlineData(100.5, 101.0, -0.5)]
        [InlineData(100.499, 100.0, 0.499)]
        [InlineData(100.0, 100.0, 0.0)]
        public void CalculateDocumentRounding_ValidRawTotal_ReturnsExpectedRounding(decimal rawTotal, decimal expectedRounded, decimal expectedRoundingVal)
        {
            var (roundedTotal, roundingVal) = DocumentService.CalculateDocumentRounding(rawTotal);
            Assert.Equal(expectedRounded, roundedTotal);
            Assert.Equal(expectedRoundingVal, roundingVal);
        }

        [Fact]
        public async Task CreateImportCompletedAsync_DecimalQuantityWithRoundingRemainder_CreatesCashFlowRounding()
        {
            var doc = CreateValidImportDocument();
            // Quantity = 0.001, UnitPrice = 12345 -> RawTotal = 12.345. Remainder = 0.345 < 0.5 -> Rounded = 12. RoundingValue = 0.345
            doc.DocumentDetails = new List<DocumentDetail>
            {
                new DocumentDetail { BInventoryId = 1, UnitConversionId = 1, ConversionRate = 1, Quantity = 0.001m, UnitPrice = 12345m }
            };
            doc.AmountPaid = 12m;

            var bInv = CreateValidBInventory();
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetUnitConversionByIdAsync(1)).ReturnsAsync(new UnitConversion { Id = 1, ProductId = 100, ConversionPoint = 1 });

            await _service.CreateImportCompletedAsync(doc, 1);

            Assert.Equal(12.345m, doc.TotalAmount);
            Assert.Equal(0m, doc.AmountDue);
            _mockRepo.Verify(r => r.AddCashFlowAsync(It.Is<CashFlow>(cf => cf.Type == CashFlowDetailType.Rounding && cf.TotalAmount == 0.345m)), Times.Once);
        }
        #endregion
    }
}
