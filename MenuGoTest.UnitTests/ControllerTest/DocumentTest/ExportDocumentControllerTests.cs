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
    /// Test suite kiểm thử các tính năng cho ExportDocumentController.
    /// </summary>
    public class ExportDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly ExportDocumentController _controller;

        public ExportDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new ExportDocumentController(_serviceMock.Object);
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
        public async Task GetExportList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 7, Code = "XK000001", Type = DocumentType.Export, Status = DocumentStatus.Pending }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.Export, null)).ReturnsAsync(dtos);

            var result = await _controller.GetExportList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetExportById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 7, Code = "XK000001", Type = DocumentType.Export, Status = DocumentStatus.Pending };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(7)).ReturnsAsync(dto);

            var result = await _controller.GetExportById(7);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(7, data.Id);
        }

        [Fact]
        public async Task CreateExportPending_ValidDto_ReturnsOkWithDto()
        {
            var dto = new ExportDocumentCreateDto
            {
                BranchId = 10,
                Note = "Xuất nội bộ",
                Details = new List<ExportDocumentDetailCreateDto>
                {
                    new ExportDocumentDetailCreateDto { BInventoryId = 1, Quantity = 2 }
                }
            };

            var createdDoc = new Document { Id = 700, Code = "XK000700", BranchId = 10, Type = DocumentType.Export, Status = DocumentStatus.Pending };
            var responseDto = new DocumentResponseDto { Id = 700, Code = "XK000700", BranchId = 10, Type = DocumentType.Export, Status = DocumentStatus.Pending };

            _serviceMock.Setup(s => s.CreateExportPendingAsync(It.IsAny<Document>(), 1)).ReturnsAsync(createdDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(700)).ReturnsAsync(responseDto);

            var result = await _controller.CreateExportPending(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateExportPending_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateExportPending(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetExportById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetExportById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
