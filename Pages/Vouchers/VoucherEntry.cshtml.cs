using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Security.Claims;
using MiniAccountSystem.Services;

namespace MiniAccountSystem.Pages.Vouchers
{
    public class VoucherEntryModel : PageModel
    {
        private readonly string _connectionString;
        private readonly PermissionService _permissionService;

        public VoucherEntryModel(IConfiguration configuration, PermissionService permissionService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _permissionService = permissionService;
        }

        [BindProperty]
        public VoucherInput Voucher { get; set; } = new VoucherInput { Entries = new List<EntryInput> { new EntryInput() } };

        public List<Account> Accounts { get; set; } = new List<Account>();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity.IsAuthenticated! || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "VoucherEntry"))
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadAccountsAsync();
                return Page();
            }
            // Validate entries
            if (Voucher.Entries.Count < 2)
            {
                ModelState.AddModelError("", "At least two entries are required.");
                ErrorMessage = $"At least two entries are required.";
                await LoadAccountsAsync();
                return Page();
            }

            decimal totalDebit = 0, totalCredit = 0;
            foreach (var entry in Voucher.Entries)
            {
                totalDebit += entry.Debit ?? 0;
                totalCredit += entry.Credit ?? 0;
            }

            if (totalDebit != totalCredit || totalDebit == 0)
            {
                ErrorMessage = $"Debits must equal credits and cannot be zero.";
                ModelState.AddModelError("", "Debits must equal credits and cannot be zero.");
                await LoadAccountsAsync();
                return Page();
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand("sp_SaveVoucher", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                Console.WriteLine($"coming->{totalCredit}");
                // Create DataTable for entries
                var entriesTable = new DataTable();
                entriesTable.Columns.Add("AccountId", typeof(int));
                entriesTable.Columns.Add("Debit", typeof(decimal));
                entriesTable.Columns.Add("Credit", typeof(decimal));
                entriesTable.Columns.Add("Description", typeof(string));

                foreach (var entry in Voucher.Entries)
                {
                    entriesTable.Rows.Add(entry.AccountId, entry.Debit ?? 0, entry.Credit ?? 0, entry.Description);
                }

                command.Parameters.AddWithValue("@VoucherType", Voucher.VoucherType);
                command.Parameters.AddWithValue("@Date", Voucher.Date);
                command.Parameters.AddWithValue("@ReferenceNo", Voucher.ReferenceNo);
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@Entries",
                    SqlDbType = SqlDbType.Structured,
                    TypeName = "VoucherEntryType",
                    Value = entriesTable
                });
                var resultParam = new SqlParameter("@Result", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
                command.Parameters.Add(resultParam);

                await command.ExecuteNonQueryAsync();
                SuccessMessage = resultParam.Value.ToString();
                Voucher = new VoucherInput { Entries = new List<EntryInput> { new EntryInput() } }; // Reset form
                await LoadAccountsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving voucher: {ex.Message}";
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

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Accounts.Add(new Account
                {
                    AccountId = reader.GetInt32("AccountId"),
                    AccountName = reader.GetString("AccountName")
                });
            }
        }

        public class VoucherInput
        {
            public string VoucherType { get; set; } = string.Empty;
            public DateTime Date { get; set; } = DateTime.Today;
            public string ReferenceNo { get; set; } = string.Empty;
            public List<EntryInput> Entries { get; set; } = new();
        }

        public class EntryInput
        {
            public int AccountId { get; set; }
            public decimal? Debit { get; set; }
            public decimal? Credit { get; set; }
            public string Description { get; set; } = string.Empty;
        }

        public class Account
        {
            public int AccountId { get; set; }
            public string AccountName { get; set; } = string.Empty;
        }
    }
}