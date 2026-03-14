using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models
{
    public class Enrollment
    {
        [Key]
        public int EnrollmentId { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [StringLength(10)]
        public string? Grade { get; set; }
    }
}
