using LogisticsSystem.Models;

namespace LogisticsSystem.Services
{
    public interface IChatService
    {
        Task<ChatMessage> SaveMessage(ChatMessage message);
        Task<List<ChatMessage>> GetRoomMessages(string roomId, int limit = 50);
        Task<List<ChatMessage>> GetUserMessages(string userId, int limit = 50);
    }
} 