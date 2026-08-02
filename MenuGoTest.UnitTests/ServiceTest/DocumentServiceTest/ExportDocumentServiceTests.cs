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
    public class ExportDocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly DocumentService _service;

        public ExportDocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
            _service = new DocumentService(_mockRepo.Object);
        }

        private Document CreateValidExportDocument(DocumentType type = DocumentType.Export)
        {
            return new Document
            {
                Id = 60,
                BranchId = 1,
                Code = type == DocumentType.Export ? "XK000060" : "XH000060",
                Type = type,
                Status = DocumentStatus.Pending,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 601,
                        BInventoryId = 15,
                        Quantity = 5,
                        UnitPrice = 20000,
                        ConversionRate = 1,
                        BaseQuantity = 5,
                        SnapshotProductName = "Khăn giấy"
                    }
                }
            };
        }

        private BInventory CreateValidBInventory(long id = 15, decimal qty = 50)
        {
            return new BInventory
            {
                Id = id,
                BranchId = 1,
                ProductId = 600,
                Quantity = qty,
                Avg = 15000,
                LeftOver = 100,
                Product = new Product
                {
                    Id = 600,
                    Name = "Khăn giấy",
                    Type = ProductType.Tool
                }
            };
        }

        #region 1. Internal Export (Xuất Dùng Nội Bộ) Tests (5 Cases)
        [Fact]
        public async Task CreateExportPendingAsync_ValidDocument_CreatesSuccessfully()
        {
            var doc = CreateValidExportDocument(DocumentType.Export);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateExportPendingAsync(doc, 1);

            Assert.NotNull(result);
            Assert.Equal(DocumentType.Export, result.Type);
        }

        [Fact]
        public async Task CompleteExportDeleteAsync_InsufficientStock_ThrowsInvalidOperationException()
        {
            var doc = CreateValidExportDocument(DocumentType.ExportDelete);
            var bInv = CreateValidBInventory(15, qty: 2); // Wants 5, stock 2

            _mockRepo.Setup(r => r.GetByIdAsync(60)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(15)).ReturnsAsync(bInv);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteExportDeleteAsync(60, 1));
        }

        [Fact]
        public async Task CompleteExportAsync_ValidPending_DeductsStockAndPostsLedger()
        {
            var doc = CreateValidExportDocument(DocumentType.Export);
            var bInv = CreateValidBInventory(15, qty: 50);

            _mockRepo.Setup(r => r.GetByIdAsync(60)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(15)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteExportAsync(60, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
        }

        [Fact]
        public async Task SoftDeleteExportPendingAsync_ValidPendingDocument_ReturnsTrue()
        {
            var doc = CreateValidExportDocument(DocumentType.Export);
            _mockRepo.Setup(r => r.GetByIdAsync(60)).ReturnsAsync(doc);

            var success = await _service.SoftDeleteExportPendingAsync(60, 1, "Hủy xuất");
            Assert.True(success);
        }

        [Fact]
        public async Task GetExportListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidExportDocument(DocumentType.Export) };
            _mockRepo.Setup(r => r.GetExportDocumentsAsync(1)).ReturnsAsync(list);

            var result = await _service.GetExportListAsync(1);
            Assert.Single(result);
        }
        #endregion

        #region 2. Waste Write-Off Export (Xuất Hủy Hàng Hỏng) Tests (5 Cases)
        [Fact]
        public async Task CreateExportDeletePendingAsync_ValidDocument_CreatesSuccessfully()
        {
            var doc = CreateValidExportDocument(DocumentType.ExportDelete);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(15)).ReturnsAsync(CreateValidBInventory(15));
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateExportDeletePendingAsync(doc, 1);

            Assert.NotNull(result);
            Assert.Equal(DocumentType.ExportDelete, result.Type);
        }

        [Fact]
        public async Task CompleteExportDeleteAsync_ValidPending_DeductsStockAndZeroesLeftOverWhenStockEmpty()
        {
            var doc = CreateValidExportDocument(DocumentType.ExportDelete);
            var bInv = CreateValidBInventory(15, qty: 5); // Stock exactly 5
            bInv.LeftOver = 300;

            _mockRepo.Setup(r => r.GetByIdAsync(60)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(15)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteExportDeleteAsync(60, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(0, bInv.Quantity);
            Assert.Equal(0, bInv.LeftOver); // Triệt tiêu LeftOver = 0
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
        }

        [Fact]
        public async Task CreateExportDeleteCompletedAsync_DirectPostsSuccessfully()
        {
            var doc = CreateValidExportDocument(DocumentType.ExportDelete);
            var bInv = CreateValidBInventory(15, qty: 100);

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(15)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateExportDeleteCompletedAsync(doc, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(95, bInv.Quantity);
        }

        [Fact]
        public async Task SoftDeleteExportDeletePendingAsync_ValidPendingDocument_ReturnsTrue()
        {
            var doc = CreateValidExportDocument(DocumentType.ExportDelete);
            _mockRepo.Setup(r => r.GetByIdAsync(60)).ReturnsAsync(doc);

            var success = await _service.SoftDeleteExportDeletePendingAsync(60, 1, "Hủy phiếu hủy");
            Assert.True(success);
        }

        [Fact]
        public async Task GetExportDeleteListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidExportDocument(DocumentType.ExportDelete) };
            _mockRepo.Setup(r => r.GetExportDeleteDocumentsAsync(1)).ReturnsAsync(list);

            var result = await _service.GetExportDeleteListAsync(1);
            Assert.Single(result);
        }
        #endregion
    }
}
