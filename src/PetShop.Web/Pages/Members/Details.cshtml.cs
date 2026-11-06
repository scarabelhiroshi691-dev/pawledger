using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages.Members;

public sealed class DetailsModel(ApplicationDbContext db, UserManager<IdentityUser> users, TimeProvider timeProvider) : PageModel
{
    public CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("zh-CN");
    public Customer? Customer { get; private set; }
    public MemberCard? Card => Customer?.MemberCard;
    public IReadOnlyList<LedgerRow> Ledger { get; private set; } = [];
    [BindProperty] public PetInputModel PetInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id) => await LoadAsync(id) ? Page() : NotFound();
    public async Task<IActionResult> OnPostAddPetAsync(Guid id)
    {
        if (!ModelState.IsValid) { await LoadAsync(id); return Page(); }
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == id);
        if (customer is null) return NotFound();
        var now = timeProvider.GetLocalNow();
        var pet = new Pet { CustomerId = id, Name = PetInput.Name.Trim(), Species = PetInput.Species.Trim(), Breed = PetInput.Breed?.Trim() ?? string.Empty, WeightKg = PetInput.WeightKg, HealthNotes = PetInput.HealthNotes?.Trim() ?? string.Empty, CreatedAt = now };
        db.Pets.Add(pet);
        var user = await users.GetUserAsync(User);
        db.AuditLogs.Add(new AuditLog { Action = "新增宠物", EntityType = nameof(Pet), EntityId = pet.Id.ToString(), OperatorUserId = user!.Id, Detail = $"{customer.Name} / {pet.Name}", CreatedAt = now });
        await db.SaveChangesAsync();
        TempData["Success"] = $"已添加宠物 {pet.Name}。";
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(Guid id)
    {
        Customer = await db.Customers.AsNoTracking().Include(x => x.MemberCard).Include(x => x.Pets).SingleOrDefaultAsync(x => x.Id == id);
        if (Customer?.MemberCard is null) return false;
        var ledger = await db.LedgerTransactions.AsNoTracking().Where(x => x.MemberCardId == Customer.MemberCard.Id).ToListAsync();
        Ledger = ledger.OrderByDescending(x => x.CreatedAt).Take(100)
            .Select(x => new LedgerRow(x.CreatedAt.ToString("yyyy-MM-dd HH:mm"), KindName(x.Kind), x.Bucket == LedgerBucket.Principal ? "本金" : "赠送金", x.Amount, x.BalanceAfter, x.Note)).ToList();
        return true;
    }
    public string SexName(PetSex sex) => sex switch { PetSex.Male => "公", PetSex.Female => "母", _ => "未知" };
    private static string KindName(LedgerKind value) => value switch { LedgerKind.RechargePrincipal or LedgerKind.RechargeBonus => "充值", LedgerKind.SpendPrincipal or LedgerKind.SpendBonus => "消费", LedgerKind.SpendRefundPrincipal or LedgerKind.SpendRefundBonus => "消费退款", LedgerKind.RechargeRefundPrincipal or LedgerKind.RechargeBonusReversal => "充值撤销", _ => "期初" };
    public sealed record LedgerRow(string Time, string Kind, string Bucket, decimal Amount, decimal BalanceAfter, string Note);
    public sealed class PetInputModel
    {
        [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
        [Required, StringLength(30)] public string Species { get; set; } = "狗";
        [StringLength(60)] public string Breed { get; set; } = string.Empty;
        [Range(0.01, 500)] public decimal? WeightKg { get; set; }
        [StringLength(1000)] public string HealthNotes { get; set; } = string.Empty;
    }
}
