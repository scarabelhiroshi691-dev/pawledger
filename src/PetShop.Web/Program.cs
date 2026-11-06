using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Infrastructure;
using PetShop.Web.Integrations.Meituan;
using PetShop.Web.Services;

if (args.Length == 2 && args[0].Equals("--restore", StringComparison.OrdinalIgnoreCase))
{
    var paths = AppPaths.Create();
    var backup = Path.GetFullPath(args[1]);
    if (!File.Exists(backup)) { Console.Error.WriteLine("备份文件不存在。"); Environment.ExitCode = 2; return; }
    try
    {
        await using (var check = new SqliteConnection($"Data Source={backup};Mode=ReadOnly"))
        { await check.OpenAsync(); await using var command = check.CreateCommand(); command.CommandText="PRAGMA integrity_check;"; if ((string?)await command.ExecuteScalarAsync() != "ok") throw new InvalidOperationException("备份数据库完整性检查失败。"); }
        Directory.CreateDirectory(Path.GetDirectoryName(paths.DatabaseFile)!);
        if (File.Exists(paths.DatabaseFile)) File.Copy(paths.DatabaseFile, Path.Combine(paths.BackupDirectory,$"pre-restore-{DateTime.Now:yyyyMMdd-HHmmss}.db"), false);
        var staging = paths.DatabaseFile + ".restore"; File.Copy(backup, staging, true); File.Move(staging, paths.DatabaseFile, true);
        Console.WriteLine("恢复完成。原数据库已自动保存在 backups 目录。");
    }
    catch (Exception ex) { Console.Error.WriteLine($"恢复失败：{ex.Message}"); Environment.ExitCode = 3; }
    return;
}

var builder = WebApplication.CreateBuilder(args);
var appPaths = AppPaths.Create();
builder.Services.AddSingleton(appPaths);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(appPaths.DataProtectionKeysDirectory));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={appPaths.DatabaseFile};Cache=Shared;Foreign Keys=True"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(10);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // The packaged app intentionally supports trusted-LAN HTTP. HTTPS
    // deployments still receive Secure cookies automatically.
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Setup");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AuthorizePage("/Staff/Index", "AdminOnly");
    options.Conventions.AuthorizePage("/Staff/Accounts", "AdminOnly");
    options.Conventions.AuthorizePage("/Reports/Transactions", "AdminOnly");
    options.Conventions.AuthorizePage("/Reports/Reconciliation", "AdminOnly");
    options.Conventions.AuthorizePage("/System/Backup", "AdminOnly");
    options.Conventions.AuthorizePage("/System/Audit", "AdminOnly");
    options.Conventions.AuthorizePage("/GroupBuys/Reconcile", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Meituan", "AdminOnly");
}).AddMvcOptions(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddScoped<StoredValueService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<MeituanVoucherService>();
builder.Services.AddScoped<CashShiftService>();
builder.Services.AddScoped<GroupBuyService>();
builder.Services.AddSingleton<IMeituanVoucherGateway, MockMeituanVoucherGateway>();
builder.Services.AddSingleton<BackupService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await db.Database.ExecuteSqlRawAsync("PRAGMA synchronous=FULL;");

    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    foreach (var roleName in new[] { "Admin", "Clerk" })
        if (!await roles.RoleExistsAsync(roleName)) await roles.CreateAsync(new IdentityRole(roleName));

    var existingUsers = await users.Users.OrderBy(x => x.Id).ToListAsync();
    if (existingUsers.Count > 0 && !(await users.GetUsersInRoleAsync("Admin")).Any())
        await users.AddToRoleAsync(existingUsers[0], "Admin");

    if (existingUsers.Count == 1)
    {
        // SQLite cannot translate DateTimeOffset ordering. This only runs during
        // first-account bootstrap, so ordering the small staff list in memory is safe.
        var firstStaff = (await db.StaffMembers.ToListAsync()).OrderBy(x => x.CreatedAt).FirstOrDefault();
        if (firstStaff is not null && string.IsNullOrWhiteSpace(firstStaff.IdentityUserId))
        {
            firstStaff.IdentityUserId = existingUsers[0].Id;
            firstStaff.LoginEnabled = true;
            await db.SaveChangesAsync();
        }
    }
}

if (app.Environment.IsDevelopment()) app.UseMigrationsEndPoint();
else app.UseExceptionHandler("/Error");

// The packaged app serves directly from wwwroot. Keeping this middleware also
// makes Production-mode diagnostics from the project behave like the package.
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/Setup") &&
        context.Connection.RemoteIpAddress is { } remoteAddress &&
        !System.Net.IPAddress.IsLoopback(remoteAddress))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("首次设置只能在店内电脑上完成。请在电脑上打开 http://127.0.0.1:5088。", context.RequestAborted);
        return;
    }

    var bypass = path.StartsWithSegments("/Setup") || path.StartsWithSegments("/Login") ||
                 path.StartsWithSegments("/css") || path.StartsWithSegments("/js") ||
                 path.StartsWithSegments("/lib") || path.StartsWithSegments("/health") ||
                 path.StartsWithSegments("/manifest.webmanifest") || path.StartsWithSegments("/service-worker.js") ||
                 path.StartsWithSegments("/icons") ||
                 path.StartsWithSegments("/favicon.ico");
    if (!bypass)
    {
        var users = context.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        if (!await users.Users.AnyAsync())
        {
            context.Response.Redirect("/Setup");
            return;
        }
    }
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapRazorPages().WithStaticAssets();
app.Run();

public partial class Program;
