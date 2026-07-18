using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuGoBE.Dtos.Branch
{
    public class BranchViewDto
    {
        public long Id { get; set; }

        public long ChainId { get; set; }

        public long AddressId { get; set; }

        public string AddressName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}