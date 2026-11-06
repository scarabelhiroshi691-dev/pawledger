using System.Security.Cryptography;
using System.Text;

namespace PetShop.Web.Integrations.Meituan;

public sealed class MockMeituanVoucherGateway : IMeituanVoucherGateway
{
    public string ModeName => "模拟联调";
    public PetShop.Web.Data.MeituanProviderMode ProviderMode => PetShop.Web.Data.MeituanProviderMode.Mock;

    public Task<MeituanVoucherQuote> QueryAsync(string voucherCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = Normalize(voucherCode);
        if (code.Length is < 10 or > 24 || !code.All(char.IsLetterOrDigit))
            return Task.FromResult(new MeituanVoucherQuote(false, "", "", 0, 0, 0, "券码应为 10–24 位数字或字母。"));
        if (code.StartsWith("0000", StringComparison.Ordinal))
            return Task.FromResult(new MeituanVoucherQuote(false, "", "", 0, 0, 0, "模拟结果：该券已使用或不可用。"));

        var selector = SHA256.HashData(Encoding.UTF8.GetBytes(code))[0] % 3;
        var deal = selector switch
        {
            0 => ("MT-GROOM-88", "宠物基础洗护套餐", 88m, 108m),
            1 => ("MT-GROOM-128", "宠物精致洗护套餐", 128m, 158m),
            _ => ("MT-GROOM-168", "宠物洗剪造型套餐", 168m, 218m)
        };
        return Task.FromResult(new MeituanVoucherQuote(true, deal.Item1, deal.Item2, deal.Item3, deal.Item4, 1, "模拟查询成功，请核对套餐后确认核销。"));
    }

    public Task<MeituanRedeemResult> RedeemAsync(string voucherCode, string requestId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{Normalize(voucherCode)}:{requestId}")))[..16];
        return Task.FromResult(new MeituanRedeemResult(true, $"MOCK-{fingerprint}", "模拟核销成功。"));
    }

    public Task<MeituanRevokeResult> RevokeAsync(string providerTransactionId, string reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new MeituanRevokeResult(true, "模拟撤销成功。"));
    }

    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
