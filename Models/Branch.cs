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

    // Navigation
    [ForeignKey("ChainId")]
    public Chain Chain { get; set; } = null!;

    [ForeignKey("AddressId")]
    public Address Address { get; set; } = null!;

    public ICollection<BVariant> BVariants { get; set; } = new List<BVariant>();
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<Transport> Transports { get; set; } = new List<Transport>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public ICollection<Area> Areas { get; set; } = new List<Area>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<BInventoryCheck> BInventoryChecks { get; set; } = new List<BInventoryCheck>();
}
