namespace PetShop.Domain.Members;

public sealed class StoredValueCharge
{
    internal StoredValueCharge(
        Guid id,
        Guid accountId,
        decimal orderAmount,
        decimal principalPaid,
        decimal bonusPaid,
        decimal externalAmountDue)
    {
        Id = id;
        AccountId = accountId;
        OrderAmount = orderAmount;
        PrincipalPaid = principalPaid;
        BonusPaid = bonusPaid;
        ExternalAmountDue = externalAmountDue;
    }

    public Guid Id { get; }
    public Guid AccountId { get; }
    public decimal OrderAmount { get; }
    public decimal PrincipalPaid { get; }
    public decimal BonusPaid { get; }
    public decimal ExternalAmountDue { get; }
    public decimal PrincipalRefunded { get; private set; }
    public decimal BonusRefunded { get; private set; }
    public decimal StoredValuePaid => PrincipalPaid + BonusPaid;
    public decimal StoredValueRefunded => PrincipalRefunded + BonusRefunded;
    public decimal StoredValueRefundable => StoredValuePaid - StoredValueRefunded;

    internal (decimal Principal, decimal Bonus) AllocateRefund(decimal amount)
    {
        MoneyRules.EnsurePositive(amount, nameof(amount));

        if (amount > StoredValueRefundable)
        {
            throw new InvalidOperationException("退款金额不能超过该订单尚可退的会员卡支付金额。");
        }

        // 消费先扣赠送金、再扣本金；退款按相反顺序先恢复本金。
        var remainingPrincipal = PrincipalPaid - PrincipalRefunded;
        var principal = Math.Min(amount, remainingPrincipal);
        var bonus = amount - principal;

        PrincipalRefunded += principal;
        BonusRefunded += bonus;

        return (principal, bonus);
    }
}
