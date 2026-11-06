using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Integrations.Meituan;
using PetShop.Web.Services;

namespace PetShop.Web.Tests;

public sealed class MeituanVoucherServiceTests
{
    [Fact]
    public async Task Redeem_is_idempotent_and_prevents_duplicate_voucher()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var service = new MeituanVoucherService(fixture.Db, new FakeGateway(), TimeProvider.System);

        var first = await service.RedeemAsync("1234 5678 9012", "staff-1", "request-1");
        var retry = await service.RedeemAsync("123456789012", "staff-1", "request-1");

        Assert.Equal(first.Id, retry.Id);
        Assert.Equal(MeituanVoucherStatus.Redeemed, first.Status);
        Assert.StartsWith("FAKE-", first.ProviderTransactionId);
        Assert.DoesNotContain("123456789012", first.VoucherCodeMasked);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RedeemAsync("123456789012", "staff-1", "request-2"));
    }

    [Fact]
    public async Task Revoke_updates_the_original_record()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var service = new MeituanVoucherService(fixture.Db, new FakeGateway(), TimeProvider.System);
        var record = await service.RedeemAsync("ABCDEFGHIJKL", "staff-1", "request-1");

        await service.RevokeAsync(record.Id, "顾客更换套餐", "manager-1");

        Assert.Equal(MeituanVoucherStatus.Revoked, record.Status);
        Assert.Equal("顾客更换套餐", record.RevokeReason);
        Assert.Equal("manager-1", record.RevokedByUserId);
        Assert.NotNull(record.RevokedAt);
    }

    [Fact]
    public async Task Provider_exception_marks_record_unknown_and_blocks_blind_retry()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var service = new MeituanVoucherService(fixture.Db, new FakeGateway(throwOnRedeem: true), TimeProvider.System);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.RedeemAsync("999988887777", "staff-1", "request-1"));

        var record = await fixture.Db.MeituanVoucherRedemptions.SingleAsync();
        Assert.Equal(MeituanVoucherStatus.Unknown, record.Status);
        Assert.Contains("禁止直接重试", record.LastProviderMessage);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RedeemAsync("999988887777", "staff-1", "request-2"));
    }

    private sealed class FakeGateway(bool throwOnRedeem = false) : IMeituanVoucherGateway
    {
        public string ModeName => "测试网关";
        public MeituanProviderMode ProviderMode => MeituanProviderMode.Mock;
        public Task<MeituanVoucherQuote> QueryAsync(string voucherCode, CancellationToken cancellationToken) =>
            Task.FromResult(new MeituanVoucherQuote(true, "D-1", "宠物洗护套餐", 88m, 108m, 1, "可用"));
        public Task<MeituanRedeemResult> RedeemAsync(string voucherCode, string requestId, CancellationToken cancellationToken) =>
            throwOnRedeem ? throw new HttpRequestException("timeout") : Task.FromResult(new MeituanRedeemResult(true, $"FAKE-{requestId}", "成功"));
        public Task<MeituanRevokeResult> RevokeAsync(string providerTransactionId, string reason, CancellationToken cancellationToken) =>
            Task.FromResult(new MeituanRevokeResult(true, "撤销成功"));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        private TestDatabase(SqliteConnection connection, ApplicationDbContext db) { this.connection = connection; Db = db; }
        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, db);
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
