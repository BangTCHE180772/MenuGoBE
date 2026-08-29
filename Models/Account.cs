using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MenuGoBE.Models;

[Table("Accounts")]
public class Account
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string AvatarImage { get; set; } = string.Empty;

    public string CitizenIdCode { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string HashedPassword { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public long? AddressId { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AddressId")]
    public Address? Address { get; set; }

    public ICollection<TempRole> TempRoles { get; set; } = new List<TempRole>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    [InverseProperty("Account")]
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
    [InverseProperty("Account")]
    public ICollection<SalaryDetail> SalaryDetails { get; set; } = new List<SalaryDetail>();

    // 9. Inventory Module – Audit
    [InverseProperty("Creator")]
    public ICollection<Document> CreatedDocuments { get; set; } = new List<Document>();

    [InverseProperty("Deleter")]
    public ICollection<Document> DeletedDocuments { get; set; } = new List<Document>();

    [InverseProperty("Deleter")]
    public ICollection<CashFlow> DeletedCashFlows { get; set; } = new List<CashFlow>();

    [InverseProperty("Creator")]
    public ICollection<Partner> CreatedPartners { get; set; } = new List<Partner>();

}
