using System.Threading.Tasks;
using LogisticsSystem.Models;

namespace LogisticsSystem.Services.Contracts
{
    public interface IAuthService
    {
        Task<(bool Success, string ErrorMessage, User User)> ValidateUserCredentialsAsync(string email, string password);
        string GenerateJwtToken(User user);
    }
} 