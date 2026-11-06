using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;
namespace PetShop.Web.Pages.Grooming;
public sealed class IndexModel(ApplicationDbContext db) : PageModel
{
    public List<GroomingOrder> Orders {get;private set;}=[];
    public async Task OnGetAsync()=>Orders=(await db.GroomingOrders.AsNoTracking().Include(x=>x.Customer).Include(x=>x.Pet).Include(x=>x.ServiceItem).Include(x=>x.ActualGroomer).ToListAsync()).OrderByDescending(x=>x.ScheduledAt).Take(200).ToList();
    public static string StatusName(GroomingStatus x)=>x switch{GroomingStatus.Scheduled=>"已预约",GroomingStatus.Arrived=>"已到店",GroomingStatus.InService=>"服务中",GroomingStatus.AwaitingPayment=>"待结算",GroomingStatus.Completed=>"已完成",GroomingStatus.Cancelled=>"已取消",_=>"爽约"};
}
