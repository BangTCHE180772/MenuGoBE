using AutoMapper;
using MenuGoBE.Data;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Exceptions;
using MenuGoBE.Helpers;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly IUnitRepository _unitRepo;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        public ProductService(IProductRepository repo, IUnitRepository unitRepo, IMapper mapper, AppDbContext context)
        {
            _repo = repo;
            _unitRepo = unitRepo;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ProductViewDto?> GetByIdAsync(long id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null) return null;
            return _mapper.Map<ProductViewDto>(data);
        }

        public async Task<List<ProductViewDto>> GetAllAsync(ProductType? type = null)
        {
            var data = await _repo.GetAllAsync(type);
            return _mapper.Map<List<ProductViewDto>>(data);
        }

        public async Task<List<ProductViewDto>> GetByTypeAsync(string type)
        {
            if (Enum.TryParse<ProductType>(type, true, out var parsedType))
            {
                return await GetAllAsync(parsedType);
            }
            var data = await _repo.GetByTypeAsync(type);
            return _mapper.Map<List<ProductViewDto>>(data);
        }

        public async Task<ProductViewDto?> GetByIdAndTypeAsync(long id, ProductType type)
        {
            var data = await _repo.GetByIdAndTypeAsync(id, type);
            if (data == null) return null;
            return _mapper.Map<ProductViewDto>(data);
        }

        public async Task<ProductViewDto?> GetByIdAndTypeAsync(long id, string type)
        {
            if (Enum.TryParse<ProductType>(type, true, out var parsedType))
            {
                return await GetByIdAndTypeAsync(id, parsedType);
            }
            var data = await _repo.GetByIdAndTypeAsync(id, type);
            if (data == null) return null;
            return _mapper.Map<ProductViewDto>(data);
        }

        public async Task<ProductViewDto> CreateProcessedAsync(ProcessedProductCreateDto dto)
        {
            var genericDto = new ProductCreateDto
            {
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = dto.IsSellable,
                IsManageQuantity = false,
                SellPrice = dto.SellPrice,
                RecommendedTimeMinutes = dto.RecommendedTimeMinutes,
                UnitConversions = dto.UnitConversions,
                Recipe = dto.Recipe,
                MenuIds = dto.MenuIds
            };
            return await CreateAsync(genericDto, ProductType.Processed);
        }

        public async Task<ProductViewDto> CreateManufacturedAsync(ManufacturedProductCreateDto dto)
        {
            var genericDto = new ProductCreateDto
            {
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = dto.IsSellable,
                IsManageQuantity = dto.IsManageQuantity,
                MinStorage = dto.MinStorage,
                MaxStorage = dto.MaxStorage,
                SellPrice = dto.SellPrice,
                UnitConversions = dto.UnitConversions,
                Recipe = dto.Recipe,
                MenuIds = dto.MenuIds
            };
            return await CreateAsync(genericDto, ProductType.Manufactured);
        }

        public async Task<ProductViewDto> CreateRegularAsync(RegularProductCreateDto dto)
        {
            var genericDto = new ProductCreateDto
            {
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = dto.IsSellable,
                IsManageQuantity = dto.IsManageQuantity,
                MinStorage = dto.MinStorage,
                MaxStorage = dto.MaxStorage,
                SellPrice = dto.SellPrice,
                UnitConversions = dto.UnitConversions,
                MenuIds = dto.MenuIds
            };
            return await CreateAsync(genericDto, ProductType.Regular);
        }

        public async Task<ProductViewDto> CreateIngredientAsync(IngredientProductCreateDto dto)
        {
            var genericDto = new ProductCreateDto
            {
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = false,
                IsManageQuantity = dto.IsManageQuantity,
                MinStorage = dto.MinStorage,
                MaxStorage = dto.MaxStorage,
                SellPrice = 0,
                UnitConversions = dto.UnitConversions
            };
            return await CreateAsync(genericDto, ProductType.Ingredient);
        }

        public async Task<ProductViewDto> CreateToolAsync(ToolProductCreateDto dto)
        {
            var genericDto = new ProductCreateDto
            {
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = false,
                IsManageQuantity = dto.IsManageQuantity,
                MinStorage = dto.MinStorage,
                MaxStorage = dto.MaxStorage,
                SellPrice = 0,
                UnitConversions = dto.UnitConversions
            };
            return await CreateAsync(genericDto, ProductType.Tool);
        }

        public async Task<bool> UpdateProcessedAsync(ProcessedProductUpdateDto dto)
        {
            var genericDto = new ProductUpdateDto
            {
                Id = dto.Id,
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = dto.IsSellable,
                IsManageQuantity = false,
                SellPrice = dto.SellPrice,
                RecommendedTimeMinutes = dto.RecommendedTimeMinutes,
                UnitConversions = dto.UnitConversions,
                Recipe = dto.Recipe,
                MenuIds = dto.MenuIds
            };
            return await UpdateAsync(genericDto, ProductType.Processed);
        }

        public async Task<bool> UpdateManufacturedAsync(ManufacturedProductUpdateDto dto)
        {
            var genericDto = new ProductUpdateDto
            {
                Id = dto.Id,
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = dto.IsSellable,
                IsManageQuantity = dto.IsManageQuantity,
                MinStorage = dto.MinStorage,
                MaxStorage = dto.MaxStorage,
                SellPrice = dto.SellPrice,
                UnitConversions = dto.UnitConversions,
                Recipe = dto.Recipe,
                MenuIds = dto.MenuIds
            };
            return await UpdateAsync(genericDto, ProductType.Manufactured);
        }

        public async Task<bool> UpdateRegularAsync(RegularProductUpdateDto dto)
        {
            var genericDto = new ProductUpdateDto
            {
                Id = dto.Id,
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = dto.IsSellable,
                IsManageQuantity = dto.IsManageQuantity,
                MinStorage = dto.MinStorage,
                MaxStorage = dto.MaxStorage,
                SellPrice = dto.SellPrice,
                UnitConversions = dto.UnitConversions,
                MenuIds = dto.MenuIds
            };
            return await UpdateAsync(genericDto, ProductType.Regular);
        }

        public async Task<bool> UpdateIngredientAsync(IngredientProductUpdateDto dto)
        {
            var genericDto = new ProductUpdateDto
            {
                Id = dto.Id,
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = false,
                IsManageQuantity = dto.IsManageQuantity,
                MinStorage = dto.MinStorage,
                MaxStorage = dto.MaxStorage,
                SellPrice = 0,
                UnitConversions = dto.UnitConversions
            };
            return await UpdateAsync(genericDto, ProductType.Ingredient);
        }

        public async Task<bool> UpdateToolAsync(ToolProductUpdateDto dto)
        {
            var genericDto = new ProductUpdateDto
            {
                Id = dto.Id,
                GroupId = dto.GroupId,
                ImageId = dto.ImageId,
                ImageUrl = dto.ImageUrl,
                ChainId = dto.ChainId,
                Name = dto.Name,
                Description = dto.Description,
                SKUCode = dto.SKUCode,
                IsSellable = false,
                IsManageQuantity = dto.IsManageQuantity,
                MinStorage = dto.MinStorage,
                MaxStorage = dto.MaxStorage,
                SellPrice = 0,
                UnitConversions = dto.UnitConversions
            };
            return await UpdateAsync(genericDto, ProductType.Tool);
        }

        public async Task<ProductViewDto> CreateAsync(ProductCreateDto dto, ProductType? overrideType = null)
        {
            ProductType targetType;
            if (overrideType.HasValue)
            {
                targetType = overrideType.Value;
            }
            else if (dto.Type.HasValue)
            {
                targetType = dto.Type.Value;
            }
            else
            {
                throw new MenuGoException(ErrorCodes.ProductNotFound);
            }

            await ValidateProductRulesAsync(targetType, dto.GroupId, dto.SellPrice, dto.IsSellable, dto.MenuIds);

            if (await _repo.ExistsByNameAsync(dto.Name))
            {
                throw new MenuGoException(ErrorCodes.DuplicateProductName);
            }

            if (string.IsNullOrWhiteSpace(dto.SKUCode))
            {
                dto.SKUCode = await GenerateSmartSKUCodeAsync(targetType);
            }
            else
            {
                dto.SKUCode = dto.SKUCode.Trim();
                if (await _repo.ExistsBySKUAsync(dto.SKUCode))
                {
                    throw new MenuGoException(ErrorCodes.DuplicateProductSKU);
                }
            }

            // Validate unit conversions if provided
            if (dto.UnitConversions != null && dto.UnitConversions.Count > 0)
            {
                await ValidateUnitConversionsAsync(targetType, dto.UnitConversions);
            }

            // Extract recipe details from payload
            var recipeDetails = dto.Recipe;

            // Mandatory recipe check for processed & manufactured
            if (targetType == ProductType.Manufactured || targetType == ProductType.Processed)
            {
                var hasBaseMaterials = await _context.Products
                    .AnyAsync(p => p.Type == ProductType.Regular || p.Type == ProductType.Ingredient);

                if (!hasBaseMaterials)
                {
                    throw new MenuGoException(ErrorCodes.IngredientOrRegularRequired);
                }

                if (recipeDetails == null || recipeDetails.Count == 0)
                {
                    throw new MenuGoException(ErrorCodes.RecipeRequired);
                }
            }

            // Validate recipe rules and ingredient product types
            await ValidateRecipeAsync(targetType, dto.Recipe);

            var entity = _mapper.Map<Product>(dto);
            entity.Type = targetType;

            if (entity.Type == ProductType.Processed)
            {
                entity.IsManageQuantity = false;
                entity.MinStorage = 0;
                entity.MaxStorage = 1000000000;
            }
            else if (entity.Type == ProductType.Ingredient || entity.Type == ProductType.Tool)
            {
                entity.IsSellable = false;
                entity.SellPrice = 0;
                if (!entity.IsManageQuantity)
                {
                    entity.MinStorage = 0;
                    entity.MaxStorage = 1000000000;
                }
            }
            else if (!entity.IsManageQuantity)
            {
                entity.MinStorage = 0;
                entity.MaxStorage = 1000000000;
            }

            await _repo.CreateAsync(entity);
            await _repo.SaveChangesAsync(); // Generates Product ID

            // Save unit conversions
            if (dto.UnitConversions != null && dto.UnitConversions.Count > 0)
            {
                await SaveUnitConversionsAsync(entity.Id, dto.UnitConversions);
            }

            // Save direct recipe items
            if (recipeDetails != null && recipeDetails.Count > 0)
            {
                await SaveRecipeItemsAsync(entity.Id, recipeDetails);
            }

            // Populate BInventories for child branches under the same Chain
            var existingBranches = await _context.Branches
                .Where(b => b.ChainId == entity.ChainId)
                .ToListAsync();

            if (existingBranches.Count > 0)
            {
                var existingBranchIds = await _context.BInventories
                    .Where(bi => bi.ProductId == entity.Id)
                    .Select(bi => bi.BranchId)
                    .ToHashSetAsync();

                var bInventories = existingBranches
                    .Where(b => !existingBranchIds.Contains(b.Id))
                    .Select(b => new BInventory
                    {
                        BranchId = b.Id,
                        ProductId = entity.Id,
                        Type = BInventoryHelper.MapProductTypeToBInventoryType(entity.Type),
                        Avg = 0,
                        LeftOver = 0,
                        Quantity = 0,
                        ChainActive = false,
                        BranchActive = false,
                        IsManageQuantity = entity.IsManageQuantity,
                        MinStorage = entity.MinStorage,
                        MaxStorage = entity.MaxStorage,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (bInventories.Count > 0)
                {
                    await _context.BInventories.AddRangeAsync(bInventories);
                    await _context.SaveChangesAsync();
                }
            }

            // Link product to menus if specified
            if (dto.MenuIds != null && dto.MenuIds.Count > 0)
            {
                var distinctMenuIds = dto.MenuIds.Distinct().ToList();
                var menuProducts = distinctMenuIds
                    .Where(mId => !_context.MenuProducts.Any(mp => mp.MenuId == mId && mp.ProductId == entity.Id))
                    .Select(mId => new MenuProduct { MenuId = mId, ProductId = entity.Id })
                    .ToList();

                if (menuProducts.Count > 0)
                {
                    await _context.MenuProducts.AddRangeAsync(menuProducts);
                    await _context.SaveChangesAsync();
                }
            }

            var reloadedEntity = await _repo.GetByIdAsync(entity.Id);
            return _mapper.Map<ProductViewDto>(reloadedEntity ?? entity);
        }

        public async Task<ProductViewDto> CreateWithTypeAsync(ProductCreateDto dto, string type)
        {
            if (Enum.TryParse<ProductType>(type, true, out var parsedType))
            {
                return await CreateAsync(dto, parsedType);
            }
            throw new MenuGoException(ErrorCodes.ProductNotFound);
        }

        public async Task<bool> UpdateAsync(ProductUpdateDto dto, ProductType? overrideType = null)
        {
            Product targetProduct;
            if (overrideType.HasValue)
            {
                var found = await _repo.GetByIdAndTypeAsync(dto.Id, overrideType.Value);
                if (found == null) return false;
                targetProduct = found;
            }
            else
            {
                var found = await _repo.GetByIdAsync(dto.Id);
                if (found == null) return false;
                targetProduct = found;
            }

            var targetType = overrideType ?? dto.Type ?? targetProduct.Type;

            await ValidateProductRulesAsync(targetType, dto.GroupId, dto.SellPrice, dto.IsSellable, dto.MenuIds);

            if (await _repo.ExistsByNameExcludeIdAsync(dto.Name, dto.Id))
            {
                throw new MenuGoException(ErrorCodes.DuplicateProductName);
            }

            if (string.IsNullOrWhiteSpace(dto.SKUCode))
            {
                dto.SKUCode = await GenerateSmartSKUCodeAsync(targetType);
            }
            else
            {
                dto.SKUCode = dto.SKUCode.Trim();
                if (await _repo.ExistsBySKUExcludeIdAsync(dto.SKUCode, dto.Id))
                {
                    throw new MenuGoException(ErrorCodes.DuplicateProductSKU);
                }
            }

            // Validate unit conversions if provided
            if (dto.UnitConversions != null && dto.UnitConversions.Count > 0)
            {
                await ValidateUnitConversionsAsync(targetType, dto.UnitConversions);
            }

            var recipeDetails = dto.Recipe;

            // Mandatory recipe check for processed & manufactured
            if ((targetType == ProductType.Manufactured || targetType == ProductType.Processed) && (recipeDetails == null || recipeDetails.Count == 0))
            {
                throw new MenuGoException(ErrorCodes.RecipeRequired);
            }

            // Validate recipe rules and ingredients
            await ValidateRecipeAsync(targetType, dto.Recipe, dto.Id);

            // Immutability Check: If recipe items are being modified, check if product has existing transactions
            if (targetType == ProductType.Processed || targetType == ProductType.Manufactured)
            {
                var existingItems = await _context.RecipesDetaileds
                    .Where(rd => rd.ParentProductId == dto.Id)
                    .Select(rd => new { rd.IngredientProductId, rd.Quantity })
                    .ToListAsync();

                var proposedItems = recipeDetails?.Select(d => new { IngredientProductId = d.ProductId, Quantity = Math.Truncate(d.Quantity * 1000m) / 1000m }).ToList() ?? new();

                bool recipeChanged = existingItems.Count != proposedItems.Count ||
                    existingItems.Any(e => !proposedItems.Any(p => p.IngredientProductId == e.IngredientProductId && p.Quantity == e.Quantity));

                if (recipeChanged && await HasTransactionRecordsAsync(dto.Id))
                {
                    throw new MenuGoException(ErrorCodes.ProductRecipeLockedDueToTransactions);
                }
            }

            _mapper.Map(dto, targetProduct);
            targetProduct.Type = targetType; // Ensure type remains correct

            if (targetProduct.Type == ProductType.Processed)
            {
                targetProduct.IsManageQuantity = false;
                targetProduct.MinStorage = 0;
                targetProduct.MaxStorage = 1000000000;
            }
            else if (targetProduct.Type == ProductType.Ingredient || targetProduct.Type == ProductType.Tool)
            {
                targetProduct.IsSellable = false;
                targetProduct.SellPrice = 0;
                if (!targetProduct.IsManageQuantity)
                {
                    targetProduct.MinStorage = 0;
                    targetProduct.MaxStorage = 1000000000;
                }
            }
            else if (!targetProduct.IsManageQuantity)
            {
                targetProduct.MinStorage = 0;
                targetProduct.MaxStorage = 1000000000;
            }
            await _repo.UpdateAsync(targetProduct);

            // Sync BInventory IsManageQuantity, MinStorage, MaxStorage
            var relatedBInvs = await _context.BInventories.Where(b => b.ProductId == targetProduct.Id).ToListAsync();
            if (relatedBInvs.Any())
            {
                foreach (var binv in relatedBInvs)
                {
                    binv.IsManageQuantity = targetProduct.IsManageQuantity;
                    binv.MinStorage = targetProduct.MinStorage;
                    binv.MaxStorage = targetProduct.MaxStorage;
                }
                _context.BInventories.UpdateRange(relatedBInvs);
            }

            // Sync unit conversions if unit conversions array is provided in request
            if (dto.UnitConversions != null)
            {
                await _repo.SyncUnitConversionsAsync(targetProduct.Id, dto.UnitConversions);
            }

            // Sync direct recipe items
            if (recipeDetails != null)
            {
                await SaveRecipeItemsAsync(targetProduct.Id, recipeDetails);
            }

            // Sync menu associations if MenuIds array is provided in request
            if (dto.MenuIds != null)
            {
                var existingMenuProducts = await _context.MenuProducts
                    .Where(mp => mp.ProductId == targetProduct.Id)
                    .ToListAsync();

                if (existingMenuProducts.Count > 0)
                {
                    _context.MenuProducts.RemoveRange(existingMenuProducts);
                    await _context.SaveChangesAsync();
                }

                if (dto.MenuIds.Count > 0)
                {
                    var distinctMenuIds = dto.MenuIds.Distinct().ToList();
                    var newMenuProducts = distinctMenuIds.Select(mId => new MenuProduct
                    {
                        MenuId = mId,
                        ProductId = targetProduct.Id
                    }).ToList();
                    await _context.MenuProducts.AddRangeAsync(newMenuProducts);
                }
            }

            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateWithTypeAsync(ProductUpdateDto dto, string type)
        {
            if (Enum.TryParse<ProductType>(type, true, out var parsedType))
            {
                return await UpdateAsync(dto, parsedType);
            }
            return false;
        }

        public async Task<bool> DeleteAsync(long id, ProductType? expectedType = null)
        {
            Product? entity;
            if (expectedType.HasValue)
            {
                entity = await _repo.GetByIdAndTypeAsync(id, expectedType.Value);
            }
            else
            {
                entity = await _repo.GetByIdAsync(id);
            }

            if (entity == null) return false;

            // Safeguard 1: Check if product is used as an ingredient in any recipe, or has parent recipe items
            if (await _context.RecipesDetaileds.AnyAsync(rd => rd.IngredientProductId == id || rd.ParentProductId == id))
            {
                throw new MenuGoException(ErrorCodes.ProductCannotBeDeletedUsedInRecipe);
            }

            // Safeguard 2: Check if product has transaction records (OrderDetails, DocumentDetails, InventoryLedgers)
            if (await HasTransactionRecordsAsync(id))
            {
                throw new MenuGoException(ErrorCodes.ProductCannotBeDeletedUsedInTransactions);
            }

            // Safeguard 3: Check if product is assigned to any Menu
            if (await _context.MenuProducts.AnyAsync(mp => mp.ProductId == id))
            {
                throw new MenuGoException(ErrorCodes.ProductCannotBeDeletedUsedInMenu);
            }

            await _repo.ClearUnitConversionsAsync(entity.Id);

            // Remove associated BInventories
            var bInventories = await _context.BInventories.Where(bi => bi.ProductId == entity.Id).ToListAsync();
            if (bInventories.Count > 0)
            {
                _context.BInventories.RemoveRange(bInventories);
            }

            await _repo.DeleteAsync(entity);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteWithTypeAsync(long id, string type)
        {
            if (Enum.TryParse<ProductType>(type, true, out var parsedType))
            {
                return await DeleteAsync(id, parsedType);
            }
            return false;
        }

        private async Task<bool> HasTransactionRecordsAsync(long productId)
        {
            return await _context.OrderDetails.AnyAsync(od => od.ProductId == productId)
                || await _context.DocumentDetails.AnyAsync(dd => dd.BInventory != null && dd.BInventory.ProductId == productId)
                || await _context.InventoryLedgers.AnyAsync(il => il.BInventory != null && il.BInventory.ProductId == productId);
        }

        private async Task ValidateProductRulesAsync(ProductType targetType, long groupId, decimal sellPrice, bool isSellable, List<long>? menuIds)
        {
            if (groupId <= 0 || !await _context.Groups.AnyAsync(g => g.Id == groupId))
            {
                throw new MenuGoException(ErrorCodes.CategoryRequired);
            }

            if (targetType == ProductType.Processed || targetType == ProductType.Manufactured || targetType == ProductType.Regular)
            {
                if (sellPrice <= 0)
                {
                    throw new MenuGoException(ErrorCodes.SellPriceRequired);
                }
                if (sellPrice > 10000000000m)
                {
                    throw new MenuGoException(ErrorCodes.SellPriceExceedsLimit);
                }
            }
            else if (targetType == ProductType.Ingredient || targetType == ProductType.Tool)
            {
                if (sellPrice != 0)
                {
                    throw new MenuGoException(ErrorCodes.SellPriceNotAllowedForIngredientOrTool);
                }
                if (isSellable)
                {
                    throw new MenuGoException(ErrorCodes.IngredientOrToolIsSellableNotAllowed);
                }
                if (menuIds != null && menuIds.Count > 0)
                {
                    throw new MenuGoException(ErrorCodes.IngredientOrToolMenuNotAllowed);
                }
            }
        }

        private async Task ValidateUnitConversionsAsync(ProductType type, List<ProductUnitConversionDto> conversions)
        {
            if (type == ProductType.Processed)
            {
                if (conversions != null && conversions.Count > 0)
                {
                    throw new MenuGoException(ErrorCodes.ProcessedUnitConversionsNotAllowed);
                }
                return;
            }

            if (conversions == null || conversions.Count == 0)
                return;

            var baseCount = conversions.Count(c => c.IsBase);
            if (baseCount == 0)
            {
                throw new MenuGoException(ErrorCodes.BaseUnitRequired);
            }
            if (baseCount > 1)
            {
                throw new MenuGoException(ErrorCodes.OnlyOneBaseUnitAllowed);
            }

            var unitIds = conversions.Select(c => c.UnitId).ToList();
            if (unitIds.Distinct().Count() != unitIds.Count)
            {
                throw new MenuGoException(ErrorCodes.DuplicateUnitConversion);
            }

            foreach (var conversion in conversions)
            {
                if (await _unitRepo.GetByIdAsync(conversion.UnitId) == null)
                {
                    throw new MenuGoException(ErrorCodes.UnitNotFound);
                }

                if (conversion.IsBase)
                {
                    conversion.ConversionPoint = 1m; // Mặc định đơn vị gốc là 1
                }
                else
                {
                    if (conversion.ConversionPoint % 1 != 0 || conversion.ConversionPoint < 2 || conversion.ConversionPoint > 1000000m)
                    {
                        throw new MenuGoException(ErrorCodes.InvalidConversionPoint);
                    }
                }
            }
        }

        private async Task SaveUnitConversionsAsync(long productId, List<ProductUnitConversionDto> conversions)
        {
            var baseDto = conversions.First(c => c.IsBase);
            var baseEntity = new UnitConversion
            {
                ProductId = productId,
                UnitId = baseDto.UnitId,
                BaseId = null,
                ConversionPoint = 1.0m
            };

            await _context.UnitConversions.AddAsync(baseEntity);
            await _context.SaveChangesAsync();

            var derivedDtos = conversions.Where(c => !c.IsBase).ToList();
            if (derivedDtos.Count > 0)
            {
                var derivedEntities = derivedDtos.Select(dto => new UnitConversion
                {
                    ProductId = productId,
                    UnitId = dto.UnitId,
                    BaseId = baseEntity.Id,
                    ConversionPoint = dto.ConversionPoint
                }).ToList();

                await _context.UnitConversions.AddRangeAsync(derivedEntities);
                await _context.SaveChangesAsync();
            }
        }

        private async Task ValidateRecipeAsync(ProductType targetType, List<ProductRecipeDetailedDto>? recipeDetails, long? productId = null)
        {
            if (targetType != ProductType.Processed && targetType != ProductType.Manufactured)
            {
                if (recipeDetails != null && recipeDetails.Count > 0)
                {
                    throw new MenuGoException(ErrorCodes.RecipeNotAllowed);
                }
                return;
            }

            if (recipeDetails == null || recipeDetails.Count == 0)
            {
                throw new MenuGoException(ErrorCodes.RecipeRequired);
            }

            var proposedIngredientIds = recipeDetails.Select(d => d.ProductId).ToList();

            if (proposedIngredientIds.Distinct().Count() != proposedIngredientIds.Count)
            {
                throw new MenuGoException(ErrorCodes.DuplicateRecipeIngredient);
            }

            if (productId.HasValue && productId.Value > 0)
            {
                if (proposedIngredientIds.Contains(productId.Value) || await WouldFormCycleAsync(productId.Value, proposedIngredientIds))
                {
                    throw new MenuGoException(ErrorCodes.RecipeCycleDetected);
                }
            }

            foreach (var detail in recipeDetails)
            {
                if (productId.HasValue && productId.Value > 0 && detail.ProductId == productId.Value)
                {
                    throw new MenuGoException(ErrorCodes.RecipeCycleDetected);
                }

                var ingredientType = await _repo.GetProductTypeEnumAsync(detail.ProductId);
                if (!ingredientType.HasValue)
                {
                    throw new MenuGoException(ErrorCodes.ProductNotFound);
                }

                if (targetType == ProductType.Processed)
                {
                    // Can have processed, manufactured, ingredient, regular (everything except tool and self)
                    if (ingredientType == ProductType.Tool)
                    {
                        throw new MenuGoException(ErrorCodes.InvalidRecipeIngredient);
                    }
                }
                else if (targetType == ProductType.Manufactured)
                {
                    // Can have manufactured, ingredient, regular (everything except processed, tool and self)
                    if (ingredientType == ProductType.Processed || ingredientType == ProductType.Tool)
                    {
                        throw new MenuGoException(ErrorCodes.InvalidRecipeIngredient);
                    }
                }

                // Validate quantity: quantity must be >= 0.001 and <= 1,000,000, max 3 decimal places
                if (detail.Quantity < 0.001m || detail.Quantity > 1000000m || Math.Truncate(detail.Quantity * 1000m) / 1000m != detail.Quantity)
                {
                    throw new MenuGoException(ErrorCodes.InvalidRecipeQuantity);
                }
            }
        }

        private async Task SaveRecipeItemsAsync(long parentProductId, List<ProductRecipeDetailedDto> recipeDetails)
        {
            var existingItems = await _context.RecipesDetaileds
                .Where(rd => rd.ParentProductId == parentProductId)
                .ToListAsync();

            if (existingItems.Count > 0)
            {
                _context.RecipesDetaileds.RemoveRange(existingItems);
            }

            if (recipeDetails != null && recipeDetails.Count > 0)
            {
                var newItems = recipeDetails.Select(d => new RecipesDetailed
                {
                    ParentProductId = parentProductId,
                    IngredientProductId = d.ProductId,
                    Quantity = Math.Truncate(d.Quantity * 1000m) / 1000m
                }).ToList();

                await _context.RecipesDetaileds.AddRangeAsync(newItems);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> WouldFormCycleAsync(long rootProductId, List<long> proposedIngredientIds)
        {
            var queue = new Queue<long>();
            var visited = new HashSet<long>();

            foreach (var id in proposedIngredientIds)
            {
                if (id == rootProductId)
                {
                    return true; // Direct self-reference
                }
                queue.Enqueue(id);
                visited.Add(id);
            }

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();

                var ingredients = await _context.RecipesDetaileds
                    .Where(rd => rd.ParentProductId == currentId)
                    .Select(rd => rd.IngredientProductId)
                    .ToListAsync();

                foreach (var ingredientId in ingredients)
                {
                    if (ingredientId == rootProductId)
                    {
                        return true; // Cycle detected!
                    }

                    if (!visited.Contains(ingredientId))
                    {
                        visited.Add(ingredientId);
                        queue.Enqueue(ingredientId);
                    }
                }
            }

            return false;
        }

        private async Task<long?> ResolveImageIdAsync(string? imageUrl, long? existingImageId)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return existingImageId;

            var existingImage = await _context.Images
                .FirstOrDefaultAsync(img => img.ImageLink == imageUrl.Trim());

            if (existingImage != null)
                return existingImage.Id;

            var newImage = new Image
            {
                ImageLink = imageUrl.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Images.AddAsync(newImage);
            await _context.SaveChangesAsync();

            return newImage.Id;
        }

        private async Task<string> GenerateSmartSKUCodeAsync(ProductType targetType)
        {
            var prefix = targetType switch
            {
                ProductType.Ingredient => "NL",
                ProductType.Tool => "DC",
                ProductType.Processed => "CB",
                ProductType.Manufactured => "TP",
                _ => "HH"
            };

            var count = await _context.Products.CountAsync(p => p.Type == targetType);
            var nextSeq = count + 1;

            string candidate;
            do
            {
                candidate = $"{prefix}{nextSeq:D6}";
                nextSeq++;
            } while (await _repo.ExistsBySKUAsync(candidate));

            return candidate;
        }

        /// <summary>
        /// Cập nhật giá bán hàng loạt cho sản phẩm Regular, Manufactured, Processed.
        /// Trả về số lượng sản phẩm đã cập nhật thành công.
        /// </summary>
        public async Task<int> BulkUpdateSellPriceAsync(List<SellPriceBulkUpdateItemDto> items)
        {
            if (items == null || items.Count == 0)
                return 0;

            var allowedTypes = new[] { ProductType.Regular, ProductType.Manufactured, ProductType.Processed };
            var ids = items.Select(x => x.Id).Distinct().ToList();

            var products = await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            int updatedCount = 0;

            foreach (var item in items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.Id);
                if (product == null)
                    continue;

                // Chỉ cho phép cập nhật loại Regular, Manufactured, Processed
                if (!allowedTypes.Contains(product.Type))
                    throw new MenuGoException(ErrorCodes.SellPriceNotAllowedForIngredientOrTool);

                // Validate giá
                if (item.SellPrice <= 0)
                    throw new MenuGoException(ErrorCodes.SellPriceRequired);

                if (item.SellPrice > 10000000000m)
                    throw new MenuGoException(ErrorCodes.SellPriceExceedsLimit);

                product.SellPrice = item.SellPrice;
                updatedCount++;
            }

            if (updatedCount > 0)
                await _context.SaveChangesAsync();

            return updatedCount;
        }
    }
}
