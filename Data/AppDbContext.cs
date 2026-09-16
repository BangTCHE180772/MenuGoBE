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
    public DbSet<ShiftRoleRequirement> ShiftRoleRequirements { get; set; } = null!;
    public DbSet<WorkSchedule> WorkSchedules { get; set; } = null!;
    public DbSet<ShiftChangeRequest> ShiftChangeRequests { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<Payroll> Payrolls { get; set; } = null!;
    public DbSet<SalaryDetail> SalaryDetails { get; set; } = null!;
    public DbSet<HolidayConfig> HolidayConfigs { get; set; } = null!;
    public DbSet<WorkScheduleFeedback> WorkScheduleFeedbacks { get; set; } = null!;
    public DbSet<PayrollShiftDetail> PayrollShiftDetails { get; set; } = null!;
    public DbSet<PayrollSuggestion> PayrollSuggestions { get; set; } = null!;


    // 7. Orders Module
    public DbSet<Area> Areas { get; set; } = null!;
    public DbSet<Table> Tables { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<CustomerPointTransaction> CustomerPointTransactions { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
    public DbSet<LeftoverRecord> LeftoverRecords { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<ReservationDetail> ReservationDetails { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Voucher> Vouchers { get; set; } = null!;
    public DbSet<VoucherUsage> VoucherUsages { get; set; } = null!;
    public DbSet<OrderAssignment> OrderAssignments { get; set; } = null!;

    // 7b. Promotion Module
    public DbSet<Promotion> Promotions { get; set; } = null!;
    public DbSet<PromotionProduct> PromotionProducts { get; set; } = null!;
    public DbSet<PromotionBranch> PromotionBranches { get; set; } = null!;

    // 8. Device Module
    public DbSet<Device> Devices { get; set; } = null!;

    // Chat Module
    public DbSet<GuestChatSession> GuestChatSessions { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<ConversationAssignmentHistory> ConversationAssignmentHistories { get; set; } = null!;

    // 9. Inventory Module
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<BInventory> BInventories { get; set; } = null!;
    public DbSet<BInventoryBatch> BInventoryBatches { get; set; } = null!;
    public DbSet<BatchAllocation> BatchAllocations { get; set; } = null!;
    public DbSet<InventoryLedger> InventoryLedgers { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentDetail> DocumentDetails { get; set; } = null!;
    public DbSet<Partner> Partners { get; set; } = null!;
    public DbSet<CashFlow> CashFlows { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-convert all DateTime and DateTime? columns to Utc kind
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

            foreach (var property in properties)
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                    ));
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                        v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)),
                        v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                    ));
                }
            }
        }

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

        // Optimize Virtual Stock calculation
        modelBuilder.Entity<OrderDetail>()
            .HasIndex(od => new { od.ProductId, od.Status, od.CookingStatus })
            .HasDatabaseName("IX_OrderDetail_ProductId_Status_CookingStatus");

        // ── Promotion Module ────────────────────────────────────────────────

        // PromotionScope enum → string conversion
        modelBuilder.Entity<Promotion>()
            .Property(p => p.Scope)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Unique index: 1 sản phẩm chỉ nằm trong 1 promotion 1 lần
        modelBuilder.Entity<PromotionProduct>()
            .HasIndex(pp => new { pp.PromotionId, pp.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_PromotionProduct_Unique");

        // Unique index: 1 chi nhánh chỉ nằm trong 1 promotion 1 lần
        modelBuilder.Entity<PromotionBranch>()
            .HasIndex(pb => new { pb.PromotionId, pb.BranchId })
            .IsUnique()
            .HasDatabaseName("IX_PromotionBranch_Unique");


        // VoucherUsage: index cho query per-customer
        modelBuilder.Entity<VoucherUsage>()
            .HasIndex(vu => new { vu.VoucherId, vu.CustomerId })
            .HasDatabaseName("IX_VoucherUsage_VoucherCustomer");

        // OrderDetail → Promotion (optional FK, no cascade)
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Promotion)
            .WithMany()
            .HasForeignKey(od => od.PromotionId)
            .OnDelete(DeleteBehavior.SetNull);

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

        // Document: CreatedBy / PostedBy / DeletedBy → Accounts (multiple FK to same table)
        modelBuilder.Entity<Document>()
            .HasOne(d => d.Branch)
            .WithMany(b => b.Documents)
            .HasForeignKey(d => d.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.ToBranch)
            .WithMany()
            .HasForeignKey(d => d.ToBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Creator)
            .WithMany(a => a.CreatedDocuments)
            .HasForeignKey(d => d.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Poster)
            .WithMany()
            .HasForeignKey(d => d.PostedBy)
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

        // CustomerPointTransaction configuration
        modelBuilder.Entity<CustomerPointTransaction>(entity =>
        {
            entity.Property(c => c.Type)
                .HasConversion<int>();

            entity.HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Order)
                .WithMany()
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.Creator)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });



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

        modelBuilder.Entity<BInventoryBatch>(entity =>
        {
            entity.Property(b => b.Status)
                .HasConversion<string>();

            // Unique index: Mỗi mã lô chỉ xuất hiện duy nhất 1 lần trong 1 bản ghi BInventory (chi nhánh + sản phẩm)
            entity.HasIndex(b => new { b.BInventoryId, b.BatchCode })
                .IsUnique();

            // Index tối ưu hóa truy vấn FEFO
            entity.HasIndex(b => new { b.BInventoryId, b.Status, b.ExpiryDate });

            entity.HasOne(b => b.BInventory)
                .WithMany(bi => bi.Batches)
                .HasForeignKey(b => b.BInventoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.SourceBatch)
                .WithMany(sb => sb.DerivedBatches)
                .HasForeignKey(b => b.SourceBatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BatchAllocation>(entity =>
        {
            entity.Property(ba => ba.AllocationType)
                .HasConversion<string>();

            entity.HasOne(ba => ba.DocumentDetail)
                .WithMany(dd => dd.BatchAllocations)
                .HasForeignKey(ba => ba.DocumentDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ba => ba.Batch)
                .WithMany(b => b.BatchAllocations)
                .HasForeignKey(ba => ba.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryLedger>()
            .Property(il => il.DocumentType)
            .HasConversion<string>();

        // Document: 1-to-many with Partner
        modelBuilder.Entity<Document>()
            .HasOne(d => d.Partner)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.PartnerId);

        // Document: 1-to-many with DocumentDetail (Cascade Delete for child details)
        modelBuilder.Entity<Document>()
            .HasMany(d => d.DocumentDetails)
            .WithOne(dd => dd.Document)
            .HasForeignKey(dd => dd.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Document: 1-to-many with CashFlow
        modelBuilder.Entity<Document>()
            .HasMany(d => d.CashFlows)
            .WithOne(cf => cf.Document)
            .HasForeignKey(cf => cf.DocumentId);

        // CashFlow: 1-to-many with Partner
        modelBuilder.Entity<CashFlow>()
            .HasOne(cf => cf.Partner)
            .WithMany(p => p.CashFlows)
            .HasForeignKey(cf => cf.PartnerId);

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

        modelBuilder.Entity<CashFlow>()
            .Property(cf => cf.Type)
            .HasConversion<string>();

        modelBuilder.Entity<CashFlow>()
            .Property(cf => cf.PaymentMethod)
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

        modelBuilder.Entity<WorkScheduleFeedback>()
            .Property(f => f.Status)
            .HasConversion<string>();

        // ── OrderAssignment: 1 order = N assignments (1 Primary + N Support) ──
        modelBuilder.Entity<OrderAssignment>(entity =>
        {
            entity.HasIndex(e => new { e.OrderId, e.AccountId, e.AssignmentType });
            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => e.IsActive);
        });

        // ── Chat Module: GuestChatSession & Conversation Indexes ──
        modelBuilder.Entity<GuestChatSession>(entity =>
        {
            entity.HasIndex(g => g.GuestId).IsUnique();
            entity.HasIndex(g => g.Token);
            entity.HasIndex(g => g.LastActiveTime);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasIndex(c => c.GuestChatSessionId);
            entity.HasIndex(c => c.CustomerId);
            entity.HasIndex(c => c.BranchId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(m => m.ConversationId);
            entity.HasIndex(m => m.GuestChatSessionId);
        });

        // ── Global: disable cascade deletes to avoid Multiple Cascade Path ──
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Payroll>()
            .HasMany(p => p.SalaryDetails)
            .WithOne(sd => sd.Payroll)
            .HasForeignKey(sd => sd.PayrollId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payroll>()
            .HasMany(p => p.PayrollShiftDetails)
            .WithOne(psd => psd.Payroll)
            .HasForeignKey(psd => psd.PayrollId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShiftRoleRequirement>()
            .HasOne(srr => srr.Shift)
            .WithMany(s => s.RoleRequirements)
            .HasForeignKey(srr => srr.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Promotion Cascade (đặt SAU global Restrict) ──
        modelBuilder.Entity<PromotionProduct>()
            .HasOne(pp => pp.Promotion)
            .WithMany(p => p.PromotionProducts)
            .HasForeignKey(pp => pp.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PromotionBranch>()
            .HasOne(pb => pb.Promotion)
            .WithMany(p => p.PromotionBranches)
            .HasForeignKey(pb => pb.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
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
