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

        public long? NewWardId { get; set; }

        public long? OldWardId { get; set; }

        public string NewAddressName { get; set; } = string.Empty;

        public string OldAddressName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }

        public DateTime CreatedAt { get; set; }

        public string ManagerName { get; set; } = string.Empty;

        public string Status { get; set; } = "Hoạt động";

        public bool IsDeleted { get; set; } = false;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public int CheckinRadiusMeters { get; set; } = 100;

        public int PayrollPayday { get; set; } = 15;
    }
}
