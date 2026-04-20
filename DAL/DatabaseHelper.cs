using Microsoft.Data.SqlClient;
using MoDLibrary.Models;
using System.Data;
using System.Diagnostics.Contracts;

namespace MoDLibrary.DAL
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        // ── AUTH ─────────────────────────────────────────────────────────────

        public UserSession? Login(string username, string password)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_Login", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new UserSession
                {
                    UserId   = (int)reader["UserId"],
                    FullName = reader["FullName"].ToString()!,
                    Username = reader["Username"].ToString()!,
                    Role     = reader["RoleName"].ToString()!
                };
            return null;
        }

        // ── WINGS & SECTIONS ─────────────────────────────────────────────────

        public List<Wing> GetWings()
        {
            var list = new List<Wing>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_GetWings", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Wing { WingId = (int)reader["WingId"], WingName = reader["WingName"].ToString()! });
            return list;
        }

        public List<Section> GetSectionsByWing(int wingId)
        {
            var list = new List<Section>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_GetSectionsByWing", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@WingId", wingId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Section
                {
                    SectionId   = (int)reader["SectionId"],
                    SectionName = reader["SectionName"].ToString()!,
                    WingId      = wingId
                });
            return list;
        }

        // ── BOOKS ─────────────────────────────────────────────────────────────

        public List<Book> GetAllBooks()
        {
            var list = new List<Book>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_GetAllBooks", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapBook(reader));
            return list;
        }

        public List<Book> SearchBooks(string term)
        {
            var list = new List<Book>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_SearchBooks", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SearchTerm", term);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapBook(reader));
            return list;
        }

     /*  public Book? GetBookById(int bookId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT *, CASE WHEN AvailableCopies > 0 THEN 'Available' ELSE 'Not Available' END AS Availability FROM Books WHERE BookId = @BookId AND IsActive = 1", conn);
            cmd.Parameters.AddWithValue("@BookId", bookId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return MapBook(reader);
            return null;
        }*/

        public void AddBook(Book b)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_AddBook", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Title", b.Title);
            cmd.Parameters.AddWithValue("@Author", b.Author);
            cmd.Parameters.AddWithValue("@BookNumber", b.BookNumber);
            cmd.Parameters.AddWithValue("@ShelfLocation", b.ShelfLocation);
            cmd.Parameters.AddWithValue("@TotalCopies", b.TotalCopies);
            cmd.Parameters.AddWithValue("@Publisher", (object?)b.Publisher ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PublishedYear", (object?)b.PublishedYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ISBN", (object?)b.ISBN ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", b.CategoryId == 0 ? (object)DBNull.Value : b.CategoryId);
            cmd.ExecuteNonQuery();
        }

        public void UpdateBook(Book b)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_UpdateBook", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BookId", b.BookId);
            cmd.Parameters.AddWithValue("@Title", b.Title);
            cmd.Parameters.AddWithValue("@Author", b.Author);
            cmd.Parameters.AddWithValue("@BookNumber", b.BookNumber);
            cmd.Parameters.AddWithValue("@ShelfLocation", b.ShelfLocation);
            cmd.Parameters.AddWithValue("@TotalCopies", b.TotalCopies);
            cmd.Parameters.AddWithValue("@Publisher", (object?)b.Publisher ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PublishedYear", (object?)b.PublishedYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ISBN", (object?)b.ISBN ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", b.CategoryId == 0 ? (object)DBNull.Value : b.CategoryId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteBook(int bookId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_DeleteBook", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BookId", bookId);
            cmd.ExecuteNonQuery();
        }
        public List<TodayIssuedBook> GetTodayIssuedBooks()
        {
            var list = new List<TodayIssuedBook>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetTodayIssuedBooks", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new TodayIssuedBook
                    {
                        MemberName = r["MemberName"].ToString()!,
                        CNIC = r["CNIC"].ToString()!,
                        ServiceNo = r["ServiceNo"].ToString()!,
                        BookTitle = r["BookTitle"].ToString()!,
                        BookNumber = r["BookNumber"].ToString()!,
                        IssueDate = (DateTime)r["IssueDate"],
                        DueDate = (DateTime)r["DueDate"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("TodayIssued: " + ex.Message); }
            return list;
        }

        private Book MapBook(SqlDataReader r) => new Book
        {
            BookId = (int)r["BookId"],
            Title = r["Title"].ToString()!,
            Author = r["Author"].ToString()!,
            BookNumber = r["BookNumber"].ToString()!,
            ShelfLocation = r["ShelfLocation"].ToString()!,
            TotalCopies = (int)r["TotalCopies"],
            AvailableCopies = (int)r["AvailableCopies"],
            Publisher = r["Publisher"] == DBNull.Value ? null : r["Publisher"].ToString(),
            PublishedYear = r["PublishedYear"] == DBNull.Value ? null : (int?)r["PublishedYear"],
            ISBN = r["ISBN"] == DBNull.Value ? null : r["ISBN"].ToString(),
            Category = r["Category"].ToString()!,
            CategoryCode = r["CategoryCode"].ToString()!,
            CategoryId = Convert.ToInt32(r["CategoryId"]),
            ShelfId = Convert.ToInt32(r["ShelfId"]),
            ShelfCode = r["ShelfCode"].ToString()!,
            RackLetter = r["RackLetter"].ToString()!,
            CoverImagePath = r["CoverImagePath"] == DBNull.Value ? null : r["CoverImagePath"].ToString(),
            IsActive = r.GetColumnSchema().Any(c => c.ColumnName == "IsActive") &&
                       r["IsActive"] != DBNull.Value && (bool)r["IsActive"],
            Availability = r["Availability"].ToString()!,
            AvgRating = r.GetColumnSchema().Any(c => c.ColumnName == "AvgRating") ?
                       Convert.ToDouble(r["AvgRating"]) : 0,
            TotalRatings = r.GetColumnSchema().Any(c => c.ColumnName == "TotalRatings") ?
                       Convert.ToInt32(r["TotalRatings"]) : 0,
            // ── New fields ──────────────────────────────────────────
            EarliestDueDate = r["EarliestDueDate"] == DBNull.Value
                          ? null : (DateTime?)r["EarliestDueDate"],
            DaysUntilAvailable = r["DaysUntilAvailable"] == DBNull.Value
                          ? null : (int?)r["DaysUntilAvailable"]
        };


        // ── BOOK REQUESTS ─────────────────────────────────────────────────────

        public int SubmitBookRequest(BookRequest req)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_SubmitBookRequest", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MemberName", req.MemberName);
            cmd.Parameters.AddWithValue("@CNIC",       req.CNIC);
            cmd.Parameters.AddWithValue("@ServiceNo",  req.ServiceNo);
            cmd.Parameters.AddWithValue("@WingId",     req.WingId);
            cmd.Parameters.AddWithValue("@SectionId",  req.SectionId);
            cmd.Parameters.AddWithValue("@BookId",     req.BookId);
            var outParam = new SqlParameter("@NewRequestId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            cmd.ExecuteNonQuery();
            return (int)outParam.Value;
        }

        public List<BookRequest> GetAllRequests()
        {
            var list = new List<BookRequest>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_GetPendingRequests", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapRequest(reader));
            return list;
        }

        public void UpdateRequestStatus(int requestId, string status, string remark)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_UpdateRequestStatus", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@RequestId", requestId);
            cmd.Parameters.AddWithValue("@Status",    status);
            cmd.Parameters.AddWithValue("@Remark",    remark);
            cmd.ExecuteNonQuery();
        }

        private BookRequest MapRequest(SqlDataReader r) => new BookRequest
        {
            RequestId     = (int)r["RequestId"],
            MemberName    = r["MemberName"].ToString()!,
            CNIC          = r["CNIC"].ToString()!,
            ServiceNo     = r["ServiceNo"].ToString()!,
            WingName      = r["WingName"].ToString()!,
            SectionName   = r["SectionName"].ToString()!,
            BookTitle     = r["BookTitle"].ToString()!,
            BookNumber    = r["BookNumber"].ToString()!,
            ShelfLocation = r["ShelfLocation"].ToString()!,
            Status        = r["Status"].ToString()!,
            Remark        = r["Remark"] == DBNull.Value ? null : r["Remark"].ToString(),
            RequestDate   = (DateTime)r["RequestDate"]
        };

        // ── ISSUED BOOKS ──────────────────────────────────────────────────────

        public void IssueBook(int requestId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_IssueBook", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@RequestId", requestId);
            cmd.ExecuteNonQuery();
        }

        public ReturnResult ReturnBook(int issuedId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_ReturnBook", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@IssuedId", issuedId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return new ReturnResult
                {
                    FineAmount = reader["FineAmount"] == DBNull.Value ? 0 : (decimal)reader["FineAmount"],
                    DaysLate   = reader["DaysLate"]   == DBNull.Value ? 0 : (int)reader["DaysLate"]
                };
            return new ReturnResult();
        }

        public List<IssuedBook> GetAllIssuedBooks()
        {
            var list = new List<IssuedBook>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetAllIssuedBooks", conn)
                { CommandType = CommandType.StoredProcedure };
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(new IssuedBook
                    {
                        IssuedId = reader["IssuedId"] == DBNull.Value ? 0 : (int)reader["IssuedId"],
                        MemberName = reader["MemberName"] == DBNull.Value ? "" : reader["MemberName"].ToString()!,
                        CNIC = reader["CNIC"] == DBNull.Value ? "" : Convert.ToString(reader["CNIC"]),
                        ServiceNo = reader["ServiceNo"] == DBNull.Value ? "" : Convert.ToString(reader["ServiceNo"]),
                        WingName = reader["WingName"] == DBNull.Value ? "" : reader["WingName"].ToString()!,
                        SectionName = reader["SectionName"] == DBNull.Value ? "" : reader["SectionName"].ToString()!,
                        BookTitle = reader["BookTitle"] == DBNull.Value ? "" : reader["BookTitle"].ToString()!,
                        BookNumber = reader["BookNumber"] == DBNull.Value ? "" : reader["BookNumber"].ToString()!,
                        IssueDate = reader["IssueDate"] == DBNull.Value ? DateTime.Now : (DateTime)reader["IssueDate"],
                        DueDate = reader["DueDate"] == DBNull.Value ? DateTime.Now : (DateTime)reader["DueDate"],
                        ReturnDate = reader["ReturnDate"] == DBNull.Value ? null : (DateTime?)reader["ReturnDate"],
                        IsReturned = reader["IsReturned"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsReturned"]),
                        BookStatus = reader["BookStatus"] == DBNull.Value ? "" : reader["BookStatus"].ToString()!,
                        PendingFine = reader["PendingFine"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PendingFine"])
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine("IssuedBooks error: " + ex.Message);
            }
            return list;
        }

        // ── FINES ────────────────────────────────────────────────────────────

        public List<Fine> GetFines()
        {
            var list = new List<Fine>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_GetFines", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Fine
                {
                    FineId      = (int)reader["FineId"],
                    MemberName  = reader["MemberName"].ToString()!,
                    CNIC        = reader["CNIC"].ToString()!,
                    ServiceNo   = reader["ServiceNo"].ToString()!,
                    BookTitle   = reader["BookTitle"].ToString()!,
                    BookNumber  = reader["BookNumber"].ToString()!,
                    IssueDate   = (DateTime)reader["IssueDate"],
                    DueDate     = (DateTime)reader["DueDate"],
                    ReturnDate  = reader["ReturnDate"] == DBNull.Value ? null : (DateTime)reader["ReturnDate"],
                    FineAmount  = (decimal)reader["FineAmount"],
                    IsPaid      = (bool)reader["IsPaid"],
                    PaidDate    = reader["PaidDate"] == DBNull.Value ? null : (DateTime)reader["PaidDate"],
                    CalculatedAt= (DateTime)reader["CalculatedAt"]
                });
            return list;
        }

        public void MarkFinePaid(int fineId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_MarkFinePaid", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@FineId", fineId);
            cmd.ExecuteNonQuery();
        }

        // ── NOTIFICATIONS ─────────────────────────────────────────────────────

        public List<Notification> GetUnreadNotifications()
        {
            var list = new List<Notification>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetUnreadNotifications", conn)
                { CommandType = CommandType.StoredProcedure };
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Notification
                    {
                        NotificationId = reader["NotificationId"] == DBNull.Value ? 0 : (int)reader["NotificationId"],
                        RequestId = reader["RequestId"] == DBNull.Value ? 0 : (int)reader["RequestId"],
                        Message = reader["Message"] == DBNull.Value ? "" : reader["Message"].ToString()!,
                        MemberName = reader["MemberName"] == DBNull.Value ? "" : reader["MemberName"].ToString()!,
                        BookTitle = reader["BookTitle"] == DBNull.Value ? "" : reader["BookTitle"].ToString()!,
                        IsRead = reader["IsRead"] == DBNull.Value ? false : (bool)reader["IsRead"],
                        CreatedAt = reader["CreatedAt"] == DBNull.Value ? DateTime.Now : (DateTime)reader["CreatedAt"]
                    });
                }
            }
            catch (Exception ex)
            {
                // Log karo aur empty list return karo taake crash na ho
                Console.WriteLine("Notification error: " + ex.Message);
            }
            return list;
        }
        public void MarkNotificationRead(int notificationId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_MarkNotificationRead", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@NotificationId", notificationId);
            cmd.ExecuteNonQuery();
        }

        // ── DASHBOARD ─────────────────────────────────────────────────────────

        public DashboardStats GetDashboardStats()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetDashboardStats", conn) { CommandType = CommandType.StoredProcedure };
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                    return new DashboardStats
                    {
                        TotalBooks = (int)reader["TotalBooks"],
                        BooksIssued = (int)reader["BooksIssued"],
                        PendingRequests = (int)reader["PendingRequests"],
                        UnpaidFines = (int)reader["UnpaidFines"],
                        TotalFineAmount = (decimal)reader["TotalFineAmount"],
                        TotalLibrarians = (int)reader["TotalLibrarians"],
                        OverdueBooks = (int)reader["OverdueBooks"],
                        TodayReturned = (int)reader["TodayReturned"]
                    };
            }
            catch (Exception ex) { Console.WriteLine("Dashboard: " + ex.Message); }
            return new DashboardStats();
        }

        // ── LIBRARIANS ────────────────────────────────────────────────────────

        public List<LibrarianUser> GetLibrarians()
        {
            var list = new List<LibrarianUser>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_GetLibrarians", conn) { CommandType = CommandType.StoredProcedure };
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new LibrarianUser
                {
                    UserId    = (int)reader["UserId"],
                    FullName  = reader["FullName"].ToString()!,
                    Username  = reader["Username"].ToString()!,
                    IsActive  = (bool)reader["IsActive"],
                    CreatedAt = (DateTime)reader["CreatedAt"]
                });
            return list;
        }

        public void AddLibrarian(string fullName, string username, string password)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_AddLibrarian", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@FullName",     fullName);
            cmd.Parameters.AddWithValue("@Username",     username);
            cmd.Parameters.AddWithValue("@PasswordHash", password);
            cmd.ExecuteNonQuery();
        }

        public void UpdateLibrarian(int userId, string fullName, string username, bool isActive)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_UpdateLibrarian", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@UserId",   userId);
            cmd.Parameters.AddWithValue("@FullName", fullName);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
            cmd.ExecuteNonQuery();
        }

        public void DeleteLibrarian(int userId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_DeleteLibrarian", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.ExecuteNonQuery();
        }

        public List<BookRequest> GetRequestsByCNIC(string cnic)
        {
            var list = new List<BookRequest>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetRequestsByCNIC", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CNIC", cnic);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(new BookRequest
                    {
                        RequestId = (int)reader["RequestId"],
                        MemberName = reader["MemberName"].ToString()!,
                        CNIC = reader["CNIC"].ToString()!,
                        ServiceNo = reader["ServiceNo"].ToString()!,
                        WingName = reader["WingName"].ToString()!,
                        SectionName = reader["SectionName"].ToString()!,
                        BookTitle = reader["BookTitle"].ToString()!,
                        BookNumber = reader["BookNumber"].ToString()!,
                        ShelfLocation = reader["ShelfLocation"].ToString()!,
                        Status = reader["Status"].ToString()!,
                        Remark = reader["Remark"] == DBNull.Value ? null : reader["Remark"].ToString(),
                        RequestDate = (DateTime)reader["RequestDate"]
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetRequestsByCNIC error: " + ex.Message);
            }
            return list;
        }
        // ── CATEGORIES ────────────────────────────────────────────────

        public List<Category> GetCategories()
        {
            var list = new List<Category>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetCategories", conn)
                { CommandType = CommandType.StoredProcedure };
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(new Category
                    {
                        CategoryId = (int)reader["CategoryId"],
                        CategoryName = reader["CategoryName"].ToString()!,
                        RackLetter = reader["RackLetter"].ToString()!,
                        CategoryCode = reader["CategoryCode"].ToString()!
                    });
            }
            catch (Exception ex) { Console.WriteLine("Categories: " + ex.Message); }
            return list;
        }

        public List<Category> GetAllCategories()
        {
            var list = new List<Category>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_GetAllCategories", conn)
            { CommandType = CommandType.StoredProcedure };
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Category
                {
                    CategoryId = (int)reader["CategoryId"],
                    CategoryName = reader["CategoryName"].ToString()!,
                    IsActive = (bool)reader["IsActive"],
                    CreatedAt = (DateTime)reader["CreatedAt"]
                });
            return list;
        }

        public void AddCategory(string name)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_AddCategory", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@CategoryName", name);
            cmd.ExecuteNonQuery();
        }

        public void UpdateCategory(int id, string name, bool isActive)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_UpdateCategory", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@CategoryId", id);
            cmd.Parameters.AddWithValue("@CategoryName", name);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
            cmd.ExecuteNonQuery();
        }

        public void DeleteCategory(int id)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_DeleteCategory", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@CategoryId", id);
            cmd.ExecuteNonQuery();
        }

        public List<RecentRequest> GetRecentRequests()
        {
            var list = new List<RecentRequest>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetRecentRequests", conn)
                { CommandType = CommandType.StoredProcedure };
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(new RecentRequest
                    {
                        RequestId = (int)reader["RequestId"],
                        MemberName = reader["MemberName"].ToString()!,
                        BookTitle = reader["BookTitle"].ToString()!,
                        Status = reader["Status"].ToString()!,
                        RequestDate = (DateTime)reader["RequestDate"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("RecentRequests error: " + ex.Message); }
            return list;
        }

        public List<OverdueBook> GetOverdueBooks()
        {
            var list = new List<OverdueBook>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetOverdueBooks", conn)
                { CommandType = CommandType.StoredProcedure };
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(new OverdueBook
                    {
                        MemberName = reader["MemberName"].ToString()!,
                        BookTitle = reader["BookTitle"].ToString()!,
                        DueDate = (DateTime)reader["DueDate"],
                        DaysLate = (int)reader["DaysLate"],
                        FineAmount = (decimal)reader["FineAmount"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("OverdueBooks error: " + ex.Message); }
            return list;
        }

        public List<BookRequest> GetPendingRequestsOnly()
        {
            var list = new List<BookRequest>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetPendingRequestsOnly", conn)
                { CommandType = CommandType.StoredProcedure };
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(new BookRequest
                    {
                        RequestId = (int)reader["RequestId"],
                        MemberName = reader["MemberName"].ToString()!,
                        CNIC = reader["CNIC"].ToString()!,
                        ServiceNo = reader["ServiceNo"].ToString()!,
                        BookTitle = reader["BookTitle"].ToString()!,
                        BookNumber = reader["BookNumber"].ToString()!,
                        ShelfLocation = reader["ShelfLocation"].ToString()!,
                        WingName = reader["WingName"].ToString()!,
                        SectionName = reader["SectionName"].ToString()!,
                        Status = reader["Status"].ToString()!,
                        RequestDate = (DateTime)reader["RequestDate"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("PendingRequests error: " + ex.Message); }
            return list;
        }
        // ── BOOK SUGGESTIONS ──────────────────────────────────────────

        public void SubmitSuggestion(BookSuggestion s)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_SubmitSuggestion", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MemberName", s.MemberName);
            cmd.Parameters.AddWithValue("@CNIC", s.CNIC);
            cmd.Parameters.AddWithValue("@ServiceNo", s.ServiceNo);
            cmd.Parameters.AddWithValue("@BookTitle", s.BookTitle);
            cmd.Parameters.AddWithValue("@AuthorName", (object?)s.AuthorName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Reason", (object?)s.Reason ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public List<BookSuggestion> GetAllSuggestions()
        {
            var list = new List<BookSuggestion>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetAllSuggestions", conn)
                { CommandType = CommandType.StoredProcedure };
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(MapSuggestion(reader));
            }
            catch (Exception ex) { Console.WriteLine("Suggestions error: " + ex.Message); }
            return list;
        }

        public List<BookSuggestion> GetSuggestionsByCNIC(string cnic)
        {
            var list = new List<BookSuggestion>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("sp_GetSuggestionsByCNIC", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CNIC", cnic);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(MapSuggestion(reader));
            }
            catch (Exception ex) { Console.WriteLine("SuggestionsByCNIC error: " + ex.Message); }
            return list;
        }

        public void UpdateSuggestionStatus(int id, string status, string remark)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("sp_UpdateSuggestionStatus", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SuggestionId", id);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@AdminRemark", remark);
            cmd.ExecuteNonQuery();
        }

        private BookSuggestion MapSuggestion(SqlDataReader r) => new BookSuggestion
        {
            SuggestionId = (int)r["SuggestionId"],
            MemberName = r["MemberName"].ToString()!,
            CNIC = r["CNIC"].ToString()!,
            ServiceNo = r["ServiceNo"].ToString()!,
            BookTitle = r["BookTitle"].ToString()!,
            AuthorName = r["AuthorName"] == DBNull.Value ? null : r["AuthorName"].ToString(),
            Reason = r["Reason"] == DBNull.Value ? null : r["Reason"].ToString(),
            Status = r["Status"].ToString()!,
            AdminRemark = r["AdminRemark"] == DBNull.Value ? null : r["AdminRemark"].ToString(),
            SuggestedAt = (DateTime)r["SuggestedAt"],
            UpdatedAt = r["UpdatedAt"] == DBNull.Value ? null : (DateTime?)r["UpdatedAt"]
        };
        // ── ANNOUNCEMENTS ─────────────────────────────────────────────

        public List<Announcement> GetActiveAnnouncements()
        {
            var list = new List<Announcement>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetActiveAnnouncements", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Announcement
                    {
                        AnnouncementId = (int)r["AnnouncementId"],
                        Title = r["Title"].ToString()!,
                        Message = r["Message"].ToString()!,
                        PostedBy = r["PostedBy"].ToString()!,
                        ExpiryDate = r["ExpiryDate"] == DBNull.Value ? null : (DateTime?)r["ExpiryDate"],
                        CreatedAt = (DateTime)r["CreatedAt"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("Announcements: " + ex.Message); }
            return list;
        }

        public List<Announcement> GetAllAnnouncements()
        {
            var list = new List<Announcement>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetAllAnnouncements", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Announcement
                    {
                        AnnouncementId = (int)r["AnnouncementId"],
                        Title = r["Title"].ToString()!,
                        Message = r["Message"].ToString()!,
                        PostedBy = r["PostedBy"].ToString()!,
                        ExpiryDate = r["ExpiryDate"] == DBNull.Value ? null : (DateTime?)r["ExpiryDate"],
                        IsActive = (bool)r["IsActive"],
                        CreatedAt = (DateTime)r["CreatedAt"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("AllAnnouncements: " + ex.Message); }
            return list;
        }

        public void AddAnnouncement(Announcement a)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_AddAnnouncement", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Title", a.Title);
            cmd.Parameters.AddWithValue("@Message", a.Message);
            cmd.Parameters.AddWithValue("@PostedBy", a.PostedBy);
            cmd.Parameters.AddWithValue("@ExpiryDate", (object?)a.ExpiryDate ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void DeleteAnnouncement(int id)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_DeleteAnnouncement", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@AnnouncementId", id);
            cmd.ExecuteNonQuery();
        }

        // ── RESERVATIONS ──────────────────────────────────────────────

        public int ReserveBook(Reservation res)
        {
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_ReserveBook", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@MemberName", res.MemberName);
                cmd.Parameters.AddWithValue("@CNIC", res.CNIC);
                cmd.Parameters.AddWithValue("@ServiceNo", res.ServiceNo);
                cmd.Parameters.AddWithValue("@BookId", res.BookId);
                using var r = cmd.ExecuteReader();
                if (r.Read()) return Convert.ToInt32(r["Result"]);
            }
            catch (Exception ex) { Console.WriteLine("ReserveBook: " + ex.Message); }
            return 0;
        }

        public List<Reservation> GetAllReservations()
        {
            var list = new List<Reservation>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetAllReservations", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read()) list.Add(MapReservation(r));
            }
            catch (Exception ex) { Console.WriteLine("Reservations: " + ex.Message); }
            return list;
        }

        public List<Reservation> GetReservationsByCNIC(string cnic)
        {
            var list = new List<Reservation>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetReservationsByCNIC", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CNIC", cnic);
                using var r = cmd.ExecuteReader();
                while (r.Read()) list.Add(MapReservation(r));
            }
            catch (Exception ex) { Console.WriteLine("ResByCNIC: " + ex.Message); }
            return list;
        }

        public void UpdateReservationStatus(int id, string status)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_UpdateReservationStatus", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@ReservationId", id);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.ExecuteNonQuery();
        }

        private Reservation MapReservation(SqlDataReader r) => new Reservation
        {
            ReservationId = (int)r["ReservationId"],
            MemberName = r["MemberName"].ToString()!,
            CNIC = r["CNIC"].ToString()!,
            ServiceNo = r["ServiceNo"].ToString()!,
            BookTitle = r["BookTitle"].ToString()!,
            BookNumber = r["BookNumber"].ToString()!,
            AvailableCopies = (int)r["AvailableCopies"],
            Status = r["Status"].ToString()!,
            ReservedAt = (DateTime)r["ReservedAt"],
            NotifiedAt = r["NotifiedAt"] == DBNull.Value ? null : (DateTime?)r["NotifiedAt"],
            ExpiryDate = r["ExpiryDate"] == DBNull.Value ? null : (DateTime?)r["ExpiryDate"]
        };

        // ── RATINGS ───────────────────────────────────────────────────

        public void AddRating(BookRating rating)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_AddRating", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BookId", rating.BookId);
            cmd.Parameters.AddWithValue("@MemberName", rating.MemberName);
            cmd.Parameters.AddWithValue("@CNIC", rating.CNIC);
            cmd.Parameters.AddWithValue("@Rating", rating.Rating);
            cmd.Parameters.AddWithValue("@Review", (object?)rating.Review ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public List<BookRating> GetBookRatings(int bookId)
        {
            var list = new List<BookRating>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetBookRatings", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@BookId", bookId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new BookRating
                    {
                        RatingId = (int)r["RatingId"],
                        MemberName = r["MemberName"].ToString()!,
                        Rating = (int)r["Rating"],
                        Review = r["Review"] == DBNull.Value ? null : r["Review"].ToString(),
                        RatedAt = (DateTime)r["RatedAt"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("Ratings: " + ex.Message); }
            return list;
        }

        public List<BookRating> GetAllRatings()
        {
            var list = new List<BookRating>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetAllRatings", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new BookRating
                    {
                        RatingId = (int)r["RatingId"],
                        BookTitle = r["BookTitle"].ToString()!,
                        MemberName = r["MemberName"].ToString()!,
                        Rating = (int)r["Rating"],
                        Review = r["Review"] == DBNull.Value ? null : r["Review"].ToString(),
                        RatedAt = (DateTime)r["RatedAt"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("AllRatings: " + ex.Message); }
            return list;
        }

        // ── POPULAR BOOKS ─────────────────────────────────────────────

        public List<PopularBook> GetPopularBooks()
        {
            var list = new List<PopularBook>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetPopularBooks", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new PopularBook
                    {
                        BookId = (int)r["BookId"],
                        Title = r["Title"].ToString()!,
                        Author = r["Author"].ToString()!,
                        BookNumber = r["BookNumber"].ToString()!,
                        ShelfLocation = r["ShelfLocation"].ToString()!,
                        Category = r["Category"].ToString()!,
                        TotalRequests = (int)r["TotalRequests"],
                        AvgRating = Convert.ToDouble(r["AvgRating"])
                    });
            }
            catch (Exception ex) { Console.WriteLine("PopularBooks: " + ex.Message); }
            return list;
        }

        // ── MEMBER HISTORY ────────────────────────────────────────────

        public List<MemberHistoryRecord> GetMemberHistory(string searchTerm)
        {
            var list = new List<MemberHistoryRecord>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetMemberHistory", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new MemberHistoryRecord
                    {
                        MemberName = r["MemberName"].ToString()!,
                        CNIC = r["CNIC"].ToString()!,
                        ServiceNo = r["ServiceNo"].ToString()!,
                        BookTitle = r["BookTitle"].ToString()!,
                        BookNumber = r["BookNumber"].ToString()!,
                        IssueDate = Convert.ToDateTime(r["Date1"]),
                        DueDate = Convert.ToDateTime(r["Date2"]),
                        ReturnDate = r["Date3"] == DBNull.Value ? null : Convert.ToDateTime(r["Date3"]),
                        IsReturned = Convert.ToBoolean(r["IsReturned"]),
                        FineAmount = Convert.ToDecimal(r["FineAmount"]),
                        FinePaid = Convert.ToBoolean(r["FinePaid"]),
                        RecordId = (int)r["RecordId"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("MemberHistory: " + ex.Message); }
            return list;
        }
        // ── SHELVES ───────────────────────────────────────────────────

        public List<Shelf> GetAllShelves()
        {
            var list = new List<Shelf>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetAllShelves", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Shelf
                    {
                        ShelfId = (int)r["ShelfId"],
                        ShelfCode = r["ShelfCode"].ToString()!,
                        RackLetter = r["RackLetter"].ToString()!,
                        RowNumber = (int)r["RowNumber"],
                        CategoryName = r["CategoryName"].ToString()!,
                        CategoryCode = r["CategoryCode"].ToString()!,
                        IsActive = (bool)r["IsActive"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("Shelves: " + ex.Message); }
            return list;
        }

        public List<Shelf> GetShelvesByCategory(int categoryId)
        {
            var list = new List<Shelf>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetShelvesByCategory", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Shelf
                    {
                        ShelfId = (int)r["ShelfId"],
                        ShelfCode = r["ShelfCode"].ToString()!,
                        RackLetter = r["RackLetter"].ToString()!,
                        RowNumber = (int)r["RowNumber"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("ShelvesByCategory: " + ex.Message); }
            return list;
        }

        public string GenerateBookId(int categoryId)
        {
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GenerateBookId", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@CategoryId", categoryId);
                using var r = cmd.ExecuteReader();
                if (r.Read()) return r["BookId"].ToString()!;
            }
            catch (Exception ex) { Console.WriteLine("GenerateBookId: " + ex.Message); }
            return "";
        }

        // ── BOOKS UPDATE ──────────────────────────────────────────────

        public void AddBook(Book b, string coverPath)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_AddBook", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Title", b.Title);
            cmd.Parameters.AddWithValue("@Author", b.Author);
            cmd.Parameters.AddWithValue("@BookNumber", b.BookNumber);
            cmd.Parameters.AddWithValue("@ShelfId", b.ShelfId);
            cmd.Parameters.AddWithValue("@TotalCopies", b.TotalCopies);
            cmd.Parameters.AddWithValue("@Publisher", (object?)b.Publisher ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PublishedYear", (object?)b.PublishedYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ISBN", (object?)b.ISBN ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", b.CategoryId == 0 ? (object)DBNull.Value : b.CategoryId);
            cmd.Parameters.AddWithValue("@CoverImagePath", string.IsNullOrEmpty(coverPath) ? (object)DBNull.Value : coverPath);
            cmd.ExecuteNonQuery();
        }

        public void UpdateBook(Book b, string coverPath)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_UpdateBook", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@BookId", b.BookId);
            cmd.Parameters.AddWithValue("@Title", b.Title);
            cmd.Parameters.AddWithValue("@Author", b.Author);
            cmd.Parameters.AddWithValue("@BookNumber", b.BookNumber);
            cmd.Parameters.AddWithValue("@ShelfId", b.ShelfId);
            cmd.Parameters.AddWithValue("@TotalCopies", b.TotalCopies);
            cmd.Parameters.AddWithValue("@Publisher", (object?)b.Publisher ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PublishedYear", (object?)b.PublishedYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ISBN", (object?)b.ISBN ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", b.CategoryId == 0 ? (object)DBNull.Value : b.CategoryId);
            cmd.Parameters.AddWithValue("@CoverImagePath", string.IsNullOrEmpty(coverPath) ? (object)DBNull.Value : coverPath);
            cmd.ExecuteNonQuery();
        }

        public Book? GetBookById(int bookId)
        {
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetBookById", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@BookId", bookId);
                using var r = cmd.ExecuteReader();
                if (r.Read()) return MapBook(r);
            }
            catch (Exception ex) { Console.WriteLine("GetBookById: " + ex.Message); }
            return null;
        }

        //private Book MapBook(SqlDataReader r) => new Book
        //{
        //    BookId = (int)r["BookId"],
        //    Title = r["Title"].ToString()!,
        //    Author = r["Author"].ToString()!,
        //    BookNumber = r["BookNumber"].ToString()!,
        //    ShelfLocation = r["ShelfLocation"].ToString()!,
        //    TotalCopies = (int)r["TotalCopies"],
        //    AvailableCopies = (int)r["AvailableCopies"],
        //    Publisher = r["Publisher"] == DBNull.Value ? null : r["Publisher"].ToString(),
        //    PublishedYear = r["PublishedYear"] == DBNull.Value ? null : (int?)r["PublishedYear"],
        //    ISBN = r["ISBN"] == DBNull.Value ? null : r["ISBN"].ToString(),
        //    Category = r["Category"].ToString()!,
        //    CategoryCode = r["CategoryCode"].ToString()!,
        //    CategoryId = Convert.ToInt32(r["CategoryId"]),
        //    ShelfId = Convert.ToInt32(r["ShelfId"]),
        //    ShelfCode = r["ShelfCode"].ToString()!,
        //    RackLetter = r["RackLetter"].ToString()!,
        //    CoverImagePath = r["CoverImagePath"] == DBNull.Value ? null : r["CoverImagePath"].ToString(),
        //    IsActive = r["IsActive"] == DBNull.Value ? false : (bool)r["IsActive"],
        //    Availability = r["Availability"].ToString()!,
        //    AvgRating = r.GetColumnSchema().Any(c => c.ColumnName == "AvgRating") ?
        //                      Convert.ToDouble(r["AvgRating"]) : 0,
        //    TotalRatings = r.GetColumnSchema().Any(c => c.ColumnName == "TotalRatings") ?
        //                      Convert.ToInt32(r["TotalRatings"]) : 0
        //};

        // ── EBOOKS ────────────────────────────────────────────────────

        public List<EBook> GetAllEBooks()
        {
            var list = new List<EBook>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetAllEBooks", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(MapEBook(r));
            }
            catch (Exception ex) { Console.WriteLine("EBooks: " + ex.Message); }
            return list;
        }

     

        public void AddEBook(EBook e, string filePath, string coverPath)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_AddEBook", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@Title", e.Title);
            cmd.Parameters.AddWithValue("@Author", e.Author);
            cmd.Parameters.AddWithValue("@CategoryId", (object?)e.CategoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)e.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", filePath);
            cmd.Parameters.AddWithValue("@CoverImagePath", string.IsNullOrEmpty(coverPath) ? (object)DBNull.Value : coverPath);
            cmd.Parameters.AddWithValue("@FileSize", (object?)e.FileSize ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TotalPages", (object?)e.TotalPages ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PublishedYear", (object?)e.PublishedYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ISBN", (object?)e.ISBN ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void DeleteEBook(int id)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_DeleteEBook", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@EBookId", id);
            cmd.ExecuteNonQuery();
        }

        private EBook MapEBook(SqlDataReader r) => new EBook
        {
            EBookId = (int)r["EBookId"],
            Title = r["Title"].ToString()!,
            Author = r["Author"].ToString()!,
            Category = r["Category"].ToString()!,
            Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
            FilePath = r["FilePath"].ToString()!,
            CoverImagePath = r["CoverImagePath"] == DBNull.Value ? null : r["CoverImagePath"].ToString(),
            FileSize = r["FileSize"] == DBNull.Value ? null : r["FileSize"].ToString(),
            TotalPages = r["TotalPages"] == DBNull.Value ? null : (int?)r["TotalPages"],
            PublishedYear = r["PublishedYear"] == DBNull.Value ? null : (int?)r["PublishedYear"],
            ISBN = r["ISBN"] == DBNull.Value ? null : r["ISBN"].ToString(),
            IsActive = (bool)r["IsActive"],
            UploadedAt = (DateTime)r["UploadedAt"]
        };

        // ── SUBSCRIPTIONS ─────────────────────────────────────────────

        public List<LibrarySubscription> GetSubscriptions()
        {
            var list = new List<LibrarySubscription>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetSubscriptions", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read()) list.Add(MapSubscription(r));
            }
            catch (Exception ex) { Console.WriteLine("Subscriptions: " + ex.Message); }
            return list;
        }

        public List<LibrarySubscription> GetAllSubscriptions()
        {
            var list = new List<LibrarySubscription>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetAllSubscriptions", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read()) list.Add(MapSubscription(r));
            }
            catch (Exception ex) { Console.WriteLine("AllSubscriptions: " + ex.Message); }
            return list;
        }

        public void AddSubscription(LibrarySubscription s, string logoPath)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_AddSubscription", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@LibraryName", s.LibraryName);
            cmd.Parameters.AddWithValue("@Description", (object?)s.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@WebsiteUrl", s.WebsiteUrl);
            cmd.Parameters.AddWithValue("@LogoPath", string.IsNullOrEmpty(logoPath) ? (object)DBNull.Value : logoPath);
            cmd.ExecuteNonQuery();
        }

        public void UpdateSubscription(LibrarySubscription s)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_UpdateSubscription", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SubscriptionId", s.SubscriptionId);
            cmd.Parameters.AddWithValue("@LibraryName", s.LibraryName);
            cmd.Parameters.AddWithValue("@Description", (object?)s.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@WebsiteUrl", s.WebsiteUrl);
            cmd.Parameters.AddWithValue("@IsActive", s.IsActive);
            cmd.ExecuteNonQuery();
        }

        public void DeleteSubscription(int id)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_DeleteSubscription", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SubscriptionId", id);
            cmd.ExecuteNonQuery();
        }

        private LibrarySubscription MapSubscription(SqlDataReader r) => new LibrarySubscription
        {
            SubscriptionId = (int)r["SubscriptionId"],
            LibraryName = r["LibraryName"].ToString()!,
            Description = r["Description"] == DBNull.Value ? null : r["Description"].ToString(),
            WebsiteUrl = r["WebsiteUrl"].ToString()!,
            LogoPath = r["LogoPath"] == DBNull.Value ? null : r["LogoPath"].ToString(),
            IsActive = (bool)r["IsActive"],
            AddedAt = (DateTime)r["AddedAt"]
        };

        // ── LIBRARIAN DIRECT ISSUE ────────────────────────────────────

        public int LibrarianIssueBook(BookRequest req)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();

                using var cmd = new SqlCommand("sp_LibrarianIssueBook", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@MemberName", req.MemberName);
                cmd.Parameters.AddWithValue("@CNIC", req.CNIC);
                cmd.Parameters.AddWithValue("@ServiceNo", req.ServiceNo);
                cmd.Parameters.AddWithValue("@WingId", req.WingId);
                cmd.Parameters.AddWithValue("@SectionId", req.SectionId);
                cmd.Parameters.AddWithValue("@BookId", req.BookId);

                var outParam = new SqlParameter("@NewRequestId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);

                cmd.ExecuteNonQuery();

                // 🔴 IMPORTANT: safe null handling
                if (outParam.Value == DBNull.Value || outParam.Value == null)
                    return 0;

                return Convert.ToInt32(outParam.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine("LibrarianIssueBook Error: " + ex.Message);
                return -99; // 🔴 better than silent 0 (means system error)
            }
        }

        // ── REPORTS ───────────────────────────────────────────────────

        public List<IssuedBookReport> GetIssuedBooksReport(
            DateTime start, DateTime end, string status)
        {
            var list = new List<IssuedBookReport>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_ReportIssuedBooks", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@StartDate", start);
                cmd.Parameters.AddWithValue("@EndDate", end.AddDays(1).AddSeconds(-1));
                cmd.Parameters.AddWithValue("@Status", status);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new IssuedBookReport
                    {
                        MemberName = r["MemberName"].ToString()!,
                        CNIC = r["CNIC"].ToString()!,
                        ServiceNo = r["ServiceNo"].ToString()!,
                        WingName = r["WingName"].ToString()!,
                        SectionName = r["SectionName"].ToString()!,
                        BookTitle = r["BookTitle"].ToString()!,
                        BookNumber = r["BookNumber"].ToString()!,
                        IssueDate = (DateTime)r["IssueDate"],
                        DueDate = (DateTime)r["DueDate"],
                        ReturnDate = r["ReturnDate"] == DBNull.Value ? null : (DateTime?)r["ReturnDate"],
                        IsReturned = Convert.ToBoolean(r["IsReturned"]),
                        BookStatus = r["BookStatus"].ToString()!,
                        FineAmount = Convert.ToDecimal(r["FineAmount"]),
                        FinePaid = Convert.ToBoolean(r["FinePaid"])
                    });
            }
            catch (Exception ex) { Console.WriteLine("IssuedReport: " + ex.Message); }
            return list;
        }

        public List<FineReport> GetFinesReport(
            DateTime start, DateTime end, string isPaid)
        {
            var list = new List<FineReport>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_ReportFines", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@StartDate", start);
                cmd.Parameters.AddWithValue("@EndDate", end.AddDays(1).AddSeconds(-1));
                cmd.Parameters.AddWithValue("@IsPaid", isPaid);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new FineReport
                    {
                        MemberName = r["MemberName"].ToString()!,
                        CNIC = r["CNIC"].ToString()!,
                        ServiceNo = r["ServiceNo"].ToString()!,
                        BookTitle = r["BookTitle"].ToString()!,
                        BookNumber = r["BookNumber"].ToString()!,
                        IssueDate = (DateTime)r["IssueDate"],
                        DueDate = (DateTime)r["DueDate"],
                        ReturnDate = r["ReturnDate"] == DBNull.Value ? null : (DateTime?)r["ReturnDate"],
                        FineAmount = Convert.ToDecimal(r["FineAmount"]),
                        IsPaid = Convert.ToBoolean(r["IsPaid"]),
                        PaidDate = r["PaidDate"] == DBNull.Value ? null : (DateTime?)r["PaidDate"],
                        DaysLate = Convert.ToInt32(r["DaysLate"])
                    });
            }
            catch (Exception ex) { Console.WriteLine("FineReport: " + ex.Message); }
            return list;
        }

        public List<DailyActivityReport> GetDailyActivityReport(DateTime date)
        {
            var list = new List<DailyActivityReport>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_ReportDailyActivity", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@Date", date);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new DailyActivityReport
                    {
                        ActivityType = r["ActivityType"].ToString()!,
                        MemberName = r["MemberName"].ToString()!,
                        BookTitle = r["BookTitle"].ToString()!,
                        BookNumber = r["BookNumber"].ToString()!,
                        ActivityDate = (DateTime)r["ActivityDate"],
                        DueDate = (DateTime)r["DueDate"],
                        CNIC = r["CNIC"].ToString()!,
                        ServiceNo = r["ServiceNo"].ToString()!
                    });
            }
            catch (Exception ex) { Console.WriteLine("DailyReport: " + ex.Message); }
            return list;
        }

        public ReportSummary GetReportSummary(DateTime start, DateTime end)
        {
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_ReportSummary", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@StartDate", start);
                cmd.Parameters.AddWithValue("@EndDate", end.AddDays(1).AddSeconds(-1));
                using var r = cmd.ExecuteReader();
                if (r.Read())
                    return new ReportSummary
                    {
                        TotalIssued = Convert.ToInt32(r["TotalIssued"]),
                        TotalReturned = Convert.ToInt32(r["TotalReturned"]),
                        CurrentOverdue = Convert.ToInt32(r["CurrentOverdue"]),
                        TotalFines = Convert.ToDecimal(r["TotalFines"]),
                        CollectedFines = Convert.ToDecimal(r["CollectedFines"]),
                        PendingFines = Convert.ToDecimal(r["PendingFines"])
                    };
            }
            catch (Exception ex) { Console.WriteLine("Summary: " + ex.Message); }
            return new ReportSummary();
        }
        // ── MEMBERS ───────────────────────────────────────────────────

        public UserSession? MemberLogin(string username, string password)
        {
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_MemberLogin", conn)
                { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                    return new UserSession
                    {
                        UserId = (int)r["MemberId"],
                        FullName = r["FullName"].ToString()!,
                        Username = r["Username"].ToString()!,
                        Role = "Member"
                    };
            }
            catch (Exception ex) { Console.WriteLine("MemberLogin: " + ex.Message); }
            return null;
        }

        public List<Member> GetAllMembers()
        {
            var list = new List<Member>();
            try
            {
                using var conn = GetConnection(); conn.Open();
                using var cmd = new SqlCommand("sp_GetAllMembers", conn)
                { CommandType = CommandType.StoredProcedure };
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new Member
                    {
                        MemberId = (int)r["MemberId"],
                        FullName = r["FullName"].ToString()!,
                        Username = r["Username"].ToString()!,
                        IsActive = (bool)r["IsActive"],
                        CreatedAt = (DateTime)r["CreatedAt"]
                    });
            }
            catch (Exception ex) { Console.WriteLine("GetMembers: " + ex.Message); }
            return list;
        }

        public void AddMember(string fullName, string username, string password)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_AddMember", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@FullName", fullName);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);
            cmd.ExecuteNonQuery();
        }

        public void UpdateMember(int id, string fullName, string username, bool isActive)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_UpdateMember", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MemberId", id);
            cmd.Parameters.AddWithValue("@FullName", fullName);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@IsActive", isActive);
            cmd.ExecuteNonQuery();
        }

        public void ResetMemberPassword(int id, string newPassword)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_ResetMemberPassword", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MemberId", id);
            cmd.Parameters.AddWithValue("@NewPassword", newPassword);
            cmd.ExecuteNonQuery();
        }

        public void DeleteMember(int id)
        {
            using var conn = GetConnection(); conn.Open();
            using var cmd = new SqlCommand("sp_DeleteMember", conn)
            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@MemberId", id);
            cmd.ExecuteNonQuery();
        }

        public List<TodayReturnedBook> GetTodayReturnedBooks()
        {
            var list = new List<TodayReturnedBook>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT 
                br.MemberName,
                br.CNIC,
                br.ServiceNo,
                b.Title AS BookTitle,
                b.BookNumber,
                ib.ReturnDate
            FROM IssuedBooks ib
            INNER JOIN BookRequests br ON br.RequestId = ib.RequestId
            INNER JOIN Books b ON b.BookId = br.BookId
            WHERE ib.IsReturned = 1
              AND CAST(ib.ReturnDate AS DATE) = CAST(GETDATE() AS DATE)
        ", con);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new TodayReturnedBook
                    {
                        MemberName = dr["MemberName"].ToString(),
                        CNIC = dr["CNIC"].ToString(),
                        ServiceNo = dr["ServiceNo"].ToString(),
                        BookTitle = dr["BookTitle"].ToString(),
                        BookNumber = dr["BookNumber"].ToString(),
                        ReturnDate = Convert.ToDateTime(dr["ReturnDate"])
                    });
                }
            }

            return list;
        }


    }
}
