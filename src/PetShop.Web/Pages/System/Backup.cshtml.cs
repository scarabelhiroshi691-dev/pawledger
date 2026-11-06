using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.RazorPages;using PetShop.Web.Infrastructure;using PetShop.Web.Services;
namespace PetShop.Web.Pages.System;
public sealed class BackupModel(BackupService backup,AppPaths paths):PageModel
{
 public IReadOnlyList<FileInfo> Files{get;private set;}=[];public string DataDirectory=>paths.Root;public void OnGet()=>Files=backup.List();
 public async Task<IActionResult> OnPostAsync(){try{var file=await backup.CreateAsync();TempData["Success"]=$"备份完成：{Path.GetFileName(file)}";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToPage();}
}
