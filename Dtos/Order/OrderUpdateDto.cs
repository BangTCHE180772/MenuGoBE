using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuGoBE.Dtos.Order
{
    public class OrderUpdateDto
    {
        public long Id { get; set; }

        public long TableId { get; set; }

        public long? FatherId { get; set; }

        public string Status { get; set; } = string.Empty;

        public long? CustomerId { get; set; }

        public decimal TotalAmount { get; set; }

        public long? CreatedBy { get; set; }
    }
}
