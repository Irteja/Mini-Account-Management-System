using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using MiniAccountSystem.Services;

namespace MiniAccountSystem.Pages
{
    public class EditChartOfAccountsModel : PageModel
    {
        private readonly string _connectionString;
        private readonly PermissionService _permissionService;
        public EditChartOfAccountsModel(IConfiguration configuration, PermissionService permissionService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _permissionService = permissionService;
        }

        public List<Account> Accounts { get; set; } = new List<Account>();

        [BindProperty]
        public Account SelectedAccount { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? selectedAccountId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Edit chart account"))
            {
                return RedirectToPage("/Users/Login");
            }

            try
            {
                await LoadAccountsAsync();
                if (selectedAccountId.HasValue)
                {
                    SelectedAccount = Accounts.Find(a => a.AccountId == selectedAccountId.Value)!;
                    if (SelectedAccount == null)
                    {
                        ErrorMessage = "Account not found.";
                    }
                }
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading accounts: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Edit chart account"))
            {
                return RedirectToPage("/Users/Login");
            }

            if (!ModelState.IsValid)
            {
                await LoadAccountsAsync();
                return Page();
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand("sp_ManageChartOfAccounts", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@Action", "Update");
                command.Parameters.AddWithValue("@AccountId", SelectedAccount.AccountId);
                command.Parameters.AddWithValue("@AccountName", SelectedAccount.AccountName);
                command.Parameters.AddWithValue("@ParentAccountId", SelectedAccount.ParentAccountId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AccountType", SelectedAccount.AccountType ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
                SuccessMessage = "Account updated successfully.";
                await LoadAccountsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating account: {ex.Message}";
                await LoadAccountsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int accountId)
        {


            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand("sp_ManageChartOfAccounts", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@Action", "Delete");
                command.Parameters.AddWithValue("@AccountId", accountId);

                await command.ExecuteNonQueryAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error deleting account: {ex.Message}" });
            }
        }


        private async Task LoadAccountsAsync()
        {
            Accounts = new List<Account>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand("sp_ManageChartOfAccounts", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Action", "Select");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Accounts.Add(new Account
                {
                    AccountId = reader.GetInt32("AccountId"),
                    AccountName = reader.GetString("AccountName"),
                    ParentAccountId = reader.IsDBNull("ParentAccountId") ? null : reader.GetInt32("ParentAccountId"),
                    AccountType = reader.IsDBNull("AccountType") ? null : reader.GetString("AccountType")
                });
            }
        }

        public class Account
        {
            public int AccountId { get; set; }
            public string AccountName { get; set; } = string.Empty;
            public int? ParentAccountId { get; set; }
            public string AccountType { get; set; } = string.Empty;
        }
    }
}