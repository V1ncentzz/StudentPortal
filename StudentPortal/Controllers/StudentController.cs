using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;
using System.Security.Claims;

namespace StudentPortal.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StudentController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetStudentId()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            return studentIdClaim != null ? int.Parse(studentIdClaim) : 0;
        }

        // ─── Dashboard ───
        public async Task<IActionResult> Index()
        {
            var studentId = GetStudentId();
            var student = await _context.Students.FindAsync(studentId);
            ViewBag.Student = student;
            ViewBag.SubjectCount = await _context.Enrollments.CountAsync(e => e.StudentId == studentId);
            ViewBag.AnnouncementCount = await _context.Announcements.CountAsync();
            return View();
        }

        // ─── Profile ───
        public async Task<IActionResult> Profile()
        {
            var student = await _context.Students.FindAsync(GetStudentId());
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var student = await _context.Students.FindAsync(GetStudentId());
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Student model)
        {
            var student = await _context.Students.FindAsync(GetStudentId());
            if (student == null) return NotFound();

            student.Name = model.Name;
            student.Email = model.Email;
            student.Course = model.Course;
            student.YearLevel = model.YearLevel;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // ─── Subjects ───
        public async Task<IActionResult> Subjects()
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Subject)
                .Where(e => e.StudentId == GetStudentId())
                .ToListAsync();
            return View(enrollments);
        }

        // ─── Grades ───
        public async Task<IActionResult> Grades()
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Subject)
                .Where(e => e.StudentId == GetStudentId())
                .ToListAsync();
            return View(enrollments);
        }

        // ─── Materials ───
        public async Task<IActionResult> Materials()
        {
            var studentId = GetStudentId();
            var subjectIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.SubjectId)
                .ToListAsync();

            var materials = await _context.Materials
                .Include(m => m.Subject)
                .Where(m => subjectIds.Contains(m.SubjectId))
                .OrderByDescending(m => m.UploadDate)
                .ToListAsync();

            return View(materials);
        }

        public IActionResult DownloadMaterial(int id)
        {
            var material = _context.Materials.Find(id);
            if (material == null) return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, "uploads", material.FilePath);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/octet-stream", material.FileName);
        }

        // ─── Announcements ───
        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements
                .OrderByDescending(a => a.DatePosted)
                .ToListAsync();
            return View(announcements);
        }
    }
}
