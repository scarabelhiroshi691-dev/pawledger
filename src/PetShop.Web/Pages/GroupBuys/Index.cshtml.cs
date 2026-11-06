using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Services;

namespace PetShop.Web.Pages.GroupBuys;

public sealed class IndexModel(ApplicationDbContext db, GroupBuyService service, TimeProvider clock) : PageModel
{
    [BindProperty] public RegisterInput Input { get; set; } = new();
    public List<StaffMember> Groomers { get; private set; } = [];
    public List<GroupBuyRecord> Recent { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (Input.RedeemedAt == default) Input.RedeemedAt = clock.GetLocalNow().DateTime;
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await LoadAsync(cancellationToken); return Page(); }
        var local = DateTime.SpecifyKind(Input.RedeemedAt, DateTimeKind.Unspecified);
        var redeemedAt = new DateTimeOffset(local, clock.GetLocalNow().Offset);
        try
        {
            await service.RegisterAsync(Input.PlatformOrderNumber, Input.VoucherCodeSuffix, Input.DealName,
                Input.CustomerName, Input.PetName, Input.GroomerId, Input.GrossAmount,
                Input.ExpectedSettlementAmount, redeemedAt, Input.Note, UserId(), cancellationToken);
            TempData["Success"] = "团购履约已登记，并进入待结算清单。";
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Groomers = await db.StaffMembers.AsNoTracking().Where(x => x.IsActive && x.IsGroomer).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var records = await db.GroupBuyRecords.AsNoTracking().Include(x => x.Groomer).Include(x => x.Refunds).ToListAsync(cancellationToken);
        Recent = records.OrderByDescending(x => x.RedeemedAt).Take(30).ToList();
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public sealed class RegisterInput
    {
        [Required(ErrorMessage = "请填写美团订单号。"), StringLength(80)] public string PlatformOrderNumber { get; set; } = string.Empty;
        [StringLength(8)] public string VoucherCodeSuffix { get; set; } = string.Empty;
        [Required(ErrorMessage = "请填写套餐名称。"), StringLength(160)] public string DealName { get; set; } = string.Empty;
        [StringLength(50)] public string CustomerName { get; set; } = string.Empty;
        [StringLength(50)] public string PetName { get; set; } = string.Empty;
        public Guid? GroomerId { get; set; }
        [Range(0.01, 999999, ErrorMessage = "核销金额必须大于零。")] public decimal GrossAmount { get; set; }
        [Range(0, 999999, ErrorMessage = "预计结算金额不能小于零。")] public decimal ExpectedSettlementAmount { get; set; }
        [Required(ErrorMessage = "请选择官方核销时间。"), DataType(DataType.DateTime)] public DateTime RedeemedAt { get; set; }
        [StringLength(500)] public string Note { get; set; } = string.Empty;
    }
}
