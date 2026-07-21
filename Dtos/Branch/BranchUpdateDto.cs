using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuGoBE.Dtos.Address;

namespace MenuGoBE.Dtos.Branch
{
    public class BranchUpdateDto
    {
        public long Id { get; set; }
        // public long AddressId { get; set; }
        public AddressUpdateDto Address { get; set; } = new();
       
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }
    }
}