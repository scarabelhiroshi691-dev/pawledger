using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Services;

namespace PetShop.Web.Pages.Members;

public sealed class RechargeModel(ApplicationDbContext db, StoredValueService storedValue, UserManager<IdentityUser> users) : PageModel
{
    [BindProperty] public RechargeInput Input { get; set; } = new();
    public IReadOnlyList<SelectListItem> MemberOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> PaymentMethods { get; } = [new("现金", "1"), new("微信", "2"), new("支付宝", "3"), new("银行卡", "4"), new("其他", "5")];
    public async Task OnGetAsync(Guid? cardId) { Input.RequestId = Guid.NewGuid().ToString("N"); if (cardId.HasValue) Input.MemberCardId = cardId.Value; await LoadOptionsAsync(); }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await LoadOptionsAsync(); return Page(); }
        try
        {
            var user = await users.GetUserAsync(User);
            await storedValue.RechargeAsync(Input.MemberCardId, Input.Principal, Input.Bonus, Input.Method, Input.RequestId, user!.Id, Input.Note);
            var customerId = await db.MemberCards.Where(x => x.Id == Input.MemberCardId).Select(x => x.CustomerId).SingleAsync();
            TempData["Success"] = $"充值成功：本金 {Input.Principal:F2} 元，赠送金 {Input.Bonus:F2} 元。";
            return RedirectToPage("Details", new { id = customerId });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            ModelState.AddModelError(string.Empty, ex.Message); await LoadOptionsAsync(); return Page();
        }
    }
    private async Task LoadOptionsAsync() { var cards = await db.MemberCards.AsNoTracking().Include(x => x.Customer).Where(x => x.IsActive).ToListAsync(); MemberOptions = cards.OrderBy(x => x.Customer.Name).Select(x => new SelectListItem($"{x.Customer.Name} · {x.Customer.Phone} · 余额 {(x.PrincipalBalance + x.BonusBalance):F2}", x.Id.ToString())).ToList(); }
    public sealed class RechargeInput
    {
        [Required] public Guid MemberCardId { get; set; }
        [Range(0.01, 9999999)] public decimal Principal { get; set; }
        [Range(0, 9999999)] public decimal Bonus { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        [Required] public string RequestId { get; set; } = string.Empty;
        [StringLength(300)] public string Note { get; set; } = string.Empty;
    }
}
