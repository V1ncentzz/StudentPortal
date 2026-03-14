using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;
using System.Security.Claims;

namespace StudentPortal.Controllers
{
    [Authorize]
    public class ChatbotController : Controller
    {
        private readonly AppDbContext _context;

        public ChatbotController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatMessage message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Text))
                return Json(new { reply = "Please type a message." });

            var input = message.Text.Trim().ToLower();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var studentIdStr = User.FindFirst("StudentId")?.Value;
            int? studentId = studentIdStr != null ? int.Parse(studentIdStr) : null;

            var reply = await GetBotReply(input, role, studentId);
            return Json(new { reply });
        }

        private async Task<string> GetBotReply(string input, string role, int? studentId)
        {
            // Greetings
            if (ContainsAny(input, "hello", "hi", "hey", "good morning", "good afternoon", "good evening"))
                return $"👋 Hello, {User.Identity?.Name}! How can I help you today? You can ask me about subjects, grades, announcements, materials, or how to use the portal.";

            if (ContainsAny(input, "how are you", "what's up", "whats up"))
                return "😊 I'm doing great, thanks for asking! What can I help you with today?";

            // Help
            if (ContainsAny(input, "help", "what can you do", "commands", "menu", "options"))
            {
                if (role == "Admin")
                    return "🤖 Here's what I can help with:\n\n" +
                           "📊 **\"stats\"** — View portal statistics\n" +
                           "👩‍🎓 **\"students\"** — Student info\n" +
                           "📚 **\"subjects\"** — Subject info\n" +
                           "📝 **\"enrollments\"** — Enrollment info\n" +
                           "📢 **\"announcements\"** — Latest announcements\n" +
                           "📁 **\"materials\"** — Uploaded materials\n" +
                           "🔗 **\"links\"** — Quick navigation links\n\n" +
                           "Just type any keyword to get started!";
                else
                    return "🤖 Here's what I can help with:\n\n" +
                           "📚 **\"subjects\"** — View your enrolled subjects\n" +
                           "📊 **\"grades\"** — Check your grades\n" +
                           "📢 **\"announcements\"** — Latest announcements\n" +
                           "📁 **\"materials\"** — Your learning materials\n" +
                           "👤 **\"profile\"** — Your profile info\n" +
                           "🔗 **\"links\"** — Quick navigation links\n\n" +
                           "Just type any keyword to get started!";
            }

            // Stats (Admin)
            if (role == "Admin" && ContainsAny(input, "stats", "statistics", "dashboard", "overview", "summary", "count"))
            {
                var studentCount = await _context.Students.CountAsync();
                var subjectCount = await _context.Subjects.CountAsync();
                var enrollmentCount = await _context.Enrollments.CountAsync();
                var announcementCount = await _context.Announcements.CountAsync();
                var materialCount = await _context.Materials.CountAsync();

                return $"📊 **Portal Statistics:**\n\n" +
                       $"👩‍🎓 Students: **{studentCount}**\n" +
                       $"📚 Subjects: **{subjectCount}**\n" +
                       $"📝 Enrollments: **{enrollmentCount}**\n" +
                       $"📢 Announcements: **{announcementCount}**\n" +
                       $"📁 Materials: **{materialCount}**";
            }

            // Students
            if (ContainsAny(input, "student", "students"))
            {
                if (role == "Admin")
                {
                    var count = await _context.Students.CountAsync();
                    var recent = await _context.Students.OrderByDescending(s => s.StudentId).Take(3).ToListAsync();
                    var list = string.Join("\n", recent.Select(s => $"• {s.Name} ({s.Course}, Year {s.YearLevel})"));
                    return $"👩‍🎓 There are **{count}** students registered.\n\n**Latest students:**\n{list}\n\n➡️ Go to [Students](/Admin/Students) to manage them.";
                }
                else
                {
                    return "👩‍🎓 You can view your profile and personal information at [My Profile](/Student/Profile).";
                }
            }

            // Subjects
            if (ContainsAny(input, "subject", "subjects", "course", "courses"))
            {
                if (role == "Admin")
                {
                    var subjects = await _context.Subjects.ToListAsync();
                    if (subjects.Count == 0) return "📚 No subjects created yet. Go to [Add Subject](/Admin/CreateSubject) to create one.";
                    var list = string.Join("\n", subjects.Select(s => $"• {s.SubjectName} — {s.Instructor}"));
                    return $"📚 **Subjects ({subjects.Count}):**\n\n{list}\n\n➡️ [Manage Subjects](/Admin/Subjects)";
                }
                else if (studentId.HasValue)
                {
                    var enrollments = await _context.Enrollments
                        .Include(e => e.Subject)
                        .Where(e => e.StudentId == studentId.Value)
                        .ToListAsync();
                    if (enrollments.Count == 0) return "📚 You're not enrolled in any subjects yet.";
                    var list = string.Join("\n", enrollments.Select(e => $"• {e.Subject?.SubjectName} — {e.Subject?.Instructor}"));
                    return $"📚 **Your Subjects ({enrollments.Count}):**\n\n{list}\n\n➡️ [View Subjects](/Student/Subjects)";
                }
                return "📚 Visit your subjects page to see your enrolled subjects.";
            }

            // Grades
            if (ContainsAny(input, "grade", "grades", "score", "scores", "marks", "result", "results"))
            {
                if (role == "Student" && studentId.HasValue)
                {
                    var enrollments = await _context.Enrollments
                        .Include(e => e.Subject)
                        .Where(e => e.StudentId == studentId.Value)
                        .ToListAsync();
                    if (enrollments.Count == 0) return "📊 No grades available — you're not enrolled in any subjects.";
                    var list = string.Join("\n", enrollments.Select(e =>
                        $"• {e.Subject?.SubjectName}: **{(string.IsNullOrEmpty(e.Grade) ? "Pending" : e.Grade)}**"));
                    return $"📊 **Your Grades:**\n\n{list}\n\n➡️ [View Grades](/Student/Grades)";
                }
                else if (role == "Admin")
                {
                    return "📊 You can set grades from the [Enrollments](/Admin/Enrollments) page by clicking the **Grade** button.";
                }
                return "📊 Visit [My Grades](/Student/Grades) to see your grades.";
            }

            // Announcements
            if (ContainsAny(input, "announcement", "announcements", "news", "update", "updates", "notice"))
            {
                var announcements = await _context.Announcements
                    .OrderByDescending(a => a.DatePosted)
                    .Take(3)
                    .ToListAsync();

                if (announcements.Count == 0) return "📢 No announcements posted yet.";

                var list = string.Join("\n\n", announcements.Select(a =>
                    $"📌 **{a.Title}**\n_{a.DatePosted:MMM dd, yyyy}_\n{(a.Content.Length > 80 ? a.Content.Substring(0, 80) + "..." : a.Content)}"));

                var link = role == "Admin" ? "/Admin/Announcements" : "/Student/Announcements";
                return $"📢 **Latest Announcements:**\n\n{list}\n\n➡️ [View All]({link})";
            }

            // Materials
            if (ContainsAny(input, "material", "materials", "file", "files", "download", "resource", "resources", "document"))
            {
                if (role == "Admin")
                {
                    var count = await _context.Materials.CountAsync();
                    return $"📁 There are **{count}** material(s) uploaded.\n\n➡️ [Manage Materials](/Admin/Materials) | [Upload New](/Admin/UploadMaterial)";
                }
                else
                {
                    return "📁 You can download learning materials for your enrolled subjects at [Materials](/Student/Materials).";
                }
            }

            // Enrollment
            if (ContainsAny(input, "enroll", "enrollment", "enrollments", "register", "registration"))
            {
                if (role == "Admin")
                {
                    var count = await _context.Enrollments.CountAsync();
                    return $"📝 There are **{count}** enrollment(s).\n\n➡️ [Manage Enrollments](/Admin/Enrollments) | [Enroll Student](/Admin/CreateEnrollment)";
                }
                return "📝 Your enrollment is managed by your admin. Check [My Subjects](/Student/Subjects) to see what you're enrolled in.";
            }

            // Profile
            if (ContainsAny(input, "profile", "my info", "my information", "personal", "account"))
            {
                if (role == "Student" && studentId.HasValue)
                {
                    var student = await _context.Students.FindAsync(studentId.Value);
                    if (student != null)
                        return $"👤 **Your Profile:**\n\n• Name: **{student.Name}**\n• Email: **{student.Email}**\n• Course: **{student.Course}**\n• Year Level: **{student.YearLevel}**\n\n➡️ [Edit Profile](/Student/EditProfile)";
                }
                return "👤 You can view and edit your profile at [My Profile](/Student/Profile).";
            }

            // Links / Navigation
            if (ContainsAny(input, "link", "links", "navigate", "navigation", "go to", "where", "page"))
            {
                if (role == "Admin")
                    return "🔗 **Quick Links:**\n\n" +
                           "• [Dashboard](/Admin/Index)\n" +
                           "• [Students](/Admin/Students)\n" +
                           "• [Subjects](/Admin/Subjects)\n" +
                           "• [Enrollments](/Admin/Enrollments)\n" +
                           "• [Announcements](/Admin/Announcements)\n" +
                           "• [Materials](/Admin/Materials)";
                else
                    return "🔗 **Quick Links:**\n\n" +
                           "• [Dashboard](/Student/Index)\n" +
                           "• [My Profile](/Student/Profile)\n" +
                           "• [My Subjects](/Student/Subjects)\n" +
                           "• [My Grades](/Student/Grades)\n" +
                           "• [Materials](/Student/Materials)\n" +
                           "• [Announcements](/Student/Announcements)";
            }

            // Thank you
            if (ContainsAny(input, "thank", "thanks", "thank you", "ty", "appreciate"))
                return "😊 You're welcome! Let me know if there's anything else I can help with.";

            // Goodbye
            if (ContainsAny(input, "bye", "goodbye", "see you", "exit", "close"))
                return "👋 Goodbye! Have a great day! Feel free to chat anytime.";

            // Fallback
            return "🤔 I'm not sure about that. Try asking about **subjects**, **grades**, **announcements**, **materials**, **profile**, or type **\"help\"** to see all options!";
        }

        private static bool ContainsAny(string input, params string[] keywords)
        {
            return keywords.Any(k => input.Contains(k));
        }
    }

    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
    }
}
