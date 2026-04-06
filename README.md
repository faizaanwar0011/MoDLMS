# MoDLibrary — Ministry of Defence Library Management System

## Tech Stack
- **Frontend:** ASP.NET MVC Razor Views + Bootstrap 5
- **Backend:** ASP.NET Core 8 MVC (C#)
- **Database:** SQL Server (MinistryLibrary)
- **Real-time:** SignalR
- **Data Access:** ADO.NET with Stored Procedures

---

## Project Structure

```
MoDLibrary/
├── Controllers/
│   ├── AccountController.cs       ← Login / Logout
│   ├── AdminController.cs         ← Full admin management
│   ├── LibrarianController.cs     ← Librarian operations
│   └── MemberController.cs        ← Member book requests
├── Models/
│   └── Models.cs                  ← All model classes
├── DAL/
│   └── DatabaseHelper.cs          ← All DB operations (ADO.NET)
├── Hubs/
│   └── NotificationHub.cs         ← SignalR real-time hub
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _AdminLayout.cshtml
│   │   ├── _LibrarianLayout.cshtml
│   │   └── _MemberLayout.cshtml
│   ├── Account/
│   │   ├── Login.cshtml
│   │   └── AccessDenied.cshtml
│   ├── Admin/
│   │   ├── Dashboard.cshtml
│   │   ├── Books.cshtml
│   │   ├── AddBook.cshtml
│   │   ├── EditBook.cshtml
│   │   ├── IssuedBooks.cshtml
│   │   ├── Requests.cshtml
│   │   ├── Fines.cshtml
│   │   ├── Librarians.cshtml
│   │   ├── AddLibrarian.cshtml
│   │   └── EditLibrarian.cshtml
│   ├── Librarian/
│   │   ├── Dashboard.cshtml
│   │   ├── Search.cshtml
│   │   ├── Books.cshtml
│   │   ├── AddBook.cshtml
│   │   ├── EditBook.cshtml
│   │   ├── Requests.cshtml        ← With remark modal + issue button
│   │   ├── IssuedBooks.cshtml     ← With return book functionality
│   │   └── Fines.cshtml
│   └── Member/
│       ├── Search.cshtml
│       ├── RequestBook.cshtml     ← AJAX wing/section dropdowns
│       └── RequestConfirmation.cshtml
├── wwwroot/
│   ├── css/site.css
│   └── js/site.js
├── SQL/
│   └── MinistryLibrary_Schema.sql ← Run this first!
├── appsettings.json
├── Program.cs
└── MoDLibrary.csproj
```

---

## Setup Instructions

### Step 1 — Run the SQL Script

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server instance
3. Open the file: `SQL/MinistryLibrary_Schema.sql`
4. Press **F5** to execute
5. This will:
   - Create the `MinistryLibrary` database
   - Create all tables, stored procedures
   - Insert seed data (Wings, Sections, sample books)
   - Create default Admin and Librarian accounts

### Step 2 — Configure Connection String

Open `appsettings.json` and update the connection string if needed:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=MinistryLibrary;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

- `Server=.` means localhost default instance
- If using a named instance: `Server=.\SQLEXPRESS`
- If using Windows Auth (recommended): keep `Trusted_Connection=True`
- If using SQL Auth: replace with `User Id=sa;Password=yourpassword;`

### Step 3 — Open in Visual Studio

1. Open Visual Studio 2022
2. Open `MoDLibrary.csproj` (or the folder)
3. Wait for NuGet packages to restore automatically
4. Press **F5** to run

### Step 4 — Login

| Role       | Username    | Password       |
|------------|-------------|----------------|
| Admin      | `admin`     | `Admin@123`    |
| Librarian  | `librarian` | `Librarian@123`|
| Member     | No login — role is selected on login page |

---

## Features by Role

### Admin
- Dashboard with live stats
- Add / Edit / Remove books from catalog
- View all issued books (Active, Overdue, Returned)
- View all member requests
- Manage fines — mark as paid
- Add / Edit / Deactivate librarian accounts

### Librarian
- Dashboard with library stats
- **Search books** by title / author / book number → shows shelf location
- Add / Edit books
- View all requests → Send remark with quick templates → Mark as Issued
- View issued books → Process returns (auto-calculates fine)
- **Real-time popup notification** when a member submits a request
- Manage fines → Mark as paid

### Member
- Search books by title / author / book number
- View availability before requesting
- Fill issue request form:
  - Name (typed)
  - CNIC (typed, auto-formatted)
  - Service Number (typed)
  - Wing (dropdown — loaded from DB)
  - Section (dropdown — loaded from DB via AJAX based on selected Wing)
- Submit request → Librarian gets instant real-time popup notification
- Receive real-time remark notification from librarian

---

## Fine System

| Condition                  | Fine        |
|---------------------------|-------------|
| Returned within 15 days   | Rs. 0       |
| Returned after 15 days    | Rs. 50/day  |

Fine is auto-calculated on book return via stored procedure `sp_ReturnBook`.

---

## Database Tables

| Table          | Description                         |
|---------------|-------------------------------------|
| Roles          | Admin, Librarian, Member            |
| Users          | All user accounts                   |
| Wings          | AS-I, AS-II, AS-III, AS-IV          |
| Sections       | D-1 through D-32 (per wing, from DB)|
| Books          | Full book catalog                   |
| BookRequests   | Member issue requests               |
| IssuedBooks    | Physical issuance records           |
| Fines          | Fine records with paid status       |
| Notifications  | Real-time notification log          |

---

## NuGet Packages Required

These are defined in `MoDLibrary.csproj` and will auto-restore:

```
Microsoft.AspNetCore.SignalR     1.1.0
Microsoft.Data.SqlClient         5.2.1
BCrypt.Net-Next                  4.0.3
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Cannot connect to DB | Check connection string in appsettings.json |
| SignalR not working | Ensure `app.MapHub<NotificationHub>("/notificationHub")` is in Program.cs |
| Sections not loading | Check AJAX endpoint `/Member/GetSections?wingId=X` is reachable |
| Login fails | Make sure SQL script ran and seed users were inserted |
| Missing packages | Right-click solution → Restore NuGet Packages |
