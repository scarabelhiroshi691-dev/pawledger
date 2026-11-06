using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages;

public sealed class LoginModel(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users, ApplicationDbContext db) : PageModel
{
    [BindProperty] public LoginInput Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await users.Users.AnyAsync()) return RedirectToPage("/Setup");
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await users.FindByNameAsync(Input.UserName.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "账号或密码不正确。");
            return Page();
        }

        var result = await signIn.CheckPasswordSignInAsync(user, Input.Password, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var staff = await db.StaffMembers.AsNoTracking().SingleOrDefaultAsync(x => x.IdentityUserId == user.Id);
            if (staff is not null && (!staff.IsActive || !staff.LoginEnabled))
            {
                ModelState.AddModelError(string.Empty, "该员工账号已停用，请联系店长。");
                return Page();
            }
            await signIn.SignInAsync(user, Input.RememberMe);
            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Page("/Index")!);
        }
        ModelState.AddModelError(string.Empty, result.IsLockedOut ? "登录失败次数过多，请 10 分钟后重试。" : "账号或密码不正确。");
        return Page();
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "请输入登录账号。"), StringLength(40)] public string UserName { get; set; } = string.Empty;
        [Required(ErrorMessage = "请输入密码。"), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
