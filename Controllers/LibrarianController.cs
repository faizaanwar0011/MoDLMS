using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoDLibrary.DAL;
using MoDLibrary.Hubs;
using MoDLibrary.Models;
using System.Text.Json;

namespace MoDLibrary.Controllers
{
    public class LibrarianController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly IHubContext<NotificationHub> _hub;

        public LibrarianController(IConfiguration config, IHubContext<NotificationHub> hub)
        {
            _db  = new DatabaseHelper(config);
            _hub = hub;
        }

        private UserSession? GetSession()
        {
            var json = HttpContext.Session.GetString("User");
            return json == null ? null : JsonSerializer.Deserialize<UserSession>(json);
        }

        private IActionResult RequireLibrarian()
        {
            var u = GetSession();
            if (u == null) return RedirectToAction("Login", "Account");
            if (u.Role != "Librarian" && u.Role != "Admin") return RedirectToAction("AccessDenied", "Account");
            ViewBag.User = u;
            return null!;
        }

        // ── DASHBOARD ─────────────────────────────────────────────────────────
        public IActionResult Dashboard()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var vm = new LibrarianDashboardViewModel
            {
                Stats = _db.GetDashboardStats(),
                PendingRequests = _db.GetPendingRequestsOnly(),
                OverdueBooks = _db.GetOverdueBooks()
            };
            return View(vm);
        }

        // ── BOOK SEARCH ───────────────────────────────────────────────────────
        public IActionResult Search(string? q)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            ViewBag.Query = q;
            if (string.IsNullOrWhiteSpace(q))
                return View(new List<Book>());
            var books = _db.SearchBooks(q);
            return View(books);
        }

        // ── ALL BOOKS ─────────────────────────────────────────────────────────
        public IActionResult Books()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllBooks());
        }

        [HttpGet]
        public IActionResult AddBook()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            ViewBag.Categories = _db.GetCategories();
            return View(new Book());
        }

        [HttpPost]
        public IActionResult AddBook(Book model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.AddBook(model);
            TempData["Success"] = "Book added to catalog.";
            return RedirectToAction("Books");
        }
        [HttpGet]
        public IActionResult EditBook(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var book = _db.GetBookById(id);
            if (book == null) return NotFound();
            ViewBag.Categories = _db.GetCategories();
            return View(book);
        }

        [HttpPost]
        public IActionResult EditBook(Book model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.UpdateBook(model);
            TempData["Success"] = "Book updated.";
            return RedirectToAction("Books");
        }

        // ── REQUESTS ─────────────────────────────────────────────────────────
        public IActionResult Requests()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var requests = _db.GetAllRequests();
            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> SendRemark(RemarkViewModel model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.UpdateRequestStatus(model.RequestId, model.Status, model.Remark);

            // Notify member via SignalR (broadcast to Members group)
            await _hub.Clients.Group("Members").SendAsync("ReceiveRemark", new
            {
                requestId = model.RequestId,
                status    = model.Status,
                remark    = model.Remark
            });

            TempData["Success"] = "Remark sent successfully.";
            return RedirectToAction("Requests");
        }

        // ── ISSUE BOOK ────────────────────────────────────────────────────────
        [HttpPost]
        public IActionResult IssueBook(int requestId)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.IssueBook(requestId);
            TempData["Success"] = "Book marked as issued. Due date set to 15 days from today.";
            return RedirectToAction("Requests");
        }

        // ── ISSUED BOOKS ──────────────────────────────────────────────────────
        public IActionResult IssuedBooks()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllIssuedBooks());
        }

        // ── RETURN BOOK ───────────────────────────────────────────────────────
        [HttpPost]
        public IActionResult ReturnBook(int issuedId)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var result = _db.ReturnBook(issuedId);
            TempData["Success"] = result.FineAmount > 0
                ? $"Book returned. Fine of Rs. {result.FineAmount:N0} applied ({result.DaysLate} days late)."
                : "Book returned on time. No fine applied.";
            return RedirectToAction("IssuedBooks");
        }

        // ── FINES ─────────────────────────────────────────────────────────────
        public IActionResult Fines()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetFines());
        }

        [HttpPost]
        public IActionResult MarkFinePaid(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.MarkFinePaid(id);
            TempData["Success"] = "Fine marked as paid.";
            return RedirectToAction("Fines");
        }

        // ── NOTIFICATIONS (API for SignalR poll) ──────────────────────────────
        [HttpGet]
        public IActionResult GetNotifications()
        {
            var u = GetSession();
            if (u == null) return Unauthorized();
            var notifs = _db.GetUnreadNotifications();
            return Json(notifs);
        }

        [HttpPost]
        public IActionResult MarkNotificationRead(int id)
        {
            var u = GetSession();
            if (u == null) return Unauthorized();
            _db.MarkNotificationRead(id);
            return Ok();
        }

        public IActionResult Suggestions()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllSuggestions());
        }

        [HttpPost]
        public IActionResult UpdateSuggestion(SuggestionStatusViewModel model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.UpdateSuggestionStatus(model.SuggestionId, model.Status, model.AdminRemark);
            TempData["Success"] = "Suggestion updated.";
            return RedirectToAction("Suggestions");
        }
        // ── ANNOUNCEMENTS ─────────────────────────────────────────────
        public IActionResult Announcements()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllAnnouncements());
        }

        [HttpPost]
        public IActionResult AddAnnouncement(Announcement model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var u = GetSession();
            model.PostedBy = u?.FullName ?? "Librarian";
            _db.AddAnnouncement(model);
            TempData["Success"] = "Announcement posted.";
            return RedirectToAction("Announcements");
        }

        [HttpPost]
        public IActionResult DeleteAnnouncement(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.DeleteAnnouncement(id);
            TempData["Success"] = "Announcement removed.";
            return RedirectToAction("Announcements");
        }

        // ── RESERVATIONS ──────────────────────────────────────────────
        public IActionResult Reservations()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllReservations());
        }

        [HttpPost]
        public IActionResult UpdateReservation(int reservationId, string status)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.UpdateReservationStatus(reservationId, status);
            TempData["Success"] = "Reservation updated.";
            return RedirectToAction("Reservations");
        }

        // ── MEMBER HISTORY ────────────────────────────────────────────
        public IActionResult MemberHistory(string? cnic)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            ViewBag.CNIC = cnic ?? "";
            if (string.IsNullOrWhiteSpace(cnic))
                return View(new List<MemberHistoryRecord>());
            return View(_db.GetMemberHistory(cnic));
        }

        // ── RATINGS ───────────────────────────────────────────────────
        public IActionResult Ratings()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllRatings());
        }

        // ── POPULAR BOOKS ─────────────────────────────────────────────
        public IActionResult PopularBooks()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetPopularBooks());
        }
    }
}
