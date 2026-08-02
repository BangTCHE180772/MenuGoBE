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
    /// Test suite kiểm thử các tính năng cho ProductionDocumentController.
    /// </summary>
    public class ProductionDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly ProductionDocumentController _controller;

        public ProductionDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new ProductionDocumentController(_serviceMock.Object);
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
        public async Task GetProductionList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 9, Code = "SX000001", Type = DocumentType.Production, Status = DocumentStatus.Pending }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.Production, null)).ReturnsAsync(dtos);

            var result = await _controller.GetProductionList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetProductionById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 9, Code = "SX000001", Type = DocumentType.Production, Status = DocumentStatus.Pending };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(9)).ReturnsAsync(dto);

            var result = await _controller.GetProductionById(9);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(9, data.Id);
        }

        [Fact]
        public async Task CreateProductionCompleted_ValidDto_ReturnsOkWithDto()
        {
            var dto = new ProductionDocumentCreateDto
            {
                BranchId = 10,
                Note = "Sản xuất lẩu thái",
                Details = new List<ProductionDocumentDetailCreateDto>
                {
                    new ProductionDocumentDetailCreateDto { BInventoryId = 1, Quantity = 10 }
                }
            };

            var completedDoc = new Document { Id = 900, Code = "SX000900", BranchId = 10, Type = DocumentType.Production, Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 900, Code = "SX000900", BranchId = 10, Type = DocumentType.Production, Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CreateProductionCompletedAsync(It.IsAny<Document>(), 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(900)).ReturnsAsync(responseDto);

            var result = await _controller.CreateProductionCompleted(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateProductionPending_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateProductionPending(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetProductionById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetProductionById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
