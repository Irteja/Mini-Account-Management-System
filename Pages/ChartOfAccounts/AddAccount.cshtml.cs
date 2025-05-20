using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using MiniAccountSystem.Services;

public class CreateChartOfAccountsModel : PageModel
{
    private readonly string _connectionString;
    private readonly PermissionService _permissionService;

    public CreateChartOfAccountsModel(IConfiguration configuration, PermissionService permissionService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _permissionService = permissionService;
    }

    [BindProperty]
    public Account Account { get; set; } = new Account();

    public List<Account> Accounts { get; set; } = new List<Account>();

    public string ErrorMessage { get; set; } 
    public string SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Add Account"))
        {
            return RedirectToPage("/Users/Login");
        }

        await LoadAccountsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Add Account"))
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
            command.Parameters.AddWithValue("@Action", "Create");
            command.Parameters.AddWithValue("@AccountName", Account.AccountName);
            command.Parameters.AddWithValue("@ParentAccountId", (object)Account.ParentAccountId ?? DBNull.Value);
            command.Parameters.AddWithValue("@AccountType", (object)Account.AccountType ?? DBNull.Value);
            Console.WriteLine("came this far");

            await command.ExecuteNonQueryAsync();
            SuccessMessage = "Account created successfully.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating account: {ex.Message}";
            Console.WriteLine(ErrorMessage);
            await LoadAccountsAsync();
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

        // using var command = new SqlCommand("SELECT AccountId, AccountName, ParentAccountId, AccountType FROM Accounts", connection);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Accounts.Add(new Account
            {
                AccountId = reader.GetInt32("AccountId"),
                AccountName = reader.GetString("AccountName"),
                ParentAccountId = reader.IsDBNull("ParentAccountId") ? null : reader.GetInt32("ParentAccountId"),
                AccountType = reader.GetString("AccountType")
            });
        }
    }
}

public class Account
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int? ParentAccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
}


