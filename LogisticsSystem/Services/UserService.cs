using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LogisticsSystem.Models;
using LogisticsSystem.Services.Contracts;
using System.Linq;
using BCrypt.Net;

namespace LogisticsSystem.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoDB:DatabaseName"]);
            _users = database.GetCollection<User>("Users");
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _users.Find(_ => true).ToListAsync();
        }

        public async Task CreateUserAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task<ReplaceOneResult> UpdateUserAsync(User user)
        {
            return await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
        }

        public async Task DeleteUserAsync(string id)
        {
            await _users.DeleteOneAsync(u => u.Id == id);
        }

        public async Task<ManageUsersViewModel> GetManageUsersViewModelAsync()
        {
            var users = await GetAllUsersAsync();
            return new ManageUsersViewModel
            {
                AdminUsers = users.Where(u => u.Role == "Admin").ToList(),
                ShipperUsers = users.Where(u => u.Role == "Shipper").ToList(),
                DriverUsers = users.Where(u => u.Role == "Driver").ToList()
            };
        }

        public async Task<(bool Success, string ErrorMessage)> CreateUserFromViewModelAsync(RegisterViewModel model)
        {
            if (await GetUserByEmailAsync(model.Email) != null)
                return (false, "A user with the given email already exists.");
            var newUser = new User
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.Role
            };
            await CreateUserAsync(newUser);
            return (true, null);
        }

        public async Task<EditUserViewModel> GetEditUserViewModelAsync(string id)
        {
            var user = await GetUserByIdAsync(id);
            if (user == null) return null;
            return new EditUserViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateUserFromViewModelAsync(EditUserViewModel model)
        {
            var user = await GetUserByIdAsync(model.Id);
            if (user == null) return (false, "User not found.");
            user.Name = model.Name;
            user.Email = model.Email;
            user.Role = model.Role;
            if (!string.IsNullOrWhiteSpace(model.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            await UpdateUserAsync(user);
            return (true, null);
        }

        public async Task<(string Name, string Email, string Id)> GetUserDisplayInfoAsync(System.Security.Claims.ClaimsPrincipal user)
        {
            if (user.Identity.IsAuthenticated)
            {
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var dbUser = await GetUserByIdAsync(userId);
                    if (dbUser != null)
                        return (dbUser.Name, dbUser.Email, dbUser.Id);
                }
            }
            return (null, null, null);
        }
    }
}
