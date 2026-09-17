using System.Text.Json.Serialization;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Dtos.Leftover
{
    public class LeftoverRecordCreateDto
    {
        public long BranchId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LeftoverType Type { get; set; }

        public long ProductId { get; set; }

        public int Quantity { get; set; }

        public string Reason { get; set; } = string.Empty;

        // Return-only
        public long? OrderDetailId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LeftoverHandlingAction? HandlingAction { get; set; }

        public long? AtFaultAccountId { get; set; }

        // Extra-only (optional for Return)
        public long? ShiftId { get; set; }
        public DateOnly? RecordDate { get; set; }
    }
}
