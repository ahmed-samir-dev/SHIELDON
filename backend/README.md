# SHIELDON - Backend

> The core API, business logic, and database orchestration for the SHIELDON Learning Management System & Anti-Cheating Engine.
> Built with .NET 9.

---

## Project Overview

This directory contains the backend application for the **SHIELDON** platform. It provides the RESTful API endpoints, business logic orchestration, and database persistence required to power the system.

The backend is responsible for:
- Secure JWT authentication and role management.
- Orchestrating the Exam Engine logic and auto-grading.
- Persisting and analyzing anti-cheat violation logs.
- Serving analytical data for dashboards.

---

## Technology Stack

- **Framework**: .NET 9 ASP.NET Core Web API
- **Language**: C# (Nullable reference types enabled)
- **Architecture**: Clean Architecture + Vertical Slice Hybrid
- **ORM**: Entity Framework Core 9 (Code-First)
- **Database**: Microsoft SQL Server
- **Authentication**: JWT Bearer Tokens (Access + Refresh)
- **Mail System**: MailKit / MimeKit for SMTP emails
- **Validation**: FluentValidation
- **Mapping**: AutoMapper
- **Logging**: Serilog with structured sinks

---

## Prerequisites (For Beginners)

If you have zero previous technical background and want to run the backend part on your device, you need to install these tools first:

1. **.NET 9 SDK**: This is the software development kit required to build and run .NET applications.
   - Download it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0).
   - Follow the default installation steps.
2. **SQL Server**: This is the database where all users, courses, and exam data will be stored.
   - Download **SQL Server Express** (free) from [Microsoft](https://www.microsoft.com/sql-server/sql-server-downloads).
   - Choose the "Basic" installation type.
3. **SSMS (SQL Server Management Studio)**: This is a visual program to look at your database.
   - Download it from [Microsoft Docs](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).

---

## Installation & Setup

Follow these steps to get the backend running:

1. Open your terminal (or Command Prompt on Windows).
2. Navigate to the backend directory of the project:
   ```bash
   cd path/to/SHIELDON/backend
   ```
3. Open the file `appsettings.json` in a text editor like Notepad or VS Code.
4. Find the `"DefaultConnection"` line and update the connection string to point to your installed SQL Server instance. It usually looks like this:
   ```json
   "DefaultConnection": "Server=YOUR_COMPUTER_NAME\\SQLEXPRESS;Database=SHIELDON_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
5. Open your terminal in the `backend` folder and run this command to create all the database tables automatically:
   ```bash
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```
   *(Note: If the terminal says command not found, install the tool first by running: `dotnet tool install -g dotnet-ef`)*
6. Finally, start the backend API:
   ```bash
   cd SHIELDON.API
   dotnet run
   ```
7. The backend is now running! You can see the live documentation and test endpoints at:
   ```
   http://localhost:5000/swagger
   ```

---

## Architecture & Layers

The backend follows **Clean Architecture** principles to keep the code organized and independent:

- **SHIELDON.Domain**: The heart of the system. Contains C# entities representing database tables, enums, and constants. It has zero external dependencies.
- **SHIELDON.Application**: Contains the business logic and use cases organized by feature slices. This is where commands, handlers, DTOs, and request validators live.
- **SHIELDON.Infrastructure**: Handles implementation details like database persistence (EF Core), file storage, and email sending.
- **SHIELDON.API**: The entry point. Contains thin HTTP controllers that receive requests and delegate them to the application layer. No business logic lives here.

---

## API Endpoints Reference

The following API modules are implemented and available for interaction via Swagger:

### Authentication & Account
- `POST /api/auth/login` - Authenticate user & return JWT tokens.
- `POST /api/auth/register` - Create a new user account.
- `POST /api/auth/verify-email` - Validate email verification token.
- `POST /api/auth/forgot-password` - Initiate password reset.
- `POST /api/auth/reset-password` - Complete password reset.
- `POST /api/auth/refresh` - Refresh expired access tokens.

### User Profile
- `GET /api/profile` - Get current user profile details.
- `PATCH /api/profile` - Update profile information.
- `POST /api/profile/picture` - Upload a profile avatar (WebP).

### Learning Management
- `GET /api/courses` - List courses (supports paging and filtering).
- `POST /api/courses` - Create a new course (Tutor/Admin).
- `POST /api/courses/{id}/enroll` - Enroll a student in a course.
- `GET /api/materials` - List course materials and files.
- `GET /api/announcements` - Fetch course announcements.
- `GET /api/assignments` - List assignments and submissions.

### Examination & Integrity
- `GET /api/exams` - List scheduled exams.
- `POST /api/exams` - Create a new exam.
- `GET /api/coursequestionbank` - Manage course question bank.
- `POST /api/examattempts` - Start an exam attempt.
- `POST /api/violations` - Log an anti-cheat violation.
- `GET /api/examresults` - Fetch graded exam results.
- `GET /api/reattempt` - Manage re-attempt requests.

### Miscellaneous
- `GET /api/notifications` - Fetch user notifications.
- `GET /api/monitoring` - Fetch data for dashboards.
- `POST /api/ai/chat` - Interact with the SHIELDON AI Assistant.
