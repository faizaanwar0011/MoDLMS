using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoDLibrary.DAL;
using MoDLibrary.Hubs;
using MoDLibrary.Models;
using System.Text.Json;

namespace MoDLibrary.Controllers
{
    public class MemberController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly IHubContext<NotificationHub> _hub;

        public MemberController(IConfiguration config, IHubContext<NotificationHub> hub)
        {
            _db = new DatabaseHelper(config);
            _hub = hub;
        }

        private UserSession? GetSession()
        {
            var json = HttpContext.Session.GetString("User");
            return json == null ? null : JsonSerializer.Deserialize<UserSession>(json);
        }

        private IActionResult RequireMemberLogin()
        {
            var u = GetSession();
            if (u == null || u.Role != "Member")
                return RedirectToAction("Login", "Account");
            ViewBag.User = u;
            return null!;
        }

        // ── HOME — Physical Book Catalog (No Login) ───────────────
        public IActionResult Index(string? q, string? category)
        {
            ViewBag.Query = q ?? "";
            ViewBag.Category = category ?? "";
            ViewBag.Categories = _db.GetCategories();

            List<Book> books;
            if (!string.IsNullOrWhiteSpace(q))
                books = _db.SearchBooks(q).ToList();
            else
                books = _db.GetAllBooks().Where(b => b.IsActive).ToList();

            if (!string.IsNullOrWhiteSpace(category))
                books = books.Where(b => b.Category == category).ToList();

            return View(books);
        }

        // ── BOOK DETAIL (No Login) ────────────────────────────────
        public IActionResult BookDetail(int id)
        {
            var book = _db.GetBookById(id);
            if (book == null) return NotFound();
            var ratings = _db.GetBookRatings(id);
            ViewBag.Ratings = ratings;
            return View(book);
        }

        // ── EBOOKS (Login Required) ───────────────────────────────
        public IActionResult EBooks()
        {
            var u = GetSession();
            if (u == null)
            {
                TempData["Error"] = "Please login to access E-Books.";
                return RedirectToAction("Login", "Account");
            }
            ViewBag.User = u;
            return View(_db.GetAllEBooks());
        }

        public IActionResult Subscriptions()
        {
            var u = GetSession();
            if (u == null)
            {
                TempData["Error"] = "Please login to access Subscriptions.";
                return RedirectToAction("Login", "Account");
            }
            ViewBag.User = u;
            return View(_db.GetSubscriptions());
        }

        // ── RATE BOOK ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult RateBook(int bookId)
        {
            var book = _db.GetBookById(bookId);
            if (book == null) return NotFound();
            ViewBag.Book = book;
            return View(new BookRating { BookId = bookId });
        }

        [HttpPost]
        public IActionResult RateBook(BookRating model)
        {
            _db.AddRating(model);
            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction("BookDetail", new { id = model.BookId });
        }

        // ── SUGGEST BOOK ──────────────────────────────────────────
        [HttpGet]
        public IActionResult SuggestBook()
        {
            return View(new BookSuggestion());
        }

        [HttpPost]
        public IActionResult SuggestBook(BookSuggestion model)
        {
            _db.SubmitSuggestion(model);
            TempData["Success"] = "Suggestion submitted successfully!";
            return RedirectToAction("Index");
        }

        // ── MY SUGGESTIONS ────────────────────────────────────────
        public IActionResult MySuggestions(string? cnic)
        {
            ViewBag.CNIC = cnic ?? "";
            if (string.IsNullOrWhiteSpace(cnic))
                return View(new List<BookSuggestion>());
            return View(_db.GetSuggestionsByCNIC(cnic));
        }

        // ── GET SECTIONS BY WING (AJAX) ───────────────────────────
        [HttpGet]
        public IActionResult GetSections(int wingId)
        {
            return Json(_db.GetSectionsByWing(wingId));
        }
    }
}