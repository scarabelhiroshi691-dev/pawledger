using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Services;

namespace PetShop.Web.Tests;

public sealed class CashShiftServiceTests
{
    [Fact]
    public async Task Open_expense_and_close_calculate_cash_difference()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.Zero);
        var clock = new FixedTimeProvider(now);
        var service = new CashShiftService(db, clock);

        var customer = new Customer { Name = "测试会员", Phone = "13800000000", CreatedAt = now };
        var card = new MemberCard { Customer = customer, CardNumber = "SHIFT-1", CreatedAt = now };
        db.AddRange(customer, card);
        await db.SaveChangesAsync();

        var shift = await service.OpenAsync(500m, "早班", "staff-1");
        db.RechargeOrders.Add(new RechargeOrder
        {
            MemberCardId = card.Id, PrincipalAmount = 100m, BonusAmount = 0, PaymentMethod = PaymentMethod.Cash,
            RequestId = "shift-recharge", OperatorUserId = "staff-1", CreatedAt = now
        });
        db.Payments.Add(new PaymentRecord
        {
            DocumentType = DocumentType.Retail, DocumentId = Guid.NewGuid(), TotalAmount = 80m,
            ExternalPaid = 80m, ExternalMethod = PaymentMethod.Cash, RequestId = "shift-sale",
            OperatorUserId = "staff-1", CreatedAt = now
        });
        await db.SaveChangesAsync();
        await service.AddExpenseAsync(ExpenseCategory.Supplies, 30m, PaymentMethod.Cash, "供应商", "采购洗护耗材", "staff-1");

        var position = await service.CalculatePositionAsync(shift);
        Assert.Equal(650m, position.ExpectedCash);

        var closed = await service.CloseAsync(shift.Id, 640m, "短款待复核", "manager-1");
        Assert.Equal(650m, closed.ExpectedCash);
        Assert.Equal(-10m, closed.Difference);
        Assert.Equal(3, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task Prevents_duplicate_open_shift_and_cash_expense_without_open_shift()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var service = new CashShiftService(db, TimeProvider.System);

        var shift = await service.OpenAsync(0, string.Empty, "staff-1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenAsync(0, string.Empty, "staff-2"));
        await service.CloseAsync(shift.Id, 0, string.Empty, "staff-1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddExpenseAsync(
            ExpenseCategory.Other, 1, PaymentMethod.Cash, string.Empty, "测试", "staff-1"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
