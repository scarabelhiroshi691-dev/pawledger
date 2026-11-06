using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages;

public sealed class SetupModel(UserManager<IdentityUser> users, RoleManager<IdentityRole> roles, SignInManager<IdentityUser> signIn, ApplicationDbContext db, TimeProvider timeProvider) : PageModel
{
    [BindProperty] public SetupInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync() => await users.Users.AnyAsync() ? RedirectToPage("/Login") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (await users.Users.AnyAsync()) return RedirectToPage("/Login");
        if (!ModelState.IsValid) return Page();

        var user = new IdentityUser { UserName = Input.UserName.Trim() };
        var result = await users.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, Translate(error.Description));
            return Page();
        }

        if (!await roles.RoleExistsAsync("Admin")) await roles.CreateAsync(new IdentityRole("Admin"));
        await users.AddToRoleAsync(user, "Admin");
        db.StaffMembers.Add(new StaffMember { IdentityUserId = user.Id, Name = Input.DisplayName.Trim(), IsActive = true, LoginEnabled = true, IsGroomer = false, CreatedAt = timeProvider.GetLocalNow() });
        db.AuditLogs.Add(new AuditLog { Action = "首次设置", EntityType = "System", EntityId = "initial", OperatorUserId = user.Id, Detail = $"创建店长账号 {user.UserName}", CreatedAt = timeProvider.GetLocalNow() });
        await db.SaveChangesAsync();
        await signIn.SignInAsync(user, isPersistent: true);
        return RedirectToPage("/Index");
    }

    private static string Translate(string value) => value.Replace("Passwords must have at least one digit ('0'-'9').", "密码必须包含数字。", StringComparison.OrdinalIgnoreCase)
        .Replace("Passwords must be at least 8 characters.", "密码至少需要 8 位。", StringComparison.OrdinalIgnoreCase)
        .Replace("is already taken.", "已被使用。", StringComparison.OrdinalIgnoreCase);

    public sealed class SetupInput
    {
        [Required(ErrorMessage = "请输入店长姓名。"), StringLength(50)] public string DisplayName { get; set; } = string.Empty;
        [Required(ErrorMessage = "请输入登录账号。"), StringLength(40, MinimumLength = 2)] public string UserName { get; set; } = string.Empty;
        [Required(ErrorMessage = "请输入密码。"), DataType(DataType.Password), MinLength(8, ErrorMessage = "密码至少 8 位。")] public string Password { get; set; } = string.Empty;
        [Required(ErrorMessage = "请再次输入密码。"), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "两次密码不一致。")] public string ConfirmPassword { get; set; } = string.Empty;
    }
}
