using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;
using StudentPortal.Services;

namespace StudentPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ─── Dashboard ───
        public async Task<IActionResult> Index()
        {
            ViewBag.StudentCount = await _context.Students.CountAsync();
            ViewBag.SubjectCount = await _context.Subjects.CountAsync();
            ViewBag.EnrollmentCount = await _context.Enrollments.CountAsync();
            ViewBag.AnnouncementCount = await _context.Announcements.CountAsync();
            ViewBag.MaterialCount = await _context.Materials.CountAsync();
            return View();
        }

        // ─── Students ───
        public async Task<IActionResult> Students(string? search, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.Name.Contains(search) ||
                    s.Email.Contains(search) ||
                    s.Course.Contains(search));
                ViewBag.Search = search;
            }

            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);
            ViewBag.CurrentPage = page;

            var students = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(students);
        }

        [HttpGet]
        public IActionResult CreateStudent() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(Student student, string password)
        {
            if (!ModelState.IsValid)
                return View(student);

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Create user account for this student
            if (!string.IsNullOrWhiteSpace(password))
            {
                var user = new User
                {
                    Username = student.Email,
                    PasswordHash = PasswordHasher.HashPassword(password),
                    Role = "Student",
                    StudentId = student.StudentId
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Student created successfully.";
            return RedirectToAction(nameof(Students));
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(Student student)
        {
            if (!ModelState.IsValid)
                return View(student);

            _context.Students.Update(student);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction(nameof(Students));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost, ActionName("DeleteStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                // Delete associated user account
                var user = await _context.Users.FirstOrDefaultAsync(u => u.StudentId == id);
                if (user != null) _context.Users.Remove(user);

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Student deleted successfully.";
            }
            return RedirectToAction(nameof(Students));
        }

        // ─── Subjects ───
        public async Task<IActionResult> Subjects()
        {
            var subjects = await _context.Subjects.ToListAsync();
            return View(subjects);
        }

        [HttpGet]
        public IActionResult CreateSubject() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(Subject subject)
        {
            if (!ModelState.IsValid)
                return View(subject);

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Subject created successfully.";
            return RedirectToAction(nameof(Subjects));
        }

        [HttpGet]
        public async Task<IActionResult> EditSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();
            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubject(Subject subject)
        {
            if (!ModelState.IsValid)
                return View(subject);

            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Subject updated successfully.";
            return RedirectToAction(nameof(Subjects));
        }

        // ─── Enrollments ───
        public async Task<IActionResult> Enrollments()
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Subject)
                .ToListAsync();
            return View(enrollments);
        }

        [HttpGet]
        public async Task<IActionResult> CreateEnrollment()
        {
            ViewBag.Students = await _context.Students.OrderBy(s => s.Name).ToListAsync();
            ViewBag.Subjects = await _context.Subjects.OrderBy(s => s.SubjectName).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEnrollment(int studentId, List<int> subjectIds)
        {
            if (subjectIds == null || subjectIds.Count == 0)
            {
                TempData["Error"] = "Please select at least one subject.";
                return RedirectToAction(nameof(CreateEnrollment));
            }

            int added = 0;
            int skipped = 0;

            foreach (var subjectId in subjectIds)
            {
                var exists = await _context.Enrollments
                    .AnyAsync(e => e.StudentId == studentId && e.SubjectId == subjectId);

                if (exists)
                {
                    skipped++;
                    continue;
                }

                _context.Enrollments.Add(new Enrollment
                {
                    StudentId = studentId,
                    SubjectId = subjectId
                });
                added++;
            }

            await _context.SaveChangesAsync();

            if (skipped > 0)
                TempData["Success"] = $"{added} subject(s) enrolled successfully. {skipped} skipped (already enrolled).";
            else
                TempData["Success"] = $"{added} subject(s) enrolled successfully.";

            return RedirectToAction(nameof(Enrollments));
        }

        [HttpGet]
        public async Task<IActionResult> SetGrade(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);
            if (enrollment == null) return NotFound();
            return View(enrollment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetGrade(int enrollmentId, string grade)
        {
            var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
            if (enrollment == null) return NotFound();

            enrollment.Grade = grade;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Grade updated successfully.";
            return RedirectToAction(nameof(Enrollments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEnrollment(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Enrollment deleted successfully.";
            }
            return RedirectToAction(nameof(Enrollments));
        }

        // ─── Announcements ───
        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements
                .OrderByDescending(a => a.DatePosted)
                .ToListAsync();
            return View(announcements);
        }

        [HttpGet]
        public IActionResult CreateAnnouncement() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(Announcement announcement)
        {
            if (!ModelState.IsValid)
                return View(announcement);

            announcement.DatePosted = DateTime.Now;
            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Announcement posted successfully.";
            return RedirectToAction(nameof(Announcements));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Announcement deleted.";
            }
            return RedirectToAction(nameof(Announcements));
        }

        // ─── Materials ───
        public async Task<IActionResult> Materials()
        {
            var materials = await _context.Materials
                .Include(m => m.Subject)
                .OrderByDescending(m => m.UploadDate)
                .ToListAsync();
            return View(materials);
        }

        [HttpGet]
        public async Task<IActionResult> UploadMaterial()
        {
            ViewBag.Subjects = await _context.Subjects.OrderBy(s => s.SubjectName).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMaterial(int subjectId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(UploadMaterial));
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsDir, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var material = new Material
            {
                SubjectId = subjectId,
                FileName = file.FileName,
                FilePath = uniqueName,
                UploadDate = DateTime.Now
            };

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Material uploaded successfully.";
            return RedirectToAction(nameof(Materials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material != null)
            {
                var filePath = Path.Combine(_env.WebRootPath, "uploads", material.FilePath);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                _context.Materials.Remove(material);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Material deleted.";
            }
            return RedirectToAction(nameof(Materials));
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
    }
}
