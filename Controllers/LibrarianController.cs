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
        // ── BOOKS — updated with cover + shelf ───────────────────────

        [HttpGet]
        public IActionResult AddBook()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            ViewBag.Categories = _db.GetCategories();
            ViewBag.Shelves = new List<Shelf>();
            return View(new Book());
        }

        [HttpPost]
        public async Task<IActionResult> AddBook(Book model, IFormFile? coverImage)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var coverPath = await SaveImage(coverImage, "covers");
            // Auto generate book ID
            model.BookNumber = _db.GenerateBookId(model.CategoryId);
            _db.AddBook(model, coverPath);
            TempData["Success"] = "Book added. ID: " + model.BookNumber;
            return RedirectToAction("Books");
        }

        [HttpGet]
        public IActionResult EditBook(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var book = _db.GetBookById(id);
            if (book == null) return NotFound();
            ViewBag.Categories = _db.GetCategories();
            ViewBag.Shelves = _db.GetShelvesByCategory(book.CategoryId);
            return View(book);
        }

        [HttpPost]
        public async Task<IActionResult> EditBook(Book model, IFormFile? coverImage)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var coverPath = await SaveImage(coverImage, "covers");
            _db.UpdateBook(model, coverPath);
            TempData["Success"] = "Book updated.";
            return RedirectToAction("Books");
        }
        [HttpPost]
        public IActionResult DeleteBook(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.DeleteBook(id);
            TempData["Success"] = "Book removed from catalog.";
            return RedirectToAction("Books");
        }

        // ── EBOOKS ────────────────────────────────────────────────────

        public IActionResult EBooks()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllEBooks());
        }

        [HttpGet]
        public IActionResult AddEBook()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            ViewBag.Categories = _db.GetCategories();
            return View(new EBook());
        }

        [HttpPost]
        public async Task<IActionResult> AddEBook(EBook model, IFormFile? pdfFile, IFormFile? coverImage)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            if (pdfFile == null)
            {
                TempData["Error"] = "PDF file is required.";
                return RedirectToAction("EBooks");
            }
            var filePath = await SaveFile(pdfFile, "ebooks");
            var coverPath = await SaveImage(coverImage, "covers");
            model.FileSize = (pdfFile.Length / 1024.0 / 1024.0).ToString("F1") + " MB";
            _db.AddEBook(model, filePath, coverPath);
            TempData["Success"] = "E-Book uploaded successfully.";
            return RedirectToAction("EBooks");
        }

        [HttpPost]
        public IActionResult DeleteEBook(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.DeleteEBook(id);
            TempData["Success"] = "E-Book removed.";
            return RedirectToAction("EBooks");
        }

        // ── SUBSCRIPTIONS ─────────────────────────────────────────────

        public IActionResult Subscriptions()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllSubscriptions());
        }

        [HttpPost]
        public async Task<IActionResult> AddSubscription(LibrarySubscription model, IFormFile? logo)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var logoPath = await SaveImage(logo, "logos");
            _db.AddSubscription(model, logoPath);
            TempData["Success"] = "Subscription added.";
            return RedirectToAction("Subscriptions");
        }

        [HttpPost]
        public IActionResult UpdateSubscription(LibrarySubscription model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.UpdateSubscription(model);
            TempData["Success"] = "Subscription updated.";
            return RedirectToAction("Subscriptions");
        }

        [HttpPost]
        public IActionResult DeleteSubscription(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.DeleteSubscription(id);
            TempData["Success"] = "Subscription removed.";
            return RedirectToAction("Subscriptions");
        }

        // ── SHELVES ───────────────────────────────────────────────────

        public IActionResult Shelves()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllShelves());
        }

        // ── FILE UPLOAD HELPERS ───────────────────────────────────────

        private async Task<string> SaveImage(IFormFile? file, string folder)
        {
            if (file == null || file.Length == 0) return "";
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
            Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
            Directory.CreateDirectory(uploads);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }

        // ── AJAX — Get Shelves by Category ───────────────────────────
        [HttpGet]
        public IActionResult GetShelvesByCategory(int categoryId)
        {
            var u = GetSession();
            if (u == null) return Unauthorized();
            var shelves = _db.GetShelvesByCategory(categoryId);
            return Json(shelves);
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
        public IActionResult MemberHistory(string? name)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            ViewBag.MemberName = name ?? "";
            if (string.IsNullOrWhiteSpace(name))
                return View(new List<MemberHistoryRecord>());
            return View(_db.GetMemberHistory(name));
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
        // ── DIRECT ISSUE BOOK ─────────────────────────────────────────

        [HttpGet]
        public IActionResult IssueBookDirect()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            ViewBag.Wings = _db.GetWings();
          //  ViewBag.Wings.Sections=_db.GetSectionsByWingId();
            ViewBag.Books = _db.GetAllBooks().Where(b => b.IsActive && b.AvailableCopies > 0).ToList();
            return View(new BookRequest());
        }

        [HttpPost]
        public IActionResult IssueBookDirect(BookRequest model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var result = _db.LibrarianIssueBook(model);
            if (result == -1)
            {
                TempData["Error"] = "Book is not available.";
                return RedirectToAction("IssueBookDirect");
            }
            if (result == -2)
            {
                TempData["Error"] = "This member already has a book issued. Return first.";
                return RedirectToAction("IssueBookDirect");
            }
            TempData["Success"] = "Book issued successfully! Due date: " +
                DateTime.Now.AddDays(15).ToString("dd MMM yyyy");
            return RedirectToAction("IssuedBooks");
        }
        public IActionResult Books()
        {
            var check = RequireLibrarian();
            if (check != null) return check;

            var books = _db.GetAllBooks();
            return View(books);
        }


        [HttpGet]
        public IActionResult GetBookId(int categoryId)
        {
            var u = GetSession();
            if (u == null) return Unauthorized();
            var id = _db.GenerateBookId(categoryId);
            return Json(new { bookId = id });
        }
        [HttpGet]
        public IActionResult GetSections(int wingId)
        {
            var sections= _db.GetSectionsByWing(wingId);
            return Json(sections);
        }
        }
    }
//75734-7457567-5 
//86785-5758684-8
