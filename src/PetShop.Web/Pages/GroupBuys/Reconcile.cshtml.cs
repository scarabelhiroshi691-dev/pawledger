using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Services;

namespace PetShop.Web.Pages.GroupBuys;

public sealed class ReconcileModel(ApplicationDbContext db, GroupBuyService service) : PageModel
{
    [BindProperty] public SettleInput Settlement { get; set; } = new();
    [BindProperty] public RefundInput Refund { get; set; } = new();
    public List<GroupBuyRecord> Records { get; private set; } = [];
    public decimal PendingExpected => Records.Where(x => x.SettlementStatus == GroupBuySettlementStatus.Pending).Sum(x => x.ExpectedSettlementAmount);
    public decimal SettledActual => Records.Where(x => x.SettlementStatus == GroupBuySettlementStatus.Settled).Sum(x => x.ActualSettlementAmount ?? 0);
    public decimal Refunded => Records.Sum(x => x.Refunds.Sum(r => r.Amount));

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostSettleAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        TryValidateModel(Settlement, nameof(Settlement));
        if (!ModelState.IsValid) { TempData["Error"] = "请检查实际到账金额。"; return RedirectToPage(); }
        try
        {
            await service.SettleAsync(Settlement.Id, Settlement.ActualAmount, UserId(), cancellationToken);
            TempData["Success"] = "已确认平台到账。";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRefundAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        TryValidateModel(Refund, nameof(Refund));
        if (!ModelState.IsValid) { TempData["Error"] = "请填写有效退款金额和原因。"; return RedirectToPage(); }
        try
        {
            await service.AddRefundAsync(Refund.Id, Refund.Amount, Refund.Reason, UserId(), cancellationToken);
            TempData["Success"] = "团购退款已登记。";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var records = await db.GroupBuyRecords.AsNoTracking().Include(x => x.Groomer).Include(x => x.Refunds).ToListAsync(cancellationToken);
        Records = records.OrderBy(x => x.SettlementStatus).ThenByDescending(x => x.RedeemedAt).Take(200).ToList();
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public sealed class SettleInput { [Required] public Guid Id { get; set; } [Range(0, 999999)] public decimal ActualAmount { get; set; } }
    public sealed class RefundInput { [Required] public Guid Id { get; set; } [Range(0.01, 999999)] public decimal Amount { get; set; } [Required, StringLength(300)] public string Reason { get; set; } = string.Empty; }
}
