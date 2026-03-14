using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StudentPortal.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                return role == "Admin"
                    ? RedirectToAction("Index", "Admin")
                    : RedirectToAction("Index", "Student");
            }
            return RedirectToAction("Login", "Account");
        }
    }
}
