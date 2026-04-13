using DinkToPdf;
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
            _db = new DatabaseHelper(config);
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
                OverdueBooks = _db.GetOverdueBooks(),
                TodayIssuedBooks = _db.GetTodayIssuedBooks()
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
                status = model.Status,
                remark = model.Remark
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
            var sections = _db.GetSectionsByWing(wingId);
            return Json(sections);
        }


        // ── REPORTS ───────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Reports()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var vm = new ReportViewModel
            {
                StartDate = DateTime.Now.AddMonths(-1),
                EndDate = DateTime.Now,
                Summary = _db.GetReportSummary(
                                DateTime.Now.AddMonths(-1), DateTime.Now),
                IssuedBooks = _db.GetIssuedBooksReport(
                                DateTime.Now.AddMonths(-1), DateTime.Now, "All")
            };
            return View(vm);
        }

        [HttpPost]
        public IActionResult Reports(ReportViewModel model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            model.Summary = _db.GetReportSummary(model.StartDate, model.EndDate);

            switch (model.ReportType)
            {
                case "Issued":
                    model.IssuedBooks = _db.GetIssuedBooksReport(
                        model.StartDate, model.EndDate, model.StatusFilter);
                    break;
                case "Fines":
                    model.Fines = _db.GetFinesReport(
                        model.StartDate, model.EndDate, model.StatusFilter);
                    break;
                case "Daily":
                    model.DailyActivity = _db.GetDailyActivityReport(model.StartDate);
                    break;
            }
            return View(model);
        }
        // ── MEMBERS MANAGEMENT ────────────────────────────────────────

        public IActionResult Members()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllMembers());
        }

        [HttpGet]
        public IActionResult AddMember()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(new AddMemberViewModel());
        }

        [HttpPost]
        public IActionResult AddMember(AddMemberViewModel model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            try
            {
                _db.AddMember(model.FullName, model.Username, model.Password);
                TempData["Success"] = "Member account created successfully.";
            }
            catch
            {
                TempData["Error"] = "Username already exists. Try a different one.";
            }
            return RedirectToAction("Members");
        }

        [HttpGet]
        public IActionResult EditMember(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            var member = _db.GetAllMembers().FirstOrDefault(m => m.MemberId == id);
            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost]
        public IActionResult EditMember(Member model)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.UpdateMember(model.MemberId, model.FullName, model.Username, model.IsActive);
            TempData["Success"] = "Member updated successfully.";
            return RedirectToAction("Members");
        }

        [HttpPost]
        public IActionResult ResetMemberPassword(int memberId, string newPassword)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.ResetMemberPassword(memberId, newPassword);
            TempData["Success"] = "Password reset successfully.";
            return RedirectToAction("Members");
        }

        [HttpPost]
        public IActionResult DeleteMember(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.DeleteMember(id);
            TempData["Success"] = "Member deactivated.";
            return RedirectToAction("Members");
        }
        // PDF Download
        [HttpPost]
        public IActionResult DownloadReport(ReportViewModel model)
        {
            var check = RequireLibrarian(); if (check != null) return check;

            // Data load karo
            model.Summary = _db.GetReportSummary(model.StartDate, model.EndDate);
            switch (model.ReportType)
            {
                case "Issued":
                    model.IssuedBooks = _db.GetIssuedBooksReport(
                        model.StartDate, model.EndDate, model.StatusFilter);
                    break;
                case "Fines":
                    model.Fines = _db.GetFinesReport(
                        model.StartDate, model.EndDate, model.StatusFilter);
                    break;
                case "Daily":
                    model.DailyActivity = _db.GetDailyActivityReport(model.StartDate);
                    break;
            }

            // HTML banao
            var html = GenerateReportHtml(model);

            // PDF banao
            var converter = HttpContext.RequestServices
                .GetService<DinkToPdf.Contracts.IConverter>()!;

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
            ColorMode   = ColorMode.Color,
            Orientation = Orientation.Landscape,
            PaperSize   = PaperKind.A4,
            Margins     = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
        },
                Objects = {
            new ObjectSettings {
                HtmlContent = html,
                WebSettings = { DefaultEncoding = "utf-8" }
            }
        }
            };

            var pdf = converter.Convert(doc);
            var fileName = $"MoDLibrary_{model.ReportType}_Report_" +
                           $"{DateTime.Now:yyyy-MM-dd}.pdf";

            return File(pdf, "application/pdf", fileName);
        }

        private string GenerateReportHtml(ReportViewModel model)
        {
            var sb = new System.Text.StringBuilder();

            sb.Append(@"
    <html>
    <head>
    <style>
        body { font-family: Arial, sans-serif; font-size: 11px; color: #333; }
        h2 { text-align: center; color: #0a1628; margin-bottom: 4px; }
        h3 { text-align: center; color: #555; margin-bottom: 4px; font-weight: normal; }
        p  { text-align: center; color: #777; font-size: 10px; margin-bottom: 16px; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th { background: #0a1628; color: white; padding: 7px 8px;
             text-align: left; font-size: 10px; }
        td { padding: 6px 8px; border-bottom: 1px solid #e2e8f0; }
        tr:nth-child(even) { background: #f8fafc; }
        .badge-active   { color: #16a34a; font-weight: bold; }
        .badge-overdue  { color: #dc2626; font-weight: bold; }
        .badge-returned { color: #6c757d; font-weight: bold; }
        .badge-issued   { color: #0d6efd; font-weight: bold; }
        .summary-box {
            display: inline-block; background: #f8fafc;
            border: 1px solid #e2e8f0; border-radius: 6px;
            padding: 8px 16px; margin: 4px; text-align: center;
        }
        .summary-box .num  { font-size: 18px; font-weight: bold; color: #0a1628; }
        .summary-box .lbl  { font-size: 9px; color: #6c757d; text-transform: uppercase; }
        .summary-wrap { text-align: center; margin-bottom: 16px; }
        tfoot td { font-weight: bold; background: #f1f5f9; }
        .header-line { border-top: 3px solid #c9a84c; margin: 8px 0; }
    </style>
    </head>
    <body>
    ");

            // Header
            sb.Append($@"
        <h2>Ministry of Defence — MoDLibrary</h2>
        <h3>{(model.ReportType == "Issued" ? "Issued Books Report" :
                      model.ReportType == "Fines" ? "Fines Report" :
                      "Daily Activity Report")}</h3>
        <p>
            Period: {model.StartDate:dd MMM yyyy} — {model.EndDate:dd MMM yyyy}
            &nbsp;|&nbsp; Generated: {DateTime.Now:dd MMM yyyy hh:mm tt}
        </p>
        <div class='header-line'></div>
    ");

            // Summary
            sb.Append($@"
        <div class='summary-wrap'>
            <div class='summary-box'>
                <div class='num'>{model.Summary.TotalIssued}</div>
                <div class='lbl'>Total Issued</div>
            </div>
            <div class='summary-box'>
                <div class='num'>{model.Summary.TotalReturned}</div>
                <div class='lbl'>Returned</div>
            </div>
            <div class='summary-box'>
                <div class='num' style='color:#dc2626;'>
                    {model.Summary.CurrentOverdue}
                </div>
                <div class='lbl'>Overdue</div>
            </div>
            <div class='summary-box'>
                <div class='num' style='color:#dc2626;'>
                    Rs. {model.Summary.PendingFines:N0}
                </div>
                <div class='lbl'>Pending Fines</div>
            </div>
            <div class='summary-box'>
                <div class='num' style='color:#16a34a;'>
                    Rs. {model.Summary.CollectedFines:N0}
                </div>
                <div class='lbl'>Collected Fines</div>
            </div>
        </div>
    ");

            // Issued Books Table
            if (model.ReportType == "Issued" && model.IssuedBooks.Any())
            {
                sb.Append(@"
        <table>
            <thead>
                <tr>
                    <th>Member Name</th>
                    <th>CNIC</th>
                    <th>Service No</th>
                    <th>Wing</th>
                    <th>Section</th>
                    <th>Book Title</th>
                    <th>Book No</th>
                    <th>Issue Date</th>
                    <th>Due Date</th>
                    <th>Return Date</th>
                    <th>Status</th>
                    <th>Fine</th>
                </tr>
            </thead>
            <tbody>
        ");

                foreach (var r in model.IssuedBooks)
                {
                    var statusClass = r.BookStatus == "Active" ? "badge-active" :
                                      r.BookStatus == "Overdue" ? "badge-overdue" :
                                                                   "badge-returned";
                    sb.Append($@"
            <tr>
                <td>{r.MemberName}</td>
                <td>{r.CNIC}</td>
                <td>{r.ServiceNo}</td>
                <td>{r.WingName}</td>
                <td>{r.SectionName}</td>
                <td>{r.BookTitle}</td>
                <td>{r.BookNumber}</td>
                <td>{r.IssueDate:dd MMM yyyy}</td>
                <td>{r.DueDate:dd MMM yyyy}</td>
                <td>{(r.ReturnDate.HasValue ? r.ReturnDate.Value.ToString("dd MMM yyyy") : "—")}</td>
                <td class='{statusClass}'>{r.BookStatus}</td>
                <td>{(r.FineAmount > 0 ? $"Rs. {r.FineAmount:N0}" : "—")}</td>
            </tr>
            ");
                }

                sb.Append($@"
            </tbody>
            <tfoot>
                <tr>
                    <td colspan='11' style='text-align:right;'>Total Fines:</td>
                    <td style='color:#dc2626;'>
                        Rs. {model.IssuedBooks.Sum(x => x.FineAmount):N0}
                    </td>
                </tr>
            </tfoot>
        </table>
        ");
            }

            // Fines Table
            if (model.ReportType == "Fines" && model.Fines.Any())
            {
                sb.Append(@"
        <table>
            <thead>
                <tr>
                    <th>Member Name</th>
                    <th>CNIC</th>
                    <th>Service No</th>
                    <th>Book Title</th>
                    <th>Book No</th>
                    <th>Issue Date</th>
                    <th>Due Date</th>
                    <th>Return Date</th>
                    <th>Days Late</th>
                    <th>Fine Amount</th>
                    <th>Status</th>
                    <th>Paid Date</th>
                </tr>
            </thead>
            <tbody>
        ");

                foreach (var f in model.Fines)
                {
                    sb.Append($@"
            <tr>
                <td>{f.MemberName}</td>
                <td>{f.CNIC}</td>
                <td>{f.ServiceNo}</td>
                <td>{f.BookTitle}</td>
                <td>{f.BookNumber}</td>
                <td>{f.IssueDate:dd MMM yyyy}</td>
                <td style='color:#dc2626;'>{f.DueDate:dd MMM yyyy}</td>
                <td>{(f.ReturnDate.HasValue ? f.ReturnDate.Value.ToString("dd MMM yyyy") : "—")}</td>
                <td style='color:#dc2626;font-weight:bold;'>{f.DaysLate}</td>
                <td style='color:#dc2626;font-weight:bold;'>Rs. {f.FineAmount:N0}</td>
                <td class='{(f.IsPaid ? "badge-active" : "badge-overdue")}'>
                    {(f.IsPaid ? "Paid" : "Unpaid")}
                </td>
                <td>{(f.PaidDate.HasValue ? f.PaidDate.Value.ToString("dd MMM yyyy") : "—")}</td>
            </tr>
            ");
                }

                sb.Append($@"
            </tbody>
            <tfoot>
                <tr>
                    <td colspan='9' style='text-align:right;'>Total:</td>
                    <td style='color:#dc2626;'>
                        Rs. {model.Fines.Sum(f => f.FineAmount):N0}
                    </td>
                    <td colspan='2'></td>
                </tr>
            </tfoot>
        </table>
        ");
            }

            // Daily Activity Table
            if (model.ReportType == "Daily" && model.DailyActivity.Any())
            {
                sb.Append(@"
        <table>
            <thead>
                <tr>
                    <th>Activity</th>
                    <th>Member Name</th>
                    <th>CNIC</th>
                    <th>Service No</th>
                    <th>Book Title</th>
                    <th>Book No</th>
                    <th>Time</th>
                    <th>Due Date</th>
                </tr>
            </thead>
            <tbody>
        ");

                foreach (var a in model.DailyActivity)
                {
                    var actClass = a.ActivityType == "Issued" ? "badge-issued" : "badge-active";
                    sb.Append($@"
            <tr>
                <td class='{actClass}'>{a.ActivityType}</td>
                <td>{a.MemberName}</td>
                <td>{a.CNIC}</td>
                <td>{a.ServiceNo}</td>
                <td>{a.BookTitle}</td>
                <td>{a.BookNumber}</td>
                <td>{a.ActivityDate:hh:mm tt}</td>
                <td>{a.DueDate:dd MMM yyyy}</td>
            </tr>
            ");
                }

                sb.Append(@"
            </tbody>
        </table>
        ");
            }

            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
    }
//75734-7457567-5 
//86785-5758684-8
