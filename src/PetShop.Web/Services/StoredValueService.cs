using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Services;

public sealed record RechargeResult(Guid OrderId, decimal PrincipalBalance, decimal BonusBalance, bool WasDuplicate);
public sealed record ChargeResult(Guid PaymentId, decimal PrincipalPaid, decimal BonusPaid, decimal ExternalPaid, bool WasDuplicate);
public sealed record RefundResult(Guid RefundId, decimal PrincipalReturned, decimal BonusReturned, decimal ExternalReturned, bool WasDuplicate);

public sealed class StoredValueService(ApplicationDbContext db, TimeProvider timeProvider)
{
    public async Task<RechargeResult> RechargeAsync(
        Guid memberCardId, decimal principal, decimal bonus, PaymentMethod method,
        string requestId, string operatorUserId, string? note, CancellationToken cancellationToken = default)
    {
        ValidateMoney(principal, nameof(principal), positive: true);
        ValidateMoney(bonus, nameof(bonus), positive: false);
        ValidateContext(requestId, operatorUserId);

        var duplicate = await db.RechargeOrders.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (duplicate is not null)
        {
            var existingCard = await db.MemberCards.AsNoTracking().SingleAsync(x => x.Id == duplicate.MemberCardId, cancellationToken);
            return new(duplicate.Id, existingCard.PrincipalBalance, existingCard.BonusBalance, true);
        }

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var card = await db.MemberCards.SingleOrDefaultAsync(x => x.Id == memberCardId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("会员卡不存在或已停用。");
        var now = timeProvider.GetLocalNow();
        var order = new RechargeOrder
        {
            MemberCardId = card.Id, PrincipalAmount = principal, BonusAmount = bonus, PaymentMethod = method,
            RequestId = requestId, OperatorUserId = operatorUserId, Note = note?.Trim() ?? string.Empty, CreatedAt = now
        };

        card.PrincipalBalance += principal;
        card.BonusBalance += bonus;
        card.Version++;
        db.RechargeOrders.Add(order);
        AddLedger(card, LedgerBucket.Principal, LedgerKind.RechargePrincipal, principal, order.Id, null, requestId, operatorUserId, note, now);
        if (bonus > 0)
        {
            AddLedger(card, LedgerBucket.Bonus, LedgerKind.RechargeBonus, bonus, order.Id, null, requestId, operatorUserId, note, now);
        }
        AddAudit("会员充值", nameof(MemberCard), card.Id.ToString(), operatorUserId, $"本金 {principal:F2}，赠送金 {bonus:F2}，方式 {method}", now);

        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new(order.Id, card.PrincipalBalance, card.BonusBalance, false);
    }

    public async Task<ChargeResult> ChargeAsync(
        DocumentType documentType, Guid documentId, Guid? memberCardId, decimal amount,
        PaymentMethod externalMethod, string requestId, string operatorUserId, CancellationToken cancellationToken = default)
    {
        ValidateMoney(amount, nameof(amount), positive: true);
        ValidateContext(requestId, operatorUserId);
        var duplicate = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (duplicate is not null)
        {
            return new(duplicate.Id, duplicate.PrincipalPaid, duplicate.BonusPaid, duplicate.ExternalPaid, true);
        }

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        MemberCard? card = null;
        if (memberCardId.HasValue)
        {
            card = await db.MemberCards.SingleOrDefaultAsync(x => x.Id == memberCardId.Value && x.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("会员卡不存在或已停用。");
        }

        var bonusPaid = Math.Min(amount, card?.BonusBalance ?? 0);
        var remaining = amount - bonusPaid;
        var principalPaid = Math.Min(remaining, card?.PrincipalBalance ?? 0);
        var externalPaid = remaining - principalPaid;
        if (externalPaid > 0 && externalMethod == PaymentMethod.None)
        {
            throw new InvalidOperationException("会员余额不足时必须选择外部收款方式。");
        }

        var now = timeProvider.GetLocalNow();
        var payment = new PaymentRecord
        {
            DocumentType = documentType, DocumentId = documentId, MemberCardId = card?.Id, TotalAmount = amount,
            PrincipalPaid = principalPaid, BonusPaid = bonusPaid, ExternalPaid = externalPaid,
            ExternalMethod = externalPaid > 0 ? externalMethod : PaymentMethod.None,
            RequestId = requestId, OperatorUserId = operatorUserId, CreatedAt = now
        };
        db.Payments.Add(payment);

        if (card is not null)
        {
            if (bonusPaid > 0)
            {
                card.BonusBalance -= bonusPaid;
                AddLedger(card, LedgerBucket.Bonus, LedgerKind.SpendBonus, -bonusPaid, documentId, null, requestId, operatorUserId, documentType.ToString(), now);
            }
            if (principalPaid > 0)
            {
                card.PrincipalBalance -= principalPaid;
                AddLedger(card, LedgerBucket.Principal, LedgerKind.SpendPrincipal, -principalPaid, documentId, null, requestId, operatorUserId, documentType.ToString(), now);
            }
            card.Version++;
        }

        AddAudit("订单结算", documentType.ToString(), documentId.ToString(), operatorUserId,
            $"应收 {amount:F2}，本金 {principalPaid:F2}，赠送 {bonusPaid:F2}，外部 {externalPaid:F2}", now);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new(payment.Id, principalPaid, bonusPaid, externalPaid, false);
    }

    public async Task<RefundResult> RefundPaymentAsync(
        Guid paymentId, decimal amount, string requestId, string operatorUserId, string reason,
        CancellationToken cancellationToken = default)
    {
        ValidateMoney(amount, nameof(amount), positive: true);
        ValidateContext(requestId, operatorUserId);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("退款原因不能为空。", nameof(reason));

        var duplicate = await db.PaymentRefunds.AsNoTracking().FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (duplicate is not null)
        {
            return new(duplicate.Id, duplicate.PrincipalAmount, duplicate.BonusAmount, duplicate.ExternalAmount, true);
        }

        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException("原支付记录不存在。");
        var refundable = payment.TotalAmount - payment.PrincipalRefunded - payment.BonusRefunded - payment.ExternalRefunded;
        if (amount > refundable) throw new InvalidOperationException("退款金额超过原支付尚可退款金额。");

        // 混合支付部分退款：先退最后支付的外部金额，再恢复本金，最后恢复赠送金。
        var external = Math.Min(amount, payment.ExternalPaid - payment.ExternalRefunded);
        var remaining = amount - external;
        var principal = Math.Min(remaining, payment.PrincipalPaid - payment.PrincipalRefunded);
        var bonus = remaining - principal;
        var now = timeProvider.GetLocalNow();
        MemberCard? card = null;
        if (payment.MemberCardId.HasValue)
        {
            card = await db.MemberCards.SingleAsync(x => x.Id == payment.MemberCardId.Value, cancellationToken);
            if (principal > 0)
            {
                card.PrincipalBalance += principal;
                AddLedger(card, LedgerBucket.Principal, LedgerKind.SpendRefundPrincipal, principal, payment.DocumentId, payment.Id, requestId, operatorUserId, reason, now);
            }
            if (bonus > 0)
            {
                card.BonusBalance += bonus;
                AddLedger(card, LedgerBucket.Bonus, LedgerKind.SpendRefundBonus, bonus, payment.DocumentId, payment.Id, requestId, operatorUserId, reason, now);
            }
            card.Version++;
        }

        payment.ExternalRefunded += external;
        payment.PrincipalRefunded += principal;
        payment.BonusRefunded += bonus;
        var refund = new PaymentRefund
        {
            PaymentRecordId = payment.Id, PrincipalAmount = principal, BonusAmount = bonus,
            ExternalAmount = external, RequestId = requestId, OperatorUserId = operatorUserId,
            Reason = reason.Trim(), CreatedAt = now
        };
        db.PaymentRefunds.Add(refund);
        AddAudit("订单退款", payment.DocumentType.ToString(), payment.DocumentId.ToString(), operatorUserId,
            $"退款 {amount:F2}：本金 {principal:F2}，赠送 {bonus:F2}，外部 {external:F2}；{reason}", now);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new(refund.Id, principal, bonus, external, false);
    }

    public async Task ReverseRechargeAsync(Guid rechargeId, string requestId, string operatorUserId, string reason, CancellationToken cancellationToken = default)
    {
        ValidateContext(requestId, operatorUserId);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("退款原因不能为空。", nameof(reason));
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var order = await db.RechargeOrders.Include(x => x.MemberCard).SingleOrDefaultAsync(x => x.Id == rechargeId, cancellationToken)
            ?? throw new InvalidOperationException("原充值单不存在。");
        if (order.ReversedAt.HasValue) return;
        var principal = order.PrincipalAmount - order.PrincipalRefunded;
        var bonus = order.BonusAmount - order.BonusReversed;
        if (order.MemberCard.PrincipalBalance < principal || order.MemberCard.BonusBalance < bonus)
            throw new InvalidOperationException("该笔充值资金已被消费，需先退回相关消费后才能撤销充值。");

        var now = timeProvider.GetLocalNow();
        order.MemberCard.PrincipalBalance -= principal;
        order.MemberCard.BonusBalance -= bonus;
        order.MemberCard.Version++;
        if (principal > 0) AddLedger(order.MemberCard, LedgerBucket.Principal, LedgerKind.RechargeRefundPrincipal, -principal, order.Id, order.Id, requestId, operatorUserId, reason, now);
        if (bonus > 0) AddLedger(order.MemberCard, LedgerBucket.Bonus, LedgerKind.RechargeBonusReversal, -bonus, order.Id, order.Id, requestId, operatorUserId, reason, now);
        order.PrincipalRefunded += principal;
        order.BonusReversed += bonus;
        order.ReversalRequestId = requestId;
        order.ReversedAt = now;
        AddAudit("充值撤销", nameof(RechargeOrder), order.Id.ToString(), operatorUserId, $"本金 {principal:F2}，赠送 {bonus:F2}；{reason}", now);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    private void AddLedger(MemberCard card, LedgerBucket bucket, LedgerKind kind, decimal amount, Guid sourceId,
        Guid? originalId, string requestId, string operatorId, string? note, DateTimeOffset now)
    {
        var balanceAfter = bucket == LedgerBucket.Principal ? card.PrincipalBalance : card.BonusBalance;
        if (balanceAfter < 0) throw new InvalidOperationException("交易会导致会员余额为负数。");
        db.LedgerTransactions.Add(new LedgerTransaction
        {
            MemberCardId = card.Id, Bucket = bucket, Kind = kind, Amount = amount, BalanceAfter = balanceAfter,
            SourceDocumentId = sourceId, OriginalDocumentId = originalId, RequestId = requestId,
            OperatorUserId = operatorId, Note = note?.Trim() ?? string.Empty, CreatedAt = now
        });
    }

    private void AddAudit(string action, string entityType, string entityId, string operatorId, string detail, DateTimeOffset now) =>
        db.AuditLogs.Add(new AuditLog { Action = action, EntityType = entityType, EntityId = entityId, OperatorUserId = operatorId, Detail = detail, CreatedAt = now });

    private static void ValidateMoney(decimal amount, string name, bool positive)
    {
        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount) throw new ArgumentException("金额最多保留两位小数。", name);
        if (positive ? amount <= 0 : amount < 0) throw new ArgumentOutOfRangeException(name, positive ? "金额必须大于零。" : "金额不能小于零。");
    }

    private static void ValidateContext(string requestId, string operatorUserId)
    {
        if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException("请求标识不能为空。", nameof(requestId));
        if (string.IsNullOrWhiteSpace(operatorUserId)) throw new ArgumentException("操作人不能为空。", nameof(operatorUserId));
    }
}
