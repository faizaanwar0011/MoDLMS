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
            _db  = new DatabaseHelper(config);
            _hub = hub;
        }

        private UserSession? GetSession()
        {
            var json = HttpContext.Session.GetString("User");
            return json == null ? null : JsonSerializer.Deserialize<UserSession>(json);
        }

        private IActionResult RequireMember()
        {
            var u = GetSession();
            if (u == null) return RedirectToAction("Login", "Account");
            ViewBag.User = u;
            return null!;
        }

        // ── BROWSE ALL BOOKS (with live client-side filter) ───────────────────
        public IActionResult Search(string? q)
        {
            var check = RequireMember(); if (check != null) return check;
            ViewBag.Query = q ?? "";
            var books = _db.GetAllBooks().Where(b => b.IsActive).ToList();
            return View(books);
        }

        // ── REQUEST FORM ──────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult RequestBook(int bookId)
        {
            var check = RequireMember(); if (check != null) return check;
            var book = _db.GetBookById(bookId);
            if (book == null) return NotFound();
            if (book.AvailableCopies <= 0)
            {
                TempData["Error"] = "This book is currently not available.";
                return RedirectToAction("Search");
            }
            var vm = new BookRequestViewModel
            {
                BookId    = book.BookId,
                BookTitle = book.Title,
                Wings     = _db.GetWings(),
                Sections  = new List<Section>()
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> RequestBook(BookRequestViewModel model)
        {
            var check = RequireMember(); if (check != null) return check;

            var book = _db.GetBookById(model.BookId);
            var bookTitle = book?.Title ?? $"Book #{model.BookId}";

            var req = new BookRequest
            {
                MemberName = model.MemberName,
                CNIC = model.CNIC,
                ServiceNo = model.ServiceNo,
                WingId = model.WingId,
                SectionId = model.SectionId,
                BookId = model.BookId
            };

            var newId = _db.SubmitBookRequest(req);

            // -1 matlab pehle se book issued hai
            if (newId == -1)
            {
                // Form dobara dikhao wings ke saath
                model.Wings = _db.GetWings();
                model.Sections = _db.GetSectionsByWing(model.WingId);
                ViewBag.Error = "You already have a book issued. Please return it before requesting another one.";
                return View(model);
            }

            var notifPayload = new
            {
                requestId = newId,
                memberName = model.MemberName,
                bookId = model.BookId,
                message = $"{model.MemberName} requested \"{bookTitle}\""
            };

            await _hub.Clients.Group("Librarians").SendAsync("NewBookRequest", notifPayload);
            await _hub.Clients.Group("Admins").SendAsync("NewBookRequest", notifPayload);

            TempData["Success"] = "Your request has been submitted. The librarian will notify you shortly.";
            return RedirectToAction("RequestConfirmation", new { id = newId });
        }

        public IActionResult RequestConfirmation(int id)
        {
            var check = RequireMember(); if (check != null) return check;
            ViewBag.RequestId = id;
            return View();
        }

        // ── GET SECTIONS BY WING (AJAX) ───────────────────────────────────────
        [HttpGet]
        public IActionResult GetSections(int wingId)
        {
            var sections = _db.GetSectionsByWing(wingId);
            return Json(sections);
        }

        // My Request Status page
        public IActionResult MyRequests(string? cnic)
        {
            var check = RequireMember(); if (check != null) return check;
            ViewBag.CNIC = cnic ?? "";
            if (string.IsNullOrWhiteSpace(cnic))
                return View(new List<BookRequest>());
            var requests = _db.GetRequestsByCNIC(cnic);
            return View(requests);
        }

        // ── BOOK SUGGESTIONS ──────────────────────────────────────────

        [HttpGet]
        public IActionResult SuggestBook()
        {
            var check = RequireMember(); if (check != null) return check;
            return View(new BookSuggestion());
        }

        [HttpPost]
        public IActionResult SuggestBook(BookSuggestion model)
        {
            var check = RequireMember(); if (check != null) return check;
            _db.SubmitSuggestion(model);
            TempData["Success"] = "Your book suggestion has been submitted. We will review it shortly!";
            return RedirectToAction("MySuggestions", new { cnic = model.CNIC });
        }

        [HttpGet]
        public IActionResult MySuggestions(string? cnic)
        {
            var check = RequireMember(); if (check != null) return check;
            ViewBag.CNIC = cnic ?? "";
            if (string.IsNullOrWhiteSpace(cnic))
                return View(new List<BookSuggestion>());
            return View(_db.GetSuggestionsByCNIC(cnic));
        }
        // ── RESERVE BOOK ──────────────────────────────────────────────
        [HttpGet]
        public IActionResult ReserveBook(int bookId)
        {
            var check = RequireMember(); if (check != null) return check;
            var book = _db.GetBookById(bookId);
            if (book == null) return NotFound();
            ViewBag.Book = book;
            return View();
        }

        [HttpPost]
        public IActionResult ReserveBook(Reservation model)
        {
            var check = RequireMember(); if (check != null) return check;
            var result = _db.ReserveBook(model);
            if (result == -1)
            {
                TempData["Error"] = "You have already reserved this book.";
                return RedirectToAction("Search");
            }
            TempData["Success"] = "Book reserved! You will be notified when it becomes available.";
            return RedirectToAction("MyReservations", new { cnic = model.CNIC });
        }

        // ── MY RESERVATIONS ───────────────────────────────────────────
        public IActionResult MyReservations(string? cnic)
        {
            var check = RequireMember(); if (check != null) return check;
            ViewBag.CNIC = cnic ?? "";
            if (string.IsNullOrWhiteSpace(cnic))
                return View(new List<Reservation>());
            return View(_db.GetReservationsByCNIC(cnic));
        }

        // ── RATE BOOK ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult RateBook(int bookId)
        {
            var check = RequireMember(); if (check != null) return check;
            var book = _db.GetBookById(bookId);
            if (book == null) return NotFound();
            ViewBag.Book = book;
            return View(new BookRating { BookId = bookId });
        }

        [HttpPost]
        public IActionResult RateBook(BookRating model)
        {
            var check = RequireMember(); if (check != null) return check;
            _db.AddRating(model);
            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction("Search");
        }
    }
}
