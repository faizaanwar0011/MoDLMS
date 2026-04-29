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
            if (u == null || u.Role != "Member")
                return RedirectToAction("MemberLogin", "Account");
            ViewBag.User = u;
            return View(_db.GetAllEBooks());
        }

        //public IActionResult Subscriptions()
        //{
        //    var u = GetSession();
        //    if (u == null || u.Role != "Member")
        //        return RedirectToAction("MemberLogin", "Account");
        //    ViewBag.User = u;
        //    return View(_db.GetSubscriptions());
        //}
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


        // ── SUBSCRIPTIONS WITH SESSION ────────────────────────────────

        public IActionResult Subscriptions()
        {
            var u = GetSession();
            if (u == null || u.Role != "Member")
                return RedirectToAction("MemberLogin", "Account");
            ViewBag.User = u;
            ViewBag.Settings = _db.GetSessionSettings();

            // Check current session status
            var status = _db.CheckSessionStatus(u.UserId);
            ViewBag.SessionStatus = status;

            return View(_db.GetSubscriptions());
        }

        [HttpPost]
        public IActionResult StartSession(int subscriptionId)
        {
            var u = GetSession();
            if (u == null || u.Role != "Member")
                return RedirectToAction("MemberLogin", "Account");

            var result = _db.StartSession(u.UserId, subscriptionId);

            if (result.Status == "GRANTED")
            {
                // Store session in browser session
                HttpContext.Session.SetInt32("SessionId", result.SessionId);
                HttpContext.Session.SetString("SessionEnd",
                    result.EndTime.ToString("o"));
                return RedirectToAction("ActiveSession",
                    new { sessionId = result.SessionId });
            }
            else if (result.Status == "ACTIVE")
            {
                return RedirectToAction("ActiveSession",
                    new { sessionId = result.SessionId });
            }
            else if (result.Status == "QUEUED")
            {
                TempData["QueuePosition"] = result.QueuePosition;
                TempData["WaitMinutes"] = result.WaitMinutes;
                return RedirectToAction("QueueStatus");
            }

            TempData["Error"] = "Unable to start session. Please try again.";
            return RedirectToAction("Subscriptions");
        }

        public IActionResult ActiveSession(int sessionId)
        {
            var u = GetSession();
            if (u == null || u.Role != "Member")
                return RedirectToAction("MemberLogin", "Account");

            var status = _db.CheckSessionStatus(u.UserId);
            if (status.Status != "ACTIVE")
                return RedirectToAction("SessionEnded");

            ViewBag.User = u;
            ViewBag.Status = status;
            return View();
        }

        [HttpPost]
        public IActionResult EndSession(int sessionId)
        {
            _db.EndSession(sessionId);
            HttpContext.Session.Remove("SessionId");
            HttpContext.Session.Remove("SessionEnd");
            return RedirectToAction("SessionEnded");
        }

        public IActionResult SessionEnded()
        {
            return View();
        }

        public IActionResult QueueStatus()
        {
            var u = GetSession();
            if (u == null || u.Role != "Member")
                return RedirectToAction("MemberLogin", "Account");

            var status = _db.CheckSessionStatus(u.UserId);
            ViewBag.User = u;
            ViewBag.Status = status;
            return View();
        }

        // AJAX — check session status
        [HttpGet]
        public IActionResult CheckSession()
        {
            var u = GetSession();
            if (u == null) return Json(new { status = "NONE" });
            var status = _db.CheckSessionStatus(u.UserId);
            return Json(new
            {
                status = status.Status,
                secondsRemaining = status.SecondsRemaining,
                sessionId = status.SessionId,
                queuePosition = status.QueuePosition
            });
        }
    }
}