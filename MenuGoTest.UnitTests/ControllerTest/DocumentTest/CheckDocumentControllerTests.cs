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
    /// Test suite kiểm thử các tính năng cho CheckDocumentController.
    /// </summary>
    public class CheckDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly CheckDocumentController _controller;

        public CheckDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new CheckDocumentController(_serviceMock.Object);
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
        public async Task GetCheckList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 10, Code = "KK000001", Type = DocumentType.Check, Status = DocumentStatus.Pending }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.Check, null)).ReturnsAsync(dtos);

            var result = await _controller.GetCheckList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetCheckById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 10, Code = "KK000001", Type = DocumentType.Check, Status = DocumentStatus.Pending };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(10)).ReturnsAsync(dto);

            var result = await _controller.GetCheckById(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(10, data.Id);
        }

        [Fact]
        public async Task CreateCheckCompleted_ValidDto_ReturnsOkWithDto()
        {
            var dto = new CheckDocumentCreateDto
            {
                BranchId = 10,
                Note = "Kiểm kho định kỳ",
                Details = new List<CheckDocumentDetailCreateDto>
                {
                    new CheckDocumentDetailCreateDto { BInventoryId = 1, ActualQuantity = 45 }
                }
            };

            var completedDoc = new Document { Id = 1000, Code = "KK001000", BranchId = 10, Type = DocumentType.Check, Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 1000, Code = "KK001000", BranchId = 10, Type = DocumentType.Check, Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CreateCheckCompletedAsync(It.Is<Document>(doc => doc.DocumentDetails.First().Quantity == 45 && doc.DocumentDetails.First().ActualQuantity == 45), 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(1000)).ReturnsAsync(responseDto);

            var result = await _controller.CreateCheckCompleted(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateCheckPending_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateCheckPending(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetCheckById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetCheckById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
