using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using MiniAccountSystem.Services;
public class CreateModel : PageModel
{
    [BindProperty]
    public UserInputModel NewUser { get; set; } = new UserInputModel();

    public List<SelectListItem> RoleList { get; set; }
    public string Message { get; set; } = string.Empty;

    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    private readonly PermissionService _permissionService;
    public CreateModel(IConfiguration configuration, PermissionService permissionService)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("DefaultConnection")!;
        _permissionService = permissionService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Create User"))
        {
            return RedirectToPage("/Users/Login");
        }
        LoadRoles();

        return Page();
    }

    public void LoadRoles()
    {
        RoleList = new List<SelectListItem>();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            var cmd = new SqlCommand("GetAllRoles", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                RoleList.Add(new SelectListItem
                {
                    Value = reader["RoleId"].ToString(),
                    Text = reader["RoleName"].ToString()
                });
            }
        }
    }

    public IActionResult OnPost()
    {
        LoadRoles();

        if (!ModelState.IsValid)
        {
            Message = "Invalid input.";
            return Page();
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            using (var cmd = new SqlCommand("InsertUser", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@UserName", NewUser.UserName);
                cmd.Parameters.AddWithValue("@Email", NewUser.Email);
                cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(NewUser.Password));
                cmd.Parameters.AddWithValue("@RoleId", NewUser.RoleId);

                cmd.ExecuteNonQuery();
            }
        }

        Message = "User created successfully!";
        return Page();
    }

    private string HashPassword(string password)
    {
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
    }

    public class UserInputModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
    }
}
