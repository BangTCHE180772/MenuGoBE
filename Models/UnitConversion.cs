using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("UnitConversions")]
public class UnitConversion
{
    [Key]
    public long Id { get; set; }

    public long UnitId { get; set; }

    public long? BaseId { get; set; }

    public long ProductId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ConversionPoint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("UnitId")]
    public Unit Unit { get; set; } = null!;

    [ForeignKey("BaseId")]
    public UnitConversion? Base { get; set; }

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    [InverseProperty("Base")]
    public ICollection<UnitConversion> DerivedConversions { get; set; } = new List<UnitConversion>();
}
