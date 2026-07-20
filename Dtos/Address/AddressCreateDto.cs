using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MenuGoBE.Dtos.Address
{
    public class AddressCreateDto
    {
        public string Type { get; set; } = String.Empty;

        public long? NewWardId { get; set; }

        public long? OldWardId { get; set; }
    }
}