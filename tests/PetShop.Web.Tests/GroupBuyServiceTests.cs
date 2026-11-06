using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Services;

namespace PetShop.Web.Tests;

public sealed class GroupBuyServiceTests
{
    [Fact]
    public async Task Register_settle_and_refund_leave_auditable_totals()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 9, 8, 14, 0, 0, TimeSpan.Zero);
        var service = new GroupBuyService(db, new FixedTimeProvider(now));
        var groomer = new StaffMember { Name = "小林", IsGroomer = true, IsActive = true, CreatedAt = now };
        db.StaffMembers.Add(groomer);
        await db.SaveChangesAsync();

        var record = await service.RegisterAsync("MT-20260908-001", "8291", "小型犬精洗",
            "王女士", "豆豆", groomer.Id, 128m, 108.8m, now, "已在商家端核销", "clerk-1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync("MT-20260908-001", "8291", "重复",
            string.Empty, string.Empty, null, 128m, 108.8m, now, string.Empty, "clerk-1"));

        await service.SettleAsync(record.Id, 107.5m, "admin-1");
        await service.AddRefundAsync(record.Id, 28m, "顾客协商退部分项目", "admin-1");

        var saved = await db.GroupBuyRecords.Include(x => x.Refunds).SingleAsync();
        Assert.Equal(GroupBuySettlementStatus.Settled, saved.SettlementStatus);
        Assert.Equal(107.5m, saved.ActualSettlementAmount);
        Assert.Equal(28m, saved.Refunds.Sum(x => x.Amount));
        Assert.Equal(3, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task Rejects_invalid_groomer_and_refund_above_redeemed_amount()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = DateTimeOffset.UtcNow;
        var service = new GroupBuyService(db, new FixedTimeProvider(now));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync("MT-2", string.Empty, "精洗",
            string.Empty, string.Empty, Guid.NewGuid(), 100m, 85m, now, string.Empty, "clerk-1"));
        var record = await service.RegisterAsync("MT-3", string.Empty, "精洗", string.Empty, string.Empty,
            null, 100m, 85m, now, string.Empty, "clerk-1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddRefundAsync(record.Id, 100.01m, "超额", "admin-1"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
