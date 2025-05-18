using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace MiniAccountSystem.Pages.Vouchers
{
    public class VoucherListModel : PageModel
    {
        private readonly string _connectionString;

        public VoucherListModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public List<Voucher> Vouchers { get; set; } = new List<Voucher>();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await LoadVouchersAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading vouchers: {ex.Message}";
                return Page();
            }
        }

        private async Task LoadVouchersAsync()
        {
            Vouchers = new List<Voucher>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand("SELECT v.VoucherId, v.VoucherType, v.Date, v.ReferenceNo, v.AccountId, a.AccountName, v.Debit, v.Credit, v.Description FROM Vouchers v JOIN Accounts a ON v.AccountId = a.AccountId ORDER BY v.Date DESC", connection);


            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Vouchers.Add(new Voucher
                {
                    VoucherId = reader.GetInt32("VoucherId"),
                    VoucherType = reader.GetString("VoucherType"),
                    Date = reader.GetDateTime("Date"),
                    ReferenceNo = reader.GetString("ReferenceNo"),
                    AccountId = reader.GetInt32("AccountId"),
                    AccountName = reader.GetString("AccountName"),
                    Debit = reader.GetDecimal("Debit"),
                    Credit = reader.GetDecimal("Credit"),
                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description")
                });
            }
        }

        public class Voucher
        {
            public int VoucherId { get; set; }
            public string VoucherType { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public string ReferenceNo { get; set; } = string.Empty;
            public int AccountId { get; set; }
            public string AccountName { get; set; } = string.Empty;
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public string Description { get; set; } = string.Empty;
        }
    }
}