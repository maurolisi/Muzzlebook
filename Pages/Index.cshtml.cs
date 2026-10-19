using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;

namespace Muzzlebook.Pages;

public class IndexModel : PageModel
{
    private readonly IDbConnection _db;

    public IndexModel(IDbConnection db)
    {
        _db = db;
    }






    public async Task OnGetAsync()
    {

    }

}