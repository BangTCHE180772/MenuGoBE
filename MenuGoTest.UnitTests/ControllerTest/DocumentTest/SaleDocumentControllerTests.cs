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
    /// Test suite kiểm thử các tính năng cho SaleDocumentController.
    /// </summary>
    public class SaleDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly SaleDocumentController _controller;

        public SaleDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new SaleDocumentController(_serviceMock.Object);
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
        public async Task GetSaleList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 3, Code = "XB000001", Type = DocumentType.Sale, Status = DocumentStatus.Completed }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.Sale, null)).ReturnsAsync(dtos);

            var result = await _controller.GetSaleList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetSaleById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 3, Code = "XB000001", Type = DocumentType.Sale, Status = DocumentStatus.Completed };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(3)).ReturnsAsync(dto);

            var result = await _controller.GetSaleById(3);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(3, data.Id);
        }

        [Fact]
        public async Task CreateSaleCompleted_ValidDto_ReturnsOkWithDto()
        {
            var dto = new SaleDocumentCreateDto
            {
                BranchId = 10,
                PartnerId = 30,
                Note = "Xuất bán cho khách hàng",
                Details = new List<SaleDocumentDetailCreateDto>
                {
                    new SaleDocumentDetailCreateDto { BInventoryId = 1, Quantity = 2, UnitPrice = 120000 }
                }
            };

            var completedDoc = new Document { Id = 300, Code = "XB000300", BranchId = 10, Type = DocumentType.Sale, Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 300, Code = "XB000300", BranchId = 10, Type = DocumentType.Sale, Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CreateSaleCompletedAsync(It.IsAny<Document>(), 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(300)).ReturnsAsync(responseDto);

            var result = await _controller.CreateSaleCompleted(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateSaleCompleted_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateSaleCompleted(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetSaleById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetSaleById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
