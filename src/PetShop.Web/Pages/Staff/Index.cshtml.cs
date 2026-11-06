using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages.Staff;
public sealed class IndexModel(ApplicationDbContext db, UserManager<IdentityUser> users, TimeProvider clock) : PageModel
{
    public List<StaffMember> Staff { get; private set; } = [];
    public List<ServiceItem> Services { get; private set; } = [];
    [BindProperty] public StaffInput NewStaff { get; set; } = new();
    [BindProperty] public ServiceInput NewService { get; set; } = new();
    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostStaffAsync()
    {
        ModelState.Clear(); TryValidateModel(NewStaff, nameof(NewStaff));
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        var now = clock.GetLocalNow(); var user = await users.GetUserAsync(User);
        var item = new StaffMember { Name = NewStaff.Name.Trim(), Phone = NewStaff.Phone?.Trim() ?? string.Empty, IsGroomer = NewStaff.IsGroomer, CreatedAt = now };
        db.StaffMembers.Add(item); db.AuditLogs.Add(new AuditLog { Action="新增员工", EntityType=nameof(StaffMember), EntityId=item.Id.ToString(), OperatorUserId=user!.Id, Detail=item.Name, CreatedAt=now });
        await db.SaveChangesAsync(); TempData["Success"] = "员工已添加。"; return RedirectToPage();
    }
    public async Task<IActionResult> OnPostServiceAsync()
    {
        ModelState.Clear(); TryValidateModel(NewService, nameof(NewService));
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }
        var now = clock.GetLocalNow(); var user = await users.GetUserAsync(User);
        var item = new ServiceItem { Name=NewService.Name.Trim(), StandardPrice=NewService.Price, CreatedAt=now };
        db.ServiceItems.Add(item); db.AuditLogs.Add(new AuditLog { Action="新增服务", EntityType=nameof(ServiceItem), EntityId=item.Id.ToString(), OperatorUserId=user!.Id, Detail=$"{item.Name} ¥{item.StandardPrice:F2}", CreatedAt=now });
        await db.SaveChangesAsync(); TempData["Success"] = "服务项目已添加。"; return RedirectToPage();
    }
    private async Task LoadAsync() { Staff = await db.StaffMembers.AsNoTracking().OrderByDescending(x=>x.IsActive).ThenBy(x=>x.Name).ToListAsync(); Services=await db.ServiceItems.AsNoTracking().OrderByDescending(x=>x.IsActive).ThenBy(x=>x.Name).ToListAsync(); }
    public sealed class StaffInput { [Required, StringLength(50)] public string Name {get;set;}=""; [StringLength(30)] public string Phone {get;set;}=""; public bool IsGroomer {get;set;}=true; }
    public sealed class ServiceInput { [Required, StringLength(80)] public string Name {get;set;}=""; [Range(0.01,999999)] public decimal Price {get;set;} }
}
