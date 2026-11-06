using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages.Members;

public sealed class LedgerModel(ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Query { get; set; } = string.Empty;
    public IReadOnlyList<Row> Rows { get; private set; } = [];
    public async Task OnGetAsync()
    {
        var query = db.LedgerTransactions.AsNoTracking().Include(x => x.MemberCard).ThenInclude(x => x.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Query)) { var term = Query.Trim(); query = query.Where(x => x.MemberCard.Customer.Name.Contains(term) || x.MemberCard.Customer.Phone.Contains(term) || x.MemberCard.CardNumber.Contains(term)); }
        var data = await query.ToListAsync();
        Rows = data.OrderByDescending(x => x.CreatedAt).Take(500).Select(x => new Row(x.CreatedAt.ToString("yyyy-MM-dd HH:mm"), x.MemberCard.Customer.Name, x.MemberCard.CardNumber, x.Kind.ToString(), x.Bucket == LedgerBucket.Principal ? "本金" : "赠送金", x.Amount, x.BalanceAfter, x.Note)).ToList();
    }
    public sealed record Row(string Time, string Customer, string CardNumber, string Kind, string Bucket, decimal Amount, decimal BalanceAfter, string Note);
}
