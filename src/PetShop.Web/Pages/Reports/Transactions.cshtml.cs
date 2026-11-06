using Microsoft.AspNetCore.Identity;using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using Microsoft.EntityFrameworkCore;using PetShop.Web.Data;using PetShop.Web.Services;
namespace PetShop.Web.Pages.Reports;
public sealed class TransactionsModel(ApplicationDbContext db,StoredValueService stored,UserManager<IdentityUser> users):PageModel
{
 public List<PaymentRecord> Payments{get;private set;}=[];public async Task OnGetAsync()=>await LoadAsync();
 public async Task<IActionResult> OnPostRefundAsync(Guid paymentId,decimal amount,string reason,string requestId){var user=await users.GetUserAsync(User);try{var r=await stored.RefundPaymentAsync(paymentId,amount,requestId,user!.Id,reason);TempData["Success"]=$"退款完成：退回本金 ¥{r.PrincipalReturned:F2}，赠送金 ¥{r.BonusReturned:F2}，外部 ¥{r.ExternalReturned:F2}。";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToPage();}
 private async Task LoadAsync()=>Payments=(await db.Payments.AsNoTracking().Include(x=>x.MemberCard).ThenInclude(x=>x!.Customer).ToListAsync()).OrderByDescending(x=>x.CreatedAt).Take(300).ToList();
 public static string Doc(DocumentType x)=>x==DocumentType.Grooming?"美容":"零售";
}
