using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Services;

public sealed class GroupBuyService(ApplicationDbContext db, TimeProvider clock)
{
    public async Task<GroupBuyRecord> RegisterAsync(
        string platformOrderNumber,
        string voucherCodeSuffix,
        string dealName,
        string customerName,
        string petName,
        Guid? groomerId,
        decimal grossAmount,
        decimal expectedSettlementAmount,
        DateTimeOffset redeemedAt,
        string note,
        string operatorUserId,
        CancellationToken cancellationToken = default)
    {
        platformOrderNumber = platformOrderNumber?.Trim() ?? string.Empty;
        voucherCodeSuffix = voucherCodeSuffix?.Trim() ?? string.Empty;
        dealName = dealName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(platformOrderNumber)) throw new InvalidOperationException("请填写美团订单号。");
        if (string.IsNullOrWhiteSpace(dealName)) throw new InvalidOperationException("请填写团购套餐名称。");
        if (grossAmount <= 0) throw new InvalidOperationException("核销金额必须大于零。");
        if (expectedSettlementAmount < 0) throw new InvalidOperationException("预计结算金额不能小于零。");
        if (await db.GroupBuyRecords.AnyAsync(x => x.PlatformOrderNumber == platformOrderNumber, cancellationToken))
            throw new InvalidOperationException("该美团订单号已经登记，请勿重复计入。");
        if (groomerId.HasValue && !await db.StaffMembers.AnyAsync(x => x.Id == groomerId && x.IsActive && x.IsGroomer, cancellationToken))
            throw new InvalidOperationException("请选择有效的实际美容师。");

        var now = clock.GetLocalNow();
        var record = new GroupBuyRecord
        {
            PlatformOrderNumber = platformOrderNumber,
            VoucherCodeSuffix = voucherCodeSuffix,
            DealName = dealName,
            CustomerName = customerName?.Trim() ?? string.Empty,
            PetName = petName?.Trim() ?? string.Empty,
            GroomerId = groomerId,
            GrossAmount = grossAmount,
            ExpectedSettlementAmount = expectedSettlementAmount,
            RedeemedAt = redeemedAt,
            OperatorUserId = operatorUserId,
            Note = note?.Trim() ?? string.Empty,
            CreatedAt = now
        };
        db.GroupBuyRecords.Add(record);
        db.AuditLogs.Add(new AuditLog
        {
            Action = "登记美团团购",
            EntityType = nameof(GroupBuyRecord),
            EntityId = record.Id.ToString(),
            OperatorUserId = operatorUserId,
            Detail = $"订单 {platformOrderNumber}；{dealName}；核销 ¥{grossAmount:F2}；预计结算 ¥{expectedSettlementAmount:F2}",
            CreatedAt = now
        });
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new InvalidOperationException("该美团订单号已经登记，请勿重复计入。"); }
        return record;
    }

    public async Task<GroupBuyRecord> SettleAsync(Guid id, decimal actualAmount, string operatorUserId, CancellationToken cancellationToken = default)
    {
        if (actualAmount < 0) throw new InvalidOperationException("实际到账不能小于零。");
        var record = await db.GroupBuyRecords.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("没有找到该团购记录。");
        if (record.SettlementStatus == GroupBuySettlementStatus.Settled) throw new InvalidOperationException("该记录已经确认到账。");
        var now = clock.GetLocalNow();
        record.ActualSettlementAmount = actualAmount;
        record.SettlementStatus = GroupBuySettlementStatus.Settled;
        record.SettledAt = now;
        record.ReconciledByUserId = operatorUserId;
        db.AuditLogs.Add(new AuditLog
        {
            Action = "确认团购到账", EntityType = nameof(GroupBuyRecord), EntityId = id.ToString(),
            OperatorUserId = operatorUserId,
            Detail = $"订单 {record.PlatformOrderNumber}；实际到账 ¥{actualAmount:F2}；与预计差额 ¥{actualAmount - record.ExpectedSettlementAmount:F2}", CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<GroupBuyRefund> AddRefundAsync(Guid id, decimal amount, string reason, string operatorUserId, CancellationToken cancellationToken = default)
    {
        if (amount <= 0) throw new InvalidOperationException("退款金额必须大于零。");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("请填写退款原因。");
        var record = await db.GroupBuyRecords.Include(x => x.Refunds).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("没有找到该团购记录。");
        var refunded = record.Refunds.Sum(x => x.Amount);
        if (refunded + amount > record.GrossAmount) throw new InvalidOperationException("累计退款不能超过核销金额。");
        var now = clock.GetLocalNow();
        var refund = new GroupBuyRefund
        {
            GroupBuyRecordId = id, Amount = amount, Reason = reason.Trim(), OperatorUserId = operatorUserId, CreatedAt = now
        };
        db.GroupBuyRefunds.Add(refund);
        db.AuditLogs.Add(new AuditLog
        {
            Action = "登记团购退款", EntityType = nameof(GroupBuyRecord), EntityId = id.ToString(),
            OperatorUserId = operatorUserId,
            Detail = $"订单 {record.PlatformOrderNumber}；退款 ¥{amount:F2}；{refund.Reason}", CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return refund;
    }
}
