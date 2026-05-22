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

| Role        | Description                                                                                                       |
| ----------- | ----------------------------------------------------------------------------------------------------------------- |
| **Admin**   | Full system access. Manages courses, users, all exams, and violations system-wide.                                |
| **Tutor**   | Manages assigned courses. Creates exams, uploads materials, posts announcements, and monitors exam violations.    |
| **Student** | Accesses enrolled courses, downloads materials, submits assignments, and takes exams under anti-cheat monitoring. |

---

## 🚀 Comprehensive Feature List (F1 - F31)

- **F1: Secure Login & Role-Based Redirect** (JWT, single-session enforcement)
- **F2: Email Verification** (SMTP integration, verification tokens)
- **F3: Password Reset Via Email** (forgot-password workflow)
- **F4: Profile Management** (WebP avatar upload, edit profile, change password and reset tour guide)
- **F5: Public Registration** (Student or Tutor role selection)
- **F6: Course Management & Enrollment** (CRUD, bulk review, enroll/drop)
- **F7: File Sharing** (Course Materials)
- **F8: Announcements** (post, feed, priority pinning)
- **F9: Assignment Management System** (task lifecycle, ZIP bulk export)
- **F10: Notifications** (In-app and email) & Advanced Enrollment Polish
- **F11: Exam Management & Notifications** (CRUD, publish workflow, reminders)
- **F12: Re-Attempt Requests**
- **F13: Question Bank Management** (Centralized course-level bank)
- **F14/F15: Exam Engine + Secure Token** (countdown, navigator, auto-submit)
- **F16: Exam Results & Auto-Grading** (confetti, per-question review)
- **F17: Grade Management Panel** (bulk publish, CSV export)
- **F18: Anti-Cheating Engine** contains:
  - **18-1:** Pre-exam rules acknowledgment modal
  - **18-2:** Keyboard shortcut blocking (CTRL+C, ... etc)
  - **18-3:** Window resize / minimize / split detection
  - **18-4:** Mouse monitoring (pattern analysis)
  - **18-5:** Selection by mouse blocking
  - **18-6:** Blocking Right click options to do operations
  - **18-7:** Violation intelligence layer (severity + cooldown)
  - **18-8:** Warning system and force-submit (3-strike escalation)
  - **18-9:** Monitoring continuity on reconnect
- **F19: Session timeline view**
- **F20: Violation timeline view**
- **F21: Tutor monitoring dashboard**
- **F22: Admin dashboard and users management**
- **F23: SHIELDON AI Assistant** (Gemini-powered chatbot, backend proxy, blocked during exams)
- **F24: Shepherd.js onboarding tours** (Role-based guided tours)
- **F25: SHIELDON AI Assistant** _(Integrated)_
- **F26: Real-time Chat System**
- **F27: Dynamic QR Attendance Tracking**
- **F28: Calendar & Schedule View**
- **F29: Online Payment Gateway**
- **F30: Dark / Light mode**
- **F31: English / Arabic**

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

## Installation & Setup (Step-by-Step Guide)

Follow these comprehensive steps in order to properly get the project running on your device from scratch.

### 1. Clone the Repository

This downloads the project files to your computer.

1. Open your computer's terminal (or Command Prompt / PowerShell on Windows).
2. Run this command to download the code:
   ```bash
   git clone https://github.com/ahmed-samir-dev/SHIELDON.git
   ```
3. Navigate into the newly created project folder:
   ```bash
   cd SHIELDON
   ```

### 2. Backend Database Configuration (CRUCIAL STEP)

Before running the backend, you must configure it to connect to your local SQL Server.

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Open the file `SHIELDON.API/appsettings.json` and `SHIELDON.API/appsettings.Development.json` in a text editor (like VS Code or Notepad).
3. Locate the `"ConnectionStrings"` block. You must update the `"DefaultConnection"` string to match your local SQL Server instance name.
   - **How to find your Server Name**: Open SQL Server Management Studio (SSMS). The prompt that asks you to connect will show the `Server name` (e.g., `DESKTOP-ABC123\SQLEXPRESS` or `(localdb)\MSSQLLocalDB`).
   - **Update the string**: Replace the Server part of the connection string. Make sure you use double backslashes `\\` for escaping in JSON.

   _Example of a correct connection string for SQL Express:_

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_PC_NAME\\SQLEXPRESS;Database=SHIELDON_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
   }
   ```

4. Save the file(s).

### 3. Initialize and Update the Database

Now we need to tell Entity Framework (the ORM) to build the tables in your SQL Server.

1. Keep your terminal open in the `backend` folder (NOT inside `SHIELDON.API`).
2. First, ensure you have the EF Core CLI tools installed globally on your machine. Run:
   ```bash
   dotnet tool install -g dotnet-ef
   ```
   _(If it says it is already installed, that's perfect!)_
3. Now, run the magical command to apply all migrations and build your database schema from scratch:
   ```bash
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```
4. **Verification**: Open SSMS, connect to your server, expand "Databases", and you should now see `SHIELDON_DB` with all the tables created!

### 4. Run the Backend API

1. Navigate into the API startup project folder:
   ```bash
   cd SHIELDON.API
   ```
2. Start the server:
   ```bash
   dotnet run
   ```
   _(Alternatively, if you want hot-reloading while making changes, use `dotnet watch run`)_
3. The backend is now actively running! Open your browser and go to the live API documentation at:
   👉 `http://localhost:5000/swagger`
4. Keep this terminal window open.

### 5. Run the Frontend Application

1. Open a **new, completely separate** terminal window or tab (you must leave the backend terminal running!).
2. Navigate to the root `shieldon-lms` folder, then into the frontend directory:
   ```bash
   cd path/to/shieldon-lms/frontend
   ```
3. Install all required third-party libraries (Node Modules):
   ```bash
   npm install
   ```
   _(This may take a couple of minutes depending on your internet connection)._
4. Start the Angular development server:
   ```bash
   npm start
   ```
5. Wait until the terminal says the compilation was successful.
6. Finally, open your web browser and go to:
   👉 `http://localhost:4201`

**Congratulations! You are now running SHIELDON on your local machine!**

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

The backend exposes a comprehensive RESTful API for all system functions (Authentication, Courses, Exams, Chat, Payments, etc.).

For a complete, interactive list of all API endpoints and their required payloads, please run the backend locally and visit the automatically generated Swagger documentation at:
👉 `http://localhost:5000/swagger/index.html`

---

## Git Workflow

To keep the project organized, we follow a simple Git workflow:

- **Feature Branches**: Never work directly on `main`. Create a new branch for every feature or fix:
  ```bash
  git checkout -b feature/your-feature-name
  ```
- **Pull Requests**: When a feature is complete, push it to GitHub and create a Pull Request to merge it into the `develop` or `main` branch.

---

_SHIELDON - "Integrity You Can Trust"_
