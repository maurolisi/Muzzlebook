using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;
using System.Data;
using System.Security.Claims;

namespace Muzzlebook.Pages.Musi
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDbConnection _db;
        public IndexModel(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Dog> DogsList { get; set; } = Enumerable.Empty<Dog>();

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var sql = "SELECT * FROM Dogs WHERE OwnerId = @OwnerId ORDER BY CreatedAt DESC;";
                DogsList = await _db.QueryAsync<Dog>(sql, new { OwnerId = userId });
            }

        }
    }
}
