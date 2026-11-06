using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Integrations.Meituan;

namespace PetShop.Web.Services;

public sealed class MeituanVoucherService(ApplicationDbContext db, IMeituanVoucherGateway gateway, TimeProvider clock)
{
    public string ModeName => gateway.ModeName;

    public Task<MeituanVoucherQuote> QueryAsync(string code, CancellationToken cancellationToken = default) =>
        gateway.QueryAsync(Normalize(code), cancellationToken);

    public async Task<MeituanVoucherRedemption> RedeemAsync(string code, string operatorUserId, string requestId, CancellationToken cancellationToken = default)
    {
        code = Normalize(code);
        requestId = requestId.Trim();
        if (string.IsNullOrWhiteSpace(operatorUserId)) throw new InvalidOperationException("无法识别当前操作员工。请重新登录。 ");
        if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException("缺少请求编号。", nameof(requestId));

        var previousRequest = await db.MeituanVoucherRedemptions.SingleOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (previousRequest is not null) return previousRequest;

        var codeHash = Hash(code);
        var existing = await db.MeituanVoucherRedemptions.SingleOrDefaultAsync(x => x.VoucherCodeHash == codeHash, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException(existing.Status == MeituanVoucherStatus.Revoked ? "该券曾核销后撤销，需在美团确认券状态后重新处理。" : "该券已在本系统处理，请勿重复核销。 ");

        var quote = await gateway.QueryAsync(code, cancellationToken);
        if (!quote.IsUsable) throw new InvalidOperationException(quote.Message);

        var now = clock.GetUtcNow();
        var record = new MeituanVoucherRedemption
        {
            VoucherCodeHash = codeHash,
            VoucherCodeMasked = Mask(code),
            RequestId = requestId,
            DealId = quote.DealId,
            DealName = quote.DealName,
            SalePrice = quote.SalePrice,
            MarketPrice = quote.MarketPrice,
            Quantity = quote.Quantity,
            ProviderMode = gateway.ProviderMode,
            OperatorUserId = operatorUserId,
            CreatedAt = now,
            LastProviderMessage = "已建立待核销记录。"
        };
        db.MeituanVoucherRedemptions.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await gateway.RedeemAsync(code, requestId, cancellationToken);
            record.ProviderTransactionId = result.ProviderTransactionId;
            record.LastProviderMessage = result.Message;
            record.Status = result.Success ? MeituanVoucherStatus.Redeemed : MeituanVoucherStatus.Failed;
            record.RedeemedAt = result.Success ? clock.GetUtcNow() : null;
            await db.SaveChangesAsync(cancellationToken);
            if (!result.Success) throw new InvalidOperationException(result.Message);
            return record;
        }
        catch
        {
            if (record.Status == MeituanVoucherStatus.Pending)
            {
                record.Status = MeituanVoucherStatus.Unknown;
                record.LastProviderMessage = "平台结果未知，请先核对记录，禁止直接重试。";
                await db.SaveChangesAsync(CancellationToken.None);
            }
            throw;
        }
    }

    public async Task RevokeAsync(Guid id, string reason, string operatorUserId, CancellationToken cancellationToken = default)
    {
        var record = await db.MeituanVoucherRedemptions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("未找到该核销记录。 ");
        if (record.Status == MeituanVoucherStatus.Revoked) return;
        if (record.Status != MeituanVoucherStatus.Redeemed) throw new InvalidOperationException("只有已核销记录可以撤销。 ");
        reason = reason.Trim();
        if (reason.Length < 2) throw new InvalidOperationException("请填写撤销原因。 ");

        var result = await gateway.RevokeAsync(record.ProviderTransactionId, reason, cancellationToken);
        if (!result.Success) throw new InvalidOperationException(result.Message);
        record.Status = MeituanVoucherStatus.Revoked;
        record.RevokedAt = clock.GetUtcNow();
        record.RevokedByUserId = operatorUserId;
        record.RevokeReason = reason;
        record.LastProviderMessage = result.Message;
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static string Normalize(string value) => new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    internal static string Mask(string value) => value.Length <= 6 ? new string('*', value.Length) : $"{value[..3]}****{value[^3..]}";
}
