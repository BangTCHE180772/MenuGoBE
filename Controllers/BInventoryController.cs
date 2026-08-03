using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos;
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
        public async Task<IActionResult> GetBInventoriesByBranchOnly(long branchId, [FromQuery] int? mode)
        {
            try
            {
                var query = _context.BInventories.AsNoTracking().Where(b => b.BranchId == branchId);

                if (mode.HasValue)
                {
                    query = mode.Value switch
                    {
                        1 => query.Where(b => b.Product.Type != ProductType.Processed),
                        2 => query.Where(b => b.Product.Type != ProductType.Processed),
                        3 => query.Where(b => b.Product.Type == ProductType.Manufactured),
                        4 => query.Where(b => b.Product.Type == ProductType.Processed),
                        0 => query,
                        _ => query.Where(b => b.Product.Type != ProductType.Processed)
                    };
                }

                var list = await query
                    .Select(b => new
                    {
                        id = b.Id,
                        bInventoryId = b.Id,
                        productId = b.ProductId,
                        branchId = b.BranchId,
                        name = b.Product.Name,
                        code = b.Product.SKUCode ?? $"SP{b.ProductId}",
                        type = b.Product.Type.ToString(),
                        groupName = b.Product.Group != null ? b.Product.Group.Name : "---",
                        description = b.Product.Description,
                        minStorage = b.Product.MinStorage,
                        maxStorage = b.Product.MaxStorage,
                        sellPrice = b.Product.SellPrice,
                        purchasePrice = b.Avg > 0 ? b.Avg : b.Product.SellPrice,
                        quantity = b.Quantity,
                        avg = b.Avg,
                        stockQuantity = b.Quantity,
                        unitConversions = b.Product.UnitConversions
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

                // 2. Xác định Mode
                if (mode.HasValue)
                {
                    query = mode.Value switch
                    {
                        1 => query.Where(b => b.Product.Type != ProductType.Processed),
                        2 => query.Where(b => b.Product.Type != ProductType.Processed),
                        3 => query.Where(b => b.Product.Type == ProductType.Manufactured),
                        4 => query.Where(b => b.Product.Type == ProductType.Processed),
                        0 => query,
                        _ => query.Where(b => b.Product.Type != ProductType.Processed)
                    };
                }
                else
                {
                    // Mặc định cho Inventory: Lấy toàn bộ sản phẩm loại trừ Processed
                    query = query.Where(b => b.Product.Type != ProductType.Processed);
                }

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
                    groupName = b.Product.Group != null ? b.Product.Group.Name : "---",
                    description = b.Product.Description,
                    minStorage = b.Product.MinStorage,
                    maxStorage = b.Product.MaxStorage,
                    sellPrice = b.Product.SellPrice,
                    purchasePrice = b.Avg > 0 ? b.Avg : b.Product.SellPrice,
                    quantity = b.Quantity,
                    avg = b.Avg,
                    stockQuantity = b.Quantity,
                    unitConversions = b.Product.UnitConversions
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

        [HttpGet("{binventoryId}/ledger")]
        public async Task<IActionResult> GetInventoryLedgers(
            long binventoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;

                var query = _context.InventoryLedgers
                    .AsNoTracking()
                    .Include(l => l.Document)
                    .Where(l => l.BInventoryId == binventoryId);

                int totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(l => l.PostedAt)
                    .ThenByDescending(l => l.PostingSequence)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new
                    {
                        id = l.Id,
                        documentId = l.DocumentId,
                        documentCode = l.Document != null ? l.Document.Code : $"SP-LDG-{l.Id}",
                        documentType = l.DocumentType.ToString(),
                        methodName = GetMethodName(l.DocumentType),
                        businessDate = l.PostedAt,
                        unitCost = l.RunningAverageCost,
                        runningAverageCost = l.RunningAverageCost,
                        inventoryValueDelta = l.InventoryValueDelta,
                        quantityDelta = l.QuantityDelta,
                        runningQuantity = l.RunningQuantity
                    })
                    .ToListAsync();

                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = totalPages > 0 ? totalPages : 1,
                    items
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu thẻ kho.", error = ex.Message });
            }
        }

        private static string GetMethodName(DocumentType type)
        {
            return type switch
            {
                DocumentType.Import => "Nhập hàng",
                DocumentType.Export => "Xuất hủy",
                DocumentType.Transfer => "Xuất chuyển kho",
                DocumentType.Return => "Trả hàng nhập",
                DocumentType.Check => "Kiểm kho",
                DocumentType.Production => "Sản xuất",
                _ => "Bán hàng"
            };
        }

        [HttpGet("branch/{branchId}/products-by-date")]
        public async Task<IActionResult> GetProductsByDate(
            long branchId,
            [FromQuery] DateTime? date,
            [FromQuery] string? search,
            [FromQuery] string? productType,
            [FromQuery] bool? onlyManufactured,
            [FromQuery] int? mode)
        {
            try
            {
                var targetDate = date ?? DateTime.UtcNow;

                var query = _context.BInventories
                    .AsNoTracking()
                    .Where(b => b.BranchId == branchId);

                if (mode.HasValue)
                {
                    query = mode.Value switch
                    {
                        1 => query.Where(b => b.Product.Type != ProductType.Processed),
                        2 => query.Where(b => b.Product.Type != ProductType.Processed),
                        3 => query.Where(b => b.Product.Type == ProductType.Manufactured),
                        4 => query.Where(b => b.Product.Type == ProductType.Processed),
                        0 => query,
                        _ => query.Where(b => b.Product.Type != ProductType.Processed)
                    };
                }
                else if (onlyManufactured == true || string.Equals(productType, "Manufactured", StringComparison.OrdinalIgnoreCase) || productType == "3")
                {
                    query = query.Where(b => b.Product.Type == ProductType.Manufactured);
                }
                else if (string.Equals(productType, "Processed", StringComparison.OrdinalIgnoreCase) || productType == "4" || productType == "1")
                {
                    query = query.Where(b => b.Product.Type == ProductType.Processed);
                }
                else
                {
                    query = query.Where(b => b.Product.Type != ProductType.Processed);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var trimmedSearch = search.Trim();
                    query = query.Where(b => EF.Functions.Like(b.Product.Name, $"%{trimmedSearch}%") ||
                                             (b.Product.SKUCode != null && EF.Functions.Like(b.Product.SKUCode, $"%{trimmedSearch}%")));
                }

                var bInventories = await query
                    .Select(b => new
                    {
                        id = b.Id,
                        bInventoryId = b.Id,
                        productId = b.ProductId,
                        branchId = b.BranchId,
                        name = b.Product.Name,
                        code = b.Product.SKUCode ?? $"SP{b.ProductId}",
                        type = b.Product.Type.ToString(),
                        minStorage = b.Product.MinStorage,
                        maxStorage = b.Product.MaxStorage,
                        sellPrice = b.Product.SellPrice,
                        avg = b.Avg,
                        quantity = b.Quantity,
                        baseUnit = b.Product.UnitConversions
                            .Where(uc => uc.BaseId == null)
                            .Select(uc => new
                            {
                                id = uc.Id,
                                unitId = uc.UnitId,
                                unitName = uc.Unit != null ? uc.Unit.Name : string.Empty,
                                conversionPoint = uc.ConversionPoint,
                                isBaseUnit = true
                            })
                            .FirstOrDefault(),
                        unitConversions = b.Product.UnitConversions
                            .Select(uc => new
                            {
                                id = uc.Id,
                                unitId = uc.UnitId,
                                unitName = uc.Unit != null ? uc.Unit.Name : string.Empty,
                                conversionPoint = uc.ConversionPoint,
                                isBaseUnit = uc.BaseId == null
                            }).ToList()
                    })
                    .ToListAsync();

                var bInvIds = bInventories.Select(b => b.id).ToList();

                var ledgers = await _context.InventoryLedgers
                    .AsNoTracking()
                    .Where(l => bInvIds.Contains(l.BInventoryId) && l.PostedAt <= targetDate)
                    .GroupBy(l => l.BInventoryId)
                    .Select(g => g
                        .OrderByDescending(l => l.PostedAt)
                        .ThenByDescending(l => l.PostingSequence)
                        .Select(l => new
                        {
                            l.BInventoryId,
                            l.RunningQuantity,
                            l.RunningAverageCost
                        })
                        .FirstOrDefault())
                    .ToListAsync();

                var ledgerDict = ledgers
                    .Where(l => l != null)
                    .ToDictionary(l => l!.BInventoryId, l => l!);

                var list = bInventories.Select(b =>
                {
                    decimal runningQty = b.quantity;
                    decimal runningAvgCost = b.avg > 0 ? b.avg : b.sellPrice;

                    if (ledgerDict.TryGetValue(b.id, out var l) && l != null)
                    {
                        runningQty = l.RunningQuantity;
                        if (l.RunningAverageCost > 0)
                        {
                            runningAvgCost = l.RunningAverageCost;
                        }
                    }

                    return new
                    {
                        b.id,
                        b.bInventoryId,
                        b.productId,
                        b.branchId,
                        b.name,
                        b.code,
                        b.type,
                        b.minStorage,
                        b.maxStorage,
                        b.sellPrice,
                        b.baseUnit,
                        b.unitConversions,
                        runningQuantity = runningQty,
                        runningAverageCost = runningAvgCost,
                        quantity = runningQty,
                        stockQuantity = runningQty,
                        avg = runningAvgCost,
                        purchasePrice = runningAvgCost > 0 ? runningAvgCost : (b.avg > 0 ? b.avg : b.sellPrice)
                    };
                }).ToList();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi tìm kiếm sản phẩm theo mốc thời gian.", error = ex.Message });
            }
        }

        [HttpPost("ledger-previous")]
        public async Task<IActionResult> GetPreviousLedgerSnapshots([FromBody] BInventoryLedgerSnapshotRequestDto request)
        {
            try
            {
                if (request == null || request.BInventoryIds == null || !request.BInventoryIds.Any())
                {
                    return Ok(new List<BInventoryLedgerSnapshotResponseDto>());
                }

                var result = new List<BInventoryLedgerSnapshotResponseDto>();
                var uniqueIds = request.BInventoryIds.Distinct().ToList();

                var ledgers = await _context.InventoryLedgers
                    .AsNoTracking()
                    .Where(l => uniqueIds.Contains(l.BInventoryId) && l.PostedAt < request.Date)
                    .GroupBy(l => l.BInventoryId)
                    .Select(g => g
                        .OrderByDescending(l => l.PostedAt)
                        .ThenByDescending(l => l.PostingSequence)
                        .Select(l => new
                        {
                            l.BInventoryId,
                            l.RunningQuantity,
                            l.RunningAverageCost
                        })
                        .FirstOrDefault())
                    .ToListAsync();

                var ledgerDict = ledgers
                    .Where(l => l != null)
                    .ToDictionary(l => l!.BInventoryId, l => l!);

                foreach (var id in uniqueIds)
                {
                    if (ledgerDict.TryGetValue(id, out var ledger) && ledger != null)
                    {
                        result.Add(new BInventoryLedgerSnapshotResponseDto
                        {
                            BInventoryId = id,
                            RunningQuantity = ledger.RunningQuantity,
                            RunningAverageCost = ledger.RunningAverageCost
                        });
                    }
                    else
                    {
                        result.Add(new BInventoryLedgerSnapshotResponseDto
                        {
                            BInventoryId = id,
                            RunningQuantity = 0,
                            RunningAverageCost = 0
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu thẻ kho lịch sử.", error = ex.Message });
            }
        }
    }
}