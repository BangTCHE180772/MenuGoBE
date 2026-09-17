using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Leftover
{
    public class LeftoverRecordViewDto
    {
        public long Id { get; set; }
        public long BranchId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LeftoverType Type { get; set; }

        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;

        public long? OrderDetailId { get; set; }
        public string? TableName { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LeftoverHandlingAction? HandlingAction { get; set; }

        public long? AtFaultAccountId { get; set; }
        public string? AtFaultAccountName { get; set; }

        public DateTime? UsedAt { get; set; }
        public long? UsedByOrderDetailId { get; set; }

        public long? ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public DateOnly RecordDate { get; set; }

        public long? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
