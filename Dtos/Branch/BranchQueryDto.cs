using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuGoBE.Dtos.Branch
{
    public class BranchQueryDto
    {
        public string? Keyword { get; set; }

        public string? Type { get; set; }

        public long? NewProvinceId { get; set; }

        public string? SortBy { get; set; }

        public bool Desc { get; set; } = false;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}