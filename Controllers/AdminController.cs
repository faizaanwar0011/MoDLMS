using DinkToPdf;
using Microsoft.AspNetCore.Mvc;
using MoDLibrary.DAL;
using MoDLibrary.Models;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

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
            ViewBag.Shelves = new List<Shelf>();
            return View(new Book());
        }

        [HttpPost]
        public async Task<IActionResult> AddBook(Book model, IFormFile? coverImage)
        {
            var check = RequireAdmin(); if (check != null) return check;
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
            var check = RequireAdmin(); if (check != null) return check;
            var book = _db.GetBookById(id);
            if (book == null) return NotFound();
            ViewBag.Categories = _db.GetCategories();
            ViewBag.Shelves = _db.GetShelvesByCategory(book.CategoryId);
            return View(book);
        }

        [HttpPost]
        public async Task<IActionResult> EditBook(Book model, IFormFile? coverImage)
        {
            var check = RequireAdmin(); if (check != null) return check;
            var coverPath = await SaveImage(coverImage, "covers");
            _db.UpdateBook(model, coverPath);
            TempData["Success"] = "Book updated.";
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

        // ── EBOOKS ────────────────────────────────────────────────────

        public IActionResult EBooks()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetAllEBooks());
        }

        [HttpGet]
        public IActionResult AddEBook()
        {
            var check = RequireAdmin(); if (check != null) return check;
            ViewBag.Categories = _db.GetCategories();
            return View(new EBook());
        }

        [HttpPost]
        public async Task<IActionResult> AddEBook(EBook model, IFormFile? pdfFile, IFormFile? coverImage)
        {
            var check = RequireAdmin(); if (check != null) return check;
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
            var check = RequireAdmin(); if (check != null) return check;
            _db.DeleteEBook(id);
            TempData["Success"] = "E-Book removed.";
            return RedirectToAction("EBooks");
        }
        // ── SUBSCRIPTIONS ─────────────────────────────────────────────

        public IActionResult Subscriptions()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetAllSubscriptions());
        }

        [HttpPost]
        public async Task<IActionResult> AddSubscription(LibrarySubscription model, IFormFile? logo)
        {
            var check = RequireAdmin(); if (check != null) return check;
            var logoPath = await SaveImage(logo, "logos");
            _db.AddSubscription(model, logoPath);
            TempData["Success"] = "Subscription added.";
            return RedirectToAction("Subscriptions");
        }

        [HttpPost]
        public IActionResult UpdateSubscription(LibrarySubscription model)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.UpdateSubscription(model);
            TempData["Success"] = "Subscription updated.";
            return RedirectToAction("Subscriptions");
        }

        [HttpPost]
        public IActionResult DeleteSubscription(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.DeleteSubscription(id);
            TempData["Success"] = "Subscription removed.";
            return RedirectToAction("Subscriptions");
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
        // ── SHELVES ───────────────────────────────────────────────────

        public IActionResult Shelves()
        {
            var check = RequireAdmin(); if (check != null) return check;
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

        // ── CATEGORIES ────────────────────────────────────────────────
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

        [HttpGet]
        public IActionResult GetBookId(int categoryId)
        {
            var u = GetSession();
            if (u == null) return Unauthorized();
            var id = _db.GenerateBookId(categoryId);
            return Json(new { bookId = id });
        }

        // ── REPORTS ───────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Reports()
        {
            var check = RequireAdmin(); if (check != null) return check;
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
            var check = RequireAdmin(); if (check != null) return check;
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
            var check = RequireAdmin(); if (check != null) return check;
            return View(_db.GetAllMembers());
        }

        [HttpGet]
        public IActionResult AddMember()
        {
            var check = RequireAdmin(); if (check != null) return check;
            return View(new AddMemberViewModel());
        }

        [HttpPost]
        public IActionResult AddMember(AddMemberViewModel model)
        {
            var check = RequireAdmin(); if (check != null) return check;
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
            var check = RequireAdmin(); if (check != null) return check;
            var member = _db.GetAllMembers().FirstOrDefault(m => m.MemberId == id);
            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost]
        public IActionResult EditMember(Member model)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.UpdateMember(model.MemberId, model.FullName, model.Username, model.IsActive);
            TempData["Success"] = "Member updated successfully.";
            return RedirectToAction("Members");
        }

        [HttpPost]
        public IActionResult ResetMemberPassword(int memberId, string newPassword)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.ResetMemberPassword(memberId, newPassword);
            TempData["Success"] = "Password reset successfully.";
            return RedirectToAction("Members");
        }

        [HttpPost]
        public IActionResult DeleteMember(int id)
        {
            var check = RequireAdmin(); if (check != null) return check;
            _db.DeleteMember(id);
            TempData["Success"] = "Member deactivated.";
            return RedirectToAction("Members");
        }

        // PDF Download

        [HttpPost]
        public IActionResult DownloadReport(ReportViewModel model)
        {
            var check = RequireAdmin(); if (check != null) return check;

            // Data load
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

            using var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            pdf.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4.Rotate());
            var doc = new Document(pdf);
            doc.SetMargins(20, 20, 20, 20);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var navyColor = new DeviceRgb(10, 27, 40);
            var goldColor = new DeviceRgb(201, 168, 76);
            var redColor = new DeviceRgb(220, 38, 38);
            var greenColor = new DeviceRgb(22, 163, 74);
            var grayColor = new DeviceRgb(248, 249, 250);

            // ── Header ───────────────────────────────────────────────
            doc.Add(new Paragraph("Ministry of Defence — MoDLibrary")
                .SetFont(boldFont).SetFontSize(16)
                .SetFontColor(navyColor)
                .SetTextAlignment(TextAlignment.CENTER));

            var reportTitle = model.ReportType == "Issued" ? "Issued Books Report" :
                              model.ReportType == "Fines" ? "Fines Report" :
                                                             "Daily Activity Report";
            doc.Add(new Paragraph(reportTitle)
                .SetFont(boldFont).SetFontSize(13)
                .SetFontColor(goldColor)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph(
                $"Period: {model.StartDate:dd MMM yyyy} — {model.EndDate:dd MMM yyyy}   |   " +
                $"Generated: {DateTime.Now:dd MMM yyyy hh:mm tt}")
                .SetFont(normalFont).SetFontSize(9)
                .SetFontColor(ColorConstants.GRAY)
                .SetTextAlignment(TextAlignment.CENTER));

            // Gold line
            doc.Add(new LineSeparator(
                new iText.Kernel.Pdf.Canvas.Draw.SolidLine(2f))
                .SetStrokeColor(goldColor)
                .SetMarginBottom(10));

            // ── Summary ───────────────────────────────────────────────
            var summaryTable = new Table(6)
                .UseAllAvailableWidth()
                .SetMarginBottom(15);

            void AddSummaryCell(string label, string value, DeviceRgb? color = null)
            {
                var cell = new Cell()
                    .SetBackgroundColor(grayColor)
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(8)
                    .SetTextAlignment(TextAlignment.CENTER);
                cell.Add(new Paragraph(value)
                    .SetFont(boldFont).SetFontSize(16)
                    .SetFontColor(color ?? navyColor));
                cell.Add(new Paragraph(label)
                    .SetFont(normalFont).SetFontSize(8)
                    .SetFontColor(ColorConstants.GRAY));
                summaryTable.AddCell(cell);
            }

            AddSummaryCell("Total Issued", model.Summary.TotalIssued.ToString());
            AddSummaryCell("Returned", model.Summary.TotalReturned.ToString());
            AddSummaryCell("Overdue", model.Summary.CurrentOverdue.ToString(), redColor);
            AddSummaryCell("Total Fines", $"Rs. {model.Summary.TotalFines:N0}", redColor);
            AddSummaryCell("Collected", $"Rs. {model.Summary.CollectedFines:N0}", greenColor);
            AddSummaryCell("Pending Fines", $"Rs. {model.Summary.PendingFines:N0}", redColor);
            doc.Add(summaryTable);

            // ── Issued Books Table ─────────────────────────────────────
            if (model.ReportType == "Issued" && model.IssuedBooks.Any())
            {
                doc.Add(new Paragraph($"Issued Books — {model.IssuedBooks.Count} records")
                    .SetFont(boldFont).SetFontSize(11)
                    .SetFontColor(navyColor).SetMarginBottom(6));

                var table = new Table(new float[] { 2f, 2f, 1.5f, 1f, 1f, 2.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1f, 1.5f })
                    .UseAllAvailableWidth();

                // Headers
                string[] headers = { "Member","CNIC","Service No","Wing","Section",
                                  "Book","Book No","Issue Date","Due Date",
                                  "Return Date","Status","Fine" };
                foreach (var h in headers)
                    table.AddHeaderCell(new Cell()
                        .SetBackgroundColor(navyColor)
                        .SetPadding(5)
                        .Add(new Paragraph(h)
                            .SetFont(boldFont).SetFontSize(8)
                            .SetFontColor(ColorConstants.WHITE)));

                // Rows
                var altColor = new DeviceRgb(248, 250, 252);
                int rowIdx = 0;
                foreach (var r in model.IssuedBooks)
                {
                    var bg = rowIdx++ % 2 == 0 ? ColorConstants.WHITE : altColor;
                    var statusColor = r.BookStatus == "Overdue" ? redColor :
                                      r.BookStatus == "Active" ? greenColor :
                                                                   ColorConstants.GRAY;
                    void AddCell(string text, Color? fg = null) =>
                        table.AddCell(new Cell().SetBackgroundColor(bg).SetPadding(4)
                            .Add(new Paragraph(text).SetFont(normalFont).SetFontSize(8)
                                .SetFontColor(fg ?? ColorConstants.BLACK)));

                    AddCell(r.MemberName);
                    AddCell(r.CNIC);
                    AddCell(r.ServiceNo);
                    AddCell(r.WingName);
                    AddCell(r.SectionName);
                    AddCell(r.BookTitle);
                    AddCell(r.BookNumber);
                    AddCell(r.IssueDate.ToString("dd MMM yy"));
                    AddCell(r.DueDate.ToString("dd MMM yy"),
                            r.BookStatus == "Overdue" ? redColor : null);
                    AddCell(r.ReturnDate.HasValue ? 
                            r.ReturnDate.Value.ToString("dd MMM yy") : "—");
                    AddCell(r.BookStatus, statusColor);
                    AddCell(r.FineAmount > 0 ? $"Rs.{r.FineAmount:N0}" : "—",
                            r.FineAmount > 0 ? redColor : null);
                }

                // Total row
                table.AddCell(new Cell(1, 11)
                    .SetBackgroundColor(grayColor).SetPadding(5)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("Total Fines:")
                        .SetFont(boldFont).SetFontSize(9)));
                table.AddCell(new Cell()
                    .SetBackgroundColor(grayColor).SetPadding(5)
                    .Add(new Paragraph($"Rs. {model.IssuedBooks.Sum(x => x.FineAmount):N0}")
                        .SetFont(boldFont).SetFontSize(9)
                        .SetFontColor(redColor)));

                doc.Add(table);
            }

            // ── Fines Table ───────────────────────────────────────────
            if (model.ReportType == "Fines" && model.Fines.Any())
            {
                doc.Add(new Paragraph($"Fines Report — {model.Fines.Count} records")
                    .SetFont(boldFont).SetFontSize(11)
                    .SetFontColor(navyColor).SetMarginBottom(6));

                var table = new Table(new float[] { 2f, 2f, 1.5f, 2.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1f, 1.5f, 1f, 1.5f })
                    .UseAllAvailableWidth();

                string[] headers = { "Member","CNIC","Service No","Book","Book No",
                                  "Issue Date","Due Date","Return Date",
                                  "Days Late","Fine","Status","Paid Date" };
                foreach (var h in headers)
                    table.AddHeaderCell(new Cell()
                        .SetBackgroundColor(navyColor).SetPadding(5)
                        .Add(new Paragraph(h)
                            .SetFont(boldFont).SetFontSize(8)
                            .SetFontColor(ColorConstants.WHITE)));

                int rowIdx = 0;
                var altColor = new DeviceRgb(248, 250, 252);
                foreach (var f in model.Fines)
                {
                    var bg = rowIdx++ % 2 == 0 ? ColorConstants.WHITE : altColor;
                    void AddCell(string text, Color? fg = null) =>
                        table.AddCell(new Cell().SetBackgroundColor(bg).SetPadding(4)
                            .Add(new Paragraph(text).SetFont(normalFont).SetFontSize(8)
                                .SetFontColor(fg ?? ColorConstants.BLACK)));

                    AddCell(f.MemberName);
                    AddCell(f.CNIC);
                    AddCell(f.ServiceNo);
                    AddCell(f.BookTitle);
                    AddCell(f.BookNumber);
                    AddCell(f.IssueDate.ToString("dd MMM yy"));
                    AddCell(f.DueDate.ToString("dd MMM yy"), redColor);
                    AddCell(f.ReturnDate.HasValue ?
                            f.ReturnDate.Value.ToString("dd MMM yy") : "—");
                    AddCell(f.DaysLate.ToString(), redColor);
                    AddCell($"Rs. {f.FineAmount:N0}", redColor);
                    AddCell(f.IsPaid ? "Paid" : "Unpaid",
                            f.IsPaid ? greenColor : redColor);
                    AddCell(f.PaidDate.HasValue ?
                            f.PaidDate.Value.ToString("dd MMM yy") : "—");
                }

                table.AddCell(new Cell(1, 9)
                    .SetBackgroundColor(grayColor).SetPadding(5)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("Total:")
                        .SetFont(boldFont).SetFontSize(9)));
                table.AddCell(new Cell()
                    .SetBackgroundColor(grayColor).SetPadding(5)
                    .Add(new Paragraph($"Rs. {model.Fines.Sum(f => f.FineAmount):N0}")
                        .SetFont(boldFont).SetFontSize(9)
                        .SetFontColor(redColor)));
                table.AddCell(new Cell(1, 2)
                    .SetBackgroundColor(grayColor)
                    .Add(new Paragraph("")));

                doc.Add(table);
            }

            // ── Daily Activity Table ──────────────────────────────────
            if (model.ReportType == "Daily" && model.DailyActivity.Any())
            {
                doc.Add(new Paragraph(
                    $"Daily Activity — {model.StartDate:dd MMM yyyy} " +
                    $"({model.DailyActivity.Count} records)")
                    .SetFont(boldFont).SetFontSize(11)
                    .SetFontColor(navyColor).SetMarginBottom(6));

                var table = new Table(new float[] { 1.5f, 2f, 2f, 1.5f, 2.5f, 1.5f, 1.5f, 1.5f })
                    .UseAllAvailableWidth();

                string[] headers = { "Activity","Member","CNIC","Service No",
                                  "Book","Book No","Time","Due Date" };
                foreach (var h in headers)
                    table.AddHeaderCell(new Cell()
                        .SetBackgroundColor(navyColor).SetPadding(5)
                        .Add(new Paragraph(h)
                            .SetFont(boldFont).SetFontSize(8)
                            .SetFontColor(ColorConstants.WHITE)));

                int rowIdx = 0;
                var altColor = new DeviceRgb(248, 250, 252);
                var blueColor = new DeviceRgb(13, 110, 253);
                foreach (var a in model.DailyActivity)
                {
                    var bg = rowIdx++ % 2 == 0 ? ColorConstants.WHITE : altColor;
                    var actColor = a.ActivityType == "Issued" ? blueColor : greenColor;

                    void AddCell(string text, Color? fg = null) =>
                        table.AddCell(new Cell().SetBackgroundColor(bg).SetPadding(4)
                            .Add(new Paragraph(text).SetFont(normalFont).SetFontSize(8)
                                .SetFontColor(fg ?? ColorConstants.BLACK)));

                    AddCell(a.ActivityType, actColor);
                    AddCell(a.MemberName);
                    AddCell(a.CNIC);
                    AddCell(a.ServiceNo);
                    AddCell(a.BookTitle);
                    AddCell(a.BookNumber);
                    AddCell(a.ActivityDate.ToString("hh:mm tt"));
                    AddCell(a.DueDate.ToString("dd MMM yy"));
                }

                doc.Add(table);
            }

            // Footer
            doc.Add(new LineSeparator(
                new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1f))
                .SetStrokeColor(goldColor).SetMarginTop(15));
            doc.Add(new Paragraph(
                $"MoDLibrary — Ministry of Defence | " +
                $"Fine Rate: Rs. 50/day | Loan Period: 15 days")
                .SetFont(normalFont).SetFontSize(8)
                .SetFontColor(ColorConstants.GRAY)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Close();

            var fileName = $"MoDLibrary_{model.ReportType}_" +
                           $"{DateTime.Now:yyyy-MM-dd}.pdf";
            return File(ms.ToArray(), "application/pdf", fileName);
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
                <td>{a.DueDate:dd MMM yy}</td>
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
