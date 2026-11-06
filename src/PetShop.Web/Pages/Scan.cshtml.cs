using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages;

public sealed class ScanModel(ApplicationDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Code { get; set; } = string.Empty;
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Code = Code.Trim();
        if (string.IsNullOrWhiteSpace(Code)) return Page();

        var card = await db.MemberCards.AsNoTracking().SingleOrDefaultAsync(x => x.CardNumber == Code);
        if (card is not null) return RedirectToPage("/Members/Details", new { id = card.CustomerId });
        var customer = await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Phone == Code);
        if (customer is not null) return RedirectToPage("/Members/Details", new { id = customer.Id });
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Barcode == Code);
        if (product is not null) return RedirectToPage("/Products/Sale", new { productId = product.Id });
        ErrorMessage = "没有识别到会员或商品，请检查编码后重试。";
        return Page();
    }
}
