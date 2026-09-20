using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("Partners")]
public class Partner
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public PartnerType Type { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public long? AddressId { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    public string? ImageUrls { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("AddressId")]
    public Address? Address { get; set; }

    [ForeignKey("CreatedBy")]
    public Account? Creator { get; set; }

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<CashFlow> CashFlows { get; set; } = new List<CashFlow>();

    public long? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }
}
