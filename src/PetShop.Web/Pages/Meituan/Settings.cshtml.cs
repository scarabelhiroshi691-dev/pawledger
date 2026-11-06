using Microsoft.AspNetCore.Mvc.RazorPages;
using PetShop.Web.Integrations.Meituan;

namespace PetShop.Web.Pages.Meituan;

public sealed class SettingsModel(IMeituanVoucherGateway gateway) : PageModel
{
    public string ModeName => gateway.ModeName;
}
