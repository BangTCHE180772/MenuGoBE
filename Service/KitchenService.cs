using MenuGoBE.Dtos.Kitchen;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Interface.Services;
using MenuGoBE.Models;
using MenuGoBE.Hubs;
using Microsoft.AspNetCore.SignalR;
using MenuGoBE.Models.Enums;
using DocumentEntity = MenuGoBE.Models.Document;

namespace MenuGoBE.Service;

public class KitchenService : IKitchenService
{
    private readonly IProductRepository _productRepo;
    private readonly IAreaRepository _areaRepo;
    private readonly ITableRepository _tableRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IOrderDetailRepository _orderDetailRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IRecipesDetailedRepository _recipesRepo;
    private readonly IBInventoryRepository _bInventoryRepo;
    private readonly IHubContext<NotificationHub> _hubContext;

    public KitchenService(
        IProductRepository productRepo,
        IAreaRepository areaRepo,
        ITableRepository tableRepo,
        IOrderRepository orderRepo,
        IOrderDetailRepository orderDetailRepo,
        IDocumentRepository documentRepo,
        IRecipesDetailedRepository recipesRepo,
        IBInventoryRepository bInventoryRepo,
        IHubContext<NotificationHub> hubContext)
    {
        _productRepo = productRepo;
        _areaRepo = areaRepo;
        _tableRepo = tableRepo;
        _orderRepo = orderRepo;
        _orderDetailRepo = orderDetailRepo;
        _documentRepo = documentRepo;
        _recipesRepo = recipesRepo;
        _bInventoryRepo = bInventoryRepo;
        _hubContext = hubContext;
    }

    public async Task<bool> RequestRestockAsync(RequestRestockDto dto, long? accountId)
    {
        var product = await _productRepo.GetByIdAsync(dto.ProductId);
        if (product == null) return false;

        var areas = await _areaRepo.GetAllAsync();
        var internalArea = areas.FirstOrDefault(a => a.BranchId == dto.BranchId && a.Status == "Internal");
        
        if (internalArea == null)
        {
            internalArea = new Area
            {
                BranchId = dto.BranchId,
                Name = "Khu Vực Bếp (Nội Bộ)",
                Status = "Internal",
                IsActive = true
            };
            await _areaRepo.CreateAsync(internalArea);
            await _areaRepo.SaveChangesAsync();
        }

        var tables = await _tableRepo.GetAllAsync();
        var internalTable = tables.FirstOrDefault(t => t.AreaId == internalArea.Id && t.Status == "Internal");
        
        if (internalTable == null)
        {
            internalTable = new Table
            {
                AreaId = internalArea.Id,
                Name = "Bàn Sản Xuất",
                Status = "Internal",
                IsActive = true
            };
            await _tableRepo.CreateAsync(internalTable);
            await _tableRepo.SaveChangesAsync();
        }

        var activeOrders = await _orderRepo.GetAllAsync();
        var internalOrder = activeOrders.FirstOrDefault(o => o.TableId == internalTable.Id && o.Status == "Internal");

        if (internalOrder == null)
        {
            internalOrder = new Order
            {
                TableId = internalTable.Id,
                Status = "Internal",
                CreatedBy = accountId,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = 0
            };
            await _orderRepo.CreateAsync(internalOrder);
            await _orderRepo.SaveChangesAsync();
        }

        var orderDetail = new OrderDetail
        {
            OrderId = internalOrder.Id,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Price = 0,
            Status = "Confirmed",
            CookingStatus = "Waiting",
            Note = "Yêu cầu sản xuất thêm",
            CreatedAt = DateTime.UtcNow
        };

        await _orderDetailRepo.CreateAsync(orderDetail);
        await _orderDetailRepo.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ReceiveKitchenOrderUpdate", dto.BranchId);

        return true;
    }

    public async Task<bool> HandleInternalRestockCompletionAsync(long orderDetailId)
    {
        var detail = await _orderDetailRepo.GetByIdAsync(orderDetailId);
        if (detail == null) return false;
        
        var order = await _orderRepo.GetByIdAsync(detail.OrderId);
        if (order == null || order.Status != "Internal") return false;
        
        var table = await _tableRepo.GetByIdAsync(order.TableId);
        if (table == null) return false;
        var area = await _areaRepo.GetByIdAsync(table.AreaId);
        if (area == null) return false;

        var branchId = area.BranchId;

        var docDetails = new List<DocumentDetail>();
        var ledgersToCreate = new List<InventoryLedger>();
        var updatedBInvs = new List<BInventory>();

        var recipes = await _recipesRepo.GetRecipesByProductIdAsync(detail.ProductId);

        foreach (var recipe in recipes)
        {
            var totalQtyNeeded = recipe.Quantity * detail.Quantity;
            
            var bInventory = await _bInventoryRepo.GetByProductAndBranchAsync(recipe.IngredientProductId, branchId);
            if (bInventory == null)
            {
                bInventory = new BInventory
                {
                    BranchId = branchId,
                    ProductId = recipe.IngredientProductId,
                    Quantity = 0,
                    Type = BInventoryType.RawMaterial,
                    IsManageQuantity = true,
                    ChainActive = true,
                    BranchActive = true
                };
                await _bInventoryRepo.CreateAsync(bInventory);
            }
            
            bInventory.Quantity -= totalQtyNeeded;
            updatedBInvs.Add(bInventory);
            
            var docDetail = new DocumentDetail
            {
                BInventory = bInventory,
                Quantity = totalQtyNeeded,
                UnitPrice = 0,
                Note = "Trừ nguyên liệu sản xuất nội bộ",
                CreatedAt = DateTime.UtcNow
            };
            docDetails.Add(docDetail);
        }

#if FALSE
            var ledger = new InventoryLedger
            {
                QuantityDelta = -totalQtyNeeded,
                RunningQuantity = bInventory.Quantity,
                BusinessDate = DateTime.UtcNow,
                DocumentType = DocumentType.Production,
                CreatedAt = DateTime.UtcNow
            };
            ledgersToCreate.Add(ledger);

        var productInventory = await _bInventoryRepo.GetByProductAndBranchAsync(detail.ProductId, branchId);
        if (productInventory == null)
        {
            productInventory = new BInventory
            {
                BranchId = branchId,
                ProductId = detail.ProductId,
                Quantity = 0,
                Type = BInventoryType.Finished,
                IsManageQuantity = true,
                ChainActive = true,
                BranchActive = true
            };
            await _bInventoryRepo.CreateAsync(productInventory);
        }
        productInventory.Quantity += detail.Quantity;
        updatedBInvs.Add(productInventory);

        var productDocDetail = new DocumentDetail
        {
            BInventory = productInventory,
            Quantity = detail.Quantity,
            UnitPrice = 0,
            Note = "Nhập thành phẩm sau sản xuất",
            CreatedAt = DateTime.UtcNow
        };
        docDetails.Add(productDocDetail);

        var productLedger = new InventoryLedger
        {
            QuantityDelta = detail.Quantity,
            RunningQuantity = productInventory.Quantity,
            BusinessDate = DateTime.UtcNow,
            DocumentType = DocumentType.Production,
            CreatedAt = DateTime.UtcNow
        };
        ledgersToCreate.Add(productLedger);

        var document = new DocumentEntity
        {
            BranchId = branchId,
            Type = DocumentType.Production,
            Code = $"PROD-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            BusinessDate = DateTime.UtcNow,
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepo.ExecuteDocumentTransactionAsync(
            document, docDetails, new List<DocumentPartner>(), updatedBInvs, ledgersToCreate, new List<InventoryLedger>(), new List<CashFlow>(), new List<CashFlowDetail>());
#endif

        detail.CookingStatus = "Served";
        await _orderDetailRepo.UpdateAsync(detail);
        await _orderDetailRepo.SaveChangesAsync();
        
        await _hubContext.Clients.All.SendAsync("ReceiveKitchenOrderUpdate", branchId);
        return true;
    }
}
