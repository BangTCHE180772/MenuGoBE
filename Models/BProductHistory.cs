using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("BProductHistories")]
public class BProductHistory
{
    [Key]
    public long Id { get; set; }

    public long BProductId { get; set; }

    public long? DocumentDetailId { get; set; }

    public BProductChangeType ChangeType { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal QuantityBefore { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal QuantityAfter { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal AvgBefore { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal AvgAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BProductId")]
    public BProduct BProduct { get; set; } = null!;

    [ForeignKey("DocumentDetailId")]
    public DocumentDetail? DocumentDetail { get; set; }
}
