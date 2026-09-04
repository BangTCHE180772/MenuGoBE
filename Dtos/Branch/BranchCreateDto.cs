using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Address;

namespace MenuGoBE.Dtos.Branch
{
    public class BranchCreateDto
    {
        public long ChainId { get; set; }

        // public long AddressId { get; set; }
        public AddressCreateDto Address { get; set; } = new();

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }

        public string Status { get; set; } = "Hoạt động";

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public int PayrollPayday { get; set; } = 15;
    }
}