using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Models;
using StudentPortal.Services;
using System.Security.Claims;

namespace StudentPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectBasedOnRole();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null || !PasswordHasher.PasswordVerify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString())
            };

            if (user.StudentId.HasValue)
            {
                claims.Add(new Claim("StudentId", user.StudentId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectBasedOnRole(user.Role);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private IActionResult RedirectBasedOnRole(string? role = null)
        {
            role ??= User.FindFirst(ClaimTypes.Role)?.Value;
            return role == "Admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Student");
        }
    }
}
