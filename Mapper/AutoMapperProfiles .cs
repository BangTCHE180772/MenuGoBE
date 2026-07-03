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

namespace MenuGoBE.Mapper
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<Chain, ChainViewDto>();

            CreateMap<ChainCreateDto, Chain>();

            CreateMap<ChainUpdateDto, Chain>();

            CreateMap<Table, TableViewDto>();
            CreateMap<TableCreateDto, Table>();
            CreateMap<TableUpdateDto, Table>();

            CreateMap<Area, AreaViewDto>();
            CreateMap<AreaCreateDto, Area>();
            CreateMap<AreaUpdateDto, Area>();

            CreateMap<Order, OrderViewDto>();
            CreateMap<OrderCreateDto, Order>();
            CreateMap<OrderUpdateDto, Order>();
            CreateMap<OrderDetail, OrderDetailViewDto>();
            CreateMap<OrderDetailCreateDto, OrderDetail>();
            CreateMap<OrderDetailUpdateDto, OrderDetail>();

            CreateMap<Account, AccountViewDto>()
                .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src => src.Contracts.Select(c => c.RoleId).ToList()));

            CreateMap<AccountCreateDto, Account>();
            CreateMap<AccountUpdateDto, Account>();

            CreateMap<WorkSchedule, WorkScheduleViewDto>();
            CreateMap<WorkScheduleCreateDto, WorkSchedule>();
            CreateMap<WorkScheduleUpdateDto, WorkSchedule>();
        }
    }
}