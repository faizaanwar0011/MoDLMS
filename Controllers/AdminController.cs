using Microsoft.AspNetCore.Mvc;
using MoDLibrary.DAL;
using MoDLibrary.Models;
using System.Text.Json;

namespace MoDLibrary.Controllers
{
    public class AdminController : Controller
    {
        private readonly DatabaseHelper _db;
        public AdminController(IConfiguration config) { _db = new DatabaseHelper(config); }

        private UserSession? GetSession()
        {
            var json = HttpContext.Session.GetString("User");
            return json == null ? null : JsonSerializer.Deserialize<UserSession>(json);
        }

        private IActionResult RequireAdmin()
        {
            var u = GetSession();
            if (u == null) return RedirectToAction("Login", "Account");
            if (u.Role != "Admin") return RedirectToAction("AccessDenied", "Account");
            ViewBag.User = u;
            return null!;
        }

        // ── DASHBOARD ─────────────────────────────────────────────────────────
        public IActionResult Dashboard()
        {
            var check = RequireAdmin(); if (check != null) return check;
            var vm = new AdminDashboardViewModel
            {
                Stats = _db.GetDashboardStats(),
                RecentRequests = _db.GetRecentRequests(),
                OverdueBooks = _db.GetOverdueBooks()
            };
            return View(vm);
        }

        // ── BOOKS ─────────────────────────────────────────────────────────────
        public IActionResult Books()
        {
            var check = RequireAdmin(); if (check != null) return check;
            var books = _db.GetAllBooks();
            return View(books);
        }
        [HttpGet]
        public IActionResult AddBook()
        {
            var check = RequireAdmin(); if (check != null) return check;
            ViewBag.Categories = _db.GetCategories();
            return View(new Book());
        }

        [HttpPost]
        public IActionResult AddBook(Book model)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.AddBook(model);
            TempData["Success"] = "Book added successfully.";
            return RedirectToAction("Books");
        }

        [HttpGet]
        public IActionResult EditBook(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            var book = _db.GetBookById(id);
            if (book == null) return NotFound();
            ViewBag.Categories = _db.GetCategories();
            return View(book);
        }

        [HttpPost]
        public IActionResult EditBook(Book model)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.UpdateBook(model);
            TempData["Success"] = "Book updated successfully.";
            return RedirectToAction("Books");
        }

        [HttpPost]
        public IActionResult DeleteBook(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.DeleteBook(id);
            TempData["Success"] = "Book removed from catalog.";
            return RedirectToAction("Books");
        }

        // ── ISSUED BOOKS ──────────────────────────────────────────────────────
        public IActionResult IssuedBooks()
        {
            var check = RequireAdmin(); if (check != null) return check;
            var issued = _db.GetAllIssuedBooks();
            return View(issued);
        }

        // ── REQUESTS ─────────────────────────────────────────────────────────
        public IActionResult Requests()
        {
            var check = RequireAdmin(); if (check != null) return check;
            var requests = _db.GetAllRequests();
            return View(requests);
        }

        // ── FINES ─────────────────────────────────────────────────────────────
        public IActionResult Fines()
        {
            var check = RequireAdmin(); if (check != null) return check;
            var fines = _db.GetFines();
            return View(fines);
        }

        [HttpPost]
        public IActionResult MarkFinePaid(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.MarkFinePaid(id);
            TempData["Success"] = "Fine marked as paid.";
            return RedirectToAction("Fines");
        }

        // ── LIBRARIANS ────────────────────────────────────────────────────────
        public IActionResult Librarians()
        {
            var check = RequireAdmin(); if (check != null) return check;
            var librarians = _db.GetLibrarians();
            return View(librarians);
        }

        [HttpGet]
        public IActionResult AddLibrarian()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(new AddLibrarianViewModel());
        }

        [HttpPost]
        public IActionResult AddLibrarian(AddLibrarianViewModel model)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.AddLibrarian(model.FullName, model.Username, model.Password);
            TempData["Success"] = "Librarian account created.";
            return RedirectToAction("Librarians");
        }

        [HttpGet]
        public IActionResult EditLibrarian(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            var list = _db.GetLibrarians();
            var lib  = list.FirstOrDefault(l => l.UserId == id);
            if (lib == null) return NotFound();
            return View(lib);
        }

        [HttpPost]
        public IActionResult EditLibrarian(LibrarianUser model)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.UpdateLibrarian(model.UserId, model.FullName, model.Username, model.IsActive);
            TempData["Success"] = "Librarian updated.";
            return RedirectToAction("Librarians");
        }

        [HttpPost]
        public IActionResult DeleteLibrarian(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.DeleteLibrarian(id);
            TempData["Success"] = "Librarian deactivated.";
            return RedirectToAction("Librarians");
        }

        // ── CATEGORIES ────────────────────────────────────────────────

        public IActionResult Categories()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetAllCategories());
        }

        [HttpPost]
        public IActionResult AddCategory(string categoryName)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.AddCategory(categoryName);
            TempData["Success"] = "Category added successfully.";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public IActionResult UpdateCategory(int categoryId, string categoryName, bool isActive)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.UpdateCategory(categoryId, categoryName, isActive);
            TempData["Success"] = "Category updated.";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.DeleteCategory(id);
            TempData["Success"] = "Category deactivated.";
            return RedirectToAction("Categories");
        }
        // ── BOOK SUGGESTIONS ──────────────────────────────────────────

        public IActionResult Suggestions()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetAllSuggestions());
        }

        [HttpPost]
        public IActionResult UpdateSuggestion(SuggestionStatusViewModel model)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.UpdateSuggestionStatus(model.SuggestionId, model.Status, model.AdminRemark);
            TempData["Success"] = "Suggestion status updated.";
            return RedirectToAction("Suggestions");
        }
        // ── ANNOUNCEMENTS ─────────────────────────────────────────────
        public IActionResult Announcements()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetAllAnnouncements());
        }

        [HttpPost]
        public IActionResult AddAnnouncement(Announcement model)
        {
            var check = RequireAdmin(); if (check != null) return check;
            var u = GetSession();
            model.PostedBy = u?.FullName ?? "Admin";
            _db.AddAnnouncement(model);
            TempData["Success"] = "Announcement posted successfully.";
            return RedirectToAction("Announcements");
        }

        [HttpPost]
        public IActionResult DeleteAnnouncement(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.DeleteAnnouncement(id);
            TempData["Success"] = "Announcement removed.";
            return RedirectToAction("Announcements");
        }

        // ── MEMBER HISTORY ────────────────────────────────────────────
        public IActionResult MemberHistory(string? cnic)
        {
            var check = RequireAdmin(); if (check != null) return check;
            ViewBag.CNIC = cnic ?? "";
            if (string.IsNullOrWhiteSpace(cnic))
                return View(new List<MemberHistoryRecord>());
            return View(_db.GetMemberHistory(cnic));
        }

        // ── RESERVATIONS ──────────────────────────────────────────────
        public IActionResult Reservations()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetAllReservations());
        }

        // ── RATINGS ───────────────────────────────────────────────────
        public IActionResult Ratings()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetAllRatings());
        }

        // ── POPULAR BOOKS ─────────────────────────────────────────────
        public IActionResult PopularBooks()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetPopularBooks());
        }
    }
}
