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
using MenuGoBE.Dtos.Chat;
using MenuGoBE.Dtos.Auth;

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
            CreateMap<ChainUpdateDto, Chain>()
                .ForMember(dest => dest.Address, opt => opt.Ignore())
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            //Branch
            CreateMap<Branch, BranchViewDto>()
                .ForMember(dest => dest.NewWardId, opt => opt.MapFrom(src => src.Address != null ? src.Address.NewWardId : null))
                .ForMember(dest => dest.OldWardId, opt => opt.MapFrom(src => src.Address != null ? src.Address.OldWardId : null))
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
                    dest => dest.NewWardId,
                    opt => opt.MapFrom(src => src.Address != null ? src.Address.NewWardId : null))
                .ForMember(
                    dest => dest.OldWardId,
                    opt => opt.MapFrom(src => src.Address != null ? src.Address.OldWardId : null))
                .ForMember(
                    dest => dest.ManagerName,
                    opt => opt.MapFrom(src =>
                        src.Contracts
                            .Where(c => c.Role != null && c.Role.Name.ToLower() == "Manager".ToLower() && c.Status.ToLower() == "Active".ToLower() && c.Account != null)
                            .Select(c => c.Account.Name)
                            .FirstOrDefault() ?? "Chưa phân công"
                    ));
            CreateMap<BranchCreateDto, Branch>();
            CreateMap<BranchUpdateDto, Branch>()
                .ForMember(dest => dest.Address, opt => opt.Ignore())
                .ForMember(dest => dest.AddressId, opt => opt.Ignore())
                .ForMember(dest => dest.ChainId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

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
                .ForMember(dest => dest.AreaName, opt => opt.MapFrom(src => src.Table != null && src.Table.Area != null ? src.Table.Area.Name : null))
                .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.Table != null && src.Table.Area != null ? (long?)src.Table.Area.BranchId : (long?)null))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Table != null && src.Table.Area != null && src.Table.Area.Branch != null ? src.Table.Area.Branch.Name : null))
                .ForMember(dest => dest.BranchAddress, opt => opt.MapFrom(src => src.Table != null && src.Table.Area != null && src.Table.Area.Branch != null && src.Table.Area.Branch.Address != null ? (src.Table.Area.Branch.Address.NewWard != null ? src.Table.Area.Branch.Address.NewWard!.Name + ", " + (src.Table.Area.Branch.Address.NewWard!.NewProvince != null ? src.Table.Area.Branch.Address.NewWard!.NewProvince!.Name : "") : (src.Table.Area.Branch.Address.OldWard != null ? src.Table.Area.Branch.Address.OldWard!.Name + ", " + (src.Table.Area.Branch.Address.OldWard!.OldDistrict != null ? src.Table.Area.Branch.Address.OldWard!.OldDistrict!.Name + ", " + (src.Table.Area.Branch.Address.OldWard!.OldDistrict!.OldProvince != null ? src.Table.Area.Branch.Address.OldWard!.OldDistrict!.OldProvince!.Name : "") : "") : null)) : null))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : null))
                .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Phone : null))
                .ForMember(dest => dest.VoucherCode, opt => opt.MapFrom(src => src.Voucher != null ? src.Voucher.Code : null))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.Method).FirstOrDefault()))
                .ForMember(dest => dest.OrderDetails, opt => opt.MapFrom(src => src.OrderDetails));
            CreateMap<OrderCreateDto, Order>();
            CreateMap<OrderUpdateDto, Order>();
            CreateMap<OrderDetail, OrderDetailViewDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.ProductType, opt => opt.MapFrom(src => src.Product != null ? src.Product.Type.ToString() : string.Empty));
            CreateMap<OrderDetailCreateDto, OrderDetail>();
            CreateMap<OrderDetailUpdateDto, OrderDetail>();

            //account
            CreateMap<Account, AccountViewDto>()
                .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src =>
                    src.Contracts.Any(c => c.Status == "Active")
                        ? src.Contracts.Where(c => c.Status == "Active").Select(c => c.RoleId).Distinct().ToList()
                        : src.Contracts.Any(c => c.Status == "Expired")
                            ? src.Contracts.Where(c => c.Status == "Expired").Select(c => c.RoleId).Distinct().ToList()
                            : src.Contracts.Where(c => c.Status == "Terminated").Select(c => c.RoleId).Distinct().ToList()
                ))
                .ForMember(dest => dest.BranchId, opt => opt.MapFrom(src => src.Contracts.Where(c => c.Status == "Active").Select(c => (long?)c.BranchId).FirstOrDefault() ?? src.Contracts.Select(c => (long?)c.BranchId).FirstOrDefault()));

            CreateMap<AccountCreateDto, Account>();
            CreateMap<AccountUpdateDto, Account>();

            //work schedule
            CreateMap<WorkSchedule, WorkScheduleViewDto>();
            CreateMap<WorkScheduleCreateDto, WorkSchedule>();
            CreateMap<WorkScheduleUpdateDto, WorkSchedule>();

            CreateMap<Contract, ContractViewDto>()
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account != null ? src.Account.Name : string.Empty))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : string.Empty))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty));
            CreateMap<ContractCreateDto, Contract>();
            CreateMap<ContractUpdateDto, Contract>();

            //Shift
            CreateMap<Shift, ShiftViewDto>();
            CreateMap<ShiftCreateDto, Shift>();
            CreateMap<ShiftUpdateDto, Shift>();
            CreateMap<ShiftRoleRequirement, ShiftRoleRequirementDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty));
            CreateMap<ShiftRoleRequirementDto, ShiftRoleRequirement>();

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
            CreateMap<PayrollShiftDetail, PayrollShiftDetailViewDto>();

            CreateMap<SalaryDetail, SalaryDetailViewDto>()
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account != null ? src.Account.Name : string.Empty))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.Creator != null ? src.Creator.Name : string.Empty));
            CreateMap<SalaryDetailCreateDto, SalaryDetail>();
            CreateMap<SalaryDetailUpdateDto, SalaryDetail>();

            //Document Module Mappings
            CreateMap<Partner, DocumentPartnerResponseDto>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address != null
                    ? (src.Address.NewWard != null
                        ? src.Address.NewWard.Name + ", " + (src.Address.NewWard.NewProvince != null ? src.Address.NewWard.NewProvince.Name : "")
                        : (src.Address.OldWard != null ? src.Address.OldWard.Name + ", " + (src.Address.OldWard.OldDistrict != null ? src.Address.OldWard.OldDistrict.Name + ", " + (src.Address.OldWard.OldDistrict.OldProvince != null ? src.Address.OldWard.OldDistrict.OldProvince.Name : "") : "") : ""))
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
                .ForMember(dest => dest.InventoryLedgers, opt => opt.MapFrom(src => src.InventoryLedgers != null ? src.InventoryLedgers.ToList() : new System.Collections.Generic.List<InventoryLedger>()))
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.ImageUrls)
                    ? System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(src.ImageUrls, (System.Text.Json.JsonSerializerOptions?)null) ?? new System.Collections.Generic.List<string>()
                    : new System.Collections.Generic.List<string>()));

            CreateMap<Models.Document, DocumentListResponseDto>()
                .ForMember(dest => dest.CurrentCreatedByName, opt => opt.MapFrom(src => src.Creator != null ? src.Creator.Name : null))
                .ForMember(dest => dest.CurrentPartnerName, opt => opt.MapFrom(src => src.Partner != null ? src.Partner.Name : null))
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.ImageUrls)
                    ? System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(src.ImageUrls, (System.Text.Json.JsonSerializerOptions?)null) ?? new System.Collections.Generic.List<string>()
                    : new System.Collections.Generic.List<string>()))
                .ForMember(dest => dest.TotalQuantity, opt => opt.MapFrom(src => src.DocumentDetails != null
                    ? (src.Type == DocumentType.Production
                        ? src.DocumentDetails.Where(dt => !dt.IsDeleted && dt.FatherId == null).Sum(dt => dt.Quantity)
                        : src.DocumentDetails.Where(dt => !dt.IsDeleted).Sum(dt => dt.Quantity))
                    : 0))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount > 0
                    ? src.TotalAmount
                    : (src.DocumentDetails != null
                        ? (src.Type == DocumentType.Production
                            ? src.DocumentDetails.Where(dt => !dt.IsDeleted && dt.FatherId == null).Sum(dt => dt.Quantity * (dt.UnitPrice > 0
                                ? dt.UnitPrice
                                : ((dt.SnapshotAvgCost > 0 ? dt.SnapshotAvgCost : (dt.BInventory != null ? dt.BInventory.Avg : 0m)) * (dt.ConversionRate > 0 ? dt.ConversionRate : 1m))
                            ))
                            : src.DocumentDetails.Where(dt => !dt.IsDeleted).Sum(dt => dt.Quantity * (dt.UnitPrice > 0
                                ? dt.UnitPrice
                                : ((dt.SnapshotAvgCost > 0 ? dt.SnapshotAvgCost : (dt.BInventory != null ? dt.BInventory.Avg : 0m)) * (dt.ConversionRate > 0 ? dt.ConversionRate : 1m))
                            )))
                        : 0m)));

            CreateMap<Models.CashFlow, Dtos.Document.CashFlowResponseDto>()
                .ForMember(dest => dest.DocumentCode, opt => opt.MapFrom(src => src.Document != null ? src.Document.Code : null))
                .ForMember(dest => dest.PartnerName, opt => opt.MapFrom(src => src.Partner != null ? src.Partner.Name : null))
                .ForMember(dest => dest.Direction, opt => opt.MapFrom(src => src.Direction.ToString()))
                .ForMember(dest => dest.DirectionValue, opt => opt.MapFrom(src => (int)src.Direction))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.TypeValue, opt => opt.MapFrom(src => (int)src.Type))
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.StatusValue, opt => opt.MapFrom(src => (int)src.Status))
                .ForMember(dest => dest.SourceType, opt => opt.MapFrom(src => src.Document != null ? src.Document.Type.ToString() : null))
                .ForMember(dest => dest.SourceId, opt => opt.MapFrom(src => src.DocumentId))
                .ForMember(dest => dest.SourceCode, opt => opt.MapFrom(src => src.Document != null ? src.Document.Code : null))
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Document != null ? src.Document.OrderId : null));

            // Customer Auth & Portal Mappings
            CreateMap<Customer, CustomerProfileDto>();

            // Chat Mappings
            CreateMap<Conversation, ConversationViewDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : (src.GuestChatSession != null ? src.GuestChatSession.GuestName : null)))
                .ForMember(dest => dest.GuestName, opt => opt.MapFrom(src => src.GuestChatSession != null ? src.GuestChatSession.GuestName : null))
                .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : ""))
                .ForMember(dest => dest.AssignedEmployeeName, opt => opt.MapFrom(src => src.AssignedEmployee != null ? src.AssignedEmployee.Name : null));

            CreateMap<Message, MessageViewDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : (src.GuestChatSession != null ? src.GuestChatSession.GuestName : null)))
                .ForMember(dest => dest.GuestName, opt => opt.MapFrom(src => src.GuestChatSession != null ? src.GuestChatSession.GuestName : null))
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Name : null));

            CreateMap<ConversationAssignmentHistory, ConversationAssignmentHistoryViewDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee != null ? src.Employee.Name : null))
                .ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.Creator != null ? src.Creator.Name : null));
        }
    }
}