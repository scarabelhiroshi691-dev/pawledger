namespace PetShop.Domain.Members;

public sealed class MemberAccount
{
    private readonly List<LedgerEntry> _entries = [];

    public MemberAccount(Guid id, Guid ownerId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("会员账户编号不能为空。", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("宠主编号不能为空。", nameof(ownerId));
        }

        Id = id;
        OwnerId = ownerId;
    }

    public Guid Id { get; }
    public Guid OwnerId { get; }
    public decimal PrincipalBalance { get; private set; }
    public decimal BonusBalance { get; private set; }
    public decimal AvailableBalance => PrincipalBalance + BonusBalance;
    public IReadOnlyList<LedgerEntry> Entries => _entries.AsReadOnly();

    public void Recharge(
        decimal principal,
        decimal bonus,
        Guid rechargeId,
        Guid performedByUserId,
        string requestId,
        DateTimeOffset occurredAt)
    {
        MoneyRules.EnsurePositive(principal, nameof(principal));
        MoneyRules.EnsureNonNegative(bonus, nameof(bonus));
        EnsureOperationContext(rechargeId, performedByUserId, requestId);

        AddEntry(FundBucket.Principal, LedgerEntryType.RechargePrincipal, principal,
            rechargeId, null, performedByUserId, requestId, occurredAt);

        if (bonus > 0)
        {
            AddEntry(FundBucket.Bonus, LedgerEntryType.RechargeBonus, bonus,
                rechargeId, null, performedByUserId, requestId, occurredAt);
        }
    }

    public StoredValueCharge Spend(
        decimal orderAmount,
        Guid orderId,
        Guid performedByUserId,
        string requestId,
        DateTimeOffset occurredAt)
    {
        MoneyRules.EnsurePositive(orderAmount, nameof(orderAmount));
        EnsureOperationContext(orderId, performedByUserId, requestId);

        var bonusPaid = Math.Min(orderAmount, BonusBalance);
        var remaining = orderAmount - bonusPaid;
        var principalPaid = Math.Min(remaining, PrincipalBalance);
        var externalAmountDue = remaining - principalPaid;

        if (bonusPaid > 0)
        {
            AddEntry(FundBucket.Bonus, LedgerEntryType.SpendBonus, -bonusPaid,
                orderId, null, performedByUserId, requestId, occurredAt);
        }

        if (principalPaid > 0)
        {
            AddEntry(FundBucket.Principal, LedgerEntryType.SpendPrincipal, -principalPaid,
                orderId, null, performedByUserId, requestId, occurredAt);
        }

        return new StoredValueCharge(
            orderId,
            Id,
            orderAmount,
            principalPaid,
            bonusPaid,
            externalAmountDue);
    }

    public void RefundSpend(
        StoredValueCharge charge,
        decimal amount,
        Guid refundId,
        Guid performedByUserId,
        string requestId,
        DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(charge);
        EnsureOperationContext(refundId, performedByUserId, requestId);

        if (charge.AccountId != Id)
        {
            throw new InvalidOperationException("不能把其他会员账户的消费退入当前账户。");
        }

        var allocation = charge.AllocateRefund(amount);

        if (allocation.Principal > 0)
        {
            AddEntry(FundBucket.Principal, LedgerEntryType.SpendRefundPrincipal,
                allocation.Principal, refundId, charge.Id, performedByUserId, requestId, occurredAt);
        }

        if (allocation.Bonus > 0)
        {
            AddEntry(FundBucket.Bonus, LedgerEntryType.SpendRefundBonus,
                allocation.Bonus, refundId, charge.Id, performedByUserId, requestId, occurredAt);
        }
    }

    private void AddEntry(
        FundBucket bucket,
        LedgerEntryType type,
        decimal amount,
        Guid sourceDocumentId,
        Guid? originalEntryId,
        Guid performedByUserId,
        string requestId,
        DateTimeOffset occurredAt)
    {
        if (amount == 0)
        {
            throw new InvalidOperationException("资金流水金额不能为零。");
        }

        var nextPrincipal = PrincipalBalance + (bucket == FundBucket.Principal ? amount : 0);
        var nextBonus = BonusBalance + (bucket == FundBucket.Bonus ? amount : 0);

        if (nextPrincipal < 0 || nextBonus < 0)
        {
            throw new InvalidOperationException("交易会导致会员余额为负数。");
        }

        _entries.Add(new LedgerEntry(
            Guid.NewGuid(), Id, bucket, type, amount, sourceDocumentId, originalEntryId,
            performedByUserId, requestId, occurredAt));

        PrincipalBalance = nextPrincipal;
        BonusBalance = nextBonus;
    }

    private static void EnsureOperationContext(
        Guid sourceDocumentId,
        Guid performedByUserId,
        string requestId)
    {
        if (sourceDocumentId == Guid.Empty)
        {
            throw new ArgumentException("业务单据编号不能为空。", nameof(sourceDocumentId));
        }

        if (performedByUserId == Guid.Empty)
        {
            throw new ArgumentException("操作人编号不能为空。", nameof(performedByUserId));
        }

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("幂等请求标识不能为空。", nameof(requestId));
        }
    }
}
