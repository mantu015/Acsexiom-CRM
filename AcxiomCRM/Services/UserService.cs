using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAuditService _auditService;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
        }

        public async Task<PagedResult<UserItemViewModel>> GetUsersAsync(string? searchTerm, string? role, int page = 1, int pageSize = 10)
        {
            var query = _userManager.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(term) ||
                                         (u.Email != null && u.Email.ToLower().Contains(term)) ||
                                         (u.UserName != null && u.UserName.ToLower().Contains(term)));
            }

            var total = await query.CountAsync();
            var users = await query.OrderByDescending(u => u.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var list = new List<UserItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

                if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role))
                {
                    continue;
                }

                list.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "None",
                    IsActive = user.IsActive,
                    IsLockedOut = isLocked,
                    FailedLoginCount = user.AccessFailedCount,
                    CreatedDate = user.CreatedDate,
                    LastLoginDate = user.LastLoginDate
                });
            }

            return new PagedResult<UserItemViewModel>
            {
                Items = list,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = total
            };
        }

        public async Task<UserEditViewModel?> GetUserForEditAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Sales Executive",
                IsActive = user.IsActive,
                UnlockAccount = false
            };
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(UserCreateViewModel model, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            if (!SystemRoles.AllRoles.Contains(model.Role, StringComparer.Ordinal))
            {
                return (false, "Select a valid role for the new user.");
            }

            var existingByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingByEmail != null)
            {
                return (false, "A user with this email address already exists.");
            }

            var existingByName = await _userManager.FindByNameAsync(model.UserName);
            if (existingByName != null)
            {
                return (false, "A user with this username already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                IsActive = model.IsActive,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return (false, errors);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "User",
                user.Id,
                null,
                $"UserName: {user.UserName}, Role: {model.Role}",
                $"Created new system user {user.FullName} with role {model.Role}",
                "Success",
                ipAddress);

            return (true, "User created successfully!");
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(UserEditViewModel model, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return (false, "User not found.");

            // Safety guard: prevent admin from demoting or deactivating themselves
            if (user.Id == currentUserId)
            {
                if (!model.IsActive)
                {
                    return (false, "You cannot deactivate your own account.");
                }
                if (model.Role != SystemRoles.Admin)
                {
                    return (false, "You cannot remove your own Administrator role.");
                }
            }

            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.IsActive = model.IsActive;

            if (model.UnlockAccount)
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
            }

            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
            {
                return (false, string.Join("; ", updateRes.Errors.Select(e => e.Description)));
            }

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);

                await _auditService.LogAsync(
                    currentUserId,
                    currentUserName,
                    "Role Change",
                    "User",
                    user.Id,
                    string.Join(",", currentRoles),
                    model.Role,
                    $"Changed role for user {user.FullName} to {model.Role}",
                    "Success",
                    ipAddress);
            }

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Update",
                "User",
                user.Id,
                null,
                $"Active: {user.IsActive}",
                $"Updated user {user.FullName}",
                "Success",
                ipAddress);

            return (true, "User updated successfully!");
        }

        public async Task<(bool Success, string Message)> ToggleUserActiveStatusAsync(string id, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return (false, "User not found.");

            if (user.Id == currentUserId)
            {
                return (false, "You cannot deactivate your own account.");
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var action = user.IsActive ? "Activated" : "Deactivated";
            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Security",
                "User",
                user.Id,
                $"Active: {!user.IsActive}",
                $"Active: {user.IsActive}",
                $"{action} user {user.FullName}",
                "Success",
                ipAddress);

            return (true, $"User successfully {action.ToLower()}.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(UserResetPasswordViewModel model, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return (false, "User not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Security",
                "User",
                user.Id,
                null,
                "Password Reset",
                $"Admin reset password for user {user.FullName}",
                "Success",
                ipAddress);

            return (true, "Password has been successfully reset.");
        }

        public async Task<List<UserItemViewModel>> GetSalesExecutivesForDropdownAsync()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
            var list = new List<UserItemViewModel>();
            foreach (var user in users)
            {
                list.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty
                });
            }
            return list.OrderBy(u => u.FullName).ToList();
        }
    }
}
