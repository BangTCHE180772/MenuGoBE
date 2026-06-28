using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Suppliers")]
public class Supplier
{
    [Key]
    public long Id { get; set; }

    public long ChainId { get; set; }

    public long BranchId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public long AddressId { get; set; }

    public string Note { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("ChainId")]
    public Chain Chain { get; set; } = null!;

    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("AddressId")]
    public Address Address { get; set; } = null!;

    public ICollection<SuppliersTransaction> SuppliersTransactions { get; set; } = new List<SuppliersTransaction>();
}
