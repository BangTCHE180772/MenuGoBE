using System;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
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

        [HttpGet]
        public async Task<IActionResult> GetBInventories([FromQuery] long? branchId, [FromQuery] string? search)
        {
            try
            {
                var query = _context.BInventories
                    .Include(b => b.Product)
                        .ThenInclude(p => p.UnitConversions)
                            .ThenInclude(uc => uc.Unit)
                    .AsQueryable();

                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(b => b.BranchId == branchId.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.Trim().ToLower();
                    query = query.Where(b => b.Product.Name.ToLower().Contains(lowerSearch) ||
                                             (b.Product.SKUCode != null && b.Product.SKUCode.ToLower().Contains(lowerSearch)));
                }

                var list = await query.Select(b => new
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
                    unitConversions = b.Product.UnitConversions.Select(uc => new
                    {
                        id = uc.Id,
                        unitId = uc.UnitId,
                        unitName = uc.Unit != null ? uc.Unit.Name : string.Empty,
                        conversionPoint = uc.ConversionPoint
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
