using MenuGoBE.Dtos.Kitchen;
using MenuGoBE.Interface.Repository;
using MenuGoBE.Interface.Repository.Document;
using MenuGoBE.Interface.Services;
using MenuGoBE.Interface.Services.Document;
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
    private readonly IDocumentService _documentService;
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
        IDocumentService documentService,
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
        _documentService = documentService;
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
        else
        {
            var existingDetails = await _orderDetailRepo.GetByOrderIdAsync(internalOrder.Id);
            var pendingQuantity = existingDetails
                .Where(od => od.ProductId == dto.ProductId && od.Status != "Cancelled" && od.CookingStatus != "Served")
                .Sum(od => od.Quantity);

            if (pendingQuantity + dto.Quantity > 5)
            {
                throw new Exception($"Bếp đang thực hiện {pendingQuantity} Ca món này. Vui lòng không yêu cầu vượt quá 5 Ca cùng lúc!");
            }
        }

        var orderDetail = new OrderDetail
        {
            OrderId = internalOrder.Id,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Price = 0,
            Status = "Confirmed",
            CookingStatus = "Waiting",
            Note = "Yêu cầu sản xuất thêm theo Ca (1 Ca = 10 Phần)",
            CreatedAt = DateTime.UtcNow
        };

        await _orderDetailRepo.CreateAsync(orderDetail);
        await _orderDetailRepo.SaveChangesAsync();

        try 
        {
            await _hubContext.Clients.All.SendAsync("ReceiveKitchenOrderUpdate", dto.BranchId);
        }
        catch 
        {
            // Ignore signalR errors to not block the request
        }

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

            var recipes = await _recipesRepo.GetRecipesByProductIdAsync(detail.ProductId);

            foreach (var recipe in recipes)
            {
                var bInventory = await _bInventoryRepo.GetByProductAndBranchAsync(recipe.IngredientProductId, branchId);
                if (bInventory == null)
                {
                    var ingredientProduct = await _productRepo.GetByIdAsync(recipe.IngredientProductId);
                    bInventory = new BInventory
                    {
                        BranchId = branchId,
                        ProductId = recipe.IngredientProductId,
                        Quantity = 0,
                        Type = BInventoryType.RawMaterial,
                        IsManageQuantity = ingredientProduct?.IsManageQuantity ?? true,
                        ChainActive = true,
                        BranchActive = true
                    };
                    await _bInventoryRepo.CreateAsync(bInventory);
                }
            }
            try 
            {
                await _bInventoryRepo.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Bỏ qua lỗi xung đột tạo trùng do click nhiều lần
            }

            var productInventory = await _bInventoryRepo.GetByProductAndBranchAsync(detail.ProductId, branchId);
            if (productInventory == null)
            {
                var finishedProduct = await _productRepo.GetByIdAsync(detail.ProductId);
                productInventory = new BInventory
                {
                    BranchId = branchId,
                    ProductId = detail.ProductId,
                    Quantity = 0,
                    Type = BInventoryType.Finished,
                    IsManageQuantity = finishedProduct?.IsManageQuantity ?? true,
                    ChainActive = true,
                    BranchActive = true
                };
                try
                {
                    await _bInventoryRepo.CreateAsync(productInventory);
                    await _bInventoryRepo.SaveChangesAsync();
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException)
                {
                    // Lỗi trùng lặp, query lại
                    productInventory = await _bInventoryRepo.GetByProductAndBranchAsync(detail.ProductId, branchId);
                    if (productInventory == null) return false;
                }
            }

            var document = new DocumentEntity
            {
                BranchId = branchId,
                Type = DocumentType.Production,
                TotalAmount = 0,
                Note = "Sản xuất nội bộ (Theo Ca)",
                DocumentDetails = new List<DocumentDetail>
                {
                    new DocumentDetail
                    {
                        BInventoryId = productInventory.Id,
                        BaseQuantity = detail.Quantity * 10,
                        Quantity = detail.Quantity * 10,
                        UnitPrice = 0,
                        Note = "Nhập thành phẩm sau sản xuất (1 Ca = 10 Phần)"
                    }
                }
            };

            await _documentService.CreateProductionCompletedAsync(document, order.CreatedBy ?? 1);

            await _hubContext.Clients.All.SendAsync("ReceiveKitchenOrderUpdate", branchId);
            return true;
        }
}
