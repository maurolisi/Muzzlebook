using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace Muzzlebook.Pages
{
    [Authorize]
    public class AdminDbModel : PageModel
    {
        private readonly IDbConnection _db;

        public AdminDbModel(IDbConnection db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string? SelectedTable { get; set; }

        [BindProperty]
        public string? CustomQuery { get; set; }

        public List<string> Tables { get; set; } = new();
        public List<ColumnInfo> Columns { get; set; } = new();
        public List<dynamic> Rows { get; set; } = new();

        // Proprietà per i risultati della query personalizzata
        public IEnumerable<dynamic> QueryResultRows { get; set; } = Enumerable.Empty<dynamic>();
        public List<string> QueryResultColumns { get; set; } = new();
        public string? QueryError { get; set; }
        public string? QuerySuccessMessage { get; set; }

        public class ColumnInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public int Pk { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Tables = (await _db.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;")).ToList();

            if (!string.IsNullOrEmpty(SelectedTable) && Tables.Contains(SelectedTable))
            {
                Columns = (await _db.QueryAsync<ColumnInfo>($"PRAGMA table_info({SelectedTable});")).ToList();
                string pkCol = Columns.FirstOrDefault(c => c.Pk == 1)?.Name ?? "";
                string orderClause = !string.IsNullOrEmpty(pkCol) ? $"ORDER BY {pkCol} DESC" : "";

                Rows = (await _db.QueryAsync($"SELECT * FROM {SelectedTable} {orderClause} LIMIT 100;")).ToList();
            }

            return Page();
        }

        // Handler per eseguire la query personalizzata inserita nella Textbox
        public async Task<IActionResult> OnPostExecuteQueryAsync()
        {
            Tables = (await _db.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;")).ToList();

            if (string.IsNullOrWhiteSpace(CustomQuery))
            {
                QueryError = "Inserisci una query SQL valida.";
                return Page();
            }

            string trimmedQuery = CustomQuery.Trim().ToUpperInvariant();

            try
            {
                if (trimmedQuery.StartsWith("SELECT"))
                {
                    // Esegue la SELECT e mappa i risultati come dizionari per leggere le chiavi dinamicamente
                    var results = (await _db.QueryAsync(CustomQuery)).ToList();
                    QueryResultRows = results;

                    if (results.Any())
                    {
                        var firstRow = (IDictionary<string, object>)results.First();
                        QueryResultColumns = firstRow.Keys.ToList();
                    }
                    QuerySuccessMessage = $"Query eseguita con successo. Restituiti {results.Count} record.";
                }
                else
                {
                    // Esegue comandi di modifica (INSERT, UPDATE, DELETE, CREATE, ecc.)
                    int affectedRows = await _db.ExecuteAsync(CustomQuery);
                    QuerySuccessMessage = "Comando eseguito con successo. Righe interessate: " + affectedRows;
                }
            }
            catch (Exception ex)
            {
                QueryError = $"Errore SQL: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string tableName, string pkName, string pkValue)
        {
            var validTables = await _db.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type='table';");
            if (!validTables.Contains(tableName) || string.IsNullOrEmpty(pkValue)) return BadRequest();

            var sql = $"DELETE FROM {tableName} WHERE {pkName} = @PkValue";
            await _db.ExecuteAsync(sql, new { PkValue = pkValue });

            return RedirectToPage(new { SelectedTable = tableName });
        }

        public async Task<IActionResult> OnPostSaveRecordAsync(string tableName, IFormCollection form)
        {
            var validTables = (await _db.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type='table'")).ToList();
            if (!validTables.Contains(tableName)) return BadRequest();

            var cols = await _db.QueryAsync<ColumnInfo>($"PRAGMA table_info({tableName})");
            string pkName = cols.FirstOrDefault(c => c.Pk == 1)?.Name ?? "Id";

            bool isUpdate = form.ContainsKey(pkName) && !string.IsNullOrWhiteSpace(form[pkName]);

            var parameters = new DynamicParameters();
            var insertCols = new List<string>();
            var insertVals = new List<string>();
            var updateSets = new List<string>();

            foreach (var col in cols)
            {
                string cName = col.Name;
                if (cName == pkName && !isUpdate) continue;

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
                        parameters.Add(cName, val);
                    }
                }
            }

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