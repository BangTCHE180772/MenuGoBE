using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("BProducts")]
public class BProduct
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public long ProductId { get; set; }

    public string Type { get; set; } = string.Empty;

    [Column(TypeName = "decimal(27,9)")]
    public decimal PricePerBaseUnit { get; set; }

    /// <summary>Tiền leftover (tồn dư giá trị)</summary>
    [Column(TypeName = "decimal(27,9)")]
    public decimal LeftOver { get; set; }

    public bool ChainActive { get; set; } = true;

    public bool BranchActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    public ICollection<BProductBatch> BProductBatches { get; set; } = new List<BProductBatch>();
    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
}
