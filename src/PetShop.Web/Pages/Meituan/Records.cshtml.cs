using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Services;

namespace PetShop.Web.Pages.Meituan;

public sealed class RecordsModel(ApplicationDbContext db, MeituanVoucherService service) : PageModel
{
    public IReadOnlyList<MeituanVoucherRedemption> Records { get; private set; } = [];
    [TempData] public string? Notice { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // SQLite cannot translate DateTimeOffset ordering. This is a single-store
        // audit list, so sorting the local result in memory is safe and predictable.
        var records = await db.MeituanVoucherRedemptions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Records = records
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToList();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            await service.RevokeAsync(id, reason, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, cancellationToken);
            Notice = "撤销成功，核销记录已更新。 ";
        }
        catch (InvalidOperationException ex) { Notice = ex.Message; }
        return RedirectToPage();
    }

    public static string StatusText(MeituanVoucherStatus status) => status switch
    {
        MeituanVoucherStatus.Pending => "处理中", MeituanVoucherStatus.Redeemed => "已核销",
        MeituanVoucherStatus.Revoked => "已撤销", MeituanVoucherStatus.Failed => "失败", _ => "结果未知"
    };
}
