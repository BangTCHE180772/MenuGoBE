using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("InventoryLedgers")]
public class InventoryLedger
{
    [Key]
    public long Id { get; set; }

    public long BInventoryId { get; set; }

    public long? DocumentId { get; set; }

    public long? DocumentDetailId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long PostingSequence { get; set; }

    public DateTime BusinessDate { get; set; }

    public DocumentType DocumentType { get; set; }

    [Column(TypeName = "decimal(51,3)")]
    public decimal QuantityDelta { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal InventoryValueDelta { get; set; }

    [Column(TypeName = "decimal(51,3)")]
    public decimal RunningQuantity { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal RunningInventoryValue { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal RunningAverageCost { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal RunningLeftOver { get; set; }

    [Column(TypeName = "decimal(51,3)")]
    public decimal? ActualQuantity { get; set; }

    [Column(TypeName = "decimal(51,3)")]
    public decimal? VarianceQuantity { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal UnitCost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("BInventoryId")]
    public BInventory BInventory { get; set; } = null!;

    [ForeignKey("DocumentId")]
    public Document? Document { get; set; }

    [ForeignKey("DocumentDetailId")]
    public DocumentDetail? DocumentDetail { get; set; }
}
