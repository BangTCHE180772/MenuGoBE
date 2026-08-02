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
    /// Test suite kiểm thử các tính năng cho CostAdjustmentDocumentController.
    /// </summary>
    public class CostAdjustmentDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly CostAdjustmentDocumentController _controller;

        public CostAdjustmentDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new CostAdjustmentDocumentController(_serviceMock.Object);
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
        public async Task GetCostAdjustmentList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 11, Code = "DCGV0001", Type = DocumentType.CostAdjustment, Status = DocumentStatus.Pending }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.CostAdjustment, null)).ReturnsAsync(dtos);

            var result = await _controller.GetCostAdjustmentList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetCostAdjustmentById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 11, Code = "DCGV0001", Type = DocumentType.CostAdjustment, Status = DocumentStatus.Pending };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(11)).ReturnsAsync(dto);

            var result = await _controller.GetCostAdjustmentById(11);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(11, data.Id);
        }

        [Fact]
        public async Task CreateCostAdjustmentCompleted_ValidDto_ReturnsOkWithDto()
        {
            var dto = new CostAdjustmentDocumentCreateDto
            {
                BranchId = 10,
                Note = "Điều chỉnh giá vốn tháng 8",
                Details = new List<CostAdjustmentDocumentDetailCreateDto>
                {
                    new CostAdjustmentDocumentDetailCreateDto { BInventoryId = 1, NewAvgCost = 55000 }
                }
            };

            var completedDoc = new Document { Id = 1100, Code = "DCGV01100", BranchId = 10, Type = DocumentType.CostAdjustment, Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 1100, Code = "DCGV01100", BranchId = 10, Type = DocumentType.CostAdjustment, Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CreateCostAdjustmentCompletedAsync(It.IsAny<Document>(), 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(1100)).ReturnsAsync(responseDto);

            var result = await _controller.CreateCostAdjustmentCompleted(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateCostAdjustmentPending_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateCostAdjustmentPending(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetCostAdjustmentById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetCostAdjustmentById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
