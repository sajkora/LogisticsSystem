using LogisticsSystem.Models;
using MongoDB.Driver;

namespace LogisticsSystem.Services
{
    public class ChatService : IChatService
    {
        private readonly IMongoCollection<ChatMessage> _messages;
        private readonly IMongoCollection<User> _users;

        public ChatService(IMongoDatabase database)
        {
            _messages = database.GetCollection<ChatMessage>("ChatMessages");
            _users = database.GetCollection<User>("Users");
        }

        public async Task<ChatMessage> SaveMessage(ChatMessage message)
        {
            await _messages.InsertOneAsync(message);
            return message;
        }

        public async Task<List<ChatMessage>> GetRoomMessages(string roomId, int limit = 50)
        {
            return await _messages
                .Find(m => m.RoomId == roomId)
                .SortByDescending(m => m.Timestamp)
                .Limit(limit > 0 ? limit : 50)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetUserMessages(string userId, int limit = 50)
        {
            var filter = Builders<ChatMessage>.Filter.Or(
                Builders<ChatMessage>.Filter.Eq(m => m.SenderId, userId),
                Builders<ChatMessage>.Filter.Regex(m => m.RoomId, new MongoDB.Bson.BsonRegularExpression($"private_{userId}_|private_.*_{userId}"))
            );
            
            return await _messages
                .Find(filter)
                .SortByDescending(m => m.Timestamp)
                .Limit(limit > 0 ? limit : 50)
                .ToListAsync();
        }

        public async Task<List<User>> GetRecentChatUsers(string userId)
        {
            // Find the most recent messages sent or received by the user
            var recentMessages = await _messages.Find(m => m.SenderId == userId || m.RoomId.Contains(userId))
                .SortByDescending(m => m.Timestamp)
                .Limit(20)
                .ToListAsync();
            var userIds = recentMessages
                .Select(m => m.SenderId == userId ? m.RoomId.Replace($"private_{userId}_", "").Replace($"private_", "").Replace(userId, "").Replace("_", "") : m.SenderId)
                .Where(id => !string.IsNullOrEmpty(id) && id != userId)
                .Distinct()
                .Where(id => id.Length == 24 && System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-fA-F0-9]{24}$"))
                .ToList();
            if (!userIds.Any()) return new List<User>();
            var filter = Builders<User>.Filter.In(u => u.Id, userIds);
            return await _users.Find(filter).ToListAsync();
        }

        public async Task<string> GetDisplayName(string userId)
        {
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user != null && !string.IsNullOrEmpty(user.Name))
                return user.Name;
            return userId;
        }
    }
} 