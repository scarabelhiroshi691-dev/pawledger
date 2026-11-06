using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PetShop.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<MemberCard> MemberCards => Set<MemberCard>();
    public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();
    public DbSet<RechargeOrder> RechargeOrders => Set<RechargeOrder>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<GroomingOrder> GroomingOrders => Set<GroomingOrder>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<RetailOrder> RetailOrders => Set<RetailOrder>();
    public DbSet<RetailOrderItem> RetailOrderItems => Set<RetailOrderItem>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MeituanVoucherRedemption> MeituanVoucherRedemptions => Set<MeituanVoucherRedemption>();
    public DbSet<CashShift> CashShifts => Set<CashShift>();
    public DbSet<StoreExpense> StoreExpenses => Set<StoreExpense>();
    public DbSet<GroupBuyRecord> GroupBuyRecords => Set<GroupBuyRecord>();
    public DbSet<GroupBuyRefund> GroupBuyRefunds => Set<GroupBuyRefund>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>().HasIndex(x => x.Phone);
        builder.Entity<MemberCard>().HasIndex(x => x.CustomerId).IsUnique();
        builder.Entity<MemberCard>().HasIndex(x => x.CardNumber).IsUnique();
        builder.Entity<MemberCard>().Property(x => x.Version).IsConcurrencyToken();
        builder.Entity<MemberCard>().HasOne(x => x.Customer).WithOne(x => x.MemberCard).HasForeignKey<MemberCard>(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<LedgerTransaction>().HasIndex(x => new { x.MemberCardId, x.CreatedAt });
        builder.Entity<LedgerTransaction>().HasIndex(x => x.RequestId);
        builder.Entity<RechargeOrder>().HasIndex(x => x.RequestId).IsUnique();
        builder.Entity<StaffMember>().HasIndex(x => x.IdentityUserId).IsUnique();
        builder.Entity<GroomingOrder>().HasIndex(x => x.OrderNumber).IsUnique();
        builder.Entity<GroomingOrder>().HasIndex(x => x.ScheduledAt);
        builder.Entity<GroomingOrder>().HasOne(x => x.ActualGroomer).WithMany().HasForeignKey(x => x.ActualGroomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Product>().HasIndex(x => x.Sku);
        builder.Entity<Product>().HasIndex(x => x.Barcode);
        builder.Entity<InventoryTransaction>().HasIndex(x => new { x.ProductId, x.CreatedAt });
        builder.Entity<RetailOrder>().HasIndex(x => x.OrderNumber).IsUnique();
        builder.Entity<PaymentRecord>().HasIndex(x => x.RequestId).IsUnique();
        builder.Entity<PaymentRecord>().HasIndex(x => new { x.DocumentType, x.DocumentId }).IsUnique();
        builder.Entity<PaymentRefund>().HasIndex(x => x.RequestId).IsUnique();
        builder.Entity<AuditLog>().HasIndex(x => x.CreatedAt);
        builder.Entity<MeituanVoucherRedemption>().HasIndex(x => x.VoucherCodeHash).IsUnique();
        builder.Entity<MeituanVoucherRedemption>().HasIndex(x => x.RequestId).IsUnique();
        builder.Entity<MeituanVoucherRedemption>().HasIndex(x => x.ProviderTransactionId);
        builder.Entity<MeituanVoucherRedemption>().HasIndex(x => x.CreatedAt);
        builder.Entity<CashShift>().HasIndex(x => x.OpenedAt);
        builder.Entity<StoreExpense>().HasIndex(x => x.OccurredAt);
        builder.Entity<StoreExpense>().HasOne(x => x.CashShift).WithMany(x => x.Expenses).HasForeignKey(x => x.CashShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<GroupBuyRecord>().HasIndex(x => x.PlatformOrderNumber).IsUnique();
        builder.Entity<GroupBuyRecord>().HasIndex(x => x.RedeemedAt);
        builder.Entity<GroupBuyRecord>().HasOne(x => x.Groomer).WithMany().HasForeignKey(x => x.GroomerId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<GroupBuyRefund>().HasIndex(x => x.CreatedAt);
        builder.Entity<GroupBuyRefund>().HasOne(x => x.GroupBuyRecord).WithMany(x => x.Refunds).HasForeignKey(x => x.GroupBuyRecordId).OnDelete(DeleteBehavior.Restrict);

        foreach (var property in builder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()).Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }
    }
}
