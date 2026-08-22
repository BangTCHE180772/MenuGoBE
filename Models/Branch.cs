using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Branches")]
public class Branch
{
    [Key]
    public long Id { get; set; }

    public long ChainId { get; set; }

    public long AddressId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public TimeOnly CloseTime { get; set; }

    public TimeOnly OpenTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PostingLockDate { get; set; }

    public string Status { get; set; } = "Hoạt động";

    public bool IsDeleted { get; set; } = false;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Navigation
    [ForeignKey("ChainId")]
    public Chain Chain { get; set; } = null!;

    [ForeignKey("AddressId")]
    public Address Address { get; set; } = null!;

    public ICollection<Area> Areas { get; set; } = new List<Area>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    // 9. Inventory Module
    public ICollection<BInventory> BInventories { get; set; } = new List<BInventory>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Partner> Partners { get; set; } = new List<Partner>();
    public ICollection<CashFlow> CashFlows { get; set; } = new List<CashFlow>();
}
