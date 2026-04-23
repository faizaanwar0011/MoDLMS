
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MoDLibrary.DAL;
using MoDLibrary.Hubs;
using MoDLibrary.Models;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        //[HttpGet]
        //public IActionResult GetShelvesByCategory(int categoryId)
        //{
        //    var u = GetSession();
        //    if (u == null) return Unauthorized();
        //    var shelves = _db.GetShelvesByCategory(categoryId);
        //    return Json(shelves);
        //}
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

        public IActionResult Categories()
        {
            var check = RequireLibrarian(); if (check != null) return check;
            return View(_db.GetAllCategories());
        }

        [HttpPost]
        public IActionResult AddCategory(string categoryName)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            try
            {
                _db.AddCategory(categoryName);
                TempData["Success"] = "Category added — " +
                    "Code and Rack Letter auto generated. 3 shelf rows created.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("already exists")
                    ? "Category already exists."
                    : "Error adding category.";
            }
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public IActionResult UpdateCategory(int categoryId, string categoryName, bool isActive)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.UpdateCategory(categoryId, categoryName, isActive);
            TempData["Success"] = "Category updated.";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var check = RequireLibrarian(); if (check != null) return check;
            _db.DeleteCategory(id);
            TempData["Success"] = "Category deactivated.";
            return RedirectToAction("Categories");
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
        public IActionResult MemberHistory(string? search)
        {
            var check = RequireLibrarian();
            if (check != null) return check;

            ViewBag.Name = search ?? "";

            if (string.IsNullOrWhiteSpace(search))
                return View(new List<MemberHistoryRecord>());

            return View(_db.GetMemberHistory(search));
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

            // Sirf MemberName aur BookId required hain
            if (string.IsNullOrWhiteSpace(model.MemberName))
            {
                TempData["Error"] = "Member name is required.";
                return RedirectToAction("IssueBookDirect");
            }
            if (model.BookId == 0)
            {
                TempData["Error"] = "Please select a book.";
                return RedirectToAction("IssueBookDirect");
            }

            // Optional fields
            if (string.IsNullOrWhiteSpace(model.CNIC))
                model.CNIC = "—";
            if (string.IsNullOrWhiteSpace(model.ServiceNo))
                model.ServiceNo = "—";
            if (model.WingId == 0) model.WingId = 1;
            if (model.SectionId == 0) model.SectionId = 1;

            var result = _db.LibrarianIssueBook(model);
            if (result == -1)
            {
                TempData["Error"] = "Book is not available.";
                return RedirectToAction("IssueBookDirect");
            }
            if (result == -2)
            {
                TempData["Error"] = "This member already has a book issued.";
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

        //public IActionResult TodayReturned()
        //  {
        //      var check = RequireLibrarian();
        //      if (check != null) return check;

        //      var data = _db.GetTodayReturnedBooks();
        //      return View(data);
        //  }

        public IActionResult TodayReturned()
        {
            var data = _db.GetTodayReturnedBooks();
            return View(data);
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
            var check = RequireLibrarian();
            if (check != null) return check;

            // Load data
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

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);

                    page.Content().Column(col =>
                    {
                        col.Item().Text("MoD Library Report")
                            .FontSize(20).Bold().AlignCenter();

                        col.Item().Text(
                            $"{model.ReportType} | {model.StartDate:dd MMM yyyy} - {model.EndDate:dd MMM yyyy}")
                            .FontSize(11).AlignCenter();

                        col.Item().PaddingVertical(10);

                        // Summary
                        col.Item().Row(row =>
                        {
                            void Box(string title, string value)
                            {
                                row.RelativeItem().Border(1).Padding(8).Column(c =>
                                {
                                    c.Item().Text(value).Bold().FontSize(14);
                                    c.Item().Text(title).FontSize(9);
                                });
                            }

                            Box("Issued", model.Summary.TotalIssued.ToString());
                            Box("Returned", model.Summary.TotalReturned.ToString());
                            Box("Overdue", model.Summary.CurrentOverdue.ToString());
                            Box("Pending", $"Rs. {model.Summary.PendingFines:N0}");
                            Box("Collected", $"Rs. {model.Summary.CollectedFines:N0}");
                        });

                        col.Item().PaddingVertical(10);

                        // Issued
                        if (model.ReportType == "Issued" && model.IssuedBooks != null)
                        {
                            col.Item().Text("Issued Books").Bold();

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Border(1).Padding(5).Text("Member").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Book").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Status").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Issue Date").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Due Date").Bold();
                                });

                                foreach (var r in model.IssuedBooks)
                                {
                                    table.Cell().Border(1).Padding(5).Text(r.MemberName);
                                    table.Cell().Border(1).Padding(5).Text(r.BookTitle);
                                    table.Cell().Border(1).Padding(5).Text(r.BookStatus);
                                    table.Cell().Border(1).Padding(5).Text(r.IssueDate.ToString("dd MMM yyyy"));
                                    table.Cell().Border(1).Padding(5).Text(r.DueDate.ToString("dd MMM yyyy"));
                                }
                            });
                        }

                        // Fines
                        if (model.ReportType == "Fines" && model.Fines != null)
                        {
                            col.Item().Text("Fines Report").Bold();

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Border(1).Padding(5).Text("Member").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Book").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Days Late").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Fine").Bold();
                                });

                                foreach (var f in model.Fines)
                                {
                                    table.Cell().Border(1).Padding(5).Text(f.MemberName);
                                    table.Cell().Border(1).Padding(5).Text(f.BookTitle);
                                    table.Cell().Border(1).Padding(5).Text(f.DaysLate.ToString());
                                    table.Cell().Border(1).Padding(5).Text($"Rs. {f.FineAmount:N0}");
                                }
                            });
                        }

                        // Daily
                        if (model.ReportType == "Daily" && model.DailyActivity != null)
                        {
                            col.Item().Text("Daily Activity").Bold();

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Border(1).Padding(5).Text("Activity").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Member").Bold();
                                    h.Cell().Border(1).Padding(5).Text("Book").Bold();
                                });

                                foreach (var a in model.DailyActivity)
                                {
                                    table.Cell().Border(1).Padding(5).Text(a.ActivityType);
                                    table.Cell().Border(1).Padding(5).Text(a.MemberName);
                                    table.Cell().Border(1).Padding(5).Text(a.BookTitle);
                                }
                            });
                        }

                        col.Item().AlignCenter().Text($"Generated: {DateTime.Now:dd MMM yyyy hh:mm tt}")
                            .FontSize(9);
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf",
                $"MoDLibrary_{model.ReportType}_Report_{DateTime.Now:yyyy-MM-dd}.pdf");
        }

       
    }
    }
//75734-7457567-5 
//86785-5758684-8
