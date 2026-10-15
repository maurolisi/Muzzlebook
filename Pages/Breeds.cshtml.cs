using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;

namespace Muzzlebook.Pages;

public class BreedsModel : PageModel
{
    private readonly IDbConnection _db;

    public BreedsModel(IDbConnection db)
    {
        _db = db;
    }

    public IEnumerable<Breed> BreedsList { get; set; } = Enumerable.Empty<Breed>();

    // Parametri per la ricerca e l'ordinamento legati alla Request GET
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortColumn { get; set; } = "Name";

    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; } = "ASC";

    public async Task OnGetAsync()
    {
        // Validazione colonne di ordinamento per prevenire SQL Injection
        var validColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "FciGroup", "Name", "Size", "OriginCountry", "Temperament"
        };

        if (!validColumns.Contains(SortColumn))
        {
            SortColumn = "Name";
        }

        var dir = SortOrder.Equals("DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

        // Costruzione dinamica della query SQL
        var sql = $@"
            SELECT * FROM Breeds 
            WHERE (@SearchTerm IS NULL OR @SearchTerm = '' OR 
                   Name LIKE @Pattern OR 
                   FciGroup LIKE @Pattern OR 
                   Size LIKE @Pattern OR 
                   OriginCountry LIKE @Pattern OR 
                   Temperament LIKE @Pattern)
            ORDER BY {SortColumn} {dir};";

        var pattern = $"%{SearchTerm?.Trim()}%";

        BreedsList = await _db.QueryAsync<Breed>(sql, new
        {
            SearchTerm = SearchTerm,
            Pattern = pattern
        });
    }

    // Helper method per generare l'inversione di direzione per i link della tabella
    public string GetNextSortOrder(string column)
    {
        if (SortColumn.Equals(column, StringComparison.OrdinalIgnoreCase))
        {
            return SortOrder.Equals("ASC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        }
        return "ASC";
    }

    // Helper method per visualizzare l'icona della freccia di ordinamento
    public string GetSortIcon(string column)
    {
        if (!SortColumn.Equals(column, StringComparison.OrdinalIgnoreCase))
            return "bi-arrow-down-up text-muted opacity-50";

        return SortOrder.Equals("ASC", StringComparison.OrdinalIgnoreCase)
            ? "bi-sort-alpha-down text-primary"
            : "bi-sort-alpha-down-alt text-primary";
    }
}