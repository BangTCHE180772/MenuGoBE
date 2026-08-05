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
    /// Test suite kiểm thử các tính năng cho CustomerReturnDocumentController.
    /// </summary>
    public class CustomerReturnDocumentControllerTests
    {
        private readonly Mock<IDocumentService> _serviceMock;
        private readonly CustomerReturnDocumentController _controller;

        public CustomerReturnDocumentControllerTests()
        {
            _serviceMock = new Mock<IDocumentService>();
            _controller = new CustomerReturnDocumentController(_serviceMock.Object);
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
        public async Task GetCustomerReturnList_ValidBranchId_ReturnsOkWithList()
        {
            var dtos = new List<DocumentListResponseDto>
            {
                new DocumentListResponseDto { Id = 4, Code = "KTR00001", Type = DocumentType.CustomerReturn, Status = DocumentStatus.Completed }
            };
            _serviceMock.Setup(s => s.GetDocumentListDtosAsync(10, DocumentType.CustomerReturn, null)).ReturnsAsync(dtos);

            var result = await _controller.GetCustomerReturnList(10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<List<DocumentListResponseDto>>(okResult.Value);
            Assert.Single(data);
        }

        [Fact]
        public async Task GetCustomerReturnById_ValidId_ReturnsOkWithDto()
        {
            var dto = new DocumentResponseDto { Id = 4, Code = "KTR00001", Type = DocumentType.CustomerReturn, Status = DocumentStatus.Completed };
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(4)).ReturnsAsync(dto);

            var result = await _controller.GetCustomerReturnById(4);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<DocumentResponseDto>(okResult.Value);
            Assert.Equal(4, data.Id);
        }

        [Fact]
        public async Task CreateCustomerReturnCompleted_ValidDto_ReturnsOkWithCompletedDto()
        {
            var dto = new CustomerReturnDocumentCreateDto
            {
                BranchId = 10,
                PartnerId = 30,
                ParentDocumentId = 3,
                Note = "Khách hàng đổi trả sản phẩm",
                Details = new List<CustomerReturnDocumentDetailCreateDto>
                {
                    new CustomerReturnDocumentDetailCreateDto { BInventoryId = 1, Quantity = 1, UnitPrice = 100000 }
                }
            };

            var completedDoc = new Document { Id = 400, Code = "KTR00400", BranchId = 10, Type = DocumentType.CustomerReturn, Status = DocumentStatus.Completed };
            var responseDto = new DocumentResponseDto { Id = 400, Code = "KTR00400", BranchId = 10, Type = DocumentType.CustomerReturn, Status = DocumentStatus.Completed };

            _serviceMock.Setup(s => s.CreateCustomerReturnCompletedAsync(It.IsAny<Document>(), 1)).ReturnsAsync(completedDoc);
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(400)).ReturnsAsync(responseDto);

            var result = await _controller.CreateCustomerReturnCompleted(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
        #endregion

        #region 2. Error Cases
        [Fact]
        public async Task CreateCustomerReturnCompleted_NullDto_ReturnsBadRequest()
        {
            var result = await _controller.CreateCustomerReturnCompleted(null!);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetCustomerReturnById_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetDocumentByIdDtoAsync(999)).ReturnsAsync((DocumentResponseDto?)null);

            var result = await _controller.GetCustomerReturnById(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
