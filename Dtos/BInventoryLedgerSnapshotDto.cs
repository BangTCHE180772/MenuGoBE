using System;
using System.Collections.Generic;

namespace MenuGoBE.Dtos
{
    public class BInventoryLedgerSnapshotRequestDto
    {
        public List<long> BInventoryIds { get; set; } = new();
        public DateTime Date { get; set; }
    }

    public class BInventoryLedgerSnapshotResponseDto
    {
        public long BInventoryId { get; set; }
        public decimal RunningQuantity { get; set; }
        public decimal RunningAverageCost { get; set; }
    }
}
