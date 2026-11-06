using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages.Reports;

public sealed class ReconciliationModel(ApplicationDbContext db, TimeProvider clock) : PageModel
{
    public decimal Recharge { get; private set; }
    public decimal CardSpend { get; private set; }
    public decimal External { get; private set; }
    public decimal Refund { get; private set; }
    public decimal Expense { get; private set; }
    public decimal CashExpense { get; private set; }
    public decimal GroupBuyGross { get; private set; }
    public decimal GroupBuyExpected { get; private set; }
    public decimal GroupBuyRefund { get; private set; }
    public decimal CardBalance { get; private set; }
    public bool Balanced { get; private set; }
    public CashShift? OpenShift { get; private set; }
    public List<CardRow> Cards { get; private set; } = [];
    public List<GroomerRow> Groomers { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var now = clock.GetLocalNow();
        var start = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        var end = start.AddDays(1);
        var allCards = await db.MemberCards.AsNoTracking().Include(x => x.Customer).ToListAsync();
        var ledger = await db.LedgerTransactions.AsNoTracking().ToListAsync();
        Cards = allCards.Select(c => new CardRow(
            c.CardNumber,
            c.Customer.Name,
            c.PrincipalBalance,
            c.BonusBalance,
            ledger.Where(x => x.MemberCardId == c.Id && x.Bucket == LedgerBucket.Principal).Sum(x => x.Amount),
            ledger.Where(x => x.MemberCardId == c.Id && x.Bucket == LedgerBucket.Bonus).Sum(x => x.Amount))).ToList();
        Balanced = Cards.All(x => x.Principal == x.LedgerPrincipal && x.Bonus == x.LedgerBonus);
        CardBalance = Cards.Sum(x => x.Principal + x.Bonus);

        var recharges = (await db.RechargeOrders.AsNoTracking().ToListAsync()).Where(x => x.CreatedAt >= start && x.CreatedAt < end).ToList();
        Recharge = recharges.Sum(x => x.PrincipalAmount - x.PrincipalRefunded);
        var payments = (await db.Payments.AsNoTracking().ToListAsync()).Where(x => x.CreatedAt >= start && x.CreatedAt < end).ToList();
        CardSpend = payments.Sum(x => x.PrincipalPaid + x.BonusPaid - x.PrincipalRefunded - x.BonusRefunded);
        External = payments.Sum(x => x.ExternalPaid - x.ExternalRefunded);
        var refunds = (await db.PaymentRefunds.AsNoTracking().ToListAsync()).Where(x => x.CreatedAt >= start && x.CreatedAt < end).ToList();
        Refund = refunds.Sum(x => x.PrincipalAmount + x.BonusAmount + x.ExternalAmount);
        var expenses = (await db.StoreExpenses.AsNoTracking().ToListAsync()).Where(x => x.OccurredAt >= start && x.OccurredAt < end).ToList();
        Expense = expenses.Sum(x => x.Amount);
        CashExpense = expenses.Where(x => x.PaymentMethod == PaymentMethod.Cash).Sum(x => x.Amount);
        OpenShift = (await db.CashShifts.AsNoTracking().ToListAsync()).SingleOrDefault(x => x.ClosedAt is null);
        var groupBuys = (await db.GroupBuyRecords.AsNoTracking()
            .Include(x => x.Groomer)
            .Include(x => x.Refunds)
            .ToListAsync()).Where(x => x.RedeemedAt >= start && x.RedeemedAt < end).ToList();
        GroupBuyGross = groupBuys.Sum(x => x.GrossAmount);
        GroupBuyExpected = groupBuys.Sum(x => x.ExpectedSettlementAmount);
        var groupBuyRefunds = (await db.GroupBuyRefunds.AsNoTracking().ToListAsync()).Where(x => x.CreatedAt >= start && x.CreatedAt < end).ToList();
        GroupBuyRefund = groupBuyRefunds.Sum(x => x.Amount);

        var groomingOrders = await db.GroomingOrders.AsNoTracking().Include(x => x.ActualGroomer)
            .Where(x => x.Status == GroomingStatus.Completed).ToListAsync();
        var storeGroomers = groomingOrders
            .Where(x => x.CompletedAt >= start && x.CompletedAt < end && x.ActualGroomer != null)
            .GroupBy(x => x.ActualGroomer!.Name)
            .Select(g => new GroomerRow(g.Key, "店内工单", g.Count(), g.Sum(x => x.AmountDue)));
        var groupBuyGroomers = groupBuys
            .Where(x => x.Groomer != null)
            .GroupBy(x => x.Groomer!.Name)
            .Select(g => new GroomerRow(g.Key, "美团团购", g.Count(), g.Sum(x => x.GrossAmount - x.Refunds.Sum(r => r.Amount))));
        Groomers = storeGroomers.Concat(groupBuyGroomers).OrderByDescending(x => x.Amount).ToList();
    }

    public sealed record CardRow(string Number, string Name, decimal Principal, decimal Bonus, decimal LedgerPrincipal, decimal LedgerBonus)
    {
        public bool Ok => Principal == LedgerPrincipal && Bonus == LedgerBonus;
    }

    public sealed record GroomerRow(string Name, string Channel, int Count, decimal Amount);
}
