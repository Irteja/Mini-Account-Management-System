using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MiniAccountSystem.Pages.Modules
{
    public class AddModuleModel : PageModel
    {
        private readonly string _connectionString;

        public AddModuleModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        [BindProperty]
        public string ModuleName { get; set; } = string.Empty;

        public string? Message { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Message = "Please provide a valid module name.";
                return Page();
            }

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("INSERT INTO Modules (ModuleName) VALUES (@ModuleName)", conn);
                cmd.Parameters.AddWithValue("@ModuleName", ModuleName);
                await cmd.ExecuteNonQueryAsync();

                Message = "Module added successfully!";
                ModelState.Clear();
                ModuleName = string.Empty;
            }
            catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
            {
                Message = "A module with this name already exists.";
                return Page();
            }
            catch (Exception ex)
            {
                Message = $"Error adding module: {ex.Message}";
                return Page();
            }

            return Page();
        }
    }
}