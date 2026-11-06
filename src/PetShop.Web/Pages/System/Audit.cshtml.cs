using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Pages.System;

public sealed class AuditModel(ApplicationDbContext db, UserManager<IdentityUser> userManager) : PageModel
{
    public List<AuditRow> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var users = await userManager.Users.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.UserName ?? "未知账号");
        var logs = await db.AuditLogs.AsNoTracking().ToListAsync();
        Rows = logs.OrderByDescending(x => x.CreatedAt).Take(500)
            .Select(x => new AuditRow(x.CreatedAt, x.Action, x.EntityType, x.EntityId,
                users.GetValueOrDefault(x.OperatorUserId, "已删除账号"), x.Detail))
            .ToList();
    }

    public sealed record AuditRow(DateTimeOffset CreatedAt, string Action, string EntityType,
        string EntityId, string Operator, string Detail);
}
