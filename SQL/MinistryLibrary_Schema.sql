-- ============================================================
-- DATABASE: MinistryLibrary
-- PROJECT:  MoDLibrary
-- ============================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'MinistryLibrary')
BEGIN
    ALTER DATABASE MinistryLibrary SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MinistryLibrary;
END
GO

CREATE DATABASE MinistryLibrary;
GO

USE MinistryLibrary;
GO

-- ============================================================
-- TABLE: Roles
-- ============================================================
CREATE TABLE Roles (
    RoleId   INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL
);
GO

INSERT INTO Roles (RoleName) VALUES ('Admin'), ('Librarian'), ('Member');
GO

-- ============================================================
-- TABLE: Users
-- ============================================================
CREATE TABLE Users (
    UserId       INT PRIMARY KEY IDENTITY(1,1),
    FullName     NVARCHAR(100) NOT NULL,
    Username     NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    RoleId       INT           NOT NULL REFERENCES Roles(RoleId),
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- TABLE: Wings
-- ============================================================
CREATE TABLE Wings (
    WingId   INT PRIMARY KEY IDENTITY(1,1),
    WingName NVARCHAR(50) NOT NULL,
    IsActive BIT          NOT NULL DEFAULT 1
);
GO

INSERT INTO Wings (WingName) VALUES ('AS-I'), ('AS-II'), ('AS-III'), ('AS-IV');
GO

-- ============================================================
-- TABLE: Sections
-- ============================================================
CREATE TABLE Sections (
    SectionId   INT PRIMARY KEY IDENTITY(1,1),
    SectionName NVARCHAR(50) NOT NULL,
    WingId      INT          NOT NULL REFERENCES Wings(WingId),
    IsActive    BIT          NOT NULL DEFAULT 1
);
GO

DECLARE @WingId INT;
DECLARE @i INT = 1;

SELECT @WingId = WingId FROM Wings WHERE WingName = 'AS-I';
SET @i = 1;
WHILE @i <= 32
BEGIN
    INSERT INTO Sections (SectionName, WingId) VALUES ('D-' + CAST(@i AS NVARCHAR), @WingId);
    SET @i = @i + 1;
END

SELECT @WingId = WingId FROM Wings WHERE WingName = 'AS-II';
SET @i = 1;
WHILE @i <= 32
BEGIN
    INSERT INTO Sections (SectionName, WingId) VALUES ('D-' + CAST(@i AS NVARCHAR), @WingId);
    SET @i = @i + 1;
END

SELECT @WingId = WingId FROM Wings WHERE WingName = 'AS-III';
SET @i = 1;
WHILE @i <= 32
BEGIN
    INSERT INTO Sections (SectionName, WingId) VALUES ('D-' + CAST(@i AS NVARCHAR), @WingId);
    SET @i = @i + 1;
END

SELECT @WingId = WingId FROM Wings WHERE WingName = 'AS-IV';
SET @i = 1;
WHILE @i <= 32
BEGIN
    INSERT INTO Sections (SectionName, WingId) VALUES ('D-' + CAST(@i AS NVARCHAR), @WingId);
    SET @i = @i + 1;
END
GO

-- ============================================================
-- TABLE: Books
-- ============================================================
CREATE TABLE Books (
    BookId          INT PRIMARY KEY IDENTITY(1,1),
    Title           NVARCHAR(200) NOT NULL,
    Author          NVARCHAR(150) NOT NULL,
    BookNumber      NVARCHAR(50)  NOT NULL UNIQUE,
    ShelfLocation   NVARCHAR(100) NOT NULL,
    TotalCopies     INT           NOT NULL DEFAULT 1,
    AvailableCopies INT           NOT NULL DEFAULT 1,
    Publisher       NVARCHAR(150),
    PublishedYear   INT,
    ISBN            NVARCHAR(20),
    Category        NVARCHAR(100),
    IsActive        BIT           NOT NULL DEFAULT 1,
    AddedAt         DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- TABLE: BookRequests
-- ============================================================
CREATE TABLE BookRequests (
    RequestId    INT PRIMARY KEY IDENTITY(1,1),
    MemberName   NVARCHAR(100) NOT NULL,
    CNIC         NVARCHAR(20)  NOT NULL,
    ServiceNo    NVARCHAR(50)  NOT NULL,
    WingId       INT           NOT NULL REFERENCES Wings(WingId),
    SectionId    INT           NOT NULL REFERENCES Sections(SectionId),
    BookId       INT           NOT NULL REFERENCES Books(BookId),
    Status       NVARCHAR(30)  NOT NULL DEFAULT 'Pending',
    -- Status: Pending, Approved, Rejected, Collected
    Remark       NVARCHAR(500),
    RequestDate  DATETIME      NOT NULL DEFAULT GETDATE(),
    UpdatedAt    DATETIME
);
GO

-- ============================================================
-- TABLE: IssuedBooks
-- ============================================================
CREATE TABLE IssuedBooks (
    IssuedId     INT PRIMARY KEY IDENTITY(1,1),
    RequestId    INT      NOT NULL REFERENCES BookRequests(RequestId),
    IssueDate    DATETIME NOT NULL DEFAULT GETDATE(),
    DueDate      DATETIME NOT NULL,
    ReturnDate   DATETIME,
    IsReturned   BIT      NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- TABLE: Fines
-- ============================================================
CREATE TABLE Fines (
    FineId      INT PRIMARY KEY IDENTITY(1,1),
    IssuedId    INT           NOT NULL REFERENCES IssuedBooks(IssuedId),
    FineAmount  DECIMAL(10,2) NOT NULL DEFAULT 0,
    IsPaid      BIT           NOT NULL DEFAULT 0,
    PaidDate    DATETIME,
    CalculatedAt DATETIME     NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- TABLE: Notifications
-- ============================================================
CREATE TABLE Notifications (
    NotificationId INT PRIMARY KEY IDENTITY(1,1),
    RequestId      INT           NOT NULL REFERENCES BookRequests(RequestId),
    Message        NVARCHAR(500) NOT NULL,
    IsRead         BIT           NOT NULL DEFAULT 0,
    CreatedAt      DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
-- SEED: Default Admin User (Password: Admin@123)
-- SHA256 hash stored - handled in app via BCrypt
-- ============================================================
INSERT INTO Users (FullName, Username, PasswordHash, RoleId)
VALUES 
('System Administrator', 'admin',    'Admin@123',     1),
('Head Librarian',       'librarian','Librarian@123', 2);
GO

-- ============================================================
-- SEED: Sample Books
-- ============================================================
INSERT INTO Books (Title, Author, BookNumber, ShelfLocation, TotalCopies, AvailableCopies, Publisher, PublishedYear, Category)
VALUES
('The Art of War',           'Sun Tzu',           'BK-001', 'Shelf A - Row 1', 3, 3, 'Oxford Press',    500,  'Military Strategy'),
('Leadership in War',        'Arthur Wellesley',  'BK-002', 'Shelf A - Row 2', 2, 2, 'Military Press',  2005, 'Leadership'),
('Modern Military Strategy', 'Colin Gray',        'BK-003', 'Shelf B - Row 1', 2, 2, 'Routledge',       2010, 'Military Strategy'),
('The Prince',               'Niccolò Machiavelli','BK-004','Shelf B - Row 2', 1, 1, 'Penguin Classics', 1532, 'Politics'),
('Defense Management',       'James Dobbins',     'BK-005', 'Shelf C - Row 1', 4, 4, 'RAND Corp',       2015, 'Management'),
('National Security Law',    'John Norton Moore', 'BK-006', 'Shelf C - Row 2', 2, 2, 'Carolina Press',  2018, 'Law'),
('Military Ethics',          'Martin Cook',       'BK-007', 'Shelf D - Row 1', 3, 3, 'SUNY Press',      2013, 'Ethics'),
('Strategic Studies',        'Lawrence Freedman', 'BK-008', 'Shelf D - Row 2', 2, 2, 'Oxford Press',    2017, 'Strategy');
GO

-- ============================================================
-- STORED PROCEDURES
-- ============================================================

-- Get all wings
CREATE PROCEDURE sp_GetWings
AS
BEGIN
    SELECT WingId, WingName FROM Wings WHERE IsActive = 1 ORDER BY WingName;
END
GO

-- Get sections by wing
CREATE PROCEDURE sp_GetSectionsByWing
    @WingId INT
AS
BEGIN
    SELECT SectionId, SectionName FROM Sections WHERE WingId = @WingId AND IsActive = 1 ORDER BY SectionName;
END
GO

-- Search books
CREATE PROCEDURE sp_SearchBooks
    @SearchTerm NVARCHAR(200)
AS
BEGIN
    SELECT b.BookId, b.Title, b.Author, b.BookNumber, b.ShelfLocation,
           b.TotalCopies, b.AvailableCopies, b.Publisher, b.PublishedYear,
           b.Category, b.ISBN,
           CASE WHEN b.AvailableCopies > 0 THEN 'Available' ELSE 'Not Available' END AS Availability
    FROM Books b
    WHERE b.IsActive = 1
      AND (b.Title LIKE '%' + @SearchTerm + '%'
        OR b.Author LIKE '%' + @SearchTerm + '%'
        OR b.BookNumber LIKE '%' + @SearchTerm + '%')
    ORDER BY b.Title;
END
GO

-- Get all books
CREATE PROCEDURE sp_GetAllBooks
AS
BEGIN
    SELECT b.BookId, b.Title, b.Author, b.BookNumber, b.ShelfLocation,
           b.TotalCopies, b.AvailableCopies, b.Publisher, b.PublishedYear,
           b.Category, b.ISBN, b.IsActive,
           CASE WHEN b.AvailableCopies > 0 THEN 'Available' ELSE 'Not Available' END AS Availability
    FROM Books b
    ORDER BY b.Title;
END
GO

-- Submit book request
CREATE PROCEDURE sp_SubmitBookRequest
    @MemberName  NVARCHAR(100),
    @CNIC        NVARCHAR(20),
    @ServiceNo   NVARCHAR(50),
    @WingId      INT,
    @SectionId   INT,
    @BookId      INT,
    @NewRequestId INT OUTPUT
AS
BEGIN
    INSERT INTO BookRequests (MemberName, CNIC, ServiceNo, WingId, SectionId, BookId)
    VALUES (@MemberName, @CNIC, @ServiceNo, @WingId, @SectionId, @BookId);
    SET @NewRequestId = SCOPE_IDENTITY();

    INSERT INTO Notifications (RequestId, Message)
    VALUES (@NewRequestId, 'New book request from ' + @MemberName + ' for BookId ' + CAST(@BookId AS NVARCHAR));
END
GO

-- Get pending requests for librarian
CREATE PROCEDURE sp_GetPendingRequests
AS
BEGIN
    SELECT r.RequestId, r.MemberName, r.CNIC, r.ServiceNo,
           w.WingName, s.SectionName,
           b.Title AS BookTitle, b.BookNumber, b.ShelfLocation,
           r.Status, r.Remark, r.RequestDate
    FROM BookRequests r
    JOIN Wings    w ON r.WingId    = w.WingId
    JOIN Sections s ON r.SectionId = s.SectionId
    JOIN Books    b ON r.BookId    = b.BookId
    ORDER BY r.RequestDate DESC;
END
GO

-- Send remark / update request status
CREATE PROCEDURE sp_UpdateRequestStatus
    @RequestId INT,
    @Status    NVARCHAR(30),
    @Remark    NVARCHAR(500)
AS
BEGIN
    UPDATE BookRequests
    SET Status    = @Status,
        Remark    = @Remark,
        UpdatedAt = GETDATE()
    WHERE RequestId = @RequestId;
END
GO

-- Issue book (mark as issued)
CREATE PROCEDURE sp_IssueBook
    @RequestId INT
AS
BEGIN
    DECLARE @BookId INT;
    SELECT @BookId = BookId FROM BookRequests WHERE RequestId = @RequestId;

    INSERT INTO IssuedBooks (RequestId, IssueDate, DueDate)
    VALUES (@RequestId, GETDATE(), DATEADD(DAY, 15, GETDATE()));

    UPDATE Books SET AvailableCopies = AvailableCopies - 1 WHERE BookId = @BookId;

    UPDATE BookRequests SET Status = 'Collected', UpdatedAt = GETDATE()
    WHERE RequestId = @RequestId;
END
GO

-- Return book and calculate fine
CREATE PROCEDURE sp_ReturnBook
    @IssuedId INT
AS
BEGIN
    DECLARE @DueDate    DATETIME;
    DECLARE @BookId     INT;
    DECLARE @RequestId  INT;
    DECLARE @FineAmount DECIMAL(10,2) = 0;
    DECLARE @DaysLate   INT = 0;

    SELECT @DueDate = ib.DueDate, @RequestId = ib.RequestId
    FROM IssuedBooks ib WHERE ib.IssuedId = @IssuedId;

    SELECT @BookId = BookId FROM BookRequests WHERE RequestId = @RequestId;

    UPDATE IssuedBooks
    SET ReturnDate = GETDATE(), IsReturned = 1
    WHERE IssuedId = @IssuedId;

    UPDATE Books SET AvailableCopies = AvailableCopies + 1 WHERE BookId = @BookId;

    IF GETDATE() > @DueDate
    BEGIN
        SET @DaysLate   = DATEDIFF(DAY, @DueDate, GETDATE());
        SET @FineAmount = @DaysLate * 50.00;

        INSERT INTO Fines (IssuedId, FineAmount)
        VALUES (@IssuedId, @FineAmount);
    END

    SELECT @FineAmount AS FineAmount, @DaysLate AS DaysLate;
END
GO

-- Get all issued books
CREATE PROCEDURE sp_GetAllIssuedBooks
AS
BEGIN
    SELECT ib.IssuedId, r.MemberName, r.CNIC, r.ServiceNo,
           w.WingName, s.SectionName,
           b.Title AS BookTitle, b.BookNumber,
           ib.IssueDate, ib.DueDate, ib.ReturnDate, ib.IsReturned,
           CASE
               WHEN ib.IsReturned = 1 THEN 'Returned'
               WHEN GETDATE() > ib.DueDate THEN 'Overdue'
               ELSE 'Active'
           END AS BookStatus,
           CASE
               WHEN ib.IsReturned = 0 AND GETDATE() > ib.DueDate
               THEN DATEDIFF(DAY, ib.DueDate, GETDATE()) * 50
               ELSE 0
           END AS PendingFine
    FROM IssuedBooks ib
    JOIN BookRequests r ON ib.RequestId = r.RequestId
    JOIN Wings        w ON r.WingId     = w.WingId
    JOIN Sections     s ON r.SectionId  = s.SectionId
    JOIN Books        b ON r.BookId     = b.BookId
    ORDER BY ib.IssueDate DESC;
END
GO

-- Get fines
CREATE PROCEDURE sp_GetFines
AS
BEGIN
    SELECT f.FineId, r.MemberName, r.CNIC, r.ServiceNo,
           b.Title AS BookTitle, b.BookNumber,
           ib.IssueDate, ib.DueDate, ib.ReturnDate,
           f.FineAmount, f.IsPaid, f.PaidDate, f.CalculatedAt
    FROM Fines f
    JOIN IssuedBooks  ib ON f.IssuedId   = ib.IssuedId
    JOIN BookRequests r  ON ib.RequestId = r.RequestId
    JOIN Books        b  ON r.BookId     = b.BookId
    ORDER BY f.CalculatedAt DESC;
END
GO

-- Mark fine as paid
CREATE PROCEDURE sp_MarkFinePaid
    @FineId INT
AS
BEGIN
    UPDATE Fines SET IsPaid = 1, PaidDate = GETDATE() WHERE FineId = @FineId;
END
GO

-- Get unread notifications
CREATE PROCEDURE sp_GetUnreadNotifications
AS
BEGIN
    SELECT n.NotificationId, n.Message, n.CreatedAt, n.RequestId,
           r.MemberName, b.Title AS BookTitle
    FROM Notifications n
    JOIN BookRequests r ON n.RequestId = r.RequestId
    JOIN Books        b ON r.BookId    = b.BookId
    WHERE n.IsRead = 0
    ORDER BY n.CreatedAt DESC;
END
GO

-- Mark notification read
CREATE PROCEDURE sp_MarkNotificationRead
    @NotificationId INT
AS
BEGIN
    UPDATE Notifications SET IsRead = 1 WHERE NotificationId = @NotificationId;
END
GO

-- Get dashboard stats
CREATE PROCEDURE sp_GetDashboardStats
AS
BEGIN
    SELECT
        (SELECT COUNT(*) FROM Books WHERE IsActive = 1)                        AS TotalBooks,
        (SELECT COUNT(*) FROM IssuedBooks WHERE IsReturned = 0)                AS BooksIssued,
        (SELECT COUNT(*) FROM BookRequests WHERE Status = 'Pending')           AS PendingRequests,
        (SELECT COUNT(*) FROM Fines WHERE IsPaid = 0)                          AS UnpaidFines,
        (SELECT ISNULL(SUM(FineAmount),0) FROM Fines WHERE IsPaid = 0)        AS TotalFineAmount,
        (SELECT COUNT(*) FROM Users WHERE RoleId = 2 AND IsActive = 1)        AS TotalLibrarians,
        (SELECT COUNT(*) FROM IssuedBooks WHERE GETDATE() > DueDate AND IsReturned = 0) AS OverdueBooks;
END
GO

-- Add book
CREATE PROCEDURE sp_AddBook
    @Title         NVARCHAR(200),
    @Author        NVARCHAR(150),
    @BookNumber    NVARCHAR(50),
    @ShelfLocation NVARCHAR(100),
    @TotalCopies   INT,
    @Publisher     NVARCHAR(150),
    @PublishedYear INT,
    @ISBN          NVARCHAR(20),
    @Category      NVARCHAR(100)
AS
BEGIN
    INSERT INTO Books (Title, Author, BookNumber, ShelfLocation, TotalCopies, AvailableCopies, Publisher, PublishedYear, ISBN, Category)
    VALUES (@Title, @Author, @BookNumber, @ShelfLocation, @TotalCopies, @TotalCopies, @Publisher, @PublishedYear, @ISBN, @Category);
END
GO

-- Update book
CREATE PROCEDURE sp_UpdateBook
    @BookId        INT,
    @Title         NVARCHAR(200),
    @Author        NVARCHAR(150),
    @BookNumber    NVARCHAR(50),
    @ShelfLocation NVARCHAR(100),
    @TotalCopies   INT,
    @Publisher     NVARCHAR(150),
    @PublishedYear INT,
    @ISBN          NVARCHAR(20),
    @Category      NVARCHAR(100)
AS
BEGIN
    UPDATE Books
    SET Title         = @Title,
        Author        = @Author,
        BookNumber    = @BookNumber,
        ShelfLocation = @ShelfLocation,
        TotalCopies   = @TotalCopies,
        Publisher     = @Publisher,
        PublishedYear = @PublishedYear,
        ISBN          = @ISBN,
        Category      = @Category
    WHERE BookId = @BookId;
END
GO

-- Delete (deactivate) book
CREATE PROCEDURE sp_DeleteBook
    @BookId INT
AS
BEGIN
    UPDATE Books SET IsActive = 0 WHERE BookId = @BookId;
END
GO

-- Manage librarians
CREATE PROCEDURE sp_GetLibrarians
AS
BEGIN
    SELECT UserId, FullName, Username, IsActive, CreatedAt
    FROM Users WHERE RoleId = 2
    ORDER BY FullName;
END
GO

CREATE PROCEDURE sp_AddLibrarian
    @FullName     NVARCHAR(100),
    @Username     NVARCHAR(50),
    @PasswordHash NVARCHAR(256)
AS
BEGIN
    INSERT INTO Users (FullName, Username, PasswordHash, RoleId)
    VALUES (@FullName, @Username, @PasswordHash, 2);
END
GO

CREATE PROCEDURE sp_UpdateLibrarian
    @UserId   INT,
    @FullName NVARCHAR(100),
    @Username NVARCHAR(50),
    @IsActive BIT
AS
BEGIN
    UPDATE Users
    SET FullName = @FullName, Username = @Username, IsActive = @IsActive
    WHERE UserId = @UserId AND RoleId = 2;
END
GO

CREATE PROCEDURE sp_DeleteLibrarian
    @UserId INT
AS
BEGIN
    UPDATE Users SET IsActive = 0 WHERE UserId = @UserId AND RoleId = 2;
END
GO

-- Login
CREATE PROCEDURE sp_Login
    @Username NVARCHAR(50),
    @Password NVARCHAR(256)
AS
BEGIN
    SELECT u.UserId, u.FullName, u.Username, r.RoleName
    FROM Users u
    JOIN Roles r ON u.RoleId = r.RoleId
    WHERE u.Username = @Username
      AND u.PasswordHash = @Password
      AND u.IsActive = 1;
END
GO

PRINT 'MinistryLibrary database created successfully.';
GO
