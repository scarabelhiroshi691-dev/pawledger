using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages.Members;

public sealed class IndexModel(ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Query { get; set; } = string.Empty;
    public CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("zh-CN");
    public IReadOnlyList<MemberRow> Members { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var query = db.Customers.AsNoTracking().Include(x => x.MemberCard).Include(x => x.Pets).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Query))
        {
            var term = Query.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Phone.Contains(term) || x.Pets.Any(p => p.Name.Contains(term)));
        }
        var data = await query.ToListAsync();
        Members = data.OrderByDescending(x => x.CreatedAt).Take(200).Select(x => new MemberRow(x.Id, x.Name, x.Phone,
            string.Join("、", x.Pets.Select(p => p.Name)), x.MemberCard == null ? "未开卡" : x.MemberCard.CardNumber,
            x.MemberCard == null ? 0 : x.MemberCard.PrincipalBalance, x.MemberCard == null ? 0 : x.MemberCard.BonusBalance)).ToList();
    }
    public sealed record MemberRow(Guid Id, string Name, string Phone, string Pets, string CardNumber, decimal Principal, decimal Bonus) { public decimal Total => Principal + Bonus; }
}
