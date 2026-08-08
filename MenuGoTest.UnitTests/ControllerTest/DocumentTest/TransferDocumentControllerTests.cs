using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MenuGoBE.Controllers;
using MenuGoBE.Controllers.Document;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Interface.Services.Document;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ControllerTest.DocumentTest
{
    /// <summary>
    /// Test suite kiểm thử các tính năng cho TransferDocumentController.
    /// </summary>
    public class TransferDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly TransferDocumentController _controller;

        public TransferDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new TransferDocumentController(_serviceMock.Object);
            SetupControllerContext(_controller, userId: 1, branchId: 10);
        }

        private void SetupControllerContext(BaseApiController controller, long userId, long branchId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("BranchId", branchId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region 1. Success Cases
        [Fact]
        public async Task GetTransferList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 5, Code = "CK000001", Type = DocumentType.Transfer, Status = DocumentStatus.Pending }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.Transfer, null)).ReturnsAsync(dtos);

            var result = await _controller.GetTransferList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetTransferById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 5, Code = "CK000001", Type = DocumentType.Transfer, Status = DocumentStatus.Pending };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(5)).ReturnsAsync(dto);

            var result = await _controller.GetTransferById(5);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(5, data.Id);
        }

        [Fact]
        public async Task CreateTransferPending_ValidDto_ReturnsOkWithDto()
        {
            var dto = new TransferDocumentCreateDto
            {
                BranchId = 10,
                ToBranchId = 11,
                Note = "Chuyển nháp",
                Details = new List<TransferDocumentDetailCreateDto>
                {
                    new TransferDocumentDetailCreateDto { BInventoryId = 1, Quantity = 5 }
                }
            };

            var createdDoc = new Document { Id = 500, Code = "CK000500", BranchId = 10, ToBranchId = 11, Type = DocumentType.Transfer, Status = DocumentStatus.Pending };
            var responseDto = new DocumentResponseDto { Id = 500, Code = "CK000500", BranchId = 10, ToBranchId = 11, Type = DocumentType.Transfer, Status = DocumentStatus.Pending };

            _serviceMock.Setup(s => s.CreateTransferPendingAsync(It.IsAny<Document>(), 1)).ReturnsAsync(createdDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(500)).ReturnsAsync(responseDto);

            var result = await _controller.CreateTransferPending(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ProcessTransferReceipt_ValidRequest_ReturnsOkWithDto()
        {
            var request = new TransferReceiptRequestDto
            {
                Status = DocumentTransferStatus.Received,
                Note = "Nhận đủ hàng",
                ReceivedDetails = new List<TransferReceiptDetailDto>
                {
                    new TransferReceiptDetailDto { DetailId = 1, ReceivedQuantity = 5 }
                }
            };

            var updatedDoc = new Document { Id = 5, Code = "CK000001", TransferStatus = DocumentTransferStatus.Received };
            var responseDto = new DocumentResponseDto { Id = 5, Code = "CK000001", TransferStatus = DocumentTransferStatus.Received };

            _serviceMock.Setup(s => s.ProcessTransferReceiptAsync(5, DocumentTransferStatus.Received, It.IsAny<List<DocumentDetail>>(), "Nhận đủ hàng", 1)).ReturnsAsync(updatedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(5)).ReturnsAsync(responseDto);

            var result = await _controller.ProcessTransferReceipt(5, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateTransferPending_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateTransferPending(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ProcessTransferReceipt_NullRequest_ReturnsBadRequest()
        {
            var result = await _controller.ProcessTransferReceipt(5, null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetTransferById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetTransferById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
