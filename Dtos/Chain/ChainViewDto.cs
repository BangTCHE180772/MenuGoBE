using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuGoBE.Dtos.Chain
{
    public class ChainViewDto
    {
        public long Id { get; set; }
        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }

        public string BackgroundImage { get; set; } = string.Empty;

        public string LogoImage { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public long AddressId { get; set; }
        public string AddressName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}