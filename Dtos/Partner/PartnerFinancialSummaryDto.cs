namespace MenuGoBE.Dtos.Partner
{
    public class PartnerFinancialSummaryDto
    {
        public long PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public decimal TotalImportAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal RemainingDebt { get; set; }
    }
}
