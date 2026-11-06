using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PetShop.Web.Data;
using PetShop.Web.Integrations.Meituan;
using PetShop.Web.Services;

namespace PetShop.Web.Pages.Meituan;

public sealed class RedeemModel(MeituanVoucherService service) : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "请输入美团团购券码。")]
    [StringLength(32, MinimumLength = 10, ErrorMessage = "券码长度应为 10–24 位。")]
    public string VoucherCode { get; set; } = string.Empty;

    [BindProperty] public string RequestId { get; set; } = string.Empty;
    [BindProperty] public bool Confirmed { get; set; }
    public MeituanVoucherQuote? Quote { get; private set; }
    public string ModeName => service.ModeName;
    public string? SuccessMessage { get; private set; }

    public void OnGet(string? code = null)
    {
        VoucherCode = code ?? string.Empty;
        RequestId = Guid.NewGuid().ToString("N");
    }

    public async Task<IActionResult> OnPostQueryAsync(CancellationToken cancellationToken)
    {
        RequestId = Guid.NewGuid().ToString("N");
        if (!ModelState.IsValid) return Page();
        Quote = await service.QueryAsync(VoucherCode, cancellationToken);
        if (!Quote.IsUsable) ModelState.AddModelError(string.Empty, Quote.Message);
        return Page();
    }

    public async Task<IActionResult> OnPostRedeemAsync(CancellationToken cancellationToken)
    {
        if (!Confirmed) ModelState.AddModelError(nameof(Confirmed), "核销前必须确认已核对套餐和顾客券码。 ");
        if (string.IsNullOrWhiteSpace(RequestId)) RequestId = Guid.NewGuid().ToString("N");
        if (!ModelState.IsValid)
        {
            Quote = await service.QueryAsync(VoucherCode, cancellationToken);
            return Page();
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var record = await service.RedeemAsync(VoucherCode, userId, RequestId, cancellationToken);
            SuccessMessage = $"核销成功：{record.DealName}，记录号 {record.ProviderTransactionId}";
            VoucherCode = string.Empty;
            RequestId = Guid.NewGuid().ToString("N");
            Confirmed = false;
            ModelState.Clear();
            return Page();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            Quote = await service.QueryAsync(VoucherCode, cancellationToken);
            return Page();
        }
    }
}
