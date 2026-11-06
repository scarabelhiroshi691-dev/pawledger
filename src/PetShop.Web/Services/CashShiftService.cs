using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Services;

public sealed record CashPosition(
    decimal OpeningCash,
    decimal CashRecharges,
    decimal CashSales,
    decimal CashExpenses)
{
    public decimal ExpectedCash => OpeningCash + CashRecharges + CashSales - CashExpenses;
}

public sealed class CashShiftService(ApplicationDbContext db, TimeProvider clock)
{
    public Task<CashShift?> GetOpenShiftAsync(CancellationToken cancellationToken = default) =>
        db.CashShifts.AsNoTracking().SingleOrDefaultAsync(x => x.ClosedAt == null, cancellationToken);

    public async Task<CashShift> OpenAsync(decimal openingCash, string note, string operatorUserId, CancellationToken cancellationToken = default)
    {
        if (openingCash < 0) throw new InvalidOperationException("备用金不能小于零。");
        if (await db.CashShifts.AnyAsync(x => x.ClosedAt == null, cancellationToken))
            throw new InvalidOperationException("已有未关班记录，请先完成当前交接班。");

        var now = clock.GetLocalNow();
        var shift = new CashShift
        {
            OpeningCash = openingCash,
            OpeningNote = note.Trim(),
            OpenedAt = now,
            OpenedByUserId = operatorUserId
        };
        db.CashShifts.Add(shift);
        db.AuditLogs.Add(new AuditLog
        {
            Action = "开班",
            EntityType = nameof(CashShift),
            EntityId = shift.Id.ToString(),
            OperatorUserId = operatorUserId,
            Detail = $"备用金 ¥{openingCash:F2}；{shift.OpeningNote}",
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return shift;
    }

    public async Task<StoreExpense> AddExpenseAsync(
        ExpenseCategory category,
        decimal amount,
        PaymentMethod paymentMethod,
        string payee,
        string note,
        string operatorUserId,
        DateTimeOffset? occurredAt = null,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0) throw new InvalidOperationException("支出金额必须大于零。");
        if (paymentMethod is PaymentMethod.None) throw new InvalidOperationException("请选择支出方式。");
        if (string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("请填写支出用途。");

        var openShift = await db.CashShifts.SingleOrDefaultAsync(x => x.ClosedAt == null, cancellationToken);
        if (paymentMethod == PaymentMethod.Cash && openShift is null)
            throw new InvalidOperationException("现金支出必须先开班，才能计入现金交接。");

        var now = clock.GetLocalNow();
        var expense = new StoreExpense
        {
            CashShiftId = openShift?.Id,
            Category = category,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Payee = payee.Trim(),
            Note = note.Trim(),
            OccurredAt = occurredAt ?? now,
            OperatorUserId = operatorUserId,
            CreatedAt = now
        };
        db.StoreExpenses.Add(expense);
        db.AuditLogs.Add(new AuditLog
        {
            Action = "登记支出",
            EntityType = nameof(StoreExpense),
            EntityId = expense.Id.ToString(),
            OperatorUserId = operatorUserId,
            Detail = $"{CategoryText(category)} ¥{amount:F2}，{PaymentText(paymentMethod)}；{expense.Note}",
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return expense;
    }

    public async Task<CashPosition> CalculatePositionAsync(CashShift shift, DateTimeOffset? until = null, CancellationToken cancellationToken = default)
    {
        var end = until ?? clock.GetLocalNow();
        // SQLite doesn't translate DateTimeOffset range comparisons reliably.
        // Filter to cash rows in SQL, then apply the shift window locally.
        var recharges = (await db.RechargeOrders.AsNoTracking()
            .Where(x => x.PaymentMethod == PaymentMethod.Cash)
            .ToListAsync(cancellationToken))
            .Where(x => x.CreatedAt >= shift.OpenedAt && x.CreatedAt <= end)
            .ToList();
        var payments = (await db.Payments.AsNoTracking()
            .Where(x => x.ExternalMethod == PaymentMethod.Cash)
            .ToListAsync(cancellationToken))
            .Where(x => x.CreatedAt >= shift.OpenedAt && x.CreatedAt <= end)
            .ToList();
        var expenses = (await db.StoreExpenses.AsNoTracking()
            .Where(x => x.PaymentMethod == PaymentMethod.Cash)
            .ToListAsync(cancellationToken))
            .Where(x => x.OccurredAt >= shift.OpenedAt && x.OccurredAt <= end)
            .ToList();

        return new CashPosition(
            shift.OpeningCash,
            recharges.Sum(x => x.PrincipalAmount - x.PrincipalRefunded),
            payments.Sum(x => x.ExternalPaid - x.ExternalRefunded),
            expenses.Sum(x => x.Amount));
    }

    public async Task<CashShift> CloseAsync(Guid shiftId, decimal countedCash, string note, string operatorUserId, CancellationToken cancellationToken = default)
    {
        if (countedCash < 0) throw new InvalidOperationException("实点现金不能小于零。");
        var shift = await db.CashShifts.SingleOrDefaultAsync(x => x.Id == shiftId, cancellationToken)
            ?? throw new InvalidOperationException("交接班记录不存在。");
        if (shift.ClosedAt is not null) throw new InvalidOperationException("该班次已经关班，不能重复提交。");

        var now = clock.GetLocalNow();
        var position = await CalculatePositionAsync(shift, now, cancellationToken);
        shift.ClosedAt = now;
        shift.ClosedByUserId = operatorUserId;
        shift.ExpectedCash = position.ExpectedCash;
        shift.CountedCash = countedCash;
        shift.Difference = countedCash - position.ExpectedCash;
        shift.ClosingNote = note.Trim();
        db.AuditLogs.Add(new AuditLog
        {
            Action = "关班",
            EntityType = nameof(CashShift),
            EntityId = shift.Id.ToString(),
            OperatorUserId = operatorUserId,
            Detail = $"应有 ¥{position.ExpectedCash:F2}，实点 ¥{countedCash:F2}，差额 ¥{shift.Difference:F2}；{shift.ClosingNote}",
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return shift;
    }

    public static string CategoryText(ExpenseCategory category) => category switch
    {
        ExpenseCategory.Supplies => "耗材采购",
        ExpenseCategory.Utilities => "水电物业",
        ExpenseCategory.Rent => "房租",
        ExpenseCategory.Wages => "工资",
        ExpenseCategory.Maintenance => "维修",
        ExpenseCategory.Marketing => "营销",
        _ => "其他"
    };

    public static string PaymentText(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "现金",
        PaymentMethod.WeChat => "微信",
        PaymentMethod.Alipay => "支付宝",
        PaymentMethod.BankCard => "银行卡",
        PaymentMethod.Other => "其他",
        _ => "未指定"
    };
}
