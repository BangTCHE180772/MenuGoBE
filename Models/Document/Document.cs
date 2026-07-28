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

    public long? ParentDocumentId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long PostingSequence { get; set; }

    public DateTime BusinessDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    public DocumentType Type { get; set; }

    public DocumentTransferStatus TransferStatus { get; set; }

    [Column(TypeName = "decimal(108,12)")]
    public decimal TotalAmount { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public long? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("ParentDocumentId")]
    public Document? ParentDocument { get; set; }

    [ForeignKey("CreatedBy")]
    public Account? Creator { get; set; }

    [ForeignKey("DeletedBy")]
    public Account? Deleter { get; set; }

    [InverseProperty("ParentDocument")]
    public ICollection<Document> ChildDocuments { get; set; } = new List<Document>();

    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
    public ICollection<DocumentPartner> DocumentPartners { get; set; } = new List<DocumentPartner>();
    public ICollection<InventoryLedger> InventoryLedgers { get; set; } = new List<InventoryLedger>();

}
