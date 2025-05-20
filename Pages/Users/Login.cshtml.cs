using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MiniAccountSystem.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly string _connectionString;

        public LoginModel(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        [BindProperty]
        public LoginInputModel LoginInput { get; set; } = new LoginInputModel();

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToPage("/ChartOfAccounts/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fill in all required fields.";
                return Page();
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand("SELECT UserId, UserName, PasswordHash FROM Users WHERE Email = @Email", connection);
                command.Parameters.AddWithValue("@Email", LoginInput.Email);

                using var reader = await command.ExecuteReaderAsync();
                if (!reader.Read())
                {
                    ErrorMessage = "Invalid email or password.";
                    return Page();
                }

                var storedHash = reader.GetString("PasswordHash");
                var userId = reader.GetInt32("UserId");
                var userName = reader.GetString("UserName");

                if (!VerifyPassword(LoginInput.Password, storedHash))
                {
                    ErrorMessage = "Invalid email or password.";
                    return Page();
                }

                // Create claims
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Email, LoginInput.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Persist cookie across sessions
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) // Cookie expires in 1 hour
                };

                // Sign in the user
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToPage("/Index"); // Redirect to homepage or dashboard
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error logging in: {ex.Message}";
                return Page();
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var inputHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            return inputHash == storedHash;
        }

        public class LoginInputModel
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}