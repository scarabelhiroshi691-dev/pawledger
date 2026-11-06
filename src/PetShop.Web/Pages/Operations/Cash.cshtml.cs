using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
using PetShop.Web.Services;

namespace PetShop.Web.Pages.Operations;

public sealed class CashModel(ApplicationDbContext db, CashShiftService shifts, TimeProvider clock) : PageModel
{
    public CashShift? OpenShift { get; private set; }
    public CashPosition? Position { get; private set; }
    public List<CashShift> RecentShifts { get; private set; } = [];
    public List<StoreExpense> TodayExpenses { get; private set; } = [];
    public decimal TodayExpenseTotal => TodayExpenses.Sum(x => x.Amount);

    [BindProperty] public OpenInput Open { get; set; } = new();
    [BindProperty] public ExpenseInput Expense { get; set; } = new();
    [BindProperty] public CloseInput Close { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostOpenAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        TryValidateModel(Open, nameof(Open));
        if (!ModelState.IsValid) { await LoadAsync(cancellationToken); return Page(); }
        try
        {
            await shifts.OpenAsync(Open.OpeningCash, Open.Note, UserId(), cancellationToken);
            TempData["Success"] = "开班成功，现金收支将从现在开始计入本班次。";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExpenseAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        TryValidateModel(Expense, nameof(Expense));
        if (!ModelState.IsValid) { await LoadAsync(cancellationToken); return Page(); }
        try
        {
            await shifts.AddExpenseAsync(Expense.Category, Expense.Amount, Expense.PaymentMethod, Expense.Payee, Expense.Note, UserId(), cancellationToken: cancellationToken);
            TempData["Success"] = $"已登记支出 ¥{Expense.Amount:F2}。";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCloseAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        TryValidateModel(Close, nameof(Close));
        if (!ModelState.IsValid) { await LoadAsync(cancellationToken); return Page(); }
        try
        {
            var closed = await shifts.CloseAsync(Close.ShiftId, Close.CountedCash, Close.Note, UserId(), cancellationToken);
            TempData["Success"] = closed.Difference == 0
                ? "关班完成，实点现金与系统一致。"
                : $"关班完成，现金差额 ¥{closed.Difference:F2}，请店长复核。";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        OpenShift = await shifts.GetOpenShiftAsync(cancellationToken);
        if (OpenShift is not null) Position = await shifts.CalculatePositionAsync(OpenShift, cancellationToken: cancellationToken);
        var recent = await db.CashShifts.AsNoTracking().ToListAsync(cancellationToken);
        RecentShifts = recent.OrderByDescending(x => x.OpenedAt).Take(20).ToList();
        var now = clock.GetLocalNow();
        var start = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        var expenses = await db.StoreExpenses.AsNoTracking().ToListAsync(cancellationToken);
        TodayExpenses = expenses.Where(x => x.OccurredAt >= start && x.OccurredAt < start.AddDays(1)).OrderByDescending(x => x.OccurredAt).ToList();
        if (OpenShift is not null) Close.ShiftId = OpenShift.Id;
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public static string CategoryText(ExpenseCategory value) => CashShiftService.CategoryText(value);
    public static string PaymentText(PaymentMethod value) => CashShiftService.PaymentText(value);

    public sealed class OpenInput
    {
        [Range(0, 999999, ErrorMessage = "备用金不能小于零。")]
        public decimal OpeningCash { get; set; }
        [StringLength(300)] public string Note { get; set; } = string.Empty;
    }

    public sealed class ExpenseInput
    {
        [Required] public ExpenseCategory Category { get; set; } = ExpenseCategory.Supplies;
        [Range(0.01, 999999, ErrorMessage = "支出金额必须大于零。")]
        public decimal Amount { get; set; }
        [Required] public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        [StringLength(80)] public string Payee { get; set; } = string.Empty;
        [Required(ErrorMessage = "请填写支出用途。"), StringLength(300)] public string Note { get; set; } = string.Empty;
    }

    public sealed class CloseInput
    {
        [Required] public Guid ShiftId { get; set; }
        [Range(0, 999999, ErrorMessage = "实点现金不能小于零。")]
        public decimal CountedCash { get; set; }
        [StringLength(300)] public string Note { get; set; } = string.Empty;
    }
}
