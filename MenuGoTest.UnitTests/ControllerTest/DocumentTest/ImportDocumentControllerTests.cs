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
    /// Test suite kiểm thử các tính năng thành công và xử lý lỗi cho ImportDocumentController.
    /// </summary>
    public class ImportDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly ImportDocumentController _controller;

        public ImportDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new ImportDocumentController(_serviceMock.Object);
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

        #region 1. Success Cases (Trường hợp thành công)
        [Fact]
        public async Task GetImportList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 1, Code = "NH000001", Type = DocumentType.Import, Status = DocumentStatus.Pending }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.Import, null)).ReturnsAsync(dtos);

            var result = await _controller.GetImportList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetImportById_ValidId_ReturnsOkWithResponseDto()
        {
            var dto = new DocumentResponseDto { Id = 1, Code = "NH000001", Type = DocumentType.Import, Status = DocumentStatus.Pending };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(1)).ReturnsAsync(dto);

            var result = await _controller.GetImportById(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(1, data.Id);
        }

        [Fact]
        public async Task CreateImportPending_ValidDto_ReturnsOkWithCreatedDocumentDto()
        {
            var dto = new ImportDocumentCreateDto
            {
                BranchId = 10,
                PartnerId = 20,
                Note = "Nhập nháp mới",
                Details = new List<ImportDocumentDetailCreateDto>
                {
                    new ImportDocumentDetailCreateDto { BInventoryId = 1, Quantity = 10, UnitPrice = 50000 }
                }
            };

            var createdDoc = new Document { Id = 100, Code = "NH000100", BranchId = 10, Type = DocumentType.Import, Status = DocumentStatus.Pending };
            var responseDto = new DocumentResponseDto { Id = 100, Code = "NH000100", BranchId = 10, Type = DocumentType.Import, Status = DocumentStatus.Pending };

            _serviceMock.Setup(s => s.CreateImportPendingAsync(It.IsAny<Document>(), 1)).ReturnsAsync(createdDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(100)).ReturnsAsync(responseDto);

            var result = await _controller.CreateImportPending(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CreateImportCompleted_ValidDto_ReturnsOkWithCompletedDocumentDto()
        {
            var dto = new ImportDocumentCreateDto
            {
                BranchId = 10,
                PartnerId = 20,
                Note = "Nhập kho chốt thẳng",
                Details = new List<ImportDocumentDetailCreateDto>
                {
                    new ImportDocumentDetailCreateDto { BInventoryId = 1, Quantity = 5, UnitPrice = 60000 }
                }
            };

            var completedDoc = new Document { Id = 101, Code = "NH000101", BranchId = 10, Type = DocumentType.Import, Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 101, Code = "NH000101", BranchId = 10, Type = DocumentType.Import, Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CreateImportCompletedAsync(It.IsAny<Document>(), 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(101)).ReturnsAsync(responseDto);

            var result = await _controller.CreateImportCompleted(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateImportPending_ValidDto_ReturnsOkWithUpdatedDocumentDto()
        {
            var dto = new ImportDocumentUpdateDto
            {
                PartnerId = 20,
                Note = "Cập nhật ghi chú",
                Details = new List<ImportDocumentDetailUpdateDto>
                {
                    new ImportDocumentDetailUpdateDto { Id = 10, BInventoryId = 1, Quantity = 12, UnitPrice = 50000 }
                }
            };

            var existingDto = new DocumentResponseDto { Id = 1, BranchId = 10, Type = DocumentType.Import };
            var updatedDoc = new Document { Id = 1, Code = "NH000001", BranchId = 10, Type = DocumentType.Import };

            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(1)).ReturnsAsync(existingDto);
            _serviceMock.Setup(s => s.UpdateImportPendingAsync(1, It.IsAny<Document>(), 1)).ReturnsAsync(updatedDoc);

            var result = await _controller.UpdateImportPending(1, dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CompleteImport_ValidId_ReturnsOkWithCompletedDto()
        {
            var completedDoc = new Document { Id = 1, Code = "NH000001", Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 1, Code = "NH000001", Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CompleteImportAsync(1, 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(1)).ReturnsAsync(responseDto);

            var result = await _controller.CompleteImport(1);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task SoftDeleteImportPending_ValidId_ReturnsOkSuccessMessage()
        {
            _serviceMock.Setup(s => s.SoftDeleteImportPendingAsync(1, 1, "Hủy thử")).ReturnsAsync(true);

            var result = await _controller.SoftDeleteImportPending(1, "Hủy thử");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Validation & Error Cases (Trường hợp kiểm tra lỗi)
        [Fact]
        public async Task CreateImportPending_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateImportPending(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateImportCompleted_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateImportCompleted(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetImportById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetImportById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateImportPending_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var dto = new ImportDocumentUpdateDto { Note = "Cập nhật" };
            var result = await _controller.UpdateImportPending(999, dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
