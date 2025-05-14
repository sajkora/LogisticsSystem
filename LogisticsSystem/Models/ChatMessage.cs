using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LogisticsSystem.Models
{
    public class ChatMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string RoomId { get; set; } = string.Empty;
    }
} 