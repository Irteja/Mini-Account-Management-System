using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Threading.Tasks;

namespace MiniAccountSystem.Services
{
    public class PermissionService
    {
        private readonly string _connectionString;

        public PermissionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<bool> CheckPermissionAsync(string email, string moduleName)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand("CheckUserModulePermission", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@ModuleName", moduleName);
            var hasPermissionParam = new SqlParameter("@HasPermission", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(hasPermissionParam);
            await cmd.ExecuteNonQueryAsync();
            return (bool)hasPermissionParam.Value;
        }
    }
}