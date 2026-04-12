using System.ComponentModel.DataAnnotations;

namespace MenengiomaBackend.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; } // Şemadaki gibi UserID yaptık

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Şemadaki gibi PasswordHash yaptık

        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = "Radyolog";

        public string Email { get; set; } = string.Empty; // Eksik olan Email kolonunu ekledik
    }
}