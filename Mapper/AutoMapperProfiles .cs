using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Models;
using MenuGoBE.Dtos.Chain;
using MenuGoBE.Dtos.Table;

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
        }
    }
}