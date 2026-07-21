using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Address;

namespace MenuGoBE.Dtos.Chain
{
    public class ChainCreateDto
    {
        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }

        public string BackgroundImage { get; set; } = string.Empty;

        public string LogoImage { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        // public long AddressId { get; set; }
        public AddressCreateDto Address { get; set; } = new();
    }
}