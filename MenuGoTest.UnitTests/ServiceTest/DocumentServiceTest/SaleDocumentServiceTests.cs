using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Mapper;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using MenuGoBE.Service.Document;
using Moq;
using Xunit;

namespace MenuGoTest.UnitTests.ServiceTest.DocumentServiceTest
{
    public class SaleDocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly DocumentService _service;

        public SaleDocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfiles>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            _service = new DocumentService(_mockRepo.Object, _mapper);
        }

        private Document CreateValidSaleDocument()
        {
            return new Document
            {
                Id = 30,
                BranchId = 1,
                Code = "HD000030",
                Type = DocumentType.Sale,
                Status = DocumentStatus.Completed,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 301,
                        BInventoryId = 10,
                        Quantity = 2,
                        UnitPrice = 50000
                    }
                }
            };
        }

        private BInventory CreateValidBInventory(long id = 10, ProductType productType = ProductType.Regular, decimal qty = 100)
        {
            return new BInventory
            {
                Id = id,
                BranchId = 1,
                ProductId = id * 100,
                Quantity = qty,
                Avg = 30000,
                LeftOver = 0,
                Product = new Product
                {
                    Id = id * 100,
                    Name = $"Sản phẩm {id}",
                    Type = productType
                }
            };
        }

        #region 1. Sale Validation & Product Types Tests (5 Cases)
        [Fact]
        public async Task CreateSaleCompletedAsync_NullDocument_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(null!, 1));
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_EmptyDetails_ThrowsArgumentException()
        {
            var saleDoc = CreateValidSaleDocument();
            saleDoc.DocumentDetails = new List<DocumentDetail>();

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(saleDoc, 1));
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_InvalidProductType_ThrowsArgumentException()
        {
            var saleDoc = CreateValidSaleDocument();
            var bInv = CreateValidBInventory(10, ProductType.Ingredient); // Ingredient not directly sellable

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInv);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(saleDoc, 1));
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_ExceedsMaxQuantity_ThrowsArgumentException()
        {
            var saleDoc = CreateValidSaleDocument();
            saleDoc.DocumentDetails.First().Quantity = 20_000_000_000m; // > 10 billion

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(CreateValidBInventory(10));

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(saleDoc, 1));
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_ExceedsMaxUnitPrice_ThrowsArgumentException()
        {
            var saleDoc = CreateValidSaleDocument();
            saleDoc.DocumentDetails.First().UnitPrice = 20_000_000_000m; // > 10 billion

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(CreateValidBInventory(10));

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(saleDoc, 1));
        }
        #endregion

        #region 2. Regular & Manufactured Direct Stock Deduction Tests (5 Cases)
        [Fact]
        public async Task CreateSaleCompletedAsync_RegularProduct_DeductsStockDirectly()
        {
            var saleDoc = CreateValidSaleDocument();
            var bInv = CreateValidBInventory(10, ProductType.Regular, qty: 50);

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 1000)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateSaleCompletedAsync(saleDoc, 1);

            Assert.Equal(48, bInv.Quantity); // 50 - 2 = 48
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_ManufacturedProduct_DeductsStockDirectlyWithoutUnrollingBOM()
        {
            var saleDoc = CreateValidSaleDocument();
            var bInv = CreateValidBInventory(10, ProductType.Manufactured, qty: 30); // Manufactured items deduct directly

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 1000)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateSaleCompletedAsync(saleDoc, 1);

            Assert.Equal(28, bInv.Quantity); // 30 - 2 = 28
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_RegularProduct_ZeroesLeftOverWhenStockReachesZero()
        {
            var saleDoc = CreateValidSaleDocument();
            saleDoc.DocumentDetails.First().Quantity = 5;
            var bInv = CreateValidBInventory(10, ProductType.Regular, qty: 5);
            bInv.LeftOver = 200;

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 1000)).ReturnsAsync(bInv);

            await _service.CreateSaleCompletedAsync(saleDoc, 1);

            Assert.Equal(0, bInv.Quantity);
            Assert.Equal(0, bInv.LeftOver);
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_InsufficientStockRegularProduct_ThrowsInvalidOperationException()
        {
            var saleDoc = CreateValidSaleDocument();
            saleDoc.DocumentDetails.First().Quantity = 10;
            var bInv = CreateValidBInventory(10, ProductType.Regular, qty: 5); // Only 5 in stock

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 1000)).ReturnsAsync(bInv);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(saleDoc, 1));
        }

        [Fact]
        public async Task GetSaleListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidSaleDocument() };
            _mockRepo.Setup(r => r.GetSaleDocumentsAsync(1)).ReturnsAsync(list);

            var result = await _service.GetSaleListAsync(1);

            Assert.Single(result);
        }
        #endregion

        #region 3. Processed Product BOM Unrolling & Circular Check Tests (5 Cases)
        [Fact]
        public async Task CreateSaleCompletedAsync_ProcessedProductWithoutRecipe_ThrowsInvalidOperationException()
        {
            var saleDoc = CreateValidSaleDocument();
            var bInv = CreateValidBInventory(10, ProductType.Processed);

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(It.IsAny<long>())).ReturnsAsync(new List<RecipesDetailed>());

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(saleDoc, 1));
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_ProcessedProductCircularDependency_ThrowsInvalidOperationException()
        {
            var saleDoc = CreateValidSaleDocument();
            var bInv = CreateValidBInventory(10, ProductType.Processed);

            // Circular recipe: Product 1000 has ingredient Product 1000
            var recipes = new List<RecipesDetailed>
            {
                new RecipesDetailed
                {
                    ParentProductId = 1000,
                    IngredientProductId = 1000, // Self reference -> Circular loop
                    Quantity = 1,
                    IngredientProduct = new Product { Id = 1000, Name = "Lẩu thái", Type = ProductType.Processed }
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(1000)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(1000)).ReturnsAsync(recipes);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(saleDoc, 1));
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_ProcessedProductUnrollsBOMAndDeductsIngredientStock()
        {
            var saleDoc = CreateValidSaleDocument(); // Selling 2 units of Processed Product 1000 (Lẩu thái)
            var bInvProcessed = CreateValidBInventory(10, ProductType.Processed);

            var ingredientProduct = new Product { Id = 2000, Name = "Tôm hùm", Type = ProductType.Ingredient };
            var bInvIngredient = new BInventory { Id = 20, BranchId = 1, ProductId = 2000, Quantity = 50, Avg = 20000, LeftOver = 0, Product = ingredientProduct };

            var recipes = new List<RecipesDetailed>
            {
                new RecipesDetailed
                {
                    ParentProductId = 1000,
                    IngredientProductId = 2000,
                    Quantity = 3, // Each Lẩu thái requires 3 Tôm hùm
                    IngredientProduct = ingredientProduct
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvProcessed);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(2000)).ReturnsAsync(bInvIngredient);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 2000)).ReturnsAsync(bInvIngredient);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(20)).ReturnsAsync(bInvIngredient);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(1000)).ReturnsAsync(recipes);

            var result = await _service.CreateSaleCompletedAsync(saleDoc, 1);

            // Total required = 2 * 3 = 6 Tôm hùm. New stock = 50 - 6 = 44
            Assert.Equal(44, bInvIngredient.Quantity);
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Once);
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_ProcessedProductInsufficientIngredientStock_ThrowsInvalidOperationException()
        {
            var saleDoc = CreateValidSaleDocument(); // Wants 2 units
            var bInvProcessed = CreateValidBInventory(10, ProductType.Processed);

            var ingredientProduct = new Product { Id = 2000, Name = "Tôm hùm", Type = ProductType.Ingredient };
            var bInvIngredient = new BInventory { Id = 20, BranchId = 1, ProductId = 2000, Quantity = 4, Avg = 20000, LeftOver = 0, Product = ingredientProduct }; // Only 4 in stock

            var recipes = new List<RecipesDetailed>
            {
                new RecipesDetailed
                {
                    ParentProductId = 1000,
                    IngredientProductId = 2000,
                    Quantity = 3, // 2 * 3 = 6 required
                    IngredientProduct = ingredientProduct
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvProcessed);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(2000)).ReturnsAsync(bInvIngredient);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 2000)).ReturnsAsync(bInvIngredient);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(1000)).ReturnsAsync(recipes);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateSaleCompletedAsync(saleDoc, 1));
        }

        [Fact]
        public async Task CreateSaleCompletedAsync_ProcessedProductStopsUnrollingAtManufacturedProduct()
        {
            var saleDoc = CreateValidSaleDocument(); // Selling 1 Processed Product 1000 (Súp Bò)
            saleDoc.DocumentDetails.First().Quantity = 1;
            var bInvProcessed = CreateValidBInventory(10, ProductType.Processed);

            // Intermediate product is Manufactured (Nước hầm xương) -> BOM unrolling STOPS at Manufactured
            var mfgProduct = new Product { Id = 3000, Name = "Nước hầm xương", Type = ProductType.Manufactured };
            var bInvMfg = new BInventory { Id = 30, BranchId = 1, ProductId = 3000, Quantity = 20, Avg = 15000, LeftOver = 0, Product = mfgProduct };

            var recipes = new List<RecipesDetailed>
            {
                new RecipesDetailed
                {
                    ParentProductId = 1000,
                    IngredientProductId = 3000,
                    Quantity = 2, // Requires 2 Nước hầm xương
                    IngredientProduct = mfgProduct
                }
            };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(10)).ReturnsAsync(bInvProcessed);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(3000)).ReturnsAsync(bInvMfg);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 3000)).ReturnsAsync(bInvMfg);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(30)).ReturnsAsync(bInvMfg);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(1000)).ReturnsAsync(recipes);

            await _service.CreateSaleCompletedAsync(saleDoc, 1);

            Assert.Equal(18, bInvMfg.Quantity); // Stock deducted directly from Manufactured item (20 - 2 = 18)
        }
        #endregion
    }
}
