using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuGoBE.Dtos.Table
{
    public class TableViewDto
    {
        public long Id { get; set; }

        public long AreaId { get; set; }

        public string AreaName { get; set; } = string.Empty;

        public long BranchId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
