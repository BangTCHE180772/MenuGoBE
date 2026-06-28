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

    // 3. Items & Variants Module
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<Variant> Variants { get; set; } = null!;

    // 4. Units & Conversions Module
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<UnitConversion> UnitConversions { get; set; } = null!;

    // 5. Recipes Module
    public DbSet<Recipe> Recipes { get; set; } = null!;
    public DbSet<RecipesDetailed> RecipesDetaileds { get; set; } = null!;

    // 6. Branch Variants & Inventory Module
    public DbSet<BVariant> BVariants { get; set; } = null!;
    public DbSet<Inventory> Inventories { get; set; } = null!;
    public DbSet<BVariantBatch> BVariantBatches { get; set; } = null!;
    public DbSet<BVariantBatchDetaild> BVariantBatchDetailds { get; set; } = null!;

    // 7. Suppliers & Transports Module
    public DbSet<Transport> Transports { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<SuppliersTransaction> SuppliersTransactions { get; set; } = null!;
    public DbSet<BInInventoryTransactionDetaild> BInInventoryTransactionDetailds { get; set; } = null!;

    // 8. Inventory Transactions Module
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
    public DbSet<InventoryTransactionBatch> InventoryTransactionBatches { get; set; } = null!;
    public DbSet<InventoryTransactionDetaild> InventoryTransactionDetailds { get; set; } = null!;
    public DbSet<TransportClaim> TransportClaims { get; set; } = null!;
    public DbSet<Evidence> Evidences { get; set; } = null!;

    // 9. Inventory Check Module
    public DbSet<BInventoryCheck> BInventoryChecks { get; set; } = null!;
    public DbSet<InventoryCheckDetaild> InventoryCheckDetailds { get; set; } = null!;
    public DbSet<InventoryCheckDetailedQuantity> InventoryCheckDetailedQuantities { get; set; } = null!;

    // 10. HR Module
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<TempRole> TempRoles { get; set; } = null!;
    public DbSet<Contract> Contracts { get; set; } = null!;
    public DbSet<Shift> Shifts { get; set; } = null!;
    public DbSet<WorkSchedule> WorkSchedules { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;

    // 11. Orders Module
    public DbSet<Area> Areas { get; set; } = null!;
    public DbSet<Table> Tables { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Iterate over all entities and disable cascade deletes globally to avoid Multiple Cascade Path errors
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Additional explicit configurations (if necessary, though Restrict covers most edge cases)
    }
}
