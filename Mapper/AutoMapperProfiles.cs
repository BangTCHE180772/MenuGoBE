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
                            .Where(c => c.Role != null && c.Role.Name == "MANAGER" && c.Status == "Active" && c.Account != null)
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
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Image != null ? src.Image.ImageLink : null))
                .ForMember(dest => dest.RecipeItems, opt => opt.MapFrom(src => src.RecipeItems));
            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<UnitConversion, ProductUnitConversionViewDto>();
            CreateMap<RecipesDetailed, ProductRecipeDetailedViewDto>()
                .ForMember(dest => dest.IngredientProductName, opt => opt.MapFrom(src => src.IngredientProduct != null ? src.IngredientProduct.Name : string.Empty));

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
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.Account != null ? src.Account.Name : string.Empty));
            CreateMap<SalaryDetailCreateDto, SalaryDetail>();
            CreateMap<SalaryDetailUpdateDto, SalaryDetail>();
        }
    }
}