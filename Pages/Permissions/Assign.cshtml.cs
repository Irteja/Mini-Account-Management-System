using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using MiniAccountSystem.Services;
using System.Security.Claims;

public class AssignModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly PermissionService _permissionService;
    public AssignModel(IConfiguration configuration, PermissionService permissionService)
    {
        _configuration = configuration;
        _permissionService = permissionService;
    }

    [BindProperty]
    public int SelectedRoleId { get; set; }

    [BindProperty]
    public List<ModuleAccessModel> ModulesWithAccess { get; set; } = new();

    public List<SelectListItem> RoleList { get; set; } = new();
    public string Message { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int? roleId = 1)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Permission Pages"))
        {
            return RedirectToPage("/Users/Login");
        }
        LoadRoles();

        if (roleId.HasValue)
        {
            SelectedRoleId = roleId.Value;
            LoadPermissionsForRole(roleId.Value);
        }
        return Page();
    }
    public void LoadPermissionsForRole(int roleId)
    {
        using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        conn.Open();

        string sql = @"
        SELECT m.ModuleId, m.ModuleName,
               ISNULL(rp.CanAccess, 0) AS CanAccess
        FROM Modules m
        LEFT JOIN RolePermissions rp
          ON m.ModuleId = rp.ModuleId AND rp.RoleId = @RoleId";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@RoleId", roleId);
        var reader = cmd.ExecuteReader();

        ModulesWithAccess.Clear();
        while (reader.Read())
        {
            ModulesWithAccess.Add(new ModuleAccessModel
            {
                ModuleId = (int)reader["ModuleId"],
                ModuleName = reader["ModuleName"].ToString(),
                CanAccess = (bool)reader["CanAccess"]
            });
        }
    }


    public void LoadRoles()
    {
        RoleList.Clear();
        using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        conn.Open();
        var cmd = new SqlCommand("SELECT RoleId, RoleName FROM Roles", conn);
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



    public async Task<IActionResult> OnPostAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!User.Identity!.IsAuthenticated || string.IsNullOrEmpty(email) || !await _permissionService.CheckPermissionAsync(email, "Permission Pages"))
        {
            return RedirectToPage("/Users/Login");
        }
        LoadRoles();

        if (Request.Form.ContainsKey("SelectedRoleId"))
        {
            SelectedRoleId = int.Parse(Request.Form["SelectedRoleId"]!);
        }

        if (ModulesWithAccess.Count == 0 && SelectedRoleId > 0)
        {
            // Load modules with existing permissions
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            string sql = @"
                SELECT m.ModuleId, m.ModuleName,
                       ISNULL(rp.CanAccess, 0) AS CanAccess
                FROM Modules m
                LEFT JOIN RolePermissions rp
                  ON m.ModuleId = rp.ModuleId AND rp.RoleId = @RoleId";

            var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RoleId", SelectedRoleId);
            var reader = cmd.ExecuteReader();

            ModulesWithAccess.Clear();
            while (reader.Read())
            {
                ModulesWithAccess.Add(new ModuleAccessModel
                {
                    ModuleId = (int)reader["ModuleId"],
                    ModuleName = reader["ModuleName"].ToString(),
                    CanAccess = (bool)reader["CanAccess"]
                });
            }
        }
        else if (ModulesWithAccess.Count > 0)
        {
            // Save using stored procedure
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            conn.Open();

            var dt = new DataTable();
            dt.Columns.Add("ModuleId", typeof(int));
            dt.Columns.Add("CanAccess", typeof(bool));

            foreach (var module in ModulesWithAccess)
            {
                dt.Rows.Add(module.ModuleId, module.CanAccess);
            }

            var cmd = new SqlCommand("sp_AssignRolePermissions", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@RoleId", SelectedRoleId);
            var tvpParam = cmd.Parameters.AddWithValue("@Permissions", dt);
            tvpParam.SqlDbType = SqlDbType.Structured;
            tvpParam.TypeName = "TVP_Permissions";

            cmd.ExecuteNonQuery();
            Message = "Permissions updated!";
        }
        return Page();
    }

    public class ModuleAccessModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public bool CanAccess { get; set; }
    }
}
