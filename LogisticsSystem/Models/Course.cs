using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace LogisticsSystem.Models
{
    public class Course
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("driverId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string DriverId { get; set; }

        [BsonElement("shipperId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ShipperId  { get; set; }

        [BsonElement("startingPoint")]
        [Required(ErrorMessage = "Starting Point is required.")]
        public string StartingPoint { get; set; }

        [BsonElement("destination")]
        [Required(ErrorMessage = "Destination is required.")]
        public string Destination { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("isCompleted")]
        public bool IsCompleted { get; set; }

        [BsonElement("documentFileName")]
        public string DocumentFileName { get; set; }

        [BsonElement("documentPath")]
        public string DocumentPath { get; set; }

        [BsonElement("documentUploadDate")]
        public DateTime? DocumentUploadDate { get; set; }
    }
}
