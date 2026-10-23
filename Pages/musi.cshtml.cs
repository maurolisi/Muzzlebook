using Microsoft.AspNetCore.Authorization; // 1. Aggiungi il namespace
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Muzzlebook.Pages
{
    [Authorize] // 2. Applica l'attributo alla classe
    public class musiModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}