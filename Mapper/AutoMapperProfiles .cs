using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MenuGoBE.Models;
using MenuGoBE.Dtos.Chain;

namespace MenuGoBE.Mapper
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<Chain, ChainViewDto>();

            CreateMap<ChainCreateDto, Chain>();

            CreateMap<ChainUpdateDto, Chain>();
        }
    }
}