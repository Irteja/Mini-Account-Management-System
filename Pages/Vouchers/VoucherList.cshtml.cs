using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using ClosedXML.Excel;
using MiniAccountSystem.Services;
using System.Security.Claims;

namespace MiniAccountSystem.Pages.Vouchers
{
    public class VoucherListModel : PageModel
    {
        private readonly string _connectionString;
        private readonly PermissionService _permissionService;
        public VoucherListModel(IConfiguration configuration, PermissionService permissionService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _permissionService = permissionService;
        }

        public List<Voucher> Vouchers { get; set; } = new List<Voucher>();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Voucher List"))
            {
                return RedirectToPage("/Users/Login");
            }
            
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

        public async Task<IActionResult> OnPostExportExcelAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Voucher List"))
            {
                return RedirectToPage("/Users/Login");
            }

            try
            {
                await LoadVouchersAsync();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Vouchers");


                worksheet.Cell(1, 1).Value = "Date";
                worksheet.Cell(1, 2).Value = "Voucher Type";
                worksheet.Cell(1, 3).Value = "Reference No.";
                worksheet.Cell(1, 4).Value = "Account";
                worksheet.Cell(1, 5).Value = "Debit";
                worksheet.Cell(1, 6).Value = "Credit";
                worksheet.Cell(1, 7).Value = "Description";


                for (int i = 0; i < Vouchers.Count; i++)
                {
                    var voucher = Vouchers[i];
                    worksheet.Cell(i + 2, 1).Value = voucher.Date.ToString("yyyy-MM-dd");
                    worksheet.Cell(i + 2, 2).Value = voucher.VoucherType;
                    worksheet.Cell(i + 2, 3).Value = voucher.ReferenceNo;
                    worksheet.Cell(i + 2, 4).Value = voucher.AccountName;
                    worksheet.Cell(i + 2, 5).Value = voucher.Debit;
                    worksheet.Cell(i + 2, 6).Value = voucher.Credit;
                    worksheet.Cell(i + 2, 7).Value = voucher.Description;
                }


                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Vouchers.xlsx");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to Excel: {ex.Message}";
                await LoadVouchersAsync();
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
                    Description = reader.IsDBNull("Description") ? null! : reader.GetString("Description")
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