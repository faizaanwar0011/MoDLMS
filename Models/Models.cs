namespace MoDLibrary.Models
{
    public class UserSession
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class LoginViewModel
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class Wing
    {
        public int WingId { get; set; }
        public string WingName { get; set; } = "";
    }

    public class Section
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = "";
        public int WingId { get; set; }
    }

    public class Book
    {
        public int BookId { get; set; }

        public int CategoryId {get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string BookNumber { get; set; } = "";
        public string ShelfLocation { get; set; } = "";
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public string? Publisher { get; set; }
        public int? PublishedYear { get; set; }
        public string? ISBN { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public string Availability { get; set; } = "";
    }

    public class BookRequest
    {
        public int RequestId { get; set; }
        public string MemberName { get; set; } = "";
        public string CNIC { get; set; } = "";
        public string ServiceNo { get; set; } = "";
        public int WingId { get; set; }
        public string WingName { get; set; } = "";
        public int SectionId { get; set; }
        public string SectionName { get; set; } = "";
        public int BookId { get; set; }
        public string BookTitle { get; set; } = "";
        public string BookNumber { get; set; } = "";
        public string ShelfLocation { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public string? Remark { get; set; }
        public DateTime RequestDate { get; set; }
    }

    public class BookRequestViewModel
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; } = "";
        public string MemberName { get; set; } = "";
        public string CNIC { get; set; } = "";
        public string ServiceNo { get; set; } = "";
        public int WingId { get; set; }
        public int SectionId { get; set; }
        public List<Wing> Wings { get; set; } = new();
        public List<Section> Sections { get; set; } = new();
    }

    public class IssuedBook
    {
        public int IssuedId { get; set; }
        public string MemberName { get; set; } = "";
        public string CNIC { get; set; } = "";
        public string ServiceNo { get; set; } = "";
        public string WingName { get; set; } = "";
        public string SectionName { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public string BookNumber { get; set; } = "";
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public string BookStatus { get; set; } = "";
        public decimal PendingFine { get; set; }
    }

    public class Fine
    {
        public int FineId { get; set; }
        public string MemberName { get; set; } = "";
        public string CNIC { get; set; } = "";
        public string ServiceNo { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public string BookNumber { get; set; } = "";
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public decimal FineAmount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public class Notification
    {
        public int NotificationId { get; set; }
        public int RequestId { get; set; }
        public string Message { get; set; } = "";
        public string MemberName { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LibrarianUser
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddLibrarianViewModel
    {
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class DashboardStats
    {
        public int TotalBooks { get; set; }
        public int BooksIssued { get; set; }
        public int PendingRequests { get; set; }
        public int UnpaidFines { get; set; }
        public decimal TotalFineAmount { get; set; }
        public int TotalLibrarians { get; set; }
        public int OverdueBooks { get; set; }
    }

    public class ReturnResult
    {
        public decimal FineAmount { get; set; }
        public int DaysLate { get; set; }
    }

    public class RemarkViewModel
    {
        public int RequestId { get; set; }
        public string Status { get; set; } = "";
        public string Remark { get; set; } = "";
    }
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class RecentRequest
    {
        public int RequestId { get; set; }
        public string MemberName { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime RequestDate { get; set; }
    }

    public class OverdueBook
    {
        public string MemberName { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public DateTime DueDate { get; set; }
        public int DaysLate { get; set; }
        public decimal FineAmount { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public List<RecentRequest> RecentRequests { get; set; } = new();
        public List<OverdueBook> OverdueBooks { get; set; } = new();
    }

    public class LibrarianDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public List<BookRequest> PendingRequests { get; set; } = new();
        public List<OverdueBook> OverdueBooks { get; set; } = new();
    }
    public class BookSuggestion
    {
        public int SuggestionId { get; set; }
        public string MemberName { get; set; } = "";
        public string CNIC { get; set; } = "";
        public string ServiceNo { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public string? AuthorName { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "Pending";
        public string? AdminRemark { get; set; }
        public DateTime SuggestedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SuggestionStatusViewModel
    {
        public int SuggestionId { get; set; }
        public string Status { get; set; } = "";
        public string AdminRemark { get; set; } = "";
    }
    public class Announcement
    {
        public int AnnouncementId { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string PostedBy { get; set; } = "";
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Reservation
    {
        public int ReservationId { get; set; }
        public string MemberName { get; set; } = "";
        public string CNIC { get; set; } = "";
        public string ServiceNo { get; set; } = "";
        public int BookId { get; set; }
        public string BookTitle { get; set; } = "";
        public string BookNumber { get; set; } = "";
        public int AvailableCopies { get; set; }
        public string Status { get; set; } = "Waiting";
        public DateTime ReservedAt { get; set; }
        public DateTime? NotifiedAt { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class BookRating
    {
        public int RatingId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = "";
        public string MemberName { get; set; } = "";
        public string CNIC { get; set; } = "";
        public int Rating { get; set; }
        public string? Review { get; set; }
        public DateTime RatedAt { get; set; }
    }

    public class PopularBook
    {
        public int BookId { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string BookNumber { get; set; } = "";
        public string ShelfLocation { get; set; } = "";
        public string Category { get; set; } = "";
        public int TotalRequests { get; set; }
        public double AvgRating { get; set; }
    }

    public class MemberHistoryRecord
    {
        public string MemberName { get; set; } = "";
        public string CNIC { get; set; } = "";
        public string ServiceNo { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public string BookNumber { get; set; } = "";
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public decimal FineAmount { get; set; }
        public bool FinePaid { get; set; }
        public int RecordId { get; set; }
    }
}
