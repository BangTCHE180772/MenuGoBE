using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("BBatchesHistories")]
public class BBatchesHistory
{
    [Key]
    public long Id { get; set; }

    public long BProductBatchId { get; set; }

    public long DocumentDetailId { get; set; }

    public string ChangeType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(21,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal QuantityBefore { get; set; }

    [Column(TypeName = "decimal(21,3)")]
    public decimal QuantityAfter { get; set; }

    [Column(TypeName = "decimal(27,9)")]
    public decimal AvgCostAtTime { get; set; }

    [Column(TypeName = "decimal(27,9)")]
    public decimal ValueBefore { get; set; }

    [Column(TypeName = "decimal(27,9)")]
    public decimal ValueAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BProductBatchId")]
    public BProductBatch BProductBatch { get; set; } = null!;

    [ForeignKey("DocumentDetailId")]
    public DocumentDetail DocumentDetail { get; set; } = null!;
}
