using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Partners")]
public class Partner
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public string Type { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public long? AddressId { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("AddressId")]
    public Address? Address { get; set; }

    [ForeignKey("CreatedBy")]
    public Account CreatedByAccount { get; set; } = null!;

    public ICollection<DocumentPartner> DocumentPartners { get; set; } = new List<DocumentPartner>();
    public ICollection<CashFlowDetail> CashFlowDetails { get; set; } = new List<CashFlowDetail>();
}
