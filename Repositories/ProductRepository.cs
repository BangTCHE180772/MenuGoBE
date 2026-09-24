using MenuGoBE.Data;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Exceptions;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(long id)
        {
            return await _context.Products
                .Include(p => p.Group)
                .Include(p => p.Image)
                .Include(p => p.UnitConversions)
                    .ThenInclude(uc => uc.Unit)
                .Include(p => p.RecipeItems)
                    .ThenInclude(ri => ri.IngredientProduct)
                .Include(p => p.MenuProducts)
                    .ThenInclude(mp => mp.Menu)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Product>> GetAllAsync(ProductType? type = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Group)
                .Include(p => p.Image)
                .AsQueryable();

            if (type.HasValue)
            {
                query = query.Where(p => p.Type == type.Value);
            }

            return await query.Take(200).ToListAsync();
        }

        public async Task<List<Product>> GetByTypeAsync(string type)
        {
            if (Enum.TryParse<ProductType>(type, true, out var parsedType))
            {
                return await GetAllAsync(parsedType);
            }

            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Group)
                .Include(p => p.Image)
                .Include(p => p.UnitConversions)
                    .ThenInclude(uc => uc.Unit)
                .Include(p => p.RecipeItems)
                    .ThenInclude(ri => ri.IngredientProduct)
                .Include(p => p.MenuProducts)
                    .ThenInclude(mp => mp.Menu)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAndTypeAsync(long id, string type)
        {
            if (Enum.TryParse<ProductType>(type, true, out var parsedType))
            {
                return await GetByIdAndTypeAsync(id, parsedType);
            }

            return await GetByIdAsync(id);
        }

        public async Task<Product?> GetByIdAndTypeAsync(long id, ProductType type)
        {
            return await _context.Products
                .Include(p => p.Group)
                .Include(p => p.Image)
                .Include(p => p.UnitConversions)
                    .ThenInclude(uc => uc.Unit)
                .Include(p => p.RecipeItems)
                    .ThenInclude(ri => ri.IngredientProduct)
                .Include(p => p.MenuProducts)
                    .ThenInclude(mp => mp.Menu)
                .FirstOrDefaultAsync(p => p.Id == id && p.Type == type);
        }

        public async Task CreateAsync(Product entity)
        {
            await _context.Products.AddAsync(entity);
        }

        public Task UpdateAsync(Product entity)
        {
            _context.Products.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Product entity)
        {
            _context.Products.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Products.AnyAsync(p => p.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ExistsByNameExcludeIdAsync(string name, long id)
        {
            return await _context.Products.AnyAsync(p => p.Name.ToLower() == name.ToLower() && p.Id != id);
        }

        public async Task<bool> ExistsBySKUAsync(string sku)
        {
            return await _context.Products.AnyAsync(p => p.SKUCode.ToLower() == sku.ToLower());
        }

        public async Task<bool> ExistsBySKUExcludeIdAsync(string sku, long id)
        {
            return await _context.Products.AnyAsync(p => p.SKUCode.ToLower() == sku.ToLower() && p.Id != id);
        }

        public async Task ClearUnitConversionsAsync(long productId)
        {
            var conversions = await _context.UnitConversions
                .Where(uc => uc.ProductId == productId)
                .ToListAsync();

            if (conversions.Count > 0)
            {
                var derived = conversions.Where(uc => uc.BaseId != null).ToList();
                if (derived.Count > 0)
                {
                    _context.UnitConversions.RemoveRange(derived);
                    await _context.SaveChangesAsync();
                }

                var bases = conversions.Where(uc => uc.BaseId == null).ToList();
                if (bases.Count > 0)
                {
                    _context.UnitConversions.RemoveRange(bases);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task SyncUnitConversionsAsync(long productId, List<ProductUnitConversionDto> conversions)
        {
            var existingConversions = await _context.UnitConversions
                .Where(uc => uc.ProductId == productId)
                .ToListAsync();

            if (conversions == null || conversions.Count == 0)
            {
                if (existingConversions.Count > 0)
                {
                    foreach (var existing in existingConversions)
                    {
                        bool isUsed = await _context.DocumentDetails.AnyAsync(dd => dd.UnitConversionId == existing.Id);
                        if (isUsed)
                        {
                            throw new MenuGoException(ErrorCodes.UnitConversionInUseCannotBeDeleted);
                        }
                    }

                    var derived = existingConversions.Where(uc => uc.BaseId != null).ToList();
                    if (derived.Count > 0)
                    {
                        _context.UnitConversions.RemoveRange(derived);
                        await _context.SaveChangesAsync();
                    }

                    var bases = existingConversions.Where(uc => uc.BaseId == null).ToList();
                    if (bases.Count > 0)
                    {
                        _context.UnitConversions.RemoveRange(bases);
                        await _context.SaveChangesAsync();
                    }
                }
                return;
            }

            var baseDto = conversions.First(c => c.IsBase);
            var derivedDtos = conversions.Where(c => !c.IsBase).ToList();

            // 1. Base Unit Conversion
            var existingBase = existingConversions.FirstOrDefault(uc => uc.UnitId == baseDto.UnitId);
            if (existingBase == null)
            {
                existingBase = new UnitConversion
                {
                    ProductId = productId,
                    UnitId = baseDto.UnitId,
                    BaseId = null,
                    ConversionPoint = 1.0m,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.UnitConversions.AddAsync(existingBase);
            }
            else
            {
                existingBase.BaseId = null;
                existingBase.ConversionPoint = 1.0m;
                _context.UnitConversions.Update(existingBase);
            }

            await _context.SaveChangesAsync();

            // 2. Derived Unit Conversions
            var targetDerivedUnitIds = derivedDtos.Select(d => d.UnitId).ToHashSet();

            foreach (var derivedDto in derivedDtos)
            {
                var existingDerived = existingConversions.FirstOrDefault(uc => uc.UnitId == derivedDto.UnitId);
                if (existingDerived == null)
                {
                    var newDerived = new UnitConversion
                    {
                        ProductId = productId,
                        UnitId = derivedDto.UnitId,
                        BaseId = existingBase.Id,
                        ConversionPoint = derivedDto.ConversionPoint,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.UnitConversions.AddAsync(newDerived);
                }
                else
                {
                    existingDerived.BaseId = existingBase.Id;
                    existingDerived.ConversionPoint = derivedDto.ConversionPoint;
                    _context.UnitConversions.Update(existingDerived);
                }
            }

            // 3. Remove conversions that are no longer present in payload
            var conversionsToRemove = existingConversions
                .Where(uc => uc.UnitId != baseDto.UnitId && !targetDerivedUnitIds.Contains(uc.UnitId))
                .ToList();

            if (conversionsToRemove.Count > 0)
            {
                foreach (var toRemove in conversionsToRemove)
                {
                    bool isUsed = await _context.DocumentDetails.AnyAsync(dd => dd.UnitConversionId == toRemove.Id);
                    if (isUsed)
                    {
                        throw new MenuGoException(ErrorCodes.UnitConversionInUseCannotBeDeleted);
                    }
                }

                var derivedToRemove = conversionsToRemove.Where(uc => uc.BaseId != null).ToList();
                if (derivedToRemove.Count > 0)
                {
                    _context.UnitConversions.RemoveRange(derivedToRemove);
                    await _context.SaveChangesAsync();
                }

                var baseToRemove = conversionsToRemove.Where(uc => uc.BaseId == null).ToList();
                if (baseToRemove.Count > 0)
                {
                    _context.UnitConversions.RemoveRange(baseToRemove);
                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ProductType?> GetProductTypeEnumAsync(long id)
        {
            var p = await _context.Products.FindAsync(id);
            return p?.Type;
        }

        public async Task<string?> GetProductTypeAsync(long id)
        {
            var p = await _context.Products.FindAsync(id);
            return p?.Type.ToString();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
