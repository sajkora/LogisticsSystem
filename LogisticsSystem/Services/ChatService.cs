using LogisticsSystem.Models;
using MongoDB.Driver;

namespace LogisticsSystem.Services
{
    public class ChatService : IChatService
    {
        private readonly IMongoCollection<ChatMessage> _messages;

        public ChatService(IMongoDatabase database)
        {
            _messages = database.GetCollection<ChatMessage>("ChatMessages");
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
    }
} 