using Microsoft.EntityFrameworkCore;
using MenuGoBE.Models;
using MenuGoBE.Models.Enums;

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
    public DbSet<RecipesDetailed> RecipesDetaileds { get; set; } = null!;

    // 6. HR Module
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<TempRole> TempRoles { get; set; } = null!;
    public DbSet<Contract> Contracts { get; set; } = null!;
    public DbSet<Shift> Shifts { get; set; } = null!;
    public DbSet<WorkSchedule> WorkSchedules { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<Payroll> Payrolls { get; set; } = null!;
    public DbSet<SalaryDetail> SalaryDetails { get; set; } = null!;

    // 7. Orders Module
    public DbSet<Area> Areas { get; set; } = null!;
    public DbSet<Table> Tables { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Voucher> Vouchers { get; set; } = null!;

    // 8. Device Module
    public DbSet<Device> Devices { get; set; } = null!;

    // 9. Inventory Module
    public DbSet<BInventory> BInventories { get; set; } = null!;
    public DbSet<InventoryLedger> InventoryLedgers { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentDetail> DocumentDetails { get; set; } = null!;
    public DbSet<Partner> Partners { get; set; } = null!;
    public DbSet<DocumentPartner> DocumentPartners { get; set; } = null!;
    public DbSet<CashFlow> CashFlows { get; set; } = null!;
    public DbSet<CashFlowDetail> CashFlowDetails { get; set; } = null!;

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

        // Direct Product Recipe Items configuration
        modelBuilder.Entity<RecipesDetailed>()
            .HasOne(rd => rd.ParentProduct)
            .WithMany(p => p.RecipeItems)
            .HasForeignKey(rd => rd.ParentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RecipesDetailed>()
            .HasOne(rd => rd.IngredientProduct)
            .WithMany(p => p.UsedInRecipeItems)
            .HasForeignKey(rd => rd.IngredientProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── 9. Inventory Module ─────────────────────────────────────────────

        // Document: unique index on Code
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.Code)
            .IsUnique();

        // Document: self-referencing FK (ParentDocumentId → Id)
        // One parent Document can have many child Documents
        modelBuilder.Entity<Document>()
            .HasOne(d => d.ParentDocument)
            .WithMany(d => d.ChildDocuments)
            .HasForeignKey(d => d.ParentDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Document: Auto-increment PostingSequence
        modelBuilder.Entity<Document>()
            .Property(d => d.PostingSequence)
            .ValueGeneratedOnAdd();

        // Document: CreatedBy / DeletedBy → Accounts (multiple FK to same table)
        modelBuilder.Entity<Document>()
            .HasOne(d => d.Creator)
            .WithMany(a => a.CreatedDocuments)
            .HasForeignKey(d => d.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Deleter)
            .WithMany(a => a.DeletedDocuments)
            .HasForeignKey(d => d.DeletedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // CashFlow: unique index on Code
        modelBuilder.Entity<CashFlow>()
            .HasIndex(cf => cf.Code)
            .IsUnique();



        // CashFlow: DeletedBy → Accounts
        modelBuilder.Entity<CashFlow>()
            .HasOne(cf => cf.Deleter)
            .WithMany(a => a.DeletedCashFlows)
            .HasForeignKey(cf => cf.DeletedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Partner: CreatedBy → Accounts
        modelBuilder.Entity<Partner>()
            .HasOne(p => p.Creator)
            .WithMany(a => a.CreatedPartners)
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);



        // Store enums as string names with fallback for legacy DB values (e.g. 'Food' -> Regular)
        modelBuilder.Entity<Product>()
            .Property(p => p.Type)
            .HasConversion(
                v => v.ToString(),
                v => ParseEnumProductType(v)
            );

        modelBuilder.Entity<BInventory>()
            .Property(b => b.Type)
            .HasConversion<string>();

        modelBuilder.Entity<InventoryLedger>()
            .Property(il => il.DocumentType)
            .HasConversion<string>();

        modelBuilder.Entity<Document>()
            .Property(d => d.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Document>()
            .Property(d => d.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Document>()
            .Property(d => d.TransferStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Partner>()
            .Property(p => p.Type)
            .HasConversion<string>();

        modelBuilder.Entity<CashFlow>()
            .Property(cf => cf.Direction)
            .HasConversion<string>();

        modelBuilder.Entity<CashFlow>()
            .Property(cf => cf.Status)
            .HasConversion<string>();

        modelBuilder.Entity<CashFlowDetail>()
            .Property(cfd => cfd.Type)
            .HasConversion<string>();

        modelBuilder.Entity<CashFlowDetail>()
            .Property(cfd => cfd.PaymentMethod)
            .HasConversion<string>();

        // ── 10. Payroll Module ───────────────────────────────────────────────
        modelBuilder.Entity<Payroll>()
            .Property(p => p.Status)
            .HasConversion<string>();

        modelBuilder.Entity<SalaryDetail>()
            .Property(sd => sd.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Payroll>()
            .HasIndex(p => new { p.AccountId, p.Month, p.Year });

        // ── Global: disable cascade deletes to avoid Multiple Cascade Path ──
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static ProductType ParseEnumProductType(string value)
    {
        ProductType parsed;
        if (Enum.TryParse<ProductType>(value, true, out parsed))
        {
            return parsed;
        }
        return ProductType.Regular;
    }
}
