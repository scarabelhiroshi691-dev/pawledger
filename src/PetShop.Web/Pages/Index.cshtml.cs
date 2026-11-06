using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages;

public sealed class IndexModel(ApplicationDbContext db, TimeProvider timeProvider) : PageModel
{
    public CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("zh-CN");
    public string DateText { get; private set; } = string.Empty;
    public decimal PrincipalBalance { get; private set; }
    public decimal BonusBalance { get; private set; }
    public decimal TotalBalance => PrincipalBalance + BonusBalance;
    public decimal TodayRecharge { get; private set; }
    public int TodayRechargeCount { get; private set; }
    public decimal TodayMemberSpend { get; private set; }
    public int TodayMemberSpendCount { get; private set; }
    public decimal TodayExternalPayment { get; private set; }
    public int TodayExternalPaymentCount { get; private set; }
    public decimal TodayRefund { get; private set; }
    public int TodayRefundCount { get; private set; }
    public int PendingGroupBuyCount { get; private set; }
    public bool IsReconciled { get; private set; }
    public IReadOnlyList<GroomingRow> GroomingAppointments { get; private set; } = [];
    public IReadOnlyList<LedgerRow> RecentLedger { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var now = timeProvider.GetLocalNow();
        DateText = now.ToString("yyyy年M月d日 dddd", CultureInfo.GetCultureInfo("zh-CN"));
        var start = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        var end = start.AddDays(1);

        var cards = await db.MemberCards.AsNoTracking().ToListAsync();
        var ledger = await db.LedgerTransactions.AsNoTracking().ToListAsync();
        PrincipalBalance = cards.Sum(x => x.PrincipalBalance);
        BonusBalance = cards.Sum(x => x.BonusBalance);
        IsReconciled = cards.All(card =>
            card.PrincipalBalance == ledger.Where(x => x.MemberCardId == card.Id && x.Bucket == LedgerBucket.Principal).Sum(x => x.Amount) &&
            card.BonusBalance == ledger.Where(x => x.MemberCardId == card.Id && x.Bucket == LedgerBucket.Bonus).Sum(x => x.Amount));

        var recharges = (await db.RechargeOrders.AsNoTracking().ToListAsync()).Where(x => x.CreatedAt >= start && x.CreatedAt < end).ToList();
        TodayRecharge = recharges.Sum(x => x.PrincipalAmount - x.PrincipalRefunded);
        TodayRechargeCount = recharges.Count;
        var payments = (await db.Payments.AsNoTracking().ToListAsync()).Where(x => x.CreatedAt >= start && x.CreatedAt < end).ToList();
        TodayMemberSpend = payments.Sum(x => x.PrincipalPaid + x.BonusPaid - x.PrincipalRefunded - x.BonusRefunded);
        TodayMemberSpendCount = payments.Count(x => x.PrincipalPaid + x.BonusPaid > 0);
        TodayExternalPayment = payments.Sum(x => x.ExternalPaid - x.ExternalRefunded);
        TodayExternalPaymentCount = payments.Count(x => x.ExternalPaid > 0);
        var refunds = (await db.PaymentRefunds.AsNoTracking().ToListAsync()).Where(x => x.CreatedAt >= start && x.CreatedAt < end).ToList();
        TodayRefund = refunds.Sum(x => x.PrincipalAmount + x.BonusAmount + x.ExternalAmount);
        TodayRefundCount = refunds.Count;
        PendingGroupBuyCount = await db.GroupBuyRecords.AsNoTracking().CountAsync(x => x.SettlementStatus == GroupBuySettlementStatus.Pending);

        var grooming = (await db.GroomingOrders.AsNoTracking()
            .Include(x => x.Pet).Include(x => x.ServiceItem).Include(x => x.ActualGroomer)
            .ToListAsync()).Where(x => x.ScheduledAt >= start && x.ScheduledAt < end && x.Status != GroomingStatus.Cancelled)
            .OrderBy(x => x.ScheduledAt).Take(8).ToList();
        GroomingAppointments = grooming.Select(x => new GroomingRow(x.Id, x.ScheduledAt.ToString("HH:mm"), x.Pet.Name, x.Pet.Breed, x.ServiceItem.Name,
            x.ActualGroomer == null ? "未指派" : x.ActualGroomer.Name, StateName(x.Status), StateClass(x.Status), x.AmountDue)).ToList();

        var recent = (await db.LedgerTransactions.AsNoTracking().Include(x => x.MemberCard).ThenInclude(x => x.Customer)
            .ToListAsync()).OrderByDescending(x => x.CreatedAt).Take(12).ToList();
        RecentLedger = recent.Select(x => new LedgerRow(x.CreatedAt.ToString("HH:mm"), x.MemberCard.Customer.Name,
            KindName(x.Kind), x.Bucket == LedgerBucket.Principal ? Signed(x.Amount) : "—",
            x.Bucket == LedgerBucket.Bonus ? Signed(x.Amount) : "—", x.Note)).ToList();
    }

    private static string Signed(decimal value) => $"{(value > 0 ? "+" : string.Empty)}¥{value:N2}";
    private static string StateName(GroomingStatus value) => value switch { GroomingStatus.Scheduled => "待到店", GroomingStatus.Arrived => "已到店", GroomingStatus.InService => "进行中", GroomingStatus.AwaitingPayment => "待结算", GroomingStatus.Completed => "已完成", GroomingStatus.NoShow => "爽约", _ => "已取消" };
    private static string StateClass(GroomingStatus value) => value switch { GroomingStatus.Completed => "done", GroomingStatus.InService or GroomingStatus.AwaitingPayment => "active", _ => "pending" };
    private static string KindName(LedgerKind value) => value switch { LedgerKind.RechargePrincipal or LedgerKind.RechargeBonus => "充值", LedgerKind.SpendPrincipal or LedgerKind.SpendBonus => "消费", LedgerKind.SpendRefundPrincipal or LedgerKind.SpendRefundBonus => "消费退款", LedgerKind.RechargeRefundPrincipal or LedgerKind.RechargeBonusReversal => "充值撤销", _ => "期初" };

    public sealed record GroomingRow(Guid Id, string Time, string PetName, string Breed, string Service, string Groomer, string State, string StateClass, decimal Amount)
    { public string SearchText => $"{PetName} {Breed} {Service} {Groomer} {State}"; }
    public sealed record LedgerRow(string Time, string Member, string Type, string Principal, string Bonus, string Note)
    {
        public string SearchText => $"{Member} {Type} {Note}";
        public string PrincipalClass => Principal.StartsWith('+') ? "positive" : Principal.StartsWith('-') ? "negative" : string.Empty;
        public string BonusClass => Bonus.StartsWith('+') ? "positive" : Bonus.StartsWith('-') ? "negative" : string.Empty;
    }
}
