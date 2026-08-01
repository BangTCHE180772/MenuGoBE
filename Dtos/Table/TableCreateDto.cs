using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuGoBE.Dtos.Table
{
    public class TableCreateDto
    {
        public long AreaId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Status { get; set; } = "Empty";

        public bool IsActive { get; set; }
    }
}
