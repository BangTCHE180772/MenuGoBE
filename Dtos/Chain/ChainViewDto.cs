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

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public long AddressId { get; set; }
       
        public string NewAddressName { get; set; } = string.Empty;
        
        public string OldAddressName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
    }
}