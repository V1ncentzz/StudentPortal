using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Student"; // "Admin" or "Student"

        public int? StudentId { get; set; }
        public Student? Student { get; set; }
    }
}
