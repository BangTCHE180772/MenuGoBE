using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Dtos;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models;
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
        private readonly MenuGoBE.Interface.Services.IPromotionService _promotionService;

        public BInventoryController(AppDbContext context, MenuGoBE.Interface.Services.IPromotionService promotionService)
        {
            _context = context;
            _promotionService = promotionService;
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
                        unitName = b.Product.UnitConversions.Where(uc => uc.BaseId == null).Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty).FirstOrDefault() ?? "Đĩa",
                        groupName = b.Product.Group != null ? b.Product.Group.Name : "---",
                        description = b.Product.Description,
                        minStorage = b.Product.MinStorage,
                        maxStorage = b.Product.MaxStorage,
                        sellPrice = b.Product.SellPrice,
                        purchasePrice = b.Avg > 0 ? b.Avg : b.Product.SellPrice,
                        quantity = b.Quantity,
                        avg = b.Avg,
                        stockQuantity = b.Quantity,
                        customCriticalThreshold = b.CustomCriticalThreshold,
                        customWarningThreshold = b.CustomWarningThreshold,
                        imageLink = b.Product.Image != null ? b.Product.Image.ImageLink : string.Empty,
                        imageUrl = b.Product.Image != null ? b.Product.Image.ImageLink : string.Empty,
                        imageId = b.Product.ImageId,
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
                                productId = rd.ParentProductId,
                                ingredientProductId = rd.IngredientProductId,
                                ingredientId = rd.IngredientProductId,
                                ingredientName = rd.IngredientProduct != null ? rd.IngredientProduct.Name : string.Empty,
                                ingredientCode = rd.IngredientProduct != null ? rd.IngredientProduct.SKUCode : string.Empty,
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

        #region Lấy thông tin chi tiết một mặt hàng tồn kho theo Id
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetBInventoryDetailById(long id)
        {
            try
            {
                var item = await _context.BInventories
                    .AsNoTracking()
                    .Include(b => b.Product)
                        .ThenInclude(p => p.Image)
                    .Include(b => b.Product)
                        .ThenInclude(p => p.Group)
                    .Include(b => b.Product)
                        .ThenInclude(p => p.UnitConversions)
                            .ThenInclude(uc => uc.Unit)
                    .Include(b => b.Product)
                        .ThenInclude(p => p.RecipeItems)
                    .FirstOrDefaultAsync(b => b.Id == id || b.ProductId == id);

                if (item == null)
                {
                    return NotFound(new { message = "Không tìm thấy mặt hàng tồn kho." });
                }

                var baseUnit = item.Product.UnitConversions
                    .Where(uc => uc.BaseId == null)
                    .Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                    .FirstOrDefault();

                var dto = new
                {
                    id = item.Id,
                    bInventoryId = item.Id,
                    productId = item.ProductId,
                    branchId = item.BranchId,
                    name = item.Product.Name,
                    code = item.Product.SKUCode ?? $"SP{item.ProductId}",
                    type = item.Product.Type.ToString(),
                    unitName = !string.IsNullOrEmpty(baseUnit) ? baseUnit : "Đĩa",
                    groupName = item.Product.Group != null ? item.Product.Group.Name : "---",
                    description = item.Product.Description,
                    minStorage = item.Product.MinStorage,
                    maxStorage = item.Product.MaxStorage,
                    sellPrice = item.Product.SellPrice,
                    purchasePrice = item.Avg > 0 ? item.Avg : item.Product.SellPrice,
                    quantity = item.Quantity,
                    avg = item.Avg,
                    stockQuantity = item.Quantity,
                    imageLink = item.Product.Image != null ? item.Product.Image.ImageLink : string.Empty,
                    imageUrl = item.Product.Image != null ? item.Product.Image.ImageLink : string.Empty,
                    imageId = item.Product.ImageId,
                    unitConversions = item.Product.UnitConversions
                        .Select(uc => new
                        {
                            id = uc.Id,
                            unitId = uc.UnitId,
                            unitName = uc.Unit != null ? uc.Unit.Name : string.Empty,
                            conversionPoint = uc.ConversionPoint,
                            isBaseUnit = uc.BaseId == null
                        }).ToList(),
                    recipeDetailes = item.Product.RecipeItems
                        .Select(rd => new
                        {
                            id = rd.Id,
                            productId = rd.ParentProductId,
                            ingredientProductId = rd.IngredientProductId,
                            ingredientId = rd.IngredientProductId,
                            ingredientName = rd.IngredientProduct != null ? rd.IngredientProduct.Name : string.Empty,
                            ingredientCode = rd.IngredientProduct != null ? rd.IngredientProduct.SKUCode : string.Empty,
                            quantity = rd.Quantity
                        }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy chi tiết mặt hàng tồn kho.", error = ex.Message });
            }
        }
        #endregion

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

                var list = await query.Select(b => new
                {
                    id = b.Id,
                    bInventoryId = b.Id,
                    productId = b.ProductId,
                    branchId = b.BranchId,
                    name = b.Product.Name,
                    code = b.Product.SKUCode ?? $"SP{b.ProductId}",
                    type = b.Product.Type.ToString(),
                    unitName = b.Product.UnitConversions.Where(uc => uc.BaseId == null).Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty).FirstOrDefault() ?? "Đĩa",
                    groupId = b.Product.GroupId,
                    groupName = b.Product.Group != null ? b.Product.Group.Name : "---",
                    description = b.Product.Description,
                    minStorage = b.Product.MinStorage,
                    maxStorage = b.Product.MaxStorage,
                    sellPrice = b.Product.SellPrice,
                    purchasePrice = b.Avg > 0 ? b.Avg : b.Product.SellPrice,
                    quantity = b.Quantity,
                    avg = b.Avg,
                    stockQuantity = b.Quantity,
                    customCriticalThreshold = b.CustomCriticalThreshold,
                    customWarningThreshold = b.CustomWarningThreshold,
                    imageLink = b.Product.Image != null ? b.Product.Image.ImageLink : string.Empty,
                    imageUrl = b.Product.Image != null ? b.Product.Image.ImageLink : string.Empty,
                    imageId = b.Product.ImageId,
                    createdAt = b.Product.CreatedAt,
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
                            productId = rd.ParentProductId,
                            ingredientProductId = rd.IngredientProductId,
                            ingredientId = rd.IngredientProductId,
                            ingredientName = rd.IngredientProduct != null ? rd.IngredientProduct.Name : string.Empty,
                            ingredientCode = rd.IngredientProduct != null ? rd.IngredientProduct.SKUCode : string.Empty,
                            unitName = rd.IngredientProduct != null && rd.IngredientProduct.UnitConversions != null
                                ? (rd.IngredientProduct.UnitConversions.Where(uc => uc.BaseId == null).Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty).FirstOrDefault() ?? "Đơn vị")
                                : "Đơn vị",
                            quantity = rd.Quantity
                        }).ToList(),
                    recipeItems = b.Product.RecipeItems
                        .Select(rd => new
                        {
                            id = rd.Id,
                            productId = rd.ParentProductId,
                            ingredientProductId = rd.IngredientProductId,
                            ingredientId = rd.IngredientProductId,
                            ingredientName = rd.IngredientProduct != null ? rd.IngredientProduct.Name : string.Empty,
                            ingredientCode = rd.IngredientProduct != null ? rd.IngredientProduct.SKUCode : string.Empty,
                            unitName = rd.IngredientProduct != null && rd.IngredientProduct.UnitConversions != null
                                ? (rd.IngredientProduct.UnitConversions.Where(uc => uc.BaseId == null).Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty).FirstOrDefault() ?? "Đơn vị")
                                : "Đơn vị",
                            quantity = rd.Quantity
                        }).ToList()
                }).ToListAsync();

                // Áp dụng khuyến mãi
                if (branchId.HasValue && branchId.Value > 0)
                {
                    var promos = await _promotionService.GetActivePromotionMapForBranchAsync(branchId.Value);
                    if (promos.Count > 0)
                    {
                        var discountedList = list.Select(item =>
                        {
                            decimal finalPrice = item.sellPrice;
                            decimal? originalPrice = null;

                            if (promos.TryGetValue(item.productId, out var promo) || promos.TryGetValue(0, out promo))
                            {
                                if (promo.DiscountType == "Percentage")
                                {
                                    decimal discount = (item.sellPrice * promo.DiscountValue) / 100m;
                                    if (promo.MaxDiscount > 0 && discount > promo.MaxDiscount)
                                    {
                                        discount = promo.MaxDiscount;
                                    }
                                    finalPrice = Math.Max(0, item.sellPrice - discount);
                                }
                                else
                                {
                                    finalPrice = Math.Max(0, item.sellPrice - promo.DiscountValue);
                                }
                                originalPrice = item.sellPrice;
                            }

                            return new
                            {
                                item.id,
                                item.bInventoryId,
                                item.productId,
                                item.branchId,
                                item.name,
                                item.code,
                                item.type,
                                item.unitName,
                                item.groupName,
                                item.description,
                                item.minStorage,
                                item.maxStorage,
                                sellPrice = finalPrice,
                                originalPrice, // Thêm originalPrice
                                item.purchasePrice,
                                item.quantity,
                                item.avg,
                                item.stockQuantity,
                                item.imageLink,
                                item.imageUrl,
                                item.imageId,
                                item.createdAt,
                                item.unitConversions,
                                item.recipeDetailes,
                                item.recipeItems
                            };
                        }).ToList();

                        return Ok(discountedList);
                    }
                }

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
                        createdDate = l.PostedAt,
                        createdAt = l.CreatedAt,
                        date = l.PostedAt,
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
                DocumentType.Export => "Xuất Hủy",
                DocumentType.ExportDelete => "Xuất Hủy",
                DocumentType.Transfer => "Chuyển Hàng",
                DocumentType.Return => "Trả Hàng",
                DocumentType.Check => "Kiểm kho",
                DocumentType.Production => "Sản xuất",
                DocumentType.CostAdjustment => "Điều chỉnh giá vốn",
                DocumentType.CustomerReturn => "Khách trả hàng",
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
                        imageLink = b.Product.Image != null ? b.Product.Image.ImageLink : string.Empty,
                        createdAt = b.Product.CreatedAt,
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
                            }).ToList(),
                        recipeDetailes = b.Product.RecipeItems
                            .Select(rd => new
                            {
                                id = rd.Id,
                                productId = rd.ParentProductId,
                                ingredientProductId = rd.IngredientProductId,
                                ingredientId = rd.IngredientProductId,
                                ingredientName = rd.IngredientProduct != null ? rd.IngredientProduct.Name : string.Empty,
                                ingredientCode = rd.IngredientProduct != null ? rd.IngredientProduct.SKUCode : string.Empty,
                                unitName = rd.IngredientProduct != null && rd.IngredientProduct.UnitConversions != null
                                    ? (rd.IngredientProduct.UnitConversions.Where(uc => uc.BaseId == null).Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty).FirstOrDefault() ?? "Đơn vị")
                                    : "Đơn vị",
                                quantity = rd.Quantity
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
                        b.imageLink,
                        b.createdAt,
                        runningQuantity = runningQty,
                        runningAverageCost = runningAvgCost,
                        quantity = runningQty,
                        stockQuantity = runningQty,
                        avg = runningAvgCost,
                        purchasePrice = runningAvgCost > 0 ? runningAvgCost : (b.avg > 0 ? b.avg : b.sellPrice),
                        recipeDetailes = b.recipeDetailes,
                        recipeItems = b.recipeDetailes
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

        #region Lấy Báo Cáo Tiêu Thụ Nguyên Liệu
        /// <summary>
        /// Lấy báo cáo tiêu thụ nguyên liệu và biến động sai số cho một chi nhánh trong một khoảng thời gian.
        /// </summary>
        [HttpGet("branch/{branchId}/consumption-report")]
        public async Task<IActionResult> GetConsumptionReport(
            long branchId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // Đồng bộ múi giờ UTC
                if (start.Kind != DateTimeKind.Utc) start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                if (end.Kind != DateTimeKind.Utc) end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

                var query = _context.BInventories
                    .AsNoTracking()
                    .Include(b => b.Product)
                        .ThenInclude(p => p.UnitConversions)
                            .ThenInclude(uc => uc.Unit)
                    .Where(b => b.BranchId == branchId && b.Product.Type != ProductType.Processed);

                var bInventories = await query.ToListAsync();
                var bInvIds = bInventories.Select(b => b.Id).ToList();

                // Lấy toàn bộ thẻ kho liên quan để tối ưu hóa truy vấn
                var ledgers = await _context.InventoryLedgers
                    .AsNoTracking()
                    .Where(l => bInvIds.Contains(l.BInventoryId))
                    .OrderBy(l => l.PostedAt)
                    .ThenBy(l => l.PostingSequence)
                    .ToListAsync();

                var report = new List<object>();

                foreach (var bInv in bInventories)
                {
                    var itemLedgers = ledgers.Where(l => l.BInventoryId == bInv.Id).ToList();

                    // 1. Tồn đầu kỳ (Opening Stock)
                    var lastBeforeStart = itemLedgers
                        .Where(l => l.PostedAt < start)
                        .OrderByDescending(l => l.PostedAt)
                        .ThenByDescending(l => l.PostingSequence)
                        .FirstOrDefault();
                    decimal openingStock = lastBeforeStart?.RunningQuantity ?? 0;

                    // 2. Tồn cuối kỳ (Closing Stock)
                    var lastBeforeEnd = itemLedgers
                        .Where(l => l.PostedAt <= end)
                        .OrderByDescending(l => l.PostedAt)
                        .ThenByDescending(l => l.PostingSequence)
                        .FirstOrDefault();
                    decimal closingStock = lastBeforeEnd?.RunningQuantity ?? (lastBeforeStart?.RunningQuantity ?? 0);

                    // Danh sách thẻ kho trong kỳ
                    var periodLedgers = itemLedgers.Where(l => l.PostedAt >= start && l.PostedAt <= end).ToList();

                    // 3. Nhập (Import)
                    decimal import = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Import)
                        .Sum(l => l.QuantityDelta);

                    // 4. Chuyển vào (Transfer In)
                    decimal transferIn = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Transfer && l.QuantityDelta > 0)
                        .Sum(l => l.QuantityDelta);

                    // 5. Chuyển ra (Transfer Out)
                    decimal transferOut = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Transfer && l.QuantityDelta < 0)
                        .Sum(l => Math.Abs(l.QuantityDelta));

                    // 6. Tiêu thụ định mức (Expected Consumption) / Bán hàng (trừ Khách trả hàng)
                    decimal saleQty = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Sale)
                        .Sum(l => Math.Abs(l.QuantityDelta));

                    // 6e. Khách hàng trả lại hàng (Customer Return)
                    decimal customerReturn = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.CustomerReturn)
                        .Sum(l => l.QuantityDelta);

                    decimal expectedConsumption = saleQty - customerReturn;
                    if (expectedConsumption < 0) expectedConsumption = 0;

                    // 6a. Xuất hủy (Destruction)
                    decimal destruction = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Export || l.DocumentType == DocumentType.ExportDelete)
                        .Sum(l => Math.Abs(l.QuantityDelta));

                    // 6b. Điều chỉnh (Adjustment)
                    decimal adjustment = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Check)
                        .Sum(l => l.QuantityDelta);

                    // 6c. Sản xuất / Chế biến (Production)
                    decimal production = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Production && l.QuantityDelta < 0)
                        .Sum(l => Math.Abs(l.QuantityDelta));

                    // 6d. Trả hàng nhà cung cấp (Return to Vendor)
                    decimal returnVendor = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Return)
                        .Sum(l => Math.Abs(l.QuantityDelta));

                    // 7. Tồn lý thuyết (Theoretical Stock)
                    decimal theoreticalStock = openingStock + import + transferIn - transferOut - expectedConsumption - destruction - production - returnVendor;

                    // 8. Chênh lệch (Variance) = Tồn cuối - Tồn lý thuyết
                    decimal variance = closingStock - theoreticalStock;

                    // 9. Sai số % (Variance %)
                    decimal variancePercent = 0;
                    if (expectedConsumption > 0)
                    {
                        variancePercent = Math.Round((variance / expectedConsumption) * 100, 2);
                    }
                    else if (variance != 0)
                    {
                        variancePercent = variance > 0 ? 100 : -100;
                    }

                    // Tiêu hao thực tế ghi nhận
                    decimal systemConsumption = openingStock + import + transferIn - transferOut + customerReturn - destruction - production - returnVendor - closingStock;
                    if (systemConsumption < 0) systemConsumption = 0;

                    var unitName = bInv.Product.UnitConversions
                        .Where(uc => uc.BaseId == null)
                        .Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                        .FirstOrDefault() ?? "Đĩa";

                    report.Add(new
                    {
                        id = bInv.Product.SKUCode ?? $"SP{bInv.ProductId}",
                        bInventoryId = bInv.Id,
                        name = bInv.Product.Name,
                        category = bInv.Product.Group != null ? bInv.Product.Group.Name : "---",
                        unit = unitName,
                        openingStock,
                        import,
                        transferIn,
                        transferOut,
                        destruction,
                        production,
                        returnVendor,
                        customerReturn,
                        adjustment,
                        systemConsumption,
                        expectedConsumption,
                        theoreticalStock,
                        variance,
                        variancePercent,
                        closingStock
                    });
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lập báo cáo tiêu hao.", error = ex.Message });
            }
        }
        #endregion

        #region Lấy Nhật Ký Giao Dịch Kho
        /// <summary>
        /// Lấy nhật ký giao dịch kho (Inventory Detail Report) cho một chi nhánh trong một khoảng thời gian.
        /// </summary>
        [HttpGet("branch/{branchId}/transaction-ledger")]
        public async Task<IActionResult> GetTransactionLedger(
            long branchId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // Đồng bộ múi giờ UTC
                if (start.Kind != DateTimeKind.Utc) start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                if (end.Kind != DateTimeKind.Utc) end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

                var ledgers = await _context.InventoryLedgers
                    .AsNoTracking()
                    .Include(l => l.Document)
                    .Include(l => l.BInventory)
                        .ThenInclude(b => b.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                    .Where(l => l.BInventory.BranchId == branchId && l.PostedAt >= start && l.PostedAt <= end)
                    .OrderByDescending(l => l.PostedAt)
                    .ThenByDescending(l => l.PostingSequence)
                    .ToListAsync();

                var list = ledgers.Select(l => {
                    var docTypeStr = GetTransactionTypeLabel(l.DocumentType, l.QuantityDelta);
                    decimal importQty = 0;
                    decimal exportQty = 0;
                    decimal adjustQty = 0;

                    if (docTypeStr == "Nhập kho" || docTypeStr == "Khách trả hàng")
                    {
                        importQty = l.QuantityDelta;
                    }
                    else if (docTypeStr == "Xuất kho" || docTypeStr == "Xuất hủy" || docTypeStr == "Trả hàng")
                    {
                        exportQty = Math.Abs(l.QuantityDelta);
                    }
                    else // Điều chỉnh, Kiểm kho, v.v.
                    {
                        adjustQty = l.QuantityDelta;
                    }

                    var unitName = l.BInventory?.Product?.UnitConversions?
                        .Where(uc => uc.BaseId == null)
                        .Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                        .FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(unitName))
                    {
                        unitName = "Đĩa";
                    }

                    return new
                    {
                        id = l.Id,
                        time = l.PostedAt.ToString("HH:mm dd/MM/yyyy"),
                        docCode = l.Document != null ? l.Document.Code : $"SP-LDG-{l.Id}",
                        transactionType = docTypeStr,
                        ingredientName = l.SnapshotProductName,
                        quantity = l.QuantityDelta,
                        unit = unitName,
                        importQty,
                        exportQty,
                        adjustQty,
                        createdBy = l.SnapshotPostedByName ?? "Hệ thống",
                        note = l.Document != null ? l.Document.Note : string.Empty
                    };
                }).ToList();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy nhật ký giao dịch kho.", error = ex.Message });
            }
        }

        private static string GetTransactionTypeLabel(DocumentType type, decimal delta)
        {
            return type switch
            {
                DocumentType.Import => "Nhập kho",
                DocumentType.Return => "Trả Hàng",
                DocumentType.Sale => "Bán hàng",
                DocumentType.CustomerReturn => "Khách trả hàng",
                DocumentType.Transfer => "Chuyển Hàng",
                DocumentType.Export => "Xuất Hủy",
                DocumentType.ExportDelete => "Xuất Hủy",
                DocumentType.Production => "Sản xuất",
                DocumentType.Check => "Kiểm kho",
                DocumentType.CostAdjustment => "Điều chỉnh",
                _ => delta >= 0 ? "Nhập kho" : "Xuất kho"
            };
        }
        #endregion

        #region Phase 4: Quản lý Lô Hàng, Hạn Sử Dụng & Báo Cáo Truy Vết (Batch APIs)

        #region API Lấy danh sách Lô hàng phân trang và lọc (GET /api/BInventory/batches)
        [HttpGet("batches")]
        [HttpGet("/api/Batch")]
        public async Task<IActionResult> GetBatches(
            [FromQuery] long? branchId,
            [FromQuery] long? productId,
            [FromQuery] long? binventoryId,
            [FromQuery] string? search,
            [FromQuery] BatchStatus? status,
            [FromQuery] string? expiryStatus,
            [FromQuery] int nearExpiryDays = 7,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = now.Date;
                var nearExpiryLimit = today.AddDays(nearExpiryDays > 0 ? nearExpiryDays : 7);

                var query = _context.BInventoryBatches
                    .AsNoTracking()
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Branch)
                    .Include(b => b.SourceBatch)
                        .ThenInclude(sb => sb!.BInventory)
                            .ThenInclude(sbi => sbi.Branch)
                    .AsQueryable();

                // Lọc theo Chi nhánh
                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(b => b.BInventory.BranchId == branchId.Value);
                }

                // Lọc theo Sản phẩm
                if (productId.HasValue && productId.Value > 0)
                {
                    query = query.Where(b => b.BInventory.ProductId == productId.Value);
                }

                // Lọc theo BInventory
                if (binventoryId.HasValue && binventoryId.Value > 0)
                {
                    query = query.Where(b => b.BInventoryId == binventoryId.Value);
                }

                // Lọc theo Trạng thái Lô
                if (status.HasValue)
                {
                    query = query.Where(b => b.Status == status.Value);
                }

                // Tìm kiếm theo từ khóa (Mã Lô, Tên sản phẩm, Mã SKU)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var kw = search.Trim().ToLower();
                    query = query.Where(b =>
                        b.BatchCode.ToLower().Contains(kw) ||
                        b.BInventory.Product.Name.ToLower().Contains(kw) ||
                        (b.BInventory.Product.SKUCode != null && b.BInventory.Product.SKUCode.ToLower().Contains(kw)));
                }

                // Lọc theo Trạng thái Hạn sử dụng
                if (!string.IsNullOrWhiteSpace(expiryStatus) && !expiryStatus.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var exp = expiryStatus.Trim().ToLower();
                    if (exp == "expired")
                    {
                        query = query.Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date < today && b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted);
                    }
                    else if (exp == "near_expiry" || exp == "nearexpiry")
                    {
                        query = query.Where(b => b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date >= today && b.ExpiryDate.Value.Date <= nearExpiryLimit && b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted);
                    }
                    else if (exp == "valid")
                    {
                        query = query.Where(b => (!b.ExpiryDate.HasValue || b.ExpiryDate.Value.Date > nearExpiryLimit) && b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted);
                    }
                    else if (exp == "no_expiry" || exp == "noexpiry")
                    {
                        query = query.Where(b => !b.ExpiryDate.HasValue);
                    }
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    // 1. Đang hoạt động (Active) lên đầu
                    .OrderBy(b => b.Status == BatchStatus.Active ? 0 : 1)
                    // 2. Hạn Dùng: Expired → NearExpiry (gần nhất lên trên) → Valid → NoExpiry/Depleted
                    .ThenBy(b =>
                        b.QuantityRemaining <= 0 || b.Status == BatchStatus.Depleted ? 4
                        : !b.ExpiryDate.HasValue ? 3
                        : b.ExpiryDate.Value.Date < today ? 0
                        : b.ExpiryDate.Value.Date <= nearExpiryLimit ? 1
                        : 2)
                    // Trong mỗi nhóm HSD: sắp xếp theo ngày hạn dùng tăng dần (sắp hết hạn nhất lên trên)
                    .ThenBy(b => b.ExpiryDate.HasValue ? b.ExpiryDate.Value : DateTime.MaxValue)
                    // 3. Ngày nhập xa nhất (cũ nhất) lên đầu
                    .ThenBy(b => b.ReceivedDate)
                    .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
                    .Take(Math.Max(pageSize, 1))
                    .Select(b => new BatchDetailDto
                    {
                        Id = b.Id,
                        BInventoryId = b.BInventoryId,
                        BatchCode = b.BatchCode,
                        QuantityOriginal = b.QuantityOriginal,
                        QuantityRemaining = b.QuantityRemaining,
                        UnitCost = b.UnitCost,
                        ManufactureDate = b.ManufactureDate,
                        ExpiryDate = b.ExpiryDate,
                        ReceivedDate = b.ReceivedDate,
                        SourceBatchId = b.SourceBatchId,
                        SourceBatchCode = b.SourceBatch != null ? b.SourceBatch.BatchCode : null,
                        Status = b.Status,
                        IsNotificationMuted = b.IsNotificationMuted,
                        CreatedAt = b.CreatedAt,
                        UpdatedAt = b.UpdatedAt,
                        ProductId = b.BInventory.ProductId,
                        ProductName = b.BInventory.Product.Name,
                        ProductCode = b.BInventory.Product.SKUCode ?? $"SP{b.BInventory.ProductId}",
                        UnitName = b.BInventory.Product.UnitConversions
                            .Where(uc => uc.BaseId == null)
                            .Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                            .FirstOrDefault() ?? "Đơn vị",
                        BranchId = b.BInventory.BranchId,
                        BranchName = b.BInventory.Branch.Name,
                        SourceBranchId = b.SourceBatch != null ? b.SourceBatch.BInventory.BranchId : (long?)null,
                        SourceBranchName = b.SourceBatch != null ? b.SourceBatch.BInventory.Branch.Name : null,
                        DaysUntilExpiry = b.ExpiryDate.HasValue ? (int?)(b.ExpiryDate.Value.Date - today).TotalDays : null,
                        ExpiryStatus = (b.QuantityRemaining <= 0 || b.Status == BatchStatus.Depleted)
                            ? "Depleted"
                            : (!b.ExpiryDate.HasValue
                                ? "NoExpiry"
                                : (b.ExpiryDate.Value.Date < today
                                    ? "Expired"
                                    : (b.ExpiryDate.Value.Date <= nearExpiryLimit ? "NearExpiry" : "Valid")))
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalCount,
                    page = Math.Max(page, 1),
                    pageSize = Math.Max(pageSize, 1),
                    totalPages = (int)Math.Ceiling((double)totalCount / Math.Max(pageSize, 1)),
                    items
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách Lô hàng.", error = ex.Message });
            }
        }
        #endregion

        #region API Lấy danh sách Lô theo BInventoryId (GET /api/BInventory/{binventoryId}/batches)
        [HttpGet("{binventoryId:long}/batches")]
        public async Task<IActionResult> GetBatchesByBInventoryId(long binventoryId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var nearExpiryLimit = today.AddDays(7);

                var list = await _context.BInventoryBatches
                    .AsNoTracking()
                    .Where(b => b.BInventoryId == binventoryId)
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Branch)
                    .Include(b => b.SourceBatch)
                        .ThenInclude(sb => sb!.BInventory)
                            .ThenInclude(sbi => sbi.Branch)
                    .OrderBy(b => b.ExpiryDate.HasValue ? 0 : 1)
                    .ThenBy(b => b.ExpiryDate)
                    .ThenByDescending(b => b.CreatedAt)
                    .Select(b => new BatchDetailDto
                    {
                        Id = b.Id,
                        BInventoryId = b.BInventoryId,
                        BatchCode = b.BatchCode,
                        QuantityOriginal = b.QuantityOriginal,
                        QuantityRemaining = b.QuantityRemaining,
                        UnitCost = b.UnitCost,
                        ManufactureDate = b.ManufactureDate,
                        ExpiryDate = b.ExpiryDate,
                        ReceivedDate = b.ReceivedDate,
                        SourceBatchId = b.SourceBatchId,
                        SourceBatchCode = b.SourceBatch != null ? b.SourceBatch.BatchCode : null,
                        Status = b.Status,
                        IsNotificationMuted = b.IsNotificationMuted,
                        CreatedAt = b.CreatedAt,
                        UpdatedAt = b.UpdatedAt,
                        ProductId = b.BInventory.ProductId,
                        ProductName = b.BInventory.Product.Name,
                        ProductCode = b.BInventory.Product.SKUCode ?? $"SP{b.BInventory.ProductId}",
                        UnitName = b.BInventory.Product.UnitConversions
                            .Where(uc => uc.BaseId == null)
                            .Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                            .FirstOrDefault() ?? "Đơn vị",
                        BranchId = b.BInventory.BranchId,
                        BranchName = b.BInventory.Branch.Name,
                        SourceBranchId = b.SourceBatch != null ? b.SourceBatch.BInventory.BranchId : (long?)null,
                        SourceBranchName = b.SourceBatch != null ? b.SourceBatch.BInventory.Branch.Name : null,
                        DaysUntilExpiry = b.ExpiryDate.HasValue ? (int?)(b.ExpiryDate.Value.Date - today).TotalDays : null,
                        ExpiryStatus = !b.ExpiryDate.HasValue
                            ? "NoExpiry"
                            : (b.ExpiryDate.Value.Date < today
                                ? "Expired"
                                : (b.ExpiryDate.Value.Date <= nearExpiryLimit ? "NearExpiry" : "Valid"))
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách Lô theo mặt hàng.", error = ex.Message });
            }
        }
        #endregion

        #region API Lấy chi tiết một Lô hàng (GET /api/BInventory/batches/{id})
        [HttpGet("batches/{id:long}")]
        [HttpGet("/api/Batch/{id:long}")]
        public async Task<IActionResult> GetBatchById(long id)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var nearExpiryLimit = today.AddDays(7);

                var batch = await _context.BInventoryBatches
                    .AsNoTracking()
                    .Where(b => b.Id == id)
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Branch)
                    .Include(b => b.SourceBatch)
                        .ThenInclude(sb => sb!.BInventory)
                            .ThenInclude(sbi => sbi.Branch)
                    .Select(b => new BatchDetailDto
                    {
                        Id = b.Id,
                        BInventoryId = b.BInventoryId,
                        BatchCode = b.BatchCode,
                        QuantityOriginal = b.QuantityOriginal,
                        QuantityRemaining = b.QuantityRemaining,
                        UnitCost = b.UnitCost,
                        ManufactureDate = b.ManufactureDate,
                        ExpiryDate = b.ExpiryDate,
                        ReceivedDate = b.ReceivedDate,
                        SourceBatchId = b.SourceBatchId,
                        SourceBatchCode = b.SourceBatch != null ? b.SourceBatch.BatchCode : null,
                        Status = b.Status,
                        IsNotificationMuted = b.IsNotificationMuted,
                        CreatedAt = b.CreatedAt,
                        UpdatedAt = b.UpdatedAt,
                        ProductId = b.BInventory.ProductId,
                        ProductName = b.BInventory.Product.Name,
                        ProductCode = b.BInventory.Product.SKUCode ?? $"SP{b.BInventory.ProductId}",
                        UnitName = b.BInventory.Product.UnitConversions
                            .Where(uc => uc.BaseId == null)
                            .Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                            .FirstOrDefault() ?? "Đơn vị",
                        BranchId = b.BInventory.BranchId,
                        BranchName = b.BInventory.Branch.Name,
                        SourceBranchId = b.SourceBatch != null ? b.SourceBatch.BInventory.BranchId : (long?)null,
                        SourceBranchName = b.SourceBatch != null ? b.SourceBatch.BInventory.Branch.Name : null,
                        DaysUntilExpiry = b.ExpiryDate.HasValue ? (int?)(b.ExpiryDate.Value.Date - today).TotalDays : null,
                        ExpiryStatus = !b.ExpiryDate.HasValue
                            ? "NoExpiry"
                            : (b.ExpiryDate.Value.Date < today
                                ? "Expired"
                                : (b.ExpiryDate.Value.Date <= nearExpiryLimit ? "NearExpiry" : "Valid"))
                    })
                    .FirstOrDefaultAsync();

                if (batch == null)
                {
                    return NotFound(new { message = $"Không tìm thấy Lô hàng với ID: {id}" });
                }

                return Ok(batch);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy chi tiết Lô hàng.", error = ex.Message });
            }
        }
        #endregion

        #region API Lấy lịch sử phân bổ của Lô hàng (GET /api/BInventory/batches/{id}/allocations)
        [HttpGet("batches/{id:long}/allocations")]
        [HttpGet("/api/Batch/{id:long}/allocations")]
        public async Task<IActionResult> GetBatchAllocations(
            long id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.BatchAllocations
                    .AsNoTracking()
                    .Where(a => a.BatchId == id)
                    .Include(a => a.Batch)
                    .Include(a => a.DocumentDetail)
                        .ThenInclude(dd => dd.Document)
                            .ThenInclude(d => d.Branch)
                    .Include(a => a.DocumentDetail)
                        .ThenInclude(dd => dd.Document)
                            .ThenInclude(d => d.Partner)
                    .OrderByDescending(a => a.CreatedAt)
                    .AsQueryable();

                var totalCount = await query.CountAsync();

                var items = await query
                    .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
                    .Take(Math.Max(pageSize, 1))
                    .Select(a => new BatchAllocationTraceDto
                    {
                        Id = a.Id,
                        DocumentDetailId = a.DocumentDetailId,
                        BatchId = a.BatchId,
                        BatchCode = a.Batch != null ? a.Batch.BatchCode : string.Empty,
                        AllocationType = a.AllocationType,
                        AllocationTypeName = string.Empty,
                        QuantityAllocated = a.QuantityAllocated,
                        UnitCost = a.UnitCost,
                        CreatedAt = a.CreatedAt,
                        DocumentId = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Id : (long?)null,
                        OrderId = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.OrderId : null,
                        DocumentCode = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Code : null,
                        DocumentType = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Type : null,
                        DocumentTypeName = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Type.ToString() : null,
                        DocumentPostedAt = a.DocumentDetail != null && a.DocumentDetail.Document != null ? (a.DocumentDetail.Document.OrderDate != default ? a.DocumentDetail.Document.OrderDate : a.DocumentDetail.Document.CreatedAt) : null,
                        BranchName = a.DocumentDetail != null && a.DocumentDetail.Document != null && a.DocumentDetail.Document.Branch != null ? a.DocumentDetail.Document.Branch.Name : null,
                        PartnerName = a.DocumentDetail != null && a.DocumentDetail.Document != null
                            ? (a.DocumentDetail.Document.SnapshotPartnerName ?? (a.DocumentDetail.Document.Partner != null ? a.DocumentDetail.Document.Partner.Name : null))
                            : null,
                        Note = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Note : null
                    })
                    .ToListAsync();

                foreach (var item in items)
                {
                    item.AllocationTypeName = GetAllocationTypeLabel(item.AllocationType, item.OrderId, item.DocumentCode);
                }

                return Ok(new
                {
                    totalCount,
                    page = Math.Max(page, 1),
                    pageSize = Math.Max(pageSize, 1),
                    totalPages = (int)Math.Ceiling((double)totalCount / Math.Max(pageSize, 1)),
                    items
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy lịch sử phân bổ Lô.", error = ex.Message });
            }
        }
        #endregion

        #region API Báo cáo Truy vết vòng đời Lô (GET /api/BInventory/batches/{id}/traceability)
        [HttpGet("batches/{id:long}/traceability")]
        [HttpGet("/api/Batch/{id:long}/traceability")]
        public async Task<IActionResult> GetBatchTraceability(long id)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var nearExpiryLimit = today.AddDays(7);

                var currentBatch = await _context.BInventoryBatches
                    .AsNoTracking()
                    .Where(b => b.Id == id)
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Product)
                            .ThenInclude(p => p.UnitConversions)
                                .ThenInclude(uc => uc.Unit)
                    .Include(b => b.BInventory)
                        .ThenInclude(bi => bi.Branch)
                    .Include(b => b.SourceBatch)
                        .ThenInclude(sb => sb!.BInventory)
                            .ThenInclude(sbi => sbi.Branch)
                    .Select(b => new BatchDetailDto
                    {
                        Id = b.Id,
                        BInventoryId = b.BInventoryId,
                        BatchCode = b.BatchCode,
                        QuantityOriginal = b.QuantityOriginal,
                        QuantityRemaining = b.QuantityRemaining,
                        UnitCost = b.UnitCost,
                        ManufactureDate = b.ManufactureDate,
                        ExpiryDate = b.ExpiryDate,
                        ReceivedDate = b.ReceivedDate,
                        SourceBatchId = b.SourceBatchId,
                        SourceBatchCode = b.SourceBatch != null ? b.SourceBatch.BatchCode : null,
                        Status = b.Status,
                        IsNotificationMuted = b.IsNotificationMuted,
                        CreatedAt = b.CreatedAt,
                        UpdatedAt = b.UpdatedAt,
                        ProductId = b.BInventory.ProductId,
                        ProductName = b.BInventory.Product.Name,
                        ProductCode = b.BInventory.Product.SKUCode ?? $"SP{b.BInventory.ProductId}",
                        UnitName = b.BInventory.Product.UnitConversions
                            .Where(uc => uc.BaseId == null)
                            .Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                            .FirstOrDefault() ?? "Đơn vị",
                        BranchId = b.BInventory.BranchId,
                        BranchName = b.BInventory.Branch.Name,
                        SourceBranchId = b.SourceBatch != null ? b.SourceBatch.BInventory.BranchId : (long?)null,
                        SourceBranchName = b.SourceBatch != null ? b.SourceBatch.BInventory.Branch.Name : null,
                        DaysUntilExpiry = b.ExpiryDate.HasValue ? (int?)(b.ExpiryDate.Value.Date - today).TotalDays : null,
                        ExpiryStatus = !b.ExpiryDate.HasValue
                            ? "NoExpiry"
                            : (b.ExpiryDate.Value.Date < today
                                ? "Expired"
                                : (b.ExpiryDate.Value.Date <= nearExpiryLimit ? "NearExpiry" : "Valid"))
                    })
                    .FirstOrDefaultAsync();

                if (currentBatch == null)
                {
                    return NotFound(new { message = $"Không tìm thấy Lô hàng ID: {id}" });
                }

                BatchDetailDto? sourceBatch = null;
                if (currentBatch.SourceBatchId.HasValue)
                {
                    sourceBatch = await _context.BInventoryBatches
                        .AsNoTracking()
                        .Where(b => b.Id == currentBatch.SourceBatchId.Value)
                        .Include(b => b.BInventory)
                            .ThenInclude(bi => bi.Product)
                                .ThenInclude(p => p.UnitConversions)
                                    .ThenInclude(uc => uc.Unit)
                        .Include(b => b.BInventory)
                            .ThenInclude(bi => bi.Branch)
                        .Select(b => new BatchDetailDto
                        {
                            Id = b.Id,
                            BInventoryId = b.BInventoryId,
                            BatchCode = b.BatchCode,
                            QuantityOriginal = b.QuantityOriginal,
                            QuantityRemaining = b.QuantityRemaining,
                            UnitCost = b.UnitCost,
                            ManufactureDate = b.ManufactureDate,
                            ExpiryDate = b.ExpiryDate,
                            ReceivedDate = b.ReceivedDate,
                            Status = b.Status,
                            IsNotificationMuted = b.IsNotificationMuted,
                            CreatedAt = b.CreatedAt,
                            ProductId = b.BInventory.ProductId,
                            ProductName = b.BInventory.Product.Name,
                            ProductCode = b.BInventory.Product.SKUCode ?? $"SP{b.BInventory.ProductId}",
                            UnitName = b.BInventory.Product.UnitConversions
                                .Where(uc => uc.BaseId == null)
                                .Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                                .FirstOrDefault() ?? "Đơn vị",
                            BranchId = b.BInventory.BranchId,
                        BranchName = b.BInventory.Branch.Name
                        })
                        .FirstOrDefaultAsync();
                }

                var allocations = await _context.BatchAllocations
                    .AsNoTracking()
                    .Where(a => a.BatchId == id)
                    .Include(a => a.Batch)
                    .Include(a => a.DocumentDetail)
                        .ThenInclude(dd => dd.Document)
                            .ThenInclude(d => d.Branch)
                    .Include(a => a.DocumentDetail)
                        .ThenInclude(dd => dd.Document)
                            .ThenInclude(d => d.Partner)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new BatchAllocationTraceDto
                    {
                        Id = a.Id,
                        DocumentDetailId = a.DocumentDetailId,
                        BatchId = a.BatchId,
                        BatchCode = a.Batch != null ? a.Batch.BatchCode : string.Empty,
                        AllocationType = a.AllocationType,
                        AllocationTypeName = string.Empty,
                        QuantityAllocated = a.QuantityAllocated,
                        UnitCost = a.UnitCost,
                        CreatedAt = a.CreatedAt,
                        DocumentId = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Id : (long?)null,
                        OrderId = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.OrderId : null,
                        DocumentCode = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Code : null,
                        DocumentType = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Type : null,
                        DocumentTypeName = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Type.ToString() : null,
                        DocumentPostedAt = a.DocumentDetail != null && a.DocumentDetail.Document != null ? (a.DocumentDetail.Document.OrderDate != default ? a.DocumentDetail.Document.OrderDate : a.DocumentDetail.Document.CreatedAt) : null,
                        BranchName = a.DocumentDetail != null && a.DocumentDetail.Document != null && a.DocumentDetail.Document.Branch != null ? a.DocumentDetail.Document.Branch.Name : null,
                        PartnerName = a.DocumentDetail != null && a.DocumentDetail.Document != null
                            ? (a.DocumentDetail.Document.SnapshotPartnerName ?? (a.DocumentDetail.Document.Partner != null ? a.DocumentDetail.Document.Partner.Name : null))
                            : null,
                        Note = a.DocumentDetail != null && a.DocumentDetail.Document != null ? a.DocumentDetail.Document.Note : null
                    })
                    .ToListAsync();

                foreach (var alloc in allocations)
                {
                    alloc.AllocationTypeName = GetAllocationTypeLabel(alloc.AllocationType, alloc.OrderId, alloc.DocumentCode);
                }

                #region Tìm phiếu nhập gốc ban đầu nếu có
                long? initialImportDocId = null;
                string? initialImportDocCode = null;

                var searchBatchCode = currentBatch.BatchCode;
                var searchInventoryId = currentBatch.BInventoryId;
                if (currentBatch.SourceBatchId.HasValue && sourceBatch != null)
                {
                    searchBatchCode = sourceBatch.BatchCode;
                    searchInventoryId = sourceBatch.BInventoryId;
                }

                var importDetail = await _context.DocumentDetails
                    .AsNoTracking()
                    .Include(dd => dd.Document)
                        .ThenInclude(d => d.Partner)
                    .Include(dd => dd.Document)
                        .ThenInclude(d => d.Branch)
                    .Where(dd => dd.BInventoryId == searchInventoryId
                              && dd.BatchCodeSnapshot == searchBatchCode
                              && dd.Document != null
                              && dd.Document.Type == MenuGoBE.Models.Enums.DocumentType.Import)
                    .OrderByDescending(dd => dd.Id)
                    .FirstOrDefaultAsync();

                if (importDetail != null && importDetail.Document != null)
                {
                    initialImportDocId = importDetail.Document.Id;
                    initialImportDocCode = importDetail.Document.Code;

                    // Nếu danh sách allocations chưa có bản ghi của phiếu nhập này, bổ sung vào đầu danh sách
                    var hasImportAlloc = allocations.Any(a => a.DocumentId == importDetail.Document.Id);
                    if (!hasImportAlloc)
                    {
                        allocations.Add(new BatchAllocationTraceDto
                        {
                            Id = 0,
                            DocumentDetailId = importDetail.Id,
                            BatchId = currentBatch.Id,
                            BatchCode = currentBatch.BatchCode,
                            AllocationType = (BatchAllocationType)0,
                            AllocationTypeName = "Nhập hàng",
                            QuantityAllocated = currentBatch.QuantityOriginal,
                            UnitCost = currentBatch.UnitCost,
                            CreatedAt = importDetail.Document.OrderDate != default ? importDetail.Document.OrderDate : importDetail.CreatedAt,
                            DocumentId = importDetail.Document.Id,
                            DocumentCode = importDetail.Document.Code,
                            DocumentType = MenuGoBE.Models.Enums.DocumentType.Import,
                            DocumentTypeName = "Import",
                            DocumentPostedAt = importDetail.Document.OrderDate != default ? importDetail.Document.OrderDate : importDetail.CreatedAt,
                            BranchName = importDetail.Document.Branch?.Name,
                            PartnerName = importDetail.Document.SnapshotPartnerName ?? importDetail.Document.Partner?.Name,
                            Note = importDetail.Document.Note
                        });
                    }
                }
                #endregion

                return Ok(new BatchTraceabilityDto
                {
                    CurrentBatch = currentBatch,
                    SourceBatch = sourceBatch,
                    Allocations = allocations,
                    InitialImportDocumentId = initialImportDocId,
                    InitialImportDocumentCode = initialImportDocCode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy thông tin truy vết Lô.", error = ex.Message });
            }
        }
        #endregion

        #region API Thống kê tổng hợp Hạn sử dụng (GET /api/BInventory/batches/expiry-summary)
        [HttpGet("batches/expiry-summary")]
        [HttpGet("/api/Batch/expiry-summary")]
        public async Task<IActionResult> GetBatchExpirySummary(
            [FromQuery] long? branchId,
            [FromQuery] int? nearExpiryDays = null)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                int critDays = 7;
                int warnDays = 14;

                if (branchId.HasValue && branchId.Value > 0)
                {
                    var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId.Value);
                    if (branch != null)
                    {
                        critDays = branch.BatchCriticalDays >= 7 ? branch.BatchCriticalDays : 7;
                        warnDays = branch.BatchWarningDays >= 14 ? branch.BatchWarningDays : 14;
                    }
                }

                if (nearExpiryDays.HasValue && nearExpiryDays.Value > 0)
                {
                    warnDays = nearExpiryDays.Value;
                }

                var nearLimit = today.AddDays(warnDays);
                var criticalLimit = today.AddDays(critDays);

                var query = _context.BInventoryBatches.AsNoTracking().AsQueryable();

                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(b => b.BInventory.BranchId == branchId.Value);
                }

                var totalBatches = await query.CountAsync();
                var activeBatches = await query.CountAsync(b => b.Status == BatchStatus.Active && b.QuantityRemaining > 0);
                var expiredBatches = await query.CountAsync(b => b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted && b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date < today);
                var nearExpiryBatches = await query.CountAsync(b => b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted && b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date >= today && b.ExpiryDate.Value.Date <= nearLimit);
                var criticalBatches = await query.CountAsync(b => b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted && b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date >= today && b.ExpiryDate.Value.Date <= criticalLimit);
                var validBatches = await query.CountAsync(b => b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted && b.ExpiryDate.HasValue && b.ExpiryDate.Value.Date > nearLimit);
                var noExpiryBatches = await query.CountAsync(b => b.QuantityRemaining > 0 && b.Status != BatchStatus.Depleted && !b.ExpiryDate.HasValue);

                return Ok(new BatchExpirySummaryDto
                {
                    TotalBatches = totalBatches,
                    ActiveBatches = activeBatches,
                    ExpiredBatches = expiredBatches,
                    NearExpiryBatches = nearExpiryBatches,
                    CriticalBatches = criticalBatches,
                    ValidBatches = validBatches,
                    NoExpiryBatches = noExpiryBatches
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi tổng hợp hạn sử dụng Lô hàng.", error = ex.Message });
            }
        }
        #endregion

        #region API Lấy cấu hình ngưỡng cảnh báo hạn dùng (GET /api/BInventory/batches/alert-settings)
        [HttpGet("batches/alert-settings")]
        [HttpGet("/api/Batch/alert-settings")]
        public async Task<IActionResult> GetBatchAlertSettings([FromQuery] long? branchId)
        {
            try
            {
                Branch? branch = null;
                if (branchId.HasValue && branchId.Value > 0)
                {
                    branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId.Value);
                }
                else
                {
                    branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync();
                }

                return Ok(new BatchAlertSettingsDto
                {
                    BranchId = branch?.Id ?? (branchId ?? 0),
                    CriticalDays = branch?.BatchCriticalDays >= 7 ? branch.BatchCriticalDays : 7,
                    WarningDays = branch?.BatchWarningDays >= 14 ? branch.BatchWarningDays : 14
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy cấu hình cảnh báo hạn dùng.", error = ex.Message });
            }
        }
        #endregion

        #region API Cập nhật cấu hình ngưỡng cảnh báo hạn dùng (PUT /api/BInventory/batches/alert-settings)
        [HttpPut("batches/alert-settings")]
        [HttpPut("/api/Batch/alert-settings")]
        public async Task<IActionResult> UpdateBatchAlertSettings([FromBody] BatchAlertSettingsDto dto)
        {
            try
            {
                // Kiểm tra ràng buộc tối thiểu và quan hệ giữa 2 ngưỡng
                if (dto.CriticalDays < 7)
                {
                    return BadRequest(new { message = "Số ngày thông báo cấp bách tối thiểu là 7 ngày." });
                }

                if (dto.WarningDays < 14)
                {
                    return BadRequest(new { message = "Số ngày thông báo cảnh báo tối thiểu là 14 ngày." });
                }

                if (dto.WarningDays < dto.CriticalDays)
                {
                    return BadRequest(new { message = "Số ngày cảnh báo phải lớn hơn hoặc bằng số ngày cấp bách." });
                }

                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == dto.BranchId);
                if (branch == null)
                {
                    return NotFound(new { message = $"Không tìm thấy chi nhánh với ID: {dto.BranchId}" });
                }

                // Cập nhật cấu hình ngưỡng cho chi nhánh
                branch.BatchCriticalDays = dto.CriticalDays;
                branch.BatchWarningDays = dto.WarningDays;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật cấu hình thông báo cảnh báo hạn dùng thành công.",
                    data = new BatchAlertSettingsDto
                    {
                        BranchId = branch.Id,
                        CriticalDays = branch.BatchCriticalDays,
                        WarningDays = branch.BatchWarningDays
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật cấu hình thông báo cảnh báo hạn dùng.", error = ex.Message });
            }
        }
        #endregion

        #region API Bật/Tắt chuông thông báo cho một Lô hàng (PUT /api/BInventory/batches/{id}/toggle-mute)
        [HttpPut("batches/{id:long}/toggle-mute")]
        [HttpPut("/api/Batch/{id:long}/toggle-mute")]
        public async Task<IActionResult> ToggleBatchNotificationMute(long id)
        {
            try
            {
                var batch = await _context.BInventoryBatches.FirstOrDefaultAsync(b => b.Id == id);
                if (batch == null)
                {
                    return NotFound(new { message = $"Không tìm thấy Lô hàng với ID: {id}" });
                }

                // Đảo ngược trạng thái tắt thông báo
                batch.IsNotificationMuted = !batch.IsNotificationMuted;
                batch.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var statusText = batch.IsNotificationMuted ? "Đã tắt nhận thông báo cho Lô hàng này." : "Đã bật nhận thông báo cho Lô hàng này.";
                return Ok(new
                {
                    message = statusText,
                    batchId = batch.Id,
                    isNotificationMuted = batch.IsNotificationMuted
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật trạng thái thông báo của Lô hàng.", error = ex.Message });
            }
        }
        #endregion

        #region API Lấy cấu hình cảnh báo tồn kho chi nhánh (GET /api/BInventory/stock-alert-settings)
        [HttpGet("stock-alert-settings")]
        public async Task<IActionResult> GetStockAlertSettings([FromQuery] long? branchId)
        {
            try
            {
                Branch? branch = null;
                if (branchId.HasValue && branchId.Value > 0)
                {
                    branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId.Value);
                }
                else
                {
                    branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync();
                }

                return Ok(new StockAlertSettingsDto
                {
                    BranchId = branch?.Id ?? (branchId ?? 0),
                    CriticalThreshold = branch != null && branch.StockCriticalThreshold > 0 ? branch.StockCriticalThreshold : 20,
                    WarningThreshold = branch != null && branch.StockWarningThreshold > 0 ? branch.StockWarningThreshold : 40
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy cấu hình cảnh báo tồn kho.", error = ex.Message });
            }
        }
        #endregion

        #region API Cập nhật cấu hình cảnh báo tồn kho chi nhánh (PUT /api/BInventory/stock-alert-settings)
        [HttpPut("stock-alert-settings")]
        public async Task<IActionResult> UpdateStockAlertSettings([FromBody] StockAlertSettingsDto dto)
        {
            try
            {
                // Kiểm tra ràng buộc lớn hơn 0
                if (dto.CriticalThreshold <= 0)
                {
                    return BadRequest(new { message = "Số lượng cảnh báo cấp bách phải lớn hơn 0." });
                }

                if (dto.WarningThreshold <= 0)
                {
                    return BadRequest(new { message = "Số lượng cảnh báo phải lớn hơn 0." });
                }

                if (dto.WarningThreshold < dto.CriticalThreshold)
                {
                    return BadRequest(new { message = "Số lượng cảnh báo phải lớn hơn hoặc bằng số lượng cấp bách." });
                }

                var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == dto.BranchId);
                if (branch == null)
                {
                    return NotFound(new { message = $"Không tìm thấy chi nhánh với ID: {dto.BranchId}" });
                }

                // Cập nhật cấu hình ngưỡng tồn kho cho chi nhánh
                branch.StockCriticalThreshold = dto.CriticalThreshold;
                branch.StockWarningThreshold = dto.WarningThreshold;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật cấu hình cảnh báo tồn kho chi nhánh thành công.",
                    data = new StockAlertSettingsDto
                    {
                        BranchId = branch.Id,
                        CriticalThreshold = branch.StockCriticalThreshold,
                        WarningThreshold = branch.StockWarningThreshold
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật cấu hình cảnh báo tồn kho.", error = ex.Message });
            }
        }
        #endregion

        #region API Lấy cấu hình cảnh báo tồn kho riêng của từng mặt hàng (GET /api/BInventory/{binventoryId}/alert-settings)
        [HttpGet("{binventoryId:long}/alert-settings")]
        public async Task<IActionResult> GetItemStockAlertSettings(long binventoryId)
        {
            try
            {
                var binv = await _context.BInventories
                    .AsNoTracking()
                    .Include(b => b.Product)
                        .ThenInclude(p => p.UnitConversions)
                            .ThenInclude(uc => uc.Unit)
                    .Include(b => b.Branch)
                    .FirstOrDefaultAsync(b => b.Id == binventoryId);

                if (binv == null)
                {
                    return NotFound(new { message = $"Không tìm thấy bản ghi tồn kho với ID: {binventoryId}" });
                }

                var unitName = binv.Product?.UnitConversions
                    ?.Where(uc => uc.BaseId == null)
                    ?.Select(uc => uc.Unit != null ? uc.Unit.Name : string.Empty)
                    ?.FirstOrDefault() ?? "Đơn vị";

                var branchCrit = binv.Branch != null && binv.Branch.StockCriticalThreshold > 0 ? binv.Branch.StockCriticalThreshold : 20m;
                var branchWarn = binv.Branch != null && binv.Branch.StockWarningThreshold > 0 ? binv.Branch.StockWarningThreshold : 40m;

                return Ok(new ItemStockAlertSettingsDto
                {
                    BInventoryId = binv.Id,
                    ProductId = binv.ProductId,
                    ProductName = binv.Product?.Name ?? string.Empty,
                    ProductCode = binv.Product?.SKUCode ?? $"SP{binv.ProductId}",
                    UnitName = unitName,
                    CurrentStock = binv.Quantity,
                    CustomCriticalThreshold = binv.CustomCriticalThreshold,
                    CustomWarningThreshold = binv.CustomWarningThreshold,
                    BranchCriticalThreshold = branchCrit,
                    BranchWarningThreshold = branchWarn,
                    IsAlertEnabled = binv.IsAlertEnabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy cấu hình cảnh báo mặt hàng.", error = ex.Message });
            }
        }
        #endregion

        #region API Cập nhật cấu hình cảnh báo tồn kho riêng của từng mặt hàng (PUT /api/BInventory/{binventoryId}/alert-settings)
        [HttpPut("{binventoryId:long}/alert-settings")]
        public async Task<IActionResult> UpdateItemStockAlertSettings(long binventoryId, [FromBody] UpdateItemStockAlertSettingsDto dto)
        {
            try
            {
                var binv = await _context.BInventories.FirstOrDefaultAsync(b => b.Id == binventoryId);
                if (binv == null)
                {
                    return NotFound(new { message = $"Không tìm thấy bản ghi tồn kho với ID: {binventoryId}" });
                }

                // Kiểm tra nếu có nhập giá trị thì phải > 0
                if (dto.CustomCriticalThreshold.HasValue && dto.CustomCriticalThreshold.Value <= 0)
                {
                    return BadRequest(new { message = "Số lượng cảnh báo cấp bách tùy chỉnh phải lớn hơn 0." });
                }

                if (dto.CustomWarningThreshold.HasValue && dto.CustomWarningThreshold.Value <= 0)
                {
                    return BadRequest(new { message = "Số lượng cảnh báo tùy chỉnh phải lớn hơn 0." });
                }

                if (dto.CustomCriticalThreshold.HasValue && dto.CustomWarningThreshold.HasValue && dto.CustomWarningThreshold.Value < dto.CustomCriticalThreshold.Value)
                {
                    return BadRequest(new { message = "Số lượng cảnh báo tùy chỉnh phải lớn hơn hoặc bằng số lượng cấp bách." });
                }

                // Cập nhật cấu hình riêng (null nghĩa là sử dụng theo thiết lập chung)
                binv.CustomCriticalThreshold = dto.CustomCriticalThreshold;
                binv.CustomWarningThreshold = dto.CustomWarningThreshold;
                binv.IsAlertEnabled = dto.IsAlertEnabled;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cập nhật cấu hình cảnh báo mặt hàng thành công.",
                    customCriticalThreshold = binv.CustomCriticalThreshold,
                    customWarningThreshold = binv.CustomWarningThreshold,
                    isAlertEnabled = binv.IsAlertEnabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi cập nhật cấu hình cảnh báo mặt hàng.", error = ex.Message });
            }
        }
        #endregion

        #region Helper: Chuyển đổi tên loại phân bổ tiếng Việt
        private static string GetAllocationTypeLabel(BatchAllocationType type, long? orderId = null, string? docCode = null)
        {
            if ((int)type == 0)
            {
                return "Nhập hàng";
            }

            if (type == BatchAllocationType.Sale)
            {
                if ((orderId.HasValue && orderId.Value > 0) || (!string.IsNullOrEmpty(docCode) && (docCode.StartsWith("PXBH") || docCode.StartsWith("HD"))))
                {
                    return "Bán hàng";
                }
                return "Xuất hàng";
            }

            return type switch
            {
                BatchAllocationType.Disposal => "Xuất Hủy",
                BatchAllocationType.TransferOut => "Chuyển Hàng",
                BatchAllocationType.TransferReturn => "Hoàn trả chuyển kho (Từ chối)",
                BatchAllocationType.ProductionConsumption => "Tiêu hao sản xuất",
                BatchAllocationType.ProductionReceipt => "Sản xuất chuẩn bị sẵn",
                BatchAllocationType.ReturnToSupplier => "Trả Hàng",
                BatchAllocationType.CustomerReturn => "Khách trả hàng",
                BatchAllocationType.StockCheck => "Cân đối kiểm kho",
                _ => type.ToString()
            };
        }
        #endregion

        #endregion
    }
}
