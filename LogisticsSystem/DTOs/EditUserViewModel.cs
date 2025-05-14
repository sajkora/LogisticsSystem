using System.ComponentModel.DataAnnotations;

namespace LogisticsSystem.Models
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Niepoprawny format adresu email.")]
        public string Email { get; set; }

        // Hasło nie jest wymagane przy edycji
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Rola jest wymagana.")]
        public string Role { get; set; }
    }
}
