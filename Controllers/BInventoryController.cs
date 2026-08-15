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
                    imageLink = b.Product.Image != null ? b.Product.Image.ImageLink : string.Empty,
                    createdAt = b.Product.CreatedAt,
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

                    // 6. Tiêu thụ định mức (Expected Consumption) / Bán hàng
                    decimal expectedConsumption = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Sale)
                        .Sum(l => Math.Abs(l.QuantityDelta));

                    // 6a. Xuất hủy (Destruction)
                    decimal destruction = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Export || l.DocumentType == DocumentType.ExportDelete)
                        .Sum(l => Math.Abs(l.QuantityDelta));

                    // 6b. Điều chỉnh (Adjustment)
                    decimal adjustment = periodLedgers
                        .Where(l => l.DocumentType == DocumentType.Check)
                        .Sum(l => l.QuantityDelta);

                    // 7. Tồn lý thuyết (Theoretical Stock)
                    decimal theoreticalStock = openingStock + import + transferIn - transferOut - expectedConsumption - destruction + adjustment;

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
                    decimal systemConsumption = openingStock + import + transferIn - transferOut - destruction + adjustment - closingStock;
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
                DocumentType.Return => "Trả hàng",
                DocumentType.Sale => "Xuất kho",
                DocumentType.CustomerReturn => "Khách trả hàng",
                DocumentType.Transfer => "Chuyển kho",
                DocumentType.Export => "Xuất hủy",
                DocumentType.ExportDelete => "Xuất hủy",
                DocumentType.Production => "Sản xuất",
                DocumentType.Check => "Kiểm kho",
                DocumentType.CostAdjustment => "Điều chỉnh",
                _ => delta >= 0 ? "Nhập kho" : "Xuất kho"
            };
        }
        #endregion
    }
}