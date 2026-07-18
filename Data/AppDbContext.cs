using Microsoft.EntityFrameworkCore;
using MenuGoBE.Models;

namespace MenuGoBE.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // 1. Address Module
    public DbSet<NewProvince> NewProvinces { get; set; } = null!;
    public DbSet<NewWard> NewWards { get; set; } = null!;
    public DbSet<OldProvince> OldProvinces { get; set; } = null!;
    public DbSet<OldDistrict> OldDistricts { get; set; } = null!;
    public DbSet<OldWard> OldWards { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;

    // 2. Chain & Branch Module
    public DbSet<Chain> Chains { get; set; } = null!;
    public DbSet<Branch> Branches { get; set; } = null!;

    // 3. Product Catalog Module
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<Image> Images { get; set; } = null!;
    public DbSet<Menu> Menus { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<MenuProduct> MenuProducts { get; set; } = null!;

    // 4. Units & Conversions Module
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<UnitConversion> UnitConversions { get; set; } = null!;

    // 5. Recipes Module
    public DbSet<Recipe> Recipes { get; set; } = null!;
    public DbSet<RecipesDetailed> RecipesDetaileds { get; set; } = null!;

    // 6. HR Module
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<TempRole> TempRoles { get; set; } = null!;
    public DbSet<Contract> Contracts { get; set; } = null!;
    public DbSet<Shift> Shifts { get; set; } = null!;
    public DbSet<WorkSchedule> WorkSchedules { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;

    // 7. Orders Module
    public DbSet<Area> Areas { get; set; } = null!;
    public DbSet<Table> Tables { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Voucher> Vouchers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite primary key for MenuProducts (many-to-many join table)
        modelBuilder.Entity<MenuProduct>()
            .HasKey(mp => new { mp.MenuId, mp.ProductId });

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.Name)
            .IsUnique();

        modelBuilder.Entity<Menu>()
            .HasIndex(m => m.Name)
            .IsUnique();

        modelBuilder.Entity<Unit>()
            .HasIndex(u => u.Name)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.SKUCode)
            .IsUnique();

        // Iterate over all entities and disable cascade deletes globally to avoid Multiple Cascade Path errors
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
