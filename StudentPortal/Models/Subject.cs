using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models
{
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required]
        [StringLength(100)]
        public string SubjectName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Instructor { get; set; } = string.Empty;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
