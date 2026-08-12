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
    public class ProductionDocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly DocumentService _service;

        public ProductionDocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfiles>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            _service = new DocumentService(_mockRepo.Object, _mapper);
        }

        private Document CreateValidProductionDocument()
        {
            return new Document
            {
                Id = 70,
                BranchId = 1,
                Code = "SX000070",
                Type = DocumentType.Production,
                Status = DocumentStatus.Pending,
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        Id = 701,
                        BInventoryId = 100, // Thành phẩm chế biến
                        Quantity = 10, // Sản xuất 10 Hũ Kim Chi
                        ConversionRate = 1,
                        BaseQuantity = 10,
                        UnitPrice = 0,
                        SnapshotProductName = "Kim Chi Hàn Quốc"
                    }
                }
            };
        }

        private BInventory CreateManufacturedBInventory()
        {
            return new BInventory
            {
                Id = 100,
                BranchId = 1,
                ProductId = 7000,
                Quantity = 0,
                Avg = 0,
                LeftOver = 0,
                Product = new Product
                {
                    Id = 7000,
                    Name = "Kim Chi Hàn Quốc",
                    Type = ProductType.Manufactured
                }
            };
        }

        private List<RecipesDetailed> CreateValidRecipesDetailed()
        {
            return new List<RecipesDetailed>
            {
                new RecipesDetailed
                {
                    Id = 1,
                    ParentProductId = 7000,
                    IngredientProductId = 8001, // Cải thảo
                    Quantity = 2, // 2kg Cải thảo / 1 Hũ Kim Chi
                    IngredientProduct = new Product { Id = 8001, Name = "Cải thảo", Type = ProductType.Ingredient }
                },
                new RecipesDetailed
                {
                    Id = 2,
                    ParentProductId = 7000,
                    IngredientProductId = 8002, // Ớt bột
                    Quantity = 0.5m, // 0.5kg Ớt bột / 1 Hũ Kim Chi
                    IngredientProduct = new Product { Id = 8002, Name = "Ớt bột", Type = ProductType.Ingredient }
                }
            };
        }

        #region 1. Production Validation & Recipe Checks (5 Cases)
        [Fact]
        public async Task CreateProductionPendingAsync_ValidDocument_CreatesSuccessfully()
        {
            var doc = CreateValidProductionDocument();
            var bInv = CreateManufacturedBInventory();

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(100)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(7000)).ReturnsAsync(CreateValidRecipesDetailed());
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateProductionPendingAsync(doc, 1);

            Assert.NotNull(result);
            Assert.Equal(DocumentType.Production, result.Type);
        }

        [Fact]
        public async Task CreateProductionPendingAsync_NonManufacturedProduct_ThrowsArgumentException()
        {
            var doc = CreateValidProductionDocument();
            var bInv = CreateManufacturedBInventory();
            bInv.Product.Type = ProductType.Regular; // Regular items cannot be produced

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(100)).ReturnsAsync(bInv);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CreateProductionPendingAsync(doc, 1));
        }

        [Fact]
        public async Task CompleteProductionAsync_MissingRecipeDetailed_ThrowsArgumentException()
        {
            var doc = CreateValidProductionDocument();
            var bInv = CreateManufacturedBInventory();

            _mockRepo.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(100)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(7000)).ReturnsAsync(new List<RecipesDetailed>()); // Recipe missing

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CompleteProductionAsync(70, 1));
        }

        [Fact]
        public async Task CompleteProductionAsync_RecipeWithoutIngredients_ThrowsArgumentException()
        {
            var doc = CreateValidProductionDocument();
            var bInv = CreateManufacturedBInventory();

            _mockRepo.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(100)).ReturnsAsync(bInv);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(7000)).ReturnsAsync(new List<RecipesDetailed>()); // Empty ingredients

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CompleteProductionAsync(70, 1));
        }

        [Fact]
        public async Task CompleteProductionAsync_InsufficientIngredientStock_ThrowsInvalidOperationException()
        {
            var doc = CreateValidProductionDocument(); // Produce 10 units -> Requires 10 * 2 = 20kg Cải thảo
            var bInvTP = CreateManufacturedBInventory();
            var recipes = CreateValidRecipesDetailed();

            var bInvCaiThao = new BInventory { Id = 801, BranchId = 1, ProductId = 8001, Quantity = 5, Avg = 10000, LeftOver = 0, Product = new Product { Id = 8001, Name = "Cải thảo" } }; // Only 5kg in stock

            _mockRepo.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(100)).ReturnsAsync(bInvTP);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(7000)).ReturnsAsync(recipes);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 8001)).ReturnsAsync(bInvCaiThao);

            await Assert.ThrowsAsync<MenuGoBE.Exceptions.MenuGoException>(() => _service.CompleteProductionAsync(70, 1));
        }
        #endregion

        #region 2. Production Deduct Ingredients & Add Product Stock Tests (5 Cases)
        [Fact]
        public async Task CompleteProductionAsync_ValidRecipe_DeductsIngredientsAndAddsProductStockWithNewAvgCost()
        {
            var doc = CreateValidProductionDocument(); // Produce 10 Hũ Kim Chi
            var bInvTP = CreateManufacturedBInventory(); // Current qty 0, avg 0
            var recipes = CreateValidRecipesDetailed();

            // Cải thảo (ID 8001): 20kg required (10 * 2 = 20), current stock 50kg, avg 10,000 VNĐ/kg -> Cost = 200,000 VNĐ
            var bInvCaiThao = new BInventory { Id = 801, BranchId = 1, ProductId = 8001, Quantity = 50, Avg = 10000, LeftOver = 0, Product = new Product { Id = 8001, Name = "Cải thảo" } };
            // Ớt bột (ID 8002): 5kg required (10 * 0.5 = 5), current stock 20kg, avg 40,000 VNĐ/kg -> Cost = 200,000 VNĐ
            var bInvOtBot = new BInventory { Id = 802, BranchId = 1, ProductId = 8002, Quantity = 20, Avg = 40000, LeftOver = 0, Product = new Product { Id = 8002, Name = "Ớt bột" } };

            _mockRepo.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(100)).ReturnsAsync(bInvTP);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(7000)).ReturnsAsync(recipes);

            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 8001)).ReturnsAsync(bInvCaiThao);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(801)).ReturnsAsync(bInvCaiThao);

            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 8002)).ReturnsAsync(bInvOtBot);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(802)).ReturnsAsync(bInvOtBot);

            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CompleteProductionAsync(70, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(30, bInvCaiThao.Quantity); // 50 - 20 = 30
            Assert.Equal(15, bInvOtBot.Quantity); // 20 - 5 = 15
            Assert.Equal(10, bInvTP.Quantity); // Produced 10 Hũ
            Assert.Equal(40000, bInvTP.Avg); // Total Ingredient Cost (200k + 200k = 400k) / 10 = 40,000 VNĐ/hũ
            _mockRepo.Verify(r => r.AddInventoryLedgerAsync(It.IsAny<InventoryLedger>()), Times.Exactly(3)); // 2 for ingredients, 1 for TP
        }

        [Fact]
        public async Task CreateProductionCompletedAsync_DirectPostsSuccessfully()
        {
            var doc = CreateValidProductionDocument();
            var bInvTP = CreateManufacturedBInventory();
            var recipes = CreateValidRecipesDetailed();

            var bInvCaiThao = new BInventory { Id = 801, BranchId = 1, ProductId = 8001, Quantity = 50, Avg = 10000, LeftOver = 0, Product = new Product { Id = 8001, Name = "Cải thảo" } };
            var bInvOtBot = new BInventory { Id = 802, BranchId = 1, ProductId = 8002, Quantity = 20, Avg = 40000, LeftOver = 0, Product = new Product { Id = 8002, Name = "Ớt bột" } };

            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(100)).ReturnsAsync(bInvTP);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(7000)).ReturnsAsync(recipes);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 8001)).ReturnsAsync(bInvCaiThao);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(801)).ReturnsAsync(bInvCaiThao);
            _mockRepo.Setup(r => r.GetBInventoryByBranchAndProductAsync(1, 8002)).ReturnsAsync(bInvOtBot);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(802)).ReturnsAsync(bInvOtBot);
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.CreateProductionCompletedAsync(doc, 1);

            Assert.Equal(DocumentStatus.Completed, result.Status);
            Assert.Equal(10, bInvTP.Quantity);
        }

        [Fact]
        public async Task UpdateProductionPendingAsync_ValidDocument_UpdatesSuccessfully()
        {
            var doc = CreateValidProductionDocument();
            var bInvTP = CreateManufacturedBInventory();

            _mockRepo.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(doc);
            _mockRepo.Setup(r => r.GetBInventoryByIdAsync(100)).ReturnsAsync(bInvTP);
            _mockRepo.Setup(r => r.GetRecipesByParentProductIdAsync(7000)).ReturnsAsync(CreateValidRecipesDetailed());
            _mockRepo.Setup(r => r.IsCodeExistsAsync(It.IsAny<string>(), It.IsAny<long>())).ReturnsAsync(false);

            var result = await _service.UpdateProductionPendingAsync(70, doc, 1);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SoftDeleteProductionPendingAsync_ValidPendingDocument_ReturnsTrue()
        {
            var doc = CreateValidProductionDocument();
            _mockRepo.Setup(r => r.GetByIdAsync(70)).ReturnsAsync(doc);

            var success = await _service.SoftDeleteProductionPendingAsync(70, 1, "Hủy sản xuất");
            Assert.True(success);
        }

        [Fact]
        public async Task GetProductionListAsync_ValidBranchId_ReturnsList()
        {
            var list = new List<Document> { CreateValidProductionDocument() };
            _mockRepo.Setup(r => r.GetProductionDocumentsAsync(1)).ReturnsAsync(list);

            var result = await _service.GetProductionListAsync(1);
            Assert.Single(result);
        }
        #endregion
    }
}
