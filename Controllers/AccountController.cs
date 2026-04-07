using Microsoft.AspNetCore.Mvc;
using MoDLibrary.DAL;
using MoDLibrary.Models;
using System.Text.Json;

namespace MoDLibrary.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseHelper _db;
        public AccountController(IConfiguration config) { _db = new DatabaseHelper(config); }

        // Login page pe redirect — agar already logged in
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("User") != null)
                return RedirectToRole();
            return View();
        }

        // Continue as Member — seedha catalog
        public IActionResult ContinueAsMember()
        {
            var memberSession = new UserSession
            {
                UserId = 0,
                FullName = "Guest Member",
                Username = "member",
                Role = "Member"
            };
            HttpContext.Session.SetString("User", JsonSerializer.Serialize(memberSession));
            return RedirectToAction("Index", "Member");
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            var user = _db.Login(model.Username, model.Password);
            if (user == null)
            {
                ViewBag.Error = "Invalid username or password.";
                return View(model);
            }
            HttpContext.Session.SetString("User", JsonSerializer.Serialize(user));
            return RedirectToRole(user.Role);
        }

        // Member bina login ke seedha book browse page pe ja sakta hai
      /*  public IActionResult ContinueAsMember()
        {
            var memberSession = new UserSession
            {
                UserId = 0,
                FullName = "Guest Member",
                Username = "member",
                Role = "Member"
            };
            HttpContext.Session.SetString("User", JsonSerializer.Serialize(memberSession));
            return RedirectToAction("Search", "Member");
        }*/

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToRole(string? role = null)
        {
            if (role == null)
            {
                var u = GetSession();
                role = u?.Role ?? "";
            }
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Librarian" => RedirectToAction("Dashboard", "Librarian"),
                "Member" => RedirectToAction("Search", "Member"),
                _ => RedirectToAction("Login")
            };
        }

        private UserSession? GetSession()
        {
            var json = HttpContext.Session.GetString("User");
            return json == null ? null : JsonSerializer.Deserialize<UserSession>(json);
        }
    }
}