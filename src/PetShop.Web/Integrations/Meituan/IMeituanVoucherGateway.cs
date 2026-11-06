namespace PetShop.Web.Integrations.Meituan;

public interface IMeituanVoucherGateway
{
    string ModeName { get; }
    PetShop.Web.Data.MeituanProviderMode ProviderMode { get; }
    Task<MeituanVoucherQuote> QueryAsync(string voucherCode, CancellationToken cancellationToken);
    Task<MeituanRedeemResult> RedeemAsync(string voucherCode, string requestId, CancellationToken cancellationToken);
    Task<MeituanRevokeResult> RevokeAsync(string providerTransactionId, string reason, CancellationToken cancellationToken);
}

public sealed record MeituanVoucherQuote(
    bool IsUsable,
    string DealId,
    string DealName,
    decimal SalePrice,
    decimal MarketPrice,
    int Quantity,
    string Message);

public sealed record MeituanRedeemResult(bool Success, string ProviderTransactionId, string Message);
public sealed record MeituanRevokeResult(bool Success, string Message);
