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

namespace MenuGoBE.Mapper
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            //Chain
            CreateMap<Chain, ChainViewDto>()
            .ForMember(
                dest => dest.AddressName,
                opt => opt.MapFrom
                (src =>
                    src.Address.NewWard != null
                        ? $"{src.Address.NewWard.Name}, {src.Address.NewWard.NewProvince.Name}"
                    : src.Address.OldWard != null
                        ? $"{src.Address.OldWard.Name}, {src.Address.OldWard.OldDistrict.Name}"
                    : "Chưa có địa chỉ"
                )); ;

            CreateMap<ChainCreateDto, Chain>();
            CreateMap<ChainUpdateDto, Chain>();

            //Branch
            CreateMap<Branch, BranchViewDto>()
            .ForMember(
                dest => dest.AddressName,
                opt => opt.MapFrom
                (src =>
                    src.Address.NewWard != null
                        ? $"{src.Address.NewWard.Name}, {src.Address.NewWard.NewProvince.Name}"
                    : src.Address.OldWard != null
                        ? $"{src.Address.OldWard.Name}, {src.Address.OldWard.OldDistrict.Name}"
                    : "Chưa có địa chỉ"
                )); ;
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
            CreateMap<Order, OrderViewDto>();
            CreateMap<OrderCreateDto, Order>();
            CreateMap<OrderUpdateDto, Order>();
            CreateMap<OrderDetail, OrderDetailViewDto>();
            CreateMap<OrderDetailCreateDto, OrderDetail>();
            CreateMap<OrderDetailUpdateDto, OrderDetail>();

            //account
            CreateMap<Account, AccountViewDto>()
                .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src => src.Contracts.Select(c => c.RoleId).ToList()));

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
            CreateMap<Product, ProductViewDto>();
            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.UnitConversions, opt => opt.Ignore());
            CreateMap<UnitConversion, ProductUnitConversionViewDto>();
            CreateMap<Recipe, ProductRecipeViewDto>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.RecipesDetaileds));
            CreateMap<RecipesDetailed, ProductRecipeDetailedViewDto>();
        }
    }
}