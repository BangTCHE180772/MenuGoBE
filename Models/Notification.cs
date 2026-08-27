using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long BranchId { get; set; }

        public long? AccountId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // e.g. Operational, ShelfLife, Quality, Order, Inventory, Other

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool IsImportant { get; set; } = false;

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Information"; // Critical, Warning, Information

        [MaxLength(100)]
        public string? ReferenceType { get; set; }

        public long? ReferenceId { get; set; }

        [MaxLength(500)]
        public string? RedirectUrl { get; set; }

        public long? CustomerId { get; set; }

        // Navigation
        [ForeignKey("BranchId")]
        public Branch Branch { get; set; } = null!;

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
    }
}
