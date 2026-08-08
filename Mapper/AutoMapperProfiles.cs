using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Models;
using MenuGoBE.Dtos.Chain;
using MenuGoBE.Dtos.Table;
using MenuGoBE.Dtos.Order;
using MenuGoBE.Dtos.Area;
using MenuGoBE.Dtos.Account;
using MenuGoBE.Dtos.WorkSchedule;
using MenuGoBE.Dtos.Contract;
using MenuGoBE.Dtos.Branch;
using MenuGoBE.Dtos.Shift;
using MenuGoBE.Dtos.Image;
using MenuGoBE.Dtos.Group;
using MenuGoBE.Dtos.Menu;
using MenuGoBE.Dtos.Unit;
using MenuGoBE.Dtos.Product;
using MenuGoBE.Dtos.Address;
using MenuGoBE.Dtos.Payroll;
using MenuGoBE.Dtos.SalaryDetail;
using MenuGoBE.Dtos.Document;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Mapper
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            //Chain
            CreateMap<Chain, ChainViewDto>()
                .ForMember(
                    dest => dest.NewAddressName,
                    opt => opt.MapFrom(src =>
                        src.Address.NewWard != null
                            ? $"{src.Address.NewWard.Name}, {src.Address.NewWard.NewProvince.Name}"
                            : "Chưa có địa chỉ mới"
                    ))
                .ForMember(
                    dest => dest.OldAddressName,
                    opt => opt.MapFrom(src =>
                        src.Address.OldWard != null
                            ? $"{src.Address.OldWard.Name}, {src.Address.OldWard.OldDistrict.Name}, {src.Address.OldWard.OldDistrict.OldProvince.Name}"
                            : "Chưa có địa chỉ cũ"
                    ));
            CreateMap<ChainCreateDto, Chain>();
            CreateMap<ChainUpdateDto, Chain>();

            //Branch
            CreateMap<Branch, BranchViewDto>()
                .ForMember(
                    dest => dest.NewAddressName,
                    opt => opt.MapFrom(src =>
                        src.Address.NewWard != null
                            ? $"{src.Address.NewWard.Name}, {src.Address.NewWard.NewProvince.Name}"
                            : "Chưa có địa chỉ mới"
                    ))
                .ForMember(
                    dest => dest.OldAddressName,
                    opt => opt.MapFrom(src =>
                        src.Address.OldWard != null
                            ? $"{src.Address.OldWard.Name}, {src.Address.OldWard.OldDistrict.Name}, {src.Address.OldWard.OldDistrict.OldProvince.Name}"
                            : "Chưa có địa chỉ cũ"
                    ))
                .ForMember(
                    dest => dest.ManagerName,
                    opt => opt.MapFrom(src =>
                        src.Contracts
                            .Where(c => c.Role != null && c.Role.Name.ToLower() == "Manager".ToLower() && c.Status.ToLower() == "Active".ToLower() && c.Account != null)
                            .Select(c => c.Account.Name)
                            .FirstOrDefault() ?? "Chưa phân công"
                    ));
            CreateMap<BranchCreateDto, Branch>();
            CreateMap<BranchUpdateDto, Branch>();

            //Table
            CreateMap<Table, TableViewDto>()
                .ForMember(dest => dest.AreaName, opt => opt.MapFrom(src => src.Area != null ? src.Area.Name : string.Empty))
                .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.Area != null ? src.Area.BranchId : 0));
            CreateMap<TableCreateDto, Table>();
            CreateMap<TableUpdateDto, Table>();

            CreateMap<Area, AreaViewDto>();
            CreateMap<AreaCreateDto, Area>();
            CreateMap<AreaUpdateDto, Area>();

            //order
            CreateMap<Order, OrderViewDto>()
                .ForMember(dest => dest.TableName, opt => opt.MapFrom(src => src.Table != null ? (src.ChildOrders.Any() ? src.Table.Name + ", " + string.Join(", ", src.ChildOrders.Select(c => c.Table.Name)) : src.Table.Name) : null))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.Method).FirstOrDefault()));
            CreateMap<OrderCreateDto, Order>();
            CreateMap<OrderUpdateDto, Order>();
            CreateMap<OrderDetail, OrderDetailViewDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty));
            CreateMap<OrderDetailCreateDto, OrderDetail>();
            CreateMap<OrderDetailUpdateDto, OrderDetail>();

            //account
            CreateMap<Account, AccountViewDto>()
                .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src => src.Contracts.Select(c => c.RoleId).ToList()))
                .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.Contracts.Where(c => c.Status == "Active").Select(c => (long?)c.BranchId).FirstOrDefault() ?? src.Contracts.Select(c => (long?)c.BranchId).FirstOrDefault()));

            CreateMap<AccountCreateDto, Account>();
            CreateMap<AccountUpdateDto, Account>();

            //work schedule
            CreateMap<WorkSchedule, WorkScheduleViewDto>();
            CreateMap<WorkScheduleCreateDto, WorkSchedule>();
            CreateMap<WorkScheduleUpdateDto, WorkSchedule>();

            CreateMap<Contract, ContractViewDto>()
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account != null ? src.Account.Name : string.Empty));
            CreateMap<ContractCreateDto, Contract>();
            CreateMap<ContractUpdateDto, Contract>();

            //Shift
            CreateMap<Shift, ShiftViewDto>();
            CreateMap<ShiftCreateDto, Shift>();
            CreateMap<ShiftUpdateDto, Shift>();
            //Image
            CreateMap<Models.Image, ImageViewDto>();
            CreateMap<ImageCreateDto, Models.Image>();
            CreateMap<ImageUpdateDto, Models.Image>();

            //Group
            CreateMap<Group, GroupViewDto>();
            CreateMap<GroupCreateDto, Group>();
            CreateMap<GroupUpdateDto, Group>();

            //Menu
            CreateMap<Menu, MenuViewDto>();
            CreateMap<MenuCreateDto, Menu>();
            CreateMap<MenuUpdateDto, Menu>();

            //Unit
            CreateMap<Unit, UnitViewDto>();
            CreateMap<UnitCreateDto, Unit>();
            CreateMap<UnitUpdateDto, Unit>();

            //Product
            CreateMap<Product, ProductViewDto>()
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Group != null ? src.Group.Name : null))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Image != null ? src.Image.ImageLink : null))
                .ForMember(dest => dest.MenuIds, opt => opt.MapFrom(src => src.MenuProducts.Select(mp => mp.MenuId).ToList()))
                .ForMember(dest => dest.Menus, opt => opt.MapFrom(src => src.MenuProducts.Where(mp => mp.Menu != null).Select(mp => new ProductMenuInfoViewDto { Id = mp.Menu.Id, Name = mp.Menu.Name }).ToList()))
                .ForMember(dest => dest.UnitConversions, opt => opt.MapFrom(src => src.UnitConversions))
                .ForMember(dest => dest.RecipeItems, opt => opt.MapFrom(src => src.RecipeItems));
            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.UnitConversions, opt => opt.Ignore());

            CreateMap<ProcessedProductCreateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<ProcessedProductUpdateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<ManufacturedProductCreateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<ManufacturedProductUpdateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<RegularProductCreateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<RegularProductUpdateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<IngredientProductCreateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<IngredientProductUpdateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<ToolProductCreateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<ToolProductUpdateDto, Product>().ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<UnitConversion, ProductUnitConversionViewDto>()
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Unit != null ? src.Unit.Name : null))
                .ForMember(dest => dest.IsBase, opt => opt.MapFrom(src => src.BaseId == null));
            CreateMap<RecipesDetailed, ProductRecipeDetailedViewDto>()
                .ForMember(dest => dest.IngredientProductName, opt => opt.MapFrom(src => src.IngredientProduct != null ? src.IngredientProduct.Name : string.Empty))
                .ForMember(dest => dest.IngredientSKUCode, opt => opt.MapFrom(src => src.IngredientProduct != null ? src.IngredientProduct.SKUCode : string.Empty));

            //Address
            CreateMap<AddressCreateDto, Address>();
            CreateMap<AddressUpdateDto, Address>();

            //NewWard & OldWard
            CreateMap<NewWard, NewWardViewDto>()
                .ForMember(dest => dest.NewProvinceName, opt => opt.MapFrom(src => src.NewProvince != null ? src.NewProvince.Name : string.Empty));

            CreateMap<OldWard, OldWardViewDto>()
                .ForMember(dest => dest.OldDistrictName, opt => opt.MapFrom(src => src.OldDistrict != null ? src.OldDistrict.Name : string.Empty))
                .ForMember(dest => dest.OldProvinceName, opt => opt.MapFrom(src => src.OldDistrict != null && src.OldDistrict.OldProvince != null ? src.OldDistrict.OldProvince.Name : string.Empty));

            //Provinces & Districts
            CreateMap<NewProvince, NewProvinceViewDto>();
            CreateMap<OldProvince, OldProvinceViewDto>();
            CreateMap<OldDistrict, OldDistrictViewDto>()
                .ForMember(dest => dest.OldProvinceName, opt => opt.MapFrom(src => src.OldProvince != null ? src.OldProvince.Name : string.Empty));

            //Payroll & SalaryDetail
            CreateMap<Payroll, PayrollViewDto>()
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account != null ? src.Account.Name : string.Empty))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : string.Empty));
            CreateMap<PayrollCreateDto, Payroll>();
            CreateMap<PayrollUpdateDto, Payroll>();

            CreateMap<SalaryDetail, SalaryDetailViewDto>()
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account != null ? src.Account.Name : string.Empty))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.Creator != null ? src.Creator.Name : string.Empty));
            CreateMap<SalaryDetailCreateDto, SalaryDetail>();
            CreateMap<SalaryDetailUpdateDto, SalaryDetail>();

            //Document Module Mappings
            CreateMap<Partner, DocumentPartnerResponseDto>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address != null
                    ? (src.Address.NewWard != null
                        ? $"{src.Address.NewWard.Name}, {src.Address.NewWard.NewProvince.Name}"
                        : $"{src.Address.OldWard.Name}, {src.Address.OldWard.OldDistrict.Name}, {src.Address.OldWard.OldDistrict.OldProvince.Name}")
                    : null));

            CreateMap<CashFlow, DocumentCashFlowResponseDto>();

            CreateMap<InventoryLedger, DocumentInventoryLedgerResponseDto>();

            CreateMap<UnitConversion, DocumentUnitConversionResponseDto>()
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Unit != null ? src.Unit.Name : string.Empty));

            CreateMap<Product, DocumentProductResponseDto>();

            CreateMap<BInventory, DocumentBInventoryResponseDto>();

            CreateMap<DocumentDetail, DocumentDetailResponseDto>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.BInventory != null ? (long?)src.BInventory.ProductId : null))
                .ForMember(dest => dest.BInventory, opt => opt.MapFrom(src => src.BInventory))
                .ForMember(dest => dest.CurrentProductName, opt => opt.MapFrom(src => src.BInventory != null && src.BInventory.Product != null ? src.BInventory.Product.Name : null))
                .ForMember(dest => dest.CurrentProductCode, opt => opt.MapFrom(src => src.BInventory != null && src.BInventory.Product != null ? src.BInventory.Product.SKUCode : null))
                .ForMember(dest => dest.CurrentUnitName, opt => opt.MapFrom(src =>
                    src.UnitConversion != null && src.UnitConversion.Unit != null
                        ? src.UnitConversion.Unit.Name
                        : (src.BInventory != null && src.BInventory.Product != null && src.BInventory.Product.UnitConversions.Any(uc => uc.BaseId == null && uc.Unit != null)
                            ? src.BInventory.Product.UnitConversions.FirstOrDefault(uc => uc.BaseId == null && uc.Unit != null)!.Unit.Name
                            : null)))
                .ForMember(dest => dest.CurrentBaseUnitName, opt => opt.MapFrom(src => src.BInventory != null && src.BInventory.Product != null && src.BInventory.Product.UnitConversions.Any(uc => uc.BaseId == null && uc.Unit != null) ? src.BInventory.Product.UnitConversions.FirstOrDefault(uc => uc.BaseId == null && uc.Unit != null)!.Unit.Name : null))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.SnapshotProductName) ? src.SnapshotProductName : (src.BInventory != null && src.BInventory.Product != null ? src.BInventory.Product.Name : string.Empty)))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.SnapshotProductCode) ? src.SnapshotProductCode : (src.BInventory != null && src.BInventory.Product != null ? src.BInventory.Product.SKUCode : string.Empty)))
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.SnapshotUnitName) ? src.SnapshotUnitName : (src.UnitConversion != null && src.UnitConversion.Unit != null ? src.UnitConversion.Unit.Name : (src.BInventory != null && src.BInventory.Product != null && src.BInventory.Product.UnitConversions.Any(uc => uc.BaseId == null && uc.Unit != null) ? src.BInventory.Product.UnitConversions.FirstOrDefault(uc => uc.BaseId == null && uc.Unit != null)!.Unit.Name : string.Empty))))
                .ForMember(dest => dest.BaseUnitName, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.SnapshotBaseUnitName) ? src.SnapshotBaseUnitName : (src.BInventory != null && src.BInventory.Product != null && src.BInventory.Product.UnitConversions.Any(uc => uc.BaseId == null && uc.Unit != null) ? src.BInventory.Product.UnitConversions.FirstOrDefault(uc => uc.BaseId == null && uc.Unit != null)!.Unit.Name : string.Empty)))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Quantity * src.UnitPrice))
                .ForMember(dest => dest.CurrentConversionRate, opt => opt.MapFrom(src => src.UnitConversion != null ? (decimal?)src.UnitConversion.ConversionPoint : 1))
                .ForMember(dest => dest.CurrentStockQuantity, opt => opt.MapFrom(src => src.BInventory != null ? (decimal?)src.BInventory.Quantity : null));

            CreateMap<Models.Document, DocumentResponseDto>()
                .ForMember(dest => dest.CurrentPartnerName, opt => opt.MapFrom(src => src.Partner != null ? src.Partner.Name : null))
                .ForMember(dest => dest.CurrentBranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null))
                .ForMember(dest => dest.Partner, opt => opt.MapFrom(src => src.Partner))
                .ForMember(dest => dest.CashFlows, opt => opt.MapFrom(src => src.CashFlows != null ? src.CashFlows.Where(cf => !cf.IsDeleted).ToList() : new System.Collections.Generic.List<CashFlow>()))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.DocumentDetails != null ? src.DocumentDetails.Where(dt => !dt.IsDeleted).ToList() : new System.Collections.Generic.List<DocumentDetail>()))
                .ForMember(dest => dest.InventoryLedgers, opt => opt.MapFrom(src => src.InventoryLedgers != null ? src.InventoryLedgers.ToList() : new System.Collections.Generic.List<InventoryLedger>()));

            CreateMap<Models.Document, DocumentListResponseDto>()
                .ForMember(dest => dest.CurrentCreatedByName, opt => opt.MapFrom(src => src.Creator != null ? src.Creator.Name : null))
                .ForMember(dest => dest.CurrentPartnerName, opt => opt.MapFrom(src => src.Partner != null ? src.Partner.Name : null))
                .ForMember(dest => dest.TotalQuantity, opt => opt.MapFrom(src => src.DocumentDetails != null ? src.DocumentDetails.Where(dt => !dt.IsDeleted).Sum(dt => dt.Quantity) : 0))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount > 0
                    ? src.TotalAmount
                    : (src.DocumentDetails != null
                        ? src.DocumentDetails.Where(dt => !dt.IsDeleted).Sum(dt => dt.Quantity * (dt.UnitPrice > 0
                            ? dt.UnitPrice
                            : ((dt.SnapshotAvgCost > 0 ? dt.SnapshotAvgCost : (dt.BInventory != null ? dt.BInventory.Avg : 0m)) * (dt.ConversionRate > 0 ? dt.ConversionRate : 1m))
                        ))
                        : 0m)));
        }
    }
}