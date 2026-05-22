# SHIELDON - Integrity You Can Trust

> A full-stack Learning Management System (LMS) with a built-in browser-native Anti-Cheating Engine.
> Built as a graduation project - no external exam-locking software required.

---

## Project Overview

**SHIELDON** is a comprehensive educational platform that combines a modern Learning Management System with a robust, browser-based Exam Integrity System. 

Most traditional LMS platforms depend on external software (like Safe Exam Browser or LockDown Browser) to enforce exam security, requiring students to download and install applications. **SHIELDON eliminates this dependency entirely** by building the Anti-Cheating Engine directly into the web platform using standard Web APIs.

---

## Technology Stack

### Frontend
- **Framework**: Angular 21 (Standalone Components)
- **Language**: TypeScript
- **Styling**: SCSS with CSS Custom Properties
- **Charts**: Apache ECharts (via ngx-echarts)
- **Icons**: Lucide Icons
- **Libraries**: SweetAlert2, ngx-toastr, canvas-confetti, Shepherd.js

### Backend
- **Framework**: .NET 9 ASP.NET Core Web API
- **Language**: C#
- **ORM**: Entity Framework Core 9 (Code-First)
- **Email**: MailKit / MimeKit
- **Validation**: FluentValidation
- **Mapping**: AutoMapper

### Database
- **Engine**: Microsoft SQL Server 2022

---

## Architecture

SHIELDON follows a **Clean Architecture + Vertical Slice Hybrid** approach:

- **SHIELDON.Domain**: Core business rules and entities (independent of any framework).
- **SHIELDON.Application**: Business logic organized by feature slices (Use Cases, DTOs, Validators).
- **SHIELDON.Infrastructure**: External implementations (Database persistence, Email service, File storage).
- **SHIELDON.API**: Thin HTTP controllers handling requests and delegating to the application layer.

---

## System Roles

| Role | Description |
|------|-------------|
| **Admin** | Full system access. Manages courses, users, all exams, and violations system-wide. |
| **Tutor** | Manages assigned courses. Creates exams, uploads materials, posts announcements, and monitors exam violations. |
| **Student** | Accesses enrolled courses, downloads materials, submits assignments, and takes exams under anti-cheat monitoring. |

---

## Features

### Authentication & User Management
- Secure Login with JWT Tokens (Access + Refresh tokens).
- Public Registration with role selection (Student/Tutor).
- Email Verification flow.
- Password Reset via email.
- Profile Management with WebP avatar upload.

### Core Learning Management System
- Course Management & Enrollment (Create, edit, delete courses; enroll/drop students).
- File Sharing (Tutors upload course materials; students download).
- Assignments System (Tutor posts assignments; students submit files; bulk ZIP export for tutors).
- Announcements Feed with priority pinning.
- In-App and Email Notifications.

### Examination Management System
- Centralized Question Bank (MCQ, True/False, Short Answer questions).
- Exam Creation & Scheduling with secure tokens.
- Question Randomization and Timed Exam Engine.
- Auto-Grading for objective questions.
- Grade Management Panel with weighted grading and CSV export.

### Anti-Cheating Engine
- **Fullscreen Enforcement**: Forces students to stay in fullscreen mode during exams.
- **Tab & Focus Detection**: Detects when a student switches tabs or windows.
- **Keyboard Shortcut Blocking**: Blocks copy/paste and system shortcuts.
- **Resize Detection**: Monitors window resizing or split-screen attempts.
- **Warning System**: Escalates warnings up to automatic submission on too many violations.

### Monitoring & Dashboards
- **Session Timeline**: Visual breakdown of student activity during an exam.
- **Violation Timeline**: Detailed logs of anti-cheat violations.
- **Dashboards**: Rich visual analytics powered by Apache ECharts for both Tutors and Admins.

### Enhancements
- **SHIELDON AI Assistant**: Gemini-powered chatbot for student support (blocked during exams).
- **Guided Tours**: Role-based onboarding tours powered by Shepherd.js.

---

## Prerequisites (For Beginners)

If you are new to development and want to run this project on your own computer, you need to download and install the following tools first. They are all free!

1. **Node.js**: This is required to run the frontend part.
   - Download the **LTS (Long Term Support)** version from [nodejs.org](https://nodejs.org/).
   - Run the installer and follow the default steps.
2. **.NET 9 SDK**: This is the engine that runs the backend part.
   - Download it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0).
   - Look for the ".NET SDK" installer for your operating system (Windows, Mac, or Linux).
3. **SQL Server**: This is the database where all users, courses, and exam data will be stored.
   - Download **SQL Server Express** from [Microsoft](https://www.microsoft.com/sql-server/sql-server-downloads).
   - Choose the "Basic" installation type.
4. **SSMS (SQL Server Management Studio)**: This is a visual program to look at your database.
   - Download it from [Microsoft Docs](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).
5. **Git**: This is a tool to copy project files from GitHub.
   - Download it from [git-scm.com](https://git-scm.com/).

---

## Installation & Setup

Follow these steps in order to get the project running on your device.

### 1. Clone the Repository
This downloads the project files to your computer.
1. Open your computer's terminal (or Command Prompt on Windows).
2. Type this command and press Enter:
   ```bash
   git clone https://github.com/[your-username]/shieldon-lms.git
   ```
3. Navigate into the project folder:
   ```bash
   cd shieldon-lms
   ```

### 2. Backend Setup
1. Navigate to the backend folder:
   ```bash
   cd backend
   ```
2. Open the file `appsettings.json` (or `appsettings.Development.json`) in a text editor like Notepad or VS Code.
3. Find the line that starts with `"DefaultConnection"` and update the connection string to point to your installed SQL Server instance. It usually looks something like this:
   ```json
   "DefaultConnection": "Server=YOUR_COMPUTER_NAME\\SQLEXPRESS;Database=SHIELDON_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   ```
4. Now, open your terminal in the `backend` folder and run this command to create all the database tables automatically:
   ```bash
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```
   *(Note: If the terminal says command not found, install the tool first by running: `dotnet tool install -g dotnet-ef`)*
5. Finally, start the backend API:
   ```bash
   cd SHIELDON.API
   dotnet run
   ```
   The backend is now running! You can see the live documentation at `http://localhost:5000/swagger`.

### 3. Frontend Setup
1. Open a **new, separate** terminal window or tab (keep the backend terminal running).
2. Navigate to the frontend folder:
   ```bash
   cd frontend
   ```
3. Install all the required libraries by running:
   ```bash
   npm install
   ```
   *(This may take a minute or two as it downloads everything needed).*
4. Start the frontend application:
   ```bash
   npm start
   ```
5. Open your web browser and go to `http://localhost:4201`. You should see the SHIELDON landing page!

---

## How to Test (Demo Accounts)

To test the different roles in the system, follow these instructions:

### 1. Test as Admin
The system comes with a pre-seeded Admin account (you cannot register an Admin account via the app for security reasons).
- **Email**: `admin@shieldon.com`
- **Password**: `Admin@Shieldon2025!`

### 2. Test as Tutor or Student
To test the Tutor or Student roles and experience the registration flow:
1. Go to the landing page and click on **Register** (or go to `/register`).
2. Fill in the details and select the role you want to test (**Tutor** or **Student**).
3. Complete the registration. You can now log in with the account you just created!

---

## API Endpoints Reference

The following API modules are implemented and available for interaction:

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

---

## Git Workflow

To keep the project organized, we follow a simple Git workflow:

- **Feature Branches**: Never work directly on `main`. Create a new branch for every feature or fix:
  ```bash
  git checkout -b feature/your-feature-name
  ```
- **Pull Requests**: When a feature is complete, push it to GitHub and create a Pull Request to merge it into the `develop` or `main` branch.

---

*SHIELDON - "Integrity You Can Trust"*
