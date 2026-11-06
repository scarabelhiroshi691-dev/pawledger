namespace PetShop.Domain.Members;

public enum LedgerEntryType
{
    RechargePrincipal = 1,
    RechargeBonus = 2,
    SpendPrincipal = 3,
    SpendBonus = 4,
    SpendRefundPrincipal = 5,
    SpendRefundBonus = 6,
    RechargeRefundPrincipal = 7,
    RechargeBonusReversal = 8,
    AdministrativeReversal = 9,
    OpeningBalance = 10
}
