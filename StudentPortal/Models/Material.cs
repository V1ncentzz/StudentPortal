using System.ComponentModel.DataAnnotations;

namespace StudentPortal.Models
{
    public class Material
    {
        [Key]
        public int MaterialId { get; set; }

        [Required]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }

        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.Now;
    }
}
