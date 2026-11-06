using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages.Staff;

public sealed class AccountsModel(
    UserManager<IdentityUser> users,
    RoleManager<IdentityRole> roles,
    ApplicationDbContext db,
    TimeProvider clock) : PageModel
{
    public List<AccountRow> Accounts { get; private set; } = [];
    [BindProperty] public CreateAccountInput NewAccount { get; set; } = new();

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        ModelState.Clear();
        TryValidateModel(NewAccount, nameof(NewAccount));
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var role = NewAccount.Role == "Admin" ? "Admin" : "Clerk";
        if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
        var identity = new IdentityUser { UserName = NewAccount.UserName.Trim() };
        var result = await users.CreateAsync(identity, NewAccount.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, Translate(error.Description));
            await LoadAsync();
            return Page();
        }

        var roleResult = await users.AddToRoleAsync(identity, role);
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(identity);
            ModelState.AddModelError(string.Empty, "账号角色创建失败，请重试。");
            await LoadAsync();
            return Page();
        }

        var now = clock.GetLocalNow();
        var staff = new StaffMember
        {
            IdentityUserId = identity.Id,
            Name = NewAccount.DisplayName.Trim(),
            Phone = NewAccount.Phone?.Trim() ?? string.Empty,
            IsGroomer = NewAccount.IsGroomer,
            IsActive = true,
            LoginEnabled = true,
            CreatedAt = now
        };
        db.StaffMembers.Add(staff);
        db.AuditLogs.Add(new AuditLog
        {
            Action = "创建员工账号",
            EntityType = nameof(IdentityUser),
            EntityId = identity.Id,
            OperatorUserId = users.GetUserId(User) ?? string.Empty,
            Detail = $"{staff.Name}（{identity.UserName}），角色 {RoleText(role)}",
            CreatedAt = now
        });
        try
        {
            await db.SaveChangesAsync();
        }
        catch
        {
            await users.DeleteAsync(identity);
            throw;
        }

        TempData["Success"] = $"已创建 {staff.Name} 的登录账号。";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(string userId)
    {
        var identity = await users.FindByIdAsync(userId);
        var staff = await db.StaffMembers.SingleOrDefaultAsync(x => x.IdentityUserId == userId);
        if (identity is null || staff is null)
        {
            TempData["Error"] = "没有找到对应的员工账号。";
            return RedirectToPage();
        }
        var enable = !staff.LoginEnabled;
        if (!enable && users.GetUserId(User) == userId)
        {
            TempData["Error"] = "不能停用当前正在登录的账号。";
            return RedirectToPage();
        }
        if (!enable && await users.IsInRoleAsync(identity, "Admin"))
        {
            var admins = await users.GetUsersInRoleAsync("Admin");
            var otherIds = admins.Where(x => x.Id != userId).Select(x => x.Id).ToList();
            var hasOtherActiveAdmin = await db.StaffMembers.AnyAsync(x => x.IdentityUserId != null && otherIds.Contains(x.IdentityUserId) && x.IsActive && x.LoginEnabled);
            if (!hasOtherActiveAdmin)
            {
                TempData["Error"] = "至少保留一个可登录的管理员账号。";
                return RedirectToPage();
            }
        }

        staff.LoginEnabled = enable;
        var now = clock.GetLocalNow();
        db.AuditLogs.Add(new AuditLog
        {
            Action = enable ? "启用员工账号" : "停用员工账号",
            EntityType = nameof(IdentityUser),
            EntityId = userId,
            OperatorUserId = users.GetUserId(User) ?? string.Empty,
            Detail = $"{staff.Name}（{identity.UserName}）",
            CreatedAt = now
        });
        await db.SaveChangesAsync();
        TempData["Success"] = enable ? "账号已启用。" : "账号已停用。";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var identities = await users.Users.AsNoTracking().OrderBy(x => x.UserName).ToListAsync();
        var staff = await db.StaffMembers.AsNoTracking().Where(x => x.IdentityUserId != null).ToDictionaryAsync(x => x.IdentityUserId!);
        var rows = new List<AccountRow>();
        foreach (var identity in identities)
        {
            staff.TryGetValue(identity.Id, out var member);
            var roleNames = await users.GetRolesAsync(identity);
            rows.Add(new AccountRow(
                identity.Id,
                identity.UserName ?? "—",
                member?.Name ?? "未关联员工",
                roleNames.Contains("Admin") ? "管理员" : "店员",
                member?.IsGroomer == true,
                member?.IsActive != false && member?.LoginEnabled != false,
                users.GetUserId(User) == identity.Id));
        }
        Accounts = rows;
    }

    private static string RoleText(string role) => role == "Admin" ? "管理员" : "店员";
    private static string Translate(string value) => value
        .Replace("Passwords must have at least one digit ('0'-'9').", "密码必须包含数字。", StringComparison.OrdinalIgnoreCase)
        .Replace("Passwords must be at least 8 characters.", "密码至少需要 8 位。", StringComparison.OrdinalIgnoreCase)
        .Replace("is already taken.", "已被使用。", StringComparison.OrdinalIgnoreCase);

    public sealed record AccountRow(string UserId, string UserName, string DisplayName, string Role, bool IsGroomer, bool Enabled, bool IsCurrent);

    public sealed class CreateAccountInput
    {
        [Required(ErrorMessage = "请输入员工姓名。"), StringLength(50)] public string DisplayName { get; set; } = string.Empty;
        [StringLength(30)] public string Phone { get; set; } = string.Empty;
        [Required(ErrorMessage = "请输入登录账号。"), StringLength(40, MinimumLength = 2)] public string UserName { get; set; } = string.Empty;
        [Required(ErrorMessage = "请输入初始密码。"), MinLength(8, ErrorMessage = "密码至少 8 位。"), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        [Required] public string Role { get; set; } = "Clerk";
        public bool IsGroomer { get; set; }
    }
}
