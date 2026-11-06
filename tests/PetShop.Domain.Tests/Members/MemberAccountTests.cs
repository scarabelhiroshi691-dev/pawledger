using PetShop.Domain.Members;

namespace PetShop.Domain.Tests.Members;

public sealed class MemberAccountTests
{
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _operatorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 9, 1, 9, 0, 0, TimeSpan.FromHours(8));

    [Fact]
    public void Recharge_keeps_principal_and_bonus_separate()
    {
        var account = CreateAccount();

        account.Recharge(1_000m, 100m, Guid.NewGuid(), _operatorId, "recharge-001", _now);

        Assert.Equal(1_000m, account.PrincipalBalance);
        Assert.Equal(100m, account.BonusBalance);
        Assert.Equal(1_100m, account.AvailableBalance);
        Assert.Equal(2, account.Entries.Count);
    }

    [Fact]
    public void Spend_uses_bonus_before_principal()
    {
        var account = CreateFundedAccount();

        var charge = account.Spend(180m, Guid.NewGuid(), _operatorId, "order-001", _now);

        Assert.Equal(100m, charge.BonusPaid);
        Assert.Equal(80m, charge.PrincipalPaid);
        Assert.Equal(0m, charge.ExternalAmountDue);
        Assert.Equal(920m, account.PrincipalBalance);
        Assert.Equal(0m, account.BonusBalance);
    }

    [Fact]
    public void Spend_reports_external_amount_when_balance_is_insufficient()
    {
        var account = CreateFundedAccount();

        var charge = account.Spend(1_200m, Guid.NewGuid(), _operatorId, "order-002", _now);

        Assert.Equal(100m, charge.BonusPaid);
        Assert.Equal(1_000m, charge.PrincipalPaid);
        Assert.Equal(100m, charge.ExternalAmountDue);
        Assert.Equal(0m, account.AvailableBalance);
    }

    [Fact]
    public void Full_refund_restores_the_original_fund_buckets()
    {
        var account = CreateFundedAccount();
        var charge = account.Spend(180m, Guid.NewGuid(), _operatorId, "order-003", _now);

        account.RefundSpend(charge, 180m, Guid.NewGuid(), _operatorId, "refund-001", _now);

        Assert.Equal(1_000m, account.PrincipalBalance);
        Assert.Equal(100m, account.BonusBalance);
        Assert.Equal(180m, charge.StoredValueRefunded);
    }

    [Fact]
    public void Partial_refund_restores_principal_before_bonus()
    {
        var account = CreateFundedAccount();
        var charge = account.Spend(180m, Guid.NewGuid(), _operatorId, "order-004", _now);

        account.RefundSpend(charge, 50m, Guid.NewGuid(), _operatorId, "refund-002", _now);

        Assert.Equal(970m, account.PrincipalBalance);
        Assert.Equal(0m, account.BonusBalance);
        Assert.Equal(50m, charge.PrincipalRefunded);
        Assert.Equal(0m, charge.BonusRefunded);
    }

    [Fact]
    public void Refund_cannot_exceed_the_remaining_stored_value_payment()
    {
        var account = CreateFundedAccount();
        var charge = account.Spend(180m, Guid.NewGuid(), _operatorId, "order-005", _now);

        var action = () => account.RefundSpend(
            charge, 181m, Guid.NewGuid(), _operatorId, "refund-003", _now);

        var error = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("不能超过", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.001)]
    public void Recharge_rejects_invalid_principal(decimal principal)
    {
        var account = CreateAccount();

        Assert.ThrowsAny<ArgumentException>(() =>
            account.Recharge(principal, 0m, Guid.NewGuid(), _operatorId, "invalid", _now));
    }

    private MemberAccount CreateAccount() => new(Guid.NewGuid(), _ownerId);

    private MemberAccount CreateFundedAccount()
    {
        var account = CreateAccount();
        account.Recharge(1_000m, 100m, Guid.NewGuid(), _operatorId, "seed", _now);
        return account;
    }
}
