using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PetShop.Web.Pages;

public sealed class ManageModel : PageModel
{
    public IReadOnlyList<string> MobileUrls { get; private set; } = [];
    public void OnGet()
    {
        try
        {
            MobileUrls = Dns.GetHostAddresses(Dns.GetHostName())
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(x))
                .Select(x => $"http://{x}:5088").Distinct().ToList();
        }
        catch (SocketException) { MobileUrls = []; }
    }
}
