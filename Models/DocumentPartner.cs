using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("DocumentPartners")]
public class DocumentPartner
{
    [Key]
    public long Id { get; set; }

    public long DocumentId { get; set; }

    public long PartnerId { get; set; }

    [Column(TypeName = "decimal(27,9)")]
    public decimal Cost { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DocumentId")]
    public Document Document { get; set; } = null!;

    [ForeignKey("PartnerId")]
    public Partner Partner { get; set; } = null!;
}
