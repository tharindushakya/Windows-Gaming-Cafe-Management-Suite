using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamingCafe.Core.Interfaces.Services;
using GamingCafe.Core.Models;
using GamingCafe.Data;
using Microsoft.EntityFrameworkCore;

namespace GamingCafe.API.Services
{
    /// <summary>
    /// Minimal IUserService implementation focused on role lookup and permission mapping.
    /// Permissions are computed from role using a simple mapping. This is intentionally
    /// simple; replace with a DB-backed permissions store for production.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly GamingCafeContext _db;

        public UserService(GamingCafeContext db)
        {
            _db = db;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
            => await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

        public async Task<User?> GetUserByUsernameAsync(string username)
            => await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        public async Task<User?> GetUserByEmailAsync(string email)
            => await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        public Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(int page, int pageSize, string? searchTerm = null)
            => throw new System.NotImplementedException();

        public Task<User> CreateUserAsync(User user, string password) => throw new System.NotImplementedException();
        public Task<User> UpdateUserAsync(User user) => throw new System.NotImplementedException();
        public Task DeleteUserAsync(int userId) => throw new System.NotImplementedException();
        public Task<bool> SoftDeleteUserAsync(int userId) => throw new System.NotImplementedException();
        public Task<bool> RestoreUserAsync(int userId) => throw new System.NotImplementedException();
        public Task<bool> ValidatePasswordAsync(User user, string password) => throw new System.NotImplementedException();
        public Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword) => throw new System.NotImplementedException();
        public Task<string> GeneratePasswordResetTokenAsync(int userId) => throw new System.NotImplementedException();
        public Task<bool> ResetPasswordAsync(int userId, string token, string newPassword) => throw new System.NotImplementedException();
        public Task<bool> AssignRoleAsync(int userId, string role) => throw new System.NotImplementedException();
        public Task<bool> RemoveRoleAsync(int userId, string role) => throw new System.NotImplementedException();

        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId);
            if (u == null) return Enumerable.Empty<string>();
            return new[] { u.Role.ToString() };
        }

        /// <summary>
        /// Simple mapping from role -> permissions. Admin gets everything.
        /// Manager and Staff get a subset.
        /// </summary>
        public Task<bool> HasPermissionAsync(int userId, string permission)
        {
            // In-memory mapping for demo purposes
            var role = _db.Users.Where(u => u.UserId == userId).Select(u => u.Role.ToString()).FirstOrDefault();
            if (string.IsNullOrEmpty(role)) return Task.FromResult(false);

            if (role == GamingCafe.Core.Authorization.RoleNames.Admin)
                return Task.FromResult(true);

            if (role == GamingCafe.Core.Authorization.RoleNames.Manager)
            {
                // Manager can view financials but not issue refunds by default
                if (permission == "inv:write" || permission == "view:financials")
                    return Task.FromResult(true);
                return Task.FromResult(false);
            }

            if (role == GamingCafe.Core.Authorization.RoleNames.Staff)
            {
                if (permission == "inv:write")
                    return Task.FromResult(true);
                return Task.FromResult(false);
            }

            return Task.FromResult(false);
        }

        public Task<IEnumerable<string>> GetPermissionsAsync(int userId)
        {
            var role = _db.Users.Where(u => u.UserId == userId).Select(u => u.Role.ToString()).FirstOrDefault();
            if (string.IsNullOrEmpty(role)) return Task.FromResult(Enumerable.Empty<string>());

            if (role == GamingCafe.Core.Authorization.RoleNames.Admin)
                return Task.FromResult((IEnumerable<string>)new[] { "*" }); // wildcard meaning all permissions

            if (role == GamingCafe.Core.Authorization.RoleNames.Manager)
            {
                return Task.FromResult((IEnumerable<string>)new[] { "inv:write", "view:financials" });
            }

            if (role == GamingCafe.Core.Authorization.RoleNames.Staff)
            {
                return Task.FromResult((IEnumerable<string>)new[] { "inv:write" });
            }

            return Task.FromResult(Enumerable.Empty<string>());
        }

        // Wallet / other methods not implemented in this minimal service
        public Task<decimal> GetWalletBalanceAsync(int userId) => throw new System.NotImplementedException();
        public Task<bool> AddFundsAsync(int userId, decimal amount, string description = "") => throw new System.NotImplementedException();
        public Task<bool> DeductFundsAsync(int userId, decimal amount, string description = "") => throw new System.NotImplementedException();
    }
}
