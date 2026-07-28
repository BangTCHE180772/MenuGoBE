using System;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenuGoBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BInventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BInventoryController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{branchId}")]
        public async Task<IActionResult> GetBInventoriesByBranchOnly(long branchId)
        {
            try
            {
                var list = await _context.BInventories
                    .AsNoTracking()
                    .Where(b => b.BranchId == branchId)
                    .Select(b => new
                    {
                        id = b.Id,
                        bInventoryId = b.Id,
                        productId = b.ProductId,
                        branchId = b.BranchId,
                        name = b.Product.Name,
                        code = b.Product.SKUCode ?? $"SP{b.ProductId}",
                        type = b.Product.Type.ToString(),
                        sellPrice = b.Product.SellPrice,
                        purchasePrice = b.Avg > 0 ? b.Avg : b.Product.SellPrice,
                        quantity = b.Quantity,
                        avg = b.Avg,
                        stockQuantity = b.Quantity,
                        unitConversions = b.Product.UnitConversions
                            .Where(uc => uc.BaseId == null)
                            .Select(uc => new
                            {
                                id = uc.Id,
                                unitId = uc.UnitId,
                                unitName = uc.Unit != null ? uc.Unit.Name : string.Empty,
                                conversionPoint = uc.ConversionPoint,
                                isBaseUnit = uc.BaseId == null
                            }).ToList(),
                        recipeDetailes = b.Product.RecipeItems
                            .Select(rd => new
                            {
                                id = rd.Id,
                                productId = rd.Id,
                                ingredientId = rd.Id,
                                quantity = rd.Quantity
                            }).ToList()
                    }).ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách BInventory theo chi nhánh.", error = ex.Message });
            }
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetBInventories(
            long? branchId,
            [FromQuery] string? search,
            [FromQuery] int? mode)
        {
            try
            {
                var query = _context.BInventories.AsNoTracking().AsQueryable();

                // 1. Lọc theo Chi nhánh
                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(b => b.BranchId == branchId.Value);
                }

                // 2. Xác định Mode (Mặc định mode = 1)
                int effectiveMode = mode ?? 1;

                query = effectiveMode switch
                {
                    // Mode 1: Lấy tất cả loại trừ Processed và Manufactured
                    1 => query.Where(b => b.Product.Type != ProductType.Processed && b.Product.Type != ProductType.Manufactured),

                    // Mode 2: Lấy tất cả loại trừ Processed
                    2 => query.Where(b => b.Product.Type != ProductType.Processed),

                    // Mode 3: Chỉ lấy Manufactured
                    3 => query.Where(b => b.Product.Type == ProductType.Manufactured),

                    _ => query
                };

                // 3. Lọc Search text
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var trimmedSearch = search.Trim();
                    query = query.Where(b => EF.Functions.Like(b.Product.Name, $"%{trimmedSearch}%") ||
                                             (b.Product.SKUCode != null && EF.Functions.Like(b.Product.SKUCode, $"%{trimmedSearch}%")));
                }

                // 4. Select DTO trực tiếp ở SQL (Đã sửa bỏ ép kiểu (int) ở SQL level)
                var list = await query.Select(b => new
                {
                    id = b.Id,
                    bInventoryId = b.Id,
                    productId = b.ProductId,
                    branchId = b.BranchId,
                    name = b.Product.Name,
                    code = b.Product.SKUCode ?? $"SP{b.ProductId}",
                    type = b.Product.Type.ToString(),
                    // BỎ (int)b.Product.Type Ở ĐÂY để SQL không bị lỗi p."Type"::int
                    sellPrice = b.Product.SellPrice,
                    purchasePrice = b.Avg > 0 ? b.Avg : b.Product.SellPrice,
                    quantity = b.Quantity,
                    avg = b.Avg,
                    stockQuantity = b.Quantity,
                    unitConversions = b.Product.UnitConversions
                        .Where(uc => (effectiveMode == 1 || effectiveMode == 4) || uc.BaseId == null)
                        .Select(uc => new
                        {
                            id = uc.Id,
                            unitId = uc.UnitId,
                            unitName = uc.Unit != null ? uc.Unit.Name : string.Empty,
                            conversionPoint = uc.ConversionPoint,
                            isBaseUnit = uc.BaseId == null
                        }).ToList()
                }).ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách BInventory.", error = ex.Message });
            }
        }
    }
}