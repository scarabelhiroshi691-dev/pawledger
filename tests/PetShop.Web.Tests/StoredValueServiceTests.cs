using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Services;

namespace PetShop.Web.Tests;

public sealed class StoredValueServiceTests
{
    [Fact]
    public async Task Recharge_charge_refund_and_reconcile()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options); await db.Database.EnsureCreatedAsync();
        var customer = new Customer { Name="测试会员", Phone="13800000000", CreatedAt=DateTimeOffset.Now };
        var card = new MemberCard { Customer=customer, CardNumber="TEST001", CreatedAt=DateTimeOffset.Now }; db.Add(customer); db.Add(card); await db.SaveChangesAsync();
        var service = new StoredValueService(db, TimeProvider.System);
        var recharge = await service.RechargeAsync(card.Id,1000,100,PaymentMethod.Cash,"r1","operator","");
        Assert.Equal(1000,recharge.PrincipalBalance); Assert.Equal(100,recharge.BonusBalance);
        var duplicate = await service.RechargeAsync(card.Id,1000,100,PaymentMethod.Cash,"r1","operator",""); Assert.True(duplicate.WasDuplicate);
        var charge = await service.ChargeAsync(DocumentType.Grooming,Guid.NewGuid(),card.Id,180,PaymentMethod.None,"p1","operator");
        Assert.Equal(100,charge.BonusPaid); Assert.Equal(80,charge.PrincipalPaid); Assert.Equal(0,charge.ExternalPaid);
        await service.RefundPaymentAsync(charge.PaymentId,50,"f1","operator","部分退款");
        await db.Entry(card).ReloadAsync(); Assert.Equal(970,card.PrincipalBalance); Assert.Equal(0,card.BonusBalance);
        var ledger = await db.LedgerTransactions.Where(x=>x.MemberCardId==card.Id).ToListAsync();
        Assert.Equal(card.PrincipalBalance,ledger.Where(x=>x.Bucket==LedgerBucket.Principal).Sum(x=>x.Amount));
        Assert.Equal(card.BonusBalance,ledger.Where(x=>x.Bucket==LedgerBucket.Bonus).Sum(x=>x.Amount));
    }

    [Fact]
    public async Task Recharge_reversal_is_blocked_after_balance_is_consumed()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options); await db.Database.EnsureCreatedAsync();
        var customer = new Customer { Name="测试会员", Phone="1", CreatedAt=DateTimeOffset.Now }; var card=new MemberCard{Customer=customer,CardNumber="T2",CreatedAt=DateTimeOffset.Now};db.Add(customer);db.Add(card);await db.SaveChangesAsync();var service=new StoredValueService(db,TimeProvider.System);
        var recharge=await service.RechargeAsync(card.Id,100,20,PaymentMethod.Cash,"r2","op","");await service.ChargeAsync(DocumentType.Retail,Guid.NewGuid(),card.Id,30,PaymentMethod.None,"p2","op");
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ReverseRechargeAsync(recharge.OrderId,"rr2","op","撤销"));
    }
}
