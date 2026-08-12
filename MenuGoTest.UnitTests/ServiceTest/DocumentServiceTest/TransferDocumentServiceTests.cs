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
    public class TransferDocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly DocumentService _service;

        public TransferDocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfiles>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            _service = new DocumentService(_mockRepo.Object, _mapper);
        }

        private Document CreateValidTransferDocument()
        {
            return new Document
            {
                Id = 50,
                BranchId = 1, // Chi nhánh gửi
                ToBranchId = 2, // Chi nhánh nhận
                Code = "CK000050",
                Type = DocumentType.Transfer,
                Status = DocumentStatus.Pending,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 501,
                        BInventoryId = 10,
                        Quantity = 10,
                        UnitPrice = 50000,
                        ConversionRate = 1,
                        BaseQuantity = 10,
                        SnapshotProductName = "Bột mì"
                    }
                }
            };
        }

        private BInventory CreateValidBInventory(long id = 10, long branchId = 1, decimal qty = 100)
        {
            return new BInventory
            {
                Id = id,
                BranchId = branchId,
                ProductId = 500,
                Quantity = qty,
                Avg = 40000,
                LeftOver = 0,
                Product = new Product
                {
                    Id = 500,
                    Name = "Bột mì",
                    Type = ProductType.Regular
                }
            };
        }

        #region 1. Transfer Base Rules & Validations (5 Cases)
        [Fact]
        public async Task CreateTransferPendingAsync_NullDocument_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateTransferPendingAsync(null!, 1));
        }

        [Fact]
        public async Task CreateTransferPendingAsync_MissingToBranchId_ThrowsArgumentException()
        {
            var doc = CreateValidTransferDocument();
            doc.ToBranchId = null; // Missing

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateTransferPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateTransferPendingAsync_SameSenderAndReceiverBranch_ThrowsArgumentException()
        {
            var doc = CreateValidTransferDocument();
            doc.ToBranchId = 1; // Same as BranchId

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateTransferPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateTransferPendingAsync_ExceedsMaxQuantity_ThrowsArgumentException()
        {
            var doc = CreateValidTransferDocument();
            doc.DocumentDetails.First().Quantity = 20_000_000_000m; // > 10 billion

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(CreateValidBInventory(10));

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateTransferPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CreateTransferPendingAsync_ExceedsMaxUnitPrice_ThrowsArgumentException()
        {
            var doc = CreateValidTransferDocument();
            doc.DocumentDetails.First().UnitPrice = 20_000_000_000m; // > 10 billion

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(CreateValidBInventory(10));

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateTransferPendingAsync(doc, 1));
        }
        #endregion

        #region 2. Transfer Sender Posting & InTransit Tests (5 Cases)
        [Fact]
        public async Task CompleteTransferAsync_InsufficientSenderStock_ThrowsInvalidOperationException()
        {
            var doc = CreateValidTransferDocument();
            var bInvSender = CreateValidBInventory(10, branchId: 1, qty: 5); // Only 5 in stock

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvSender);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CompleteTransferAsync(50, 1));
        }

        [Fact]
        public async Task CompleteTransferAsync_ValidPending_DeductsSenderStockAndSetsInTransit()
        {
            var doc = CreateValidTransferDocument();
            var bInvSender = CreateValidBInventory(10, branchId: 1, qty: 50);

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvSender);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteTransferAsync(50, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(DocumentTransferStatus.InTransit, result.TransferStatus);
            Assert.Equal(40, bInvSender.Quantity); // 50 - 10 = 40
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
        }

        [Fact]
        public async Task CreateTransferCompletedAsync_ValidDocument_DirectPostsInTransitSuccessfully()
        {
            var doc = CreateValidTransferDocument();
            var bInvSender = CreateValidBInventory(10, branchId: 1, qty: 100);

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvSender);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateTransferCompletedAsync(doc, 1);

            Assert.Equal(DocumentTransferStatus.InTransit, result.TransferStatus);
            Assert.Equal(90, bInvSender.Quantity); // 100 - 10 = 90
        }

        [Fact]
        public async Task SoftDeleteTransferPendingAsync_ValidPendingDocument_ReturnsTrue()
        {
            var doc = CreateValidTransferDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);

            var success = await _service.SoftDeleteTransferPendingAsync(50, 1, "Hủy chuyển");
            Assert.True(success);
        }

        [Fact]
        public async Task GetTransferListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidTransferDocument() };
            _mockRepo.Setup(r => r.GetTransferDocumentsAsync(1)).ReturnsAsync(list);

            var result = await _service.GetTransferListAsync(1);
            Assert.Single(result);
        }
        #endregion

        #region 3. Receiver Confirming (Receive & PartialReceive) Tests (5 Cases)
        [Fact]
        public async Task ProcessTransferReceiptAsync_NotInTransitStatus_ThrowsInvalidOperationException()
        {
            var doc = CreateValidTransferDocument();
            doc.Status = DocumentStatus.Pending; // Not InTransit

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() =>
                _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.Received, null, "Ghi chú", 2));
        }

        [Fact]
        public async Task ProcessTransferReceiptAsync_FullReceiveWithoutNote_ThrowsArgumentException()
        {
            var doc = CreateValidTransferDocument();
            doc.Status = DocumentStatus.Completed;
            doc.TransferStatus = DocumentTransferStatus.InTransit;

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() =>
                _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.Received, null, "   ", 2)); // Empty note
        }

        [Fact]
        public async Task ProcessTransferReceiptAsync_FullReceive_IncreasesReceiverStockAndSetsReceivedStatus()
        {
            var doc = CreateValidTransferDocument();
            doc.Status = DocumentStatus.Completed;
            doc.TransferStatus = DocumentTransferStatus.InTransit;

            var bInvSender = CreateValidBInventory(10, branchId: 1, qty: 90);
            var bInvReceiver = CreateValidBInventory(20, branchId: 2, qty: 30);

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvSender);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(2, 500)).ReturnsAsync(bInvReceiver);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(20)).ReturnsAsync(bInvReceiver);

            var result = await _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.Received, null, "Nhận đủ hàng", 2);

            Assert.Equal(DocumentTransferStatus.Received, result.TransferStatus);
            Assert.NotNull(result.ResponseDate);
            Assert.Equal(40, bInvReceiver.Quantity); // 30 + 10 = 40
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
        }

        [Fact]
        public async Task ProcessTransferReceiptAsync_PartialReceiveExceedsSentQty_ThrowsArgumentException()
        {
            var doc = CreateValidTransferDocument();
            doc.Status = DocumentStatus.Completed;
            doc.TransferStatus = DocumentTransferStatus.InTransit;

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);

            var receivedDetails = new List<DocumentDetail>
            {
                new DocumentDetail { Id = 501, Quantity = 15 } // Sent 10, wants to receive 15 -> Exceeds
            };

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() =>
                _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.PartialReceived, receivedDetails, "Ghi chú", 2));
        }

        [Fact]
        public async Task ProcessTransferReceiptAsync_PartialReceive_IncreasesReceiverStockAndRestoresRemainingToSender()
        {
            var doc = CreateValidTransferDocument(); // Sent 10 units
            doc.Status = DocumentStatus.Completed;
            doc.TransferStatus = DocumentTransferStatus.InTransit;

            var bInvSender = CreateValidBInventory(10, branchId: 1, qty: 90);
            var bInvReceiver = CreateValidBInventory(20, branchId: 2, qty: 30);

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvSender);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(2, 500)).ReturnsAsync(bInvReceiver);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(20)).ReturnsAsync(bInvReceiver);

            var receivedDetails = new List<DocumentDetail>
            {
                new DocumentDetail { Id = 501, Quantity = 7 } // Received 7
            };

            var result = await _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.PartialReceived, receivedDetails, "Nhận 7/10", 2);

            Assert.Equal(DocumentTransferStatus.PartialReceived, result.TransferStatus);
            Assert.Equal(37, bInvReceiver.Quantity); // 30 + 7 = 37
        }

        [Fact]
        public async Task ProcessTransferReceiptAsync_ReceiverBInventoryNull_CreatesNewBInventoryAndIncreasesStock()
        {
            var doc = CreateValidTransferDocument();
            doc.Status = DocumentStatus.Completed;
            doc.TransferStatus = DocumentTransferStatus.InTransit;

            var bInvSender = CreateValidBInventory(10, branchId: 1, qty: 90);

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvSender);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(2, 500)).ReturnsAsync((BInventory?)null); // Receiver has no BInventory yet

            var createdBInv = CreateValidBInventory(20, branchId: 2, qty: 10);
            _mockRepo.Setup(r => r.AddBInventoryAsync(It.IsAny<BInventory>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(20)).ReturnsAsync(createdBInv);

            var result = await _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.Received, null, "Tạo mới kho nhận", 2);

            Assert.Equal(DocumentTransferStatus.Received, result.TransferStatus);
        }
        #endregion

        #region 4. Receiver Rejecting & Receiver Metadata Tests (5 Cases)
        [Fact]
        public async Task ProcessTransferReceiptAsync_RejectOptionWithoutNote_ThrowsArgumentException()
        {
            var doc = CreateValidTransferDocument();
            doc.Status = DocumentStatus.Completed;
            doc.TransferStatus = DocumentTransferStatus.InTransit;

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() =>
                _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.Rejected, null, "   ", 2)); // Empty note
        }

        [Fact]
        public async Task ProcessTransferReceiptAsync_RejectOption_RestoresSenderStockAndSetsRejectedStatus()
        {
            var doc = CreateValidTransferDocument();
            doc.Status = DocumentStatus.Completed;
            doc.TransferStatus = DocumentTransferStatus.InTransit;

            var bInvSender = CreateValidBInventory(10, branchId: 1, qty: 90);

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvSender);

            var result = await _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.Rejected, null, "Hàng vỡ hỏng do vận chuyển", 2);

            Assert.Equal(DocumentTransferStatus.Rejected, result.TransferStatus);
            Assert.NotNull(result.ResponseDate);
            Assert.Equal(100, bInvSender.Quantity); // 90 + 10 = 100 (Restored to sender)
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
        }

        [Fact]
        public async Task ProcessTransferReceiptAsync_CapturesReceiverMetadataSnapshots()
        {
            var doc = CreateValidTransferDocument();
            doc.Status = DocumentStatus.Completed;
            doc.TransferStatus = DocumentTransferStatus.InTransit;

            var bInvSender = CreateValidBInventory(10, branchId: 1, qty: 90);
            var bInvReceiver = CreateValidBInventory(20, branchId: 2, qty: 0);
            var receiverAccount = new Account { Id = 2, Name = "Trần Thủ Kho", Email = "kho_br2@example.com" };

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvSender);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(2, 500)).ReturnsAsync(bInvReceiver);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(20)).ReturnsAsync(bInvReceiver);
            _mockRepo.Setup(r => r.GetAccountByIdAsync(2)).ReturnsAsync(receiverAccount);

            var result = await _service.ProcessTransferReceiptAsync(50, DocumentTransferStatus.Received, null, "Nhận hàng", 2);

            Assert.Equal(2, result.ReceiverAccountId);
            Assert.Equal("Trần Thủ Kho", result.SnapshotReceiverName);
            Assert.Equal("kho_br2@example.com", result.SnapshotReceiverUsername);
        }

        [Fact]
        public async Task ProcessTransferReceiptAsync_NonExistentDocument_ThrowsKeyNotFoundException()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Document?)null);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() =>
                _service.ProcessTransferReceiptAsync(999, DocumentTransferStatus.Received, null, "Ghi chú", 2));
        }

        [Fact]
        public async Task UpdateTransferPendingAsync_ValidDocument_UpdatesSuccessfully()
        {
            var doc = CreateValidTransferDocument();

            _mockRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(CreateValidBInventory(10));
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.UpdateTransferPendingAsync(50, doc, 1);
            Assert.NotNull(result);
        }
        #endregion
    }
}
