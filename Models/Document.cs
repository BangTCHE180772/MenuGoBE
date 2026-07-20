using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Documents")]
public class Document
{
    [Key]
    public long Id { get; set; }

    public long BranchId { get; set; }

    /// <summary>Self-reference: parent document (nullable)</summary>
    public long? FatherId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TransferStatus { get; set; } = string.Empty;

    [Column(TypeName = "decimal(27,9)")]
    public decimal TotalAmount { get; set; }

    /// <summary>Self-reference: the original document this reverses (nullable)</summary>
    public long? ReversalOfDocumentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }

    // Navigation
    [ForeignKey("BranchId")]
    public Branch Branch { get; set; } = null!;

    [ForeignKey("FatherId")]
    [InverseProperty("ChildDocuments")]
    public Document? Father { get; set; }

    [InverseProperty("Father")]
    public ICollection<Document> ChildDocuments { get; set; } = new List<Document>();

    [ForeignKey("ReversalOfDocumentId")]
    [InverseProperty("ReversalDocuments")]
    public Document? ReversalOfDocument { get; set; }

    [InverseProperty("ReversalOfDocument")]
    public ICollection<Document> ReversalDocuments { get; set; } = new List<Document>();

    public ICollection<DocumentDetail> DocumentDetails { get; set; } = new List<DocumentDetail>();
    public ICollection<DocumentPartner> DocumentPartners { get; set; } = new List<DocumentPartner>();
}
