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
    /// Test suite kiểm thử các tính năng cho ExportDeleteDocumentController.
    /// </summary>
    public class ExportDeleteDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly ExportDeleteDocumentController _controller;

        public ExportDeleteDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new ExportDeleteDocumentController(_serviceMock.Object);
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
        public async Task GetExportDeleteList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 8, Code = "XH000001", Type = DocumentType.ExportDelete, Status = DocumentStatus.Pending }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.ExportDelete, null)).ReturnsAsync(dtos);

            var result = await _controller.GetExportDeleteList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetExportDeleteById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 8, Code = "XH000001", Type = DocumentType.ExportDelete, Status = DocumentStatus.Pending };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(8)).ReturnsAsync(dto);

            var result = await _controller.GetExportDeleteById(8);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(8, data.Id);
        }

        [Fact]
        public async Task CreateExportDeleteCompleted_ValidDto_ReturnsOkWithDto()
        {
            var dto = new ExportDeleteDocumentCreateDto
            {
                BranchId = 10,
                Note = "Xuất hủy thực phẩm hỏng",
                Details = new List<ExportDeleteDocumentDetailCreateDto>
                {
                    new ExportDeleteDocumentDetailCreateDto { BInventoryId = 1, Quantity = 3 }
                }
            };

            var completedDoc = new Document { Id = 800, Code = "XH000800", BranchId = 10, Type = DocumentType.ExportDelete, Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 800, Code = "XH000800", BranchId = 10, Type = DocumentType.ExportDelete, Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CreateExportDeleteCompletedAsync(It.IsAny<Document>(), 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(800)).ReturnsAsync(responseDto);

            var result = await _controller.CreateExportDeleteCompleted(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateExportDeletePending_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateExportDeletePending(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetExportDeleteById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetExportDeleteById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
