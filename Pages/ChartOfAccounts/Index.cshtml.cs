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
    public class AccountTreeModel : PageModel
    {
        private readonly string _connectionString;

        private readonly PermissionService _permissionService;
        public AccountTreeModel(IConfiguration configuration, PermissionService permissionService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
             _permissionService = permissionService;
        }

        public List<Account> Accounts { get; set; } = new List<Account>();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Chart of account"))
            {
                return RedirectToPage("/Users/Login");
            }

            try
            {
                await LoadAccountsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading accounts: {ex.Message}";
                return Page();
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
                    AccountType = reader.IsDBNull("AccountType") ? null! : reader.GetString("AccountType")
                });
            }
            // Console.WriteLine($"Loaded: AccountId={Accounts[2].AccountId}, Name={Accounts[2].AccountName}, ParentId={Accounts[2].ParentAccountId}");
        }

        public class Account
        {
            public int AccountId { get; set; }
            public string AccountName { get; set; } = string.Empty;
            public int? ParentAccountId { get; set; }
            public string AccountType { get; set; } = string.Empty;
        }

        public class PartialModel
        {
            public int ParentId { get; set; }
            public List<Account> Accounts { get; set; } = new List<Account>();
        }
    }
}