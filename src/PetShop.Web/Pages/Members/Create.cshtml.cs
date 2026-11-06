using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages.Members;

public sealed class CreateModel(ApplicationDbContext db, UserManager<IdentityUser> users, TimeProvider timeProvider) : PageModel
{
    [BindProperty] public MemberInput Input { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrWhiteSpace(Input.PetName) && string.IsNullOrWhiteSpace(Input.Species)) ModelState.AddModelError("Input.Species", "录入宠物时请选择种类。");
        if (!ModelState.IsValid) return Page();
        var duplicate = await db.Customers.AnyAsync(x => x.Phone == Input.Phone.Trim());
        if (duplicate) { ModelState.AddModelError("Input.Phone", "这个手机号已存在，请从会员列表打开原档案。"); return Page(); }

        var now = timeProvider.GetLocalNow();
        var customer = new Customer { Name = Input.CustomerName.Trim(), Phone = Input.Phone.Trim(), Notes = Input.CustomerNotes?.Trim() ?? string.Empty, CreatedAt = now };
        customer.MemberCard = new MemberCard { Customer = customer, CardNumber = $"M{now:yyyyMMddHHmmss}{Random.Shared.Next(10,99)}", CreatedAt = now };
        if (!string.IsNullOrWhiteSpace(Input.PetName)) customer.Pets.Add(new Pet { Name = Input.PetName.Trim(), Species = Input.Species.Trim(), Breed = Input.Breed?.Trim() ?? string.Empty, Sex = Input.Sex, WeightKg = Input.WeightKg, VaccineDueDate = Input.VaccineDueDate, HealthNotes = Input.HealthNotes?.Trim() ?? string.Empty, CreatedAt = now });
        db.Customers.Add(customer);
        var user = await users.GetUserAsync(User);
        db.AuditLogs.Add(new AuditLog { Action = "新建会员", EntityType = nameof(Customer), EntityId = customer.Id.ToString(), OperatorUserId = user!.Id, Detail = $"{customer.Name} {customer.Phone}", CreatedAt = now });
        await db.SaveChangesAsync();
        TempData["Success"] = $"已建立会员 {customer.Name}。";
        return RedirectToPage("Details", new { id = customer.Id });
    }

    public sealed class MemberInput
    {
        [Required(ErrorMessage = "请输入宠主姓名。"), StringLength(50)] public string CustomerName { get; set; } = string.Empty;
        [Required(ErrorMessage = "请输入手机号。"), StringLength(30, MinimumLength = 5)] public string Phone { get; set; } = string.Empty;
        [StringLength(500)] public string CustomerNotes { get; set; } = string.Empty;
        [StringLength(50)] public string PetName { get; set; } = string.Empty;
        [StringLength(30)] public string Species { get; set; } = string.Empty;
        [StringLength(60)] public string Breed { get; set; } = string.Empty;
        public PetSex Sex { get; set; }
        [Range(0.01, 500)] public decimal? WeightKg { get; set; }
        public DateOnly? VaccineDueDate { get; set; }
        [StringLength(1000)] public string HealthNotes { get; set; } = string.Empty;
    }
}
