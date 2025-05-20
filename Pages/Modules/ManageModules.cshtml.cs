using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MiniAccountSystem.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MiniAccountSystem.Pages.Modules
{
    public class ManageModulesModel : PageModel
    {
        private readonly string _connectionString;
        private readonly PermissionService _permissionService;

        public ManageModulesModel(IConfiguration configuration, PermissionService permissionService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _permissionService = permissionService;
        }

        [BindProperty]
        public int? SelectedModuleId { get; set; }

        [BindProperty]
        public Module? SelectedModule { get; set; }

        public List<SelectListItem> ModuleList { get; set; } = new();

        public string? Message { get; set; }

        public async Task<IActionResult> OnGetAsync(int? moduleId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Manage Module"))
            {
                return RedirectToPage("/Users/Login");
            }

            await LoadModulesAsync();
            SelectedModuleId = moduleId;

            if (moduleId.HasValue)
            {
                SelectedModule = await GetModuleAsync(moduleId.Value);
                if (SelectedModule == null)
                {
                    Message = "Module not found.";
                    SelectedModuleId = null;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Manage Module"))
            {
                return RedirectToPage("/Users/Login");
            }
            if (!ModelState.IsValid || SelectedModule == null || !SelectedModuleId.HasValue)
            {
                Message = "Please provide a valid module name.";
                await LoadModulesAsync();
                return Page();
            }

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("UPDATE Modules SET ModuleName = @ModuleName WHERE ModuleId = @ModuleId", conn);
                cmd.Parameters.AddWithValue("@ModuleName", SelectedModule.ModuleName);
                cmd.Parameters.AddWithValue("@ModuleId", SelectedModuleId.Value);
                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    Message = "Module updated successfully!";
                }
                else
                {
                    Message = "Module not found.";
                }
            }
            catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
            {
                Message = "A module with this name already exists.";
            }
            catch (Exception ex)
            {
                Message = $"Error updating module: {ex.Message}";
            }

            await LoadModulesAsync();
            SelectedModule = await GetModuleAsync(SelectedModuleId.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Manage Module"))
            {
                return RedirectToPage("/Users/Login");
            }
            if (!SelectedModuleId.HasValue)
            {
                Message = "No module selected.";
                await LoadModulesAsync();
                return Page();
            }

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("DELETE FROM Modules WHERE ModuleId = @ModuleId", conn);
                cmd.Parameters.AddWithValue("@ModuleId", SelectedModuleId.Value);
                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    Message = "Module deleted successfully!";
                    SelectedModuleId = null;
                    SelectedModule = null;
                }
                else
                {
                    Message = "Module not found.";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting module: {ex.Message}";
            }

            await LoadModulesAsync();
            return Page();
        }

        private async Task LoadModulesAsync()
        {
            ModuleList.Clear();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT ModuleId, ModuleName FROM Modules ORDER BY ModuleName", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ModuleList.Add(new SelectListItem
                {
                    Value = reader.GetInt32(0).ToString(),
                    Text = reader.GetString(1)
                });
            }
        }

        private async Task<Module?> GetModuleAsync(int moduleId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT ModuleId, ModuleName FROM Modules WHERE ModuleId = @ModuleId", conn);
            cmd.Parameters.AddWithValue("@ModuleId", moduleId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Module
                {
                    ModuleId = reader.GetInt32(0),
                    ModuleName = reader.GetString(1)
                };
            }
            return null;
        }

        public class Module
        {
            public int ModuleId { get; set; }

            [Required]
            [StringLength(100)]
            public string ModuleName { get; set; } = string.Empty;
        }
    }
}