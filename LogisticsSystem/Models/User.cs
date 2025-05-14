using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace LogisticsSystem.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [Required(ErrorMessage = "Imię jest wymagane.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Niepoprawny format adresu email.")]
        public string Email { get; set; }

        // Nie oznaczamy [Required] – walidacja hasła odbywa się w DTO przy rejestracji
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Rola jest wymagana.")]
        public string Role { get; set; }
    }
}
