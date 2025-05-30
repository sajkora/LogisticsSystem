using System.Collections.Generic;
using System.Threading.Tasks;
using LogisticsSystem.Models;
using MongoDB.Driver;

namespace LogisticsSystem.Services.Contracts
{
    public interface IUserService
    {
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByIdAsync(string id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task CreateUserAsync(User user);
        Task<ReplaceOneResult> UpdateUserAsync(User user);
        Task DeleteUserAsync(string id);
        Task<ManageUsersViewModel> GetManageUsersViewModelAsync();
        Task<(bool Success, string ErrorMessage)> CreateUserFromViewModelAsync(RegisterViewModel model);
        Task<EditUserViewModel> GetEditUserViewModelAsync(string id);
        Task<(bool Success, string ErrorMessage)> UpdateUserFromViewModelAsync(EditUserViewModel model);
        Task<(string Name, string Email, string Id)> GetUserDisplayInfoAsync(System.Security.Claims.ClaimsPrincipal user);
    }
}
