using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;

namespace Muzzlebook.Pages;

public class IndexModel : PageModel
{




    public async Task OnGetAsync()
    {

    }


    // Handler per il Logout
    public async Task<IActionResult> OnGetLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }
}