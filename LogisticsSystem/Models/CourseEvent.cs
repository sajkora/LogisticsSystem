using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace LogisticsSystem.Models
{
    public class CourseEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("courseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public string CourseId { get; set; }

        [BsonElement("driverId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public string DriverId { get; set; }

        [BsonElement("eventType")]
        [Required]
        public string EventType { get; set; } // e.g., Delay, Breakdown, Other

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
} 