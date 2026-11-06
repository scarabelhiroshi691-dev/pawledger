using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Mvc.Rendering; using Microsoft.AspNetCore.Mvc.RazorPages; using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
namespace PetShop.Web.Pages.Grooming;
public sealed class CreateModel(ApplicationDbContext db,UserManager<IdentityUser> users,TimeProvider clock):PageModel
{
 [BindProperty] public InputModel Input{get;set;}=new(){ScheduledAt=DateTime.Now.AddHours(1)}; public List<SelectListItem> Pets{get;private set;}=[]; public List<SelectListItem> Services{get;private set;}=[];
 public async Task OnGetAsync()=>await LoadAsync();
 public async Task<IActionResult> OnPostAsync(){if(!ModelState.IsValid){await LoadAsync();return Page();} var pet=await db.Pets.Include(x=>x.Customer).SingleOrDefaultAsync(x=>x.Id==Input.PetId); var service=await db.ServiceItems.SingleOrDefaultAsync(x=>x.Id==Input.ServiceId&&x.IsActive); if(pet is null||service is null){ModelState.AddModelError("", "宠物或服务项目不存在。");await LoadAsync();return Page();} var user=await users.GetUserAsync(User);var now=clock.GetLocalNow();var order=new GroomingOrder{OrderNumber=$"MR{now:yyyyMMddHHmmssfff}",CustomerId=pet.CustomerId,PetId=pet.Id,ServiceItemId=service.Id,ScheduledAt=new DateTimeOffset(Input.ScheduledAt),Status=GroomingStatus.Scheduled,ListPrice=Input.Price,DiscountAmount=0,AmountDue=Input.Price,Notes=Input.Notes?.Trim()??string.Empty,CreatedByUserId=user!.Id,CreatedAt=now};db.Add(order);await db.SaveChangesAsync();TempData["Success"]="预约已创建。";return RedirectToPage("Details",new{id=order.Id});}
 private async Task LoadAsync(){var pets=await db.Pets.AsNoTracking().Include(x=>x.Customer).OrderBy(x=>x.Customer.Name).ToListAsync();Pets=pets.Select(x=>new SelectListItem($"{x.Customer.Name} · {x.Name}（{x.Species}）",x.Id.ToString())).ToList();var services=await db.ServiceItems.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync();Services=services.Select(x=>new SelectListItem($"{x.Name} · ¥{x.StandardPrice:F2}",x.Id.ToString())).ToList();}
 public sealed class InputModel{[Required]public Guid PetId{get;set;}[Required]public Guid ServiceId{get;set;}[Required]public DateTime ScheduledAt{get;set;}[Range(0.01,999999)]public decimal Price{get;set;}[StringLength(1000)]public string Notes{get;set;}="";}
}
