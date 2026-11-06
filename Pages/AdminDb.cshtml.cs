using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace Muzzlebook.Pages
{
    [Authorize] // Attenzione: Assicurati di essere loggato per usarla
    public class AdminDbModel : PageModel
    {
        private readonly IDbConnection _db;

        public AdminDbModel(IDbConnection db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string? SelectedTable { get; set; }

        public List<string> Tables { get; set; } = new();
        public List<ColumnInfo> Columns { get; set; } = new();
        public List<dynamic> Rows { get; set; } = new();

        // Classe per mappare l'output di "PRAGMA table_info()"
        public class ColumnInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public int Pk { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Legge tutte le tabelle create nel database SQLite
            Tables = (await _db.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;")).ToList();

            if (!string.IsNullOrEmpty(SelectedTable) && Tables.Contains(SelectedTable))
            {
                // 2. Legge dinamicamente lo schema della tabella selezionata
                Columns = (await _db.QueryAsync<ColumnInfo>($"PRAGMA table_info({SelectedTable});")).ToList();

                // Cerca la chiave primaria per ordinare gli ultimi inserimenti
                string pkCol = Columns.FirstOrDefault(c => c.Pk == 1)?.Name ?? "";
                string orderClause = !string.IsNullOrEmpty(pkCol) ? $"ORDER BY {pkCol} DESC" : "";

                // 3. Recupera gli ultimi 100 record
                Rows = (await _db.QueryAsync($"SELECT * FROM {SelectedTable} {orderClause} LIMIT 100;")).ToList();
            }

            return Page();
        }

        // Handler per l'Eliminazione
        public async Task<IActionResult> OnPostDeleteAsync(string tableName, string pkName, string pkValue)
        {
            var validTables = await _db.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type='table';");
            if (!validTables.Contains(tableName) || string.IsNullOrEmpty(pkValue)) return BadRequest();

            var sql = $"DELETE FROM {tableName} WHERE {pkName} = @PkValue";
            await _db.ExecuteAsync(sql, new { PkValue = pkValue });

            return RedirectToPage(new { SelectedTable = tableName });
        }

        // Handler Universale per Inserimento e Modifica (Upsert)
        public async Task<IActionResult> OnPostSaveRecordAsync(string tableName, IFormCollection form)
        {
            var validTables = (await _db.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type='table'")).ToList();
            if (!validTables.Contains(tableName)) return BadRequest();

            var cols = await _db.QueryAsync<ColumnInfo>($"PRAGMA table_info({tableName})");
            string pkName = cols.FirstOrDefault(c => c.Pk == 1)?.Name ?? "Id";

            // Se la Primary Key è compilata nel form, è un Update, altrimenti un Insert
            bool isUpdate = form.ContainsKey(pkName) && !string.IsNullOrWhiteSpace(form[pkName]);

            var parameters = new DynamicParameters();
            var insertCols = new List<string>();
            var insertVals = new List<string>();
            var updateSets = new List<string>();

            foreach (var col in cols)
            {
                string cName = col.Name;
                if (cName == pkName && !isUpdate) continue; // Salta la PK in fase di inserimento (è AUTOINCREMENT)

                if (form.ContainsKey(cName))
                {
                    string? val = form[cName];
                    parameters.Add(cName, string.IsNullOrWhiteSpace(val) ? null : val);

                    if (cName != pkName)
                    {
                        insertCols.Add(cName);
                        insertVals.Add("@" + cName);
                        updateSets.Add($"{cName} = @{cName}");
                    }
                    else if (isUpdate)
                    {
                        parameters.Add(cName, val); // Serve solo per la condizione WHERE dell'Update
                    }
                }
            }

            // Esecuzione dinamica
            if (isUpdate)
            {
                var sql = $"UPDATE {tableName} SET {string.Join(", ", updateSets)} WHERE {pkName} = @{pkName}";
                await _db.ExecuteAsync(sql, parameters);
            }
            else
            {
                var sql = $"INSERT INTO {tableName} ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertVals)})";
                await _db.ExecuteAsync(sql, parameters);
            }

            return RedirectToPage(new { SelectedTable = tableName });
        }
    }
}