using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MenuGoBE.Models.Enums;

namespace MenuGoBE.Models;

[Table("Documents")]
public class Document
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    public long? FatherId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    public DocumentType Type { get; set; }

    public DocumentStatus Status { get; set; }

    public DocumentTransferStatus TransferStatus { get; set; }

    [Column(TypeName = "decimal(30,12)")]
    public decimal TotalPrice { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public long? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("FatherId")]
    public Document? Father { get; set; }

    [ForeignKey("CreatedBy")]
    public Account? Creator { get; set; }

    [ForeignKey("DeletedBy")]
    public Account? Deleter { get; set; }

    [InverseProperty("Father")]
    public ICollection<Document> ChildDocuments { get; set; } = new List<Document>();

    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
    public ICollection<DocumentPartner> DocumentPartners { get; set; } = new List<DocumentPartner>();
}
