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
    /// Test suite kiểm thử các tính năng cho ReturnDocumentController.
    /// </summary>
    public class ReturnDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly ReturnDocumentController _controller;

        public ReturnDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new ReturnDocumentController(_serviceMock.Object);
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
        public async Task GetReturnList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 2, Code = "TH000001", Type = DocumentType.Return, Status = DocumentStatus.Completed }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.Return, null)).ReturnsAsync(dtos);

            var result = await _controller.GetReturnList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetReturnById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 2, Code = "TH000001", Type = DocumentType.Return, Status = DocumentStatus.Completed };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(2)).ReturnsAsync(dto);

            var result = await _controller.GetReturnById(2);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(2, data.Id);
        }

        [Fact]
        public async Task CreateReturnCompleted_ValidDto_ReturnsOkWithCompletedDto()
        {
            var dto = new ReturnDocumentCreateDto
            {
                BranchId = 10,
                PartnerId = 20,
                ParentDocumentId = 1,
                Note = "Trả hàng cho NCC",
                Details = new List<ReturnDocumentDetailCreateDto>
                {
                    new ReturnDocumentDetailCreateDto { BInventoryId = 1, Quantity = 2, UnitPrice = 50000 }
                }
            };

            var completedDoc = new Document { Id = 200, Code = "TH000200", BranchId = 10, Type = DocumentType.Return, Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 200, Code = "TH000200", BranchId = 10, Type = DocumentType.Return, Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CreateReturnCompletedAsync(It.IsAny<Document>(), 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(200)).ReturnsAsync(responseDto);

            var result = await _controller.CreateReturnCompleted(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateReturnCompleted_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateReturnCompleted(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetReturnById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetReturnById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
