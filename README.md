# 🛡️ SHIELDON - Integrity You Can Trust

<div align="center">
  <img src="https://img.shields.io/badge/.NET_9-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Angular_21-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular 21" />
  <img src="https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/SQL_Server_2022-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/Stripe-635BFF?style=for-the-badge&logo=stripe&logoColor=white" alt="Stripe" />
</div>

> 🎓 A full-stack Learning Management System (LMS) with a built-in browser-native Anti-Cheating Engine.
> Built as a graduation project — no external exam-locking software required.

---

## 📑 Table of Contents

- [🔭 Project Overview](#-project-overview)
- [⚙️ Technology Stack](#️-technology-stack)
- [🏛️ Architecture](#️-architecture)
- [👥 System Roles](#-system-roles)
- [🚀 Comprehensive Feature List (F1 – F30)](#-comprehensive-feature-list-f1--f30)
- [📋 Prerequisites (For Beginners)](#-prerequisites-for-beginners)
- [🔧 Installation & Setup (Step-by-Step Guide)](#-installation--setup-step-by-step-guide)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Backend Database Configuration](#2-backend-database-configuration-crucial-step)
  - [3. Initialize and Update the Database](#3-initialize-and-update-the-database)
  - [4. Run the Backend API](#4-run-the-backend-api)
  - [5. Run the Frontend Application](#5-run-the-frontend-application)
  - [6. Stripe Payment Setup](#6-stripe-payment-setup-optional)
- [🧪 How to Test (Demo Accounts)](#-how-to-test-demo-accounts)
- [📡 API Endpoints Reference](#-api-endpoints-reference)
- [🌿 Git Workflow](#-git-workflow)

---

## 🔭 Project Overview

**SHIELDON** is a comprehensive educational platform that combines a modern Learning Management System with a robust, browser-based Exam Integrity System.

Most traditional LMS platforms depend on external software (like Safe Exam Browser or LockDown Browser) to enforce exam security, requiring students to download and install applications. **SHIELDON eliminates this dependency entirely** by building the Anti-Cheating Engine directly into the web platform using standard Web APIs.

---

## ⚙️ Technology Stack

### 🖥️ Frontend
| Technology | Purpose |
|---|---|
| **Angular 21** | Framework (Standalone Components) |
| **TypeScript** | Language |
| **SCSS** | Styling with CSS Custom Properties |
| **Apache ECharts** | Charts & analytics (via ngx-echarts) |
| **Lucide Icons** | Icon library |
| **SweetAlert2** | Beautiful modal dialogs |
| **ngx-toastr** | Toast notifications |
| **canvas-confetti** | Celebration effects |
| **Shepherd.js** | Guided onboarding tours |

### 🔧 Backend
| Technology | Purpose |
|---|---|
| **.NET 9 ASP.NET Core** | Web API framework |
| **C#** | Language |
| **Entity Framework Core 9** | ORM (Code-First migrations) |
| **MailKit / MimeKit** | Email delivery (tested via [Mailtrap](https://mailtrap.io), production via **Google Gmail SMTP**) |
| **FluentValidation** | Request validation |
| **Stripe.net** | Payment processing |
| **Google Gemini API** | AI assistant (backend proxy) |

### 💽 Database
| Technology | Purpose |
|---|---|
| **Microsoft SQL Server 2022** | Primary relational database |

---

## 🏛️ Architecture

SHIELDON follows a **Clean Architecture + Vertical Slice Hybrid** approach:

```
┌─────────────────────────────────────────────────┐
│                  SHIELDON.API                   │  ← Thin HTTP Controllers
├─────────────────────────────────────────────────┤
│              SHIELDON.Application               │  ← Use Cases, DTOs, Validators
├─────────────────────────────────────────────────┤
│             SHIELDON.Infrastructure             │  ← DB, Email, File Storage
├─────────────────────────────────────────────────┤
│                SHIELDON.Domain                  │  ← Entities & Business Rules
├─────────────────────────────────────────────────┤
│                SHIELDON.Tests                   │  ← Unit & Integration Tests
└─────────────────────────────────────────────────┘
```

| Layer | Responsibility |
|---|---|
| **SHIELDON.API** | Thin HTTP controllers handling requests and delegating to the application layer. |
| **SHIELDON.Application** | Business logic organized by feature slices (Use Cases, DTOs, Validators). |
| **SHIELDON.Infrastructure** | External implementations (Database persistence, Email service, File storage, Payment gateway). |
| **SHIELDON.Domain** | Core business rules and entities (independent of any framework). |
| **SHIELDON.Tests** | Unit tests and integration tests to ensure correctness and prevent regressions. |

---

## 👥 System Roles

| Role | Description |
|---|---|---|
| **Admin** | Full system access. Manages courses, users, all exams, analytics, and violations system-wide. |
| **Tutor** | Manages assigned courses. Creates exams, uploads materials, posts announcements, monitors exam violations, and tracks attendance. |
| **Student** | Accesses enrolled courses, downloads materials, submits assignments, takes exams under anti-cheat monitoring, and makes payments. |

---

## 🚀 Comprehensive Feature List (F1 – F30)

| # | Feature | Details |
|---|---|---|
| F1 | **Secure Login & Role-Based Redirect** | JWT authentication, refresh tokens, single-session enforcement |
| F2 | **Email Verification** | SMTP integration (Mailtrap / Gmail), verification tokens |
| F3 | **Password Reset Via Email** | Forgot-password workflow with secure reset links |
| F4 | **Profile Management** | WebP avatar upload, edit profile, change password, reset tour guide |
| F5 | **Public Registration** | Student or Tutor role selection during sign-up |
| F6 | **Course Management & Enrollment** | Full CRUD, paginated enrollment, bulk review, enroll/drop, search & filter |
| F7 | **File Sharing (Course Materials)** | Upload, download, and manage course resources |
| F8 | **Announcements** | Post, feed, priority pinning for courses |
| F9 | **Assignment Management System** | Task lifecycle, file submissions, ZIP bulk export, review & grading |
| F10 | **Notifications** | In-app and email notifications for all key system events |
| F11 | **Exam Management & Notifications** | CRUD, publish workflow, scheduling, deadline management, reminders |
| F12 | **Re-Attempt & Re-Open Requests** | Students request re-attempts/re-opens, tutors approve with configurable extensions (24h/48h/72h/custom) |
| F13 | **Question Bank Management** | Centralized course-level question bank (MCQ, True/False, Short Answer) with image support |
| F14 | **Exam Engine + Secure Token** | Countdown timer, question navigator, auto-submit on timeout, cryptographic question randomization |
| F15 | **Exam Results & Auto-Grading** | Confetti animation, per-question review, manual grading for short answers |
| F16 | **Grade Management Panel** | Bulk publish, CSV export, weighted grade calculation |
| F17 | **Anti-Cheating Engine** | Browser-native exam integrity system (see details below) |
| F18 | **Session Timeline View** | Per-attempt timeline of all student activity during an exam |
| F19 | **Violation Timeline View** | Detailed violation log with types, timestamps, and severity |
| F20 | **Tutor Monitoring Dashboard** | Live overview of ongoing exams and violations for assigned courses |
| F21 | **Admin Dashboard & Users Management** | System-wide admin panel, user lock/unlock, tutor listing, user search |
| F22 | **SHIELDON AI Assistant** | Gemini-powered chatbot with backend proxy, automatically blocked during exams |
| F23 | **Shepherd.js Onboarding Tours** | Role-based guided tours for first-time users |
| F24 | **Tutor & Global Analytics Dashboard** | Course-level and system-wide analytics with ECharts visualizations |
| F25 | **Dynamic QR Attendance Tracking** | QR code refreshes every 15 seconds, manual override, attendance history |
| F26 | **Calendar & Schedule View** | Unified calendar with exams, assignments, and custom events |
| F27 | **Online Payment Gateway (Stripe)** | Secure checkout, payment history, pending payments, webhook processing |
| F28 | **Dark / Light Mode** | Seamless theme toggle with CSS custom properties |
| F29 | **English / Arabic (i18n)** | Full RTL support with ngx-translate |
| F30 | **Mobile Guard** | Detects and blocks mobile/tablet devices from accessing exam engine |

### 🛡️ Anti-Cheating Engine — Sub-Features (F17)

The Anti-Cheating Engine is built entirely with browser Web APIs — no plugins or extensions required:

| # | Sub-Feature | Description |
|---|---|---|
| 1 | **Pre-Exam Rules Acknowledgment Modal** | Students must read and accept exam integrity rules before starting |
| 2 | **Keyboard Shortcut Blocking** | Blocks Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+A, Ctrl+P, Ctrl+F, Ctrl+U, F12, Ctrl+Shift+I/J, Esc, Alt+Tab |
| 3 | **Window Resize / Minimize / Split Detection** | Detects split-screen, window resizing, and minimize attempts |
| 4 | **Mouse Monitoring (Pattern Analysis)** | Tracks mouse movement patterns for anomaly detection |
| 5 | **Selection by Mouse Blocking** | Prevents text selection via mouse during exams |
| 6 | **Right-Click Context Menu Blocking** | Disables right-click to prevent copy/paste/inspect operations |
| 7 | **Violation Intelligence Layer** | Severity scoring per violation type + cooldown periods to prevent duplicate flooding |
| 8 | **Warning System & Force-Submit** | 3-strike escalation — warnings → final warning → auto force-submit |
| 9 | **Monitoring Continuity on Reconnect** | Anti-cheat resumes seamlessly if the student reconnects or refreshes |

---

## 📋 Prerequisites (For Beginners)

If you are new to development and want to run this project on your own computer, you need to download and install the following tools first. They are all free! 🆓

1. **Node.js**: Required to run the frontend.
   - Download the **LTS** version from [nodejs.org](https://nodejs.org/).
   - Run the installer and follow the default steps.
2. **.NET 9 SDK**: The engine that runs the backend.
   - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0).
   - Look for the ".NET SDK" installer for your operating system.
3. **SQL Server**: The database where all data will be stored.
   - Download **SQL Server Express** from [Microsoft](https://www.microsoft.com/sql-server/sql-server-downloads).
   - Choose the "Basic" installation type.
4. **SSMS (SQL Server Management Studio)**: A visual program to inspect your database.
   - Download from [Microsoft Docs](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).
5. **Git**: A tool to clone the project from GitHub.
   - Download from [git-scm.com](https://git-scm.com/).
6. **Stripe CLI** _(optional, for payment testing)_:
   - Download from [stripe.com/docs/stripe-cli](https://docs.stripe.com/stripe-cli).

---

## 🔧 Installation & Setup (Step-by-Step Guide)

Follow these comprehensive steps in order to properly get the project running on your device from scratch. 🚀

### 1. Clone the Repository

This downloads the project files to your computer. 📥

1. Open your terminal (or Command Prompt / PowerShell on Windows).
2. Run this command:
   ```bash
   git clone https://github.com/ahmed-samir-dev/SHIELDON.git
   ```
3. Navigate into the project folder:
   ```bash
   cd SHIELDON
   ```

### 2. Backend Database Configuration (CRUCIAL STEP)

Before running the backend, you must configure it to connect to your local SQL Server.

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Open `SHIELDON.API/appsettings.json` and `SHIELDON.API/appsettings.Development.json` in a text editor.
3. Locate the `"ConnectionStrings"` block. Update the `"DefaultConnection"` string to match your local SQL Server instance name.
   - **How to find your Server Name**: Open SSMS. The connection prompt shows the `Server name` (e.g., `DESKTOP-ABC123\SQLEXPRESS` or `(localdb)\MSSQLLocalDB`).
   - **Update the string**: Replace the Server part. Use double backslashes `\\` for escaping in JSON.

   _Example:_
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_PC_NAME\\SQLEXPRESS;Database=SHIELDON_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
   }
   ```

4. Save the file(s).

### 3. Initialize and Update the Database

Now we tell Entity Framework to build the tables in your SQL Server.

1. Keep your terminal in the `backend` folder (NOT inside `SHIELDON.API`).
2. Ensure the EF Core CLI tools are installed globally:
   ```bash
   dotnet tool install -g dotnet-ef
   ```
   (If it says already installed, that's perfect!)
3. Apply all migrations:
   ```bash
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```
4. **Verification**: Open SSMS, connect to your server, expand "Databases", and you should see `SHIELDON_DB` with all tables created!

### 4. Run the Backend API

1. Navigate into the API startup project:
   ```bash
   cd SHIELDON.API
   ```
2. Start the server:
   ```bash
   dotnet run
   ```
   (Or use `dotnet watch run` for hot-reloading)
3. The backend is now running! Visit the live API docs at:
   👉 `http://localhost:5000/swagger`
4. Keep this terminal window open.

### 5. Run the Frontend Application

1. Open a **new, completely separate** terminal window (leave the backend running!).
2. Navigate to the frontend directory:
   ```bash
   cd path/to/SHIELDON/frontend
   ```
3. Install all dependencies:
   ```bash
   npm install
   ```
   (This may take a couple of minutes)
4. Start the Angular dev server:
   ```bash
   npm start
   ```
5. Wait until compilation is successful.
6. Open your browser and go to:
   👉 `http://localhost:4201`

## Congratulations! You are now running SHIELDON on your local machine!

### 6. Stripe Payment Setup (Optional)

To enable the online payment gateway, you need a Stripe account and the Stripe CLI.

#### Step A: Create a Stripe Account & Get API Keys
1. Go to [stripe.com](https://stripe.com) and create a **free** account.
2. After logging in, make sure you are in **Test mode** (toggle in the top-right of the dashboard).
3. Navigate to [Developers → API Keys](https://dashboard.stripe.com/test/apikeys).
4. You will see two keys:
   - **Publishable key** — starts with `pk_test_...`
   - **Secret key** — starts with `sk_test_...` (click "Reveal test key" to see it)
5. Copy both keys — you'll need them in the next step.

#### Step B: Install the Stripe CLI
The Stripe CLI is a command-line tool that forwards payment events from Stripe's servers to your local machine.

**Option 1 — Download manually (recommended for beginners):**
1. Go to [Stripe CLI releases](https://github.com/stripe/stripe-cli/releases).
2. Download the latest `.zip` file for your OS (e.g., `stripe_X.X.X_windows_x86_64.zip`).
3. Extract the `.zip` and place the `stripe.exe` file somewhere accessible (e.g., inside a `stripe_cli` folder in your project root).

**Option 2 — Install via package manager:**
```bash
# Windows (Scoop)
scoop install stripe

# macOS (Homebrew)
brew install stripe/stripe-cli/stripe
```

4. Verify the installation:
   ```bash
   stripe --version
   ```

5. Log in to your Stripe account from the CLI:
   ```bash
   stripe login
   ```
   This will open your browser to authenticate. Follow the instructions and press Enter when done.

#### Step C: Configure Backend
1. Open `backend/SHIELDON.API/appsettings.json`.
2. Locate the `"Stripe"` section and fill in your keys:
   ```json
   "Stripe": {
     "SecretKey": "sk_test_YOUR_SECRET_KEY",
     "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
     "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET"
   }
   ```
   > You'll get the `WebhookSecret` in the next step — leave it blank for now.

#### Step D: Run Stripe CLI for Webhooks
The Stripe CLI forwards webhook events (like `checkout.session.completed`) to your local backend so payments are processed correctly.

1. Open a **new terminal** and navigate to your project root:
   ```bash
   cd path/to/SHIELDON
   ```
2. Run the following command to start listening for Stripe events:

   **If using the bundled CLI in the project:**
   ```bash
   .\stripe_cli\stripe.exe listen --forward-to localhost:5000/api/webhooks/stripe
   ```

   **If installed globally:**
   ```bash
   stripe listen --forward-to localhost:5000/api/webhooks/stripe
   ```

3. The CLI will output a **Webhook signing secret** like this:
   ```
   > Ready! Your webhook signing secret is whsec_abc123...
   ```
4. **Copy this `whsec_...` value** and paste it into `appsettings.json` → `Stripe.WebhookSecret`.
5. **Restart the backend** after updating the secret.
6. **Keep this terminal open** while testing payments — it must be running to receive Stripe events.

#### Step E: Test Payments

Use Stripe's official test card numbers to simulate different payment scenarios. No real money is charged.

##### ✅ Success Cards

| Card Number | Scenario |
|:---|:---|
| `4242 4242 4242 4242` | Standard successful payment |
| `4000 0025 0000 3155` | Requires 3D Secure (two-step authentication) |

##### ❌ Failure / Decline Cards

| Card Number | Scenario Simulated |
|:---|:---|
| `4000 0000 0000 0002` | Generic decline |
| `4000 0000 0000 9995` | Insufficient funds |
| `4000 0000 0000 0069` | Card expired |
| `4000 0000 0000 0127` | Incorrect CVC |
| `4000 0000 0000 0119` | Processing error |

**For all test cards, use:**
- **Expiry:** Any future date (e.g., `12/30`)
- **CVC:** Any 3 digits (e.g., `123`)
- **Name / ZIP:** Any values

---

## 🧪 How to Test (Demo Accounts)

To test the different roles in the system:

### 1. Test as Admin

The system comes with a pre-seeded Admin account (you cannot register an Admin via the app for security reasons).

| Field | Value |
|---|---|
| **Email** | `admin@shieldon.com` |
| **Password** | `Admin@Shieldon2025!` |

### 2. Test as Tutor or Student

1. Go to the landing page and click **Register** (or go to `/register`).
2. Fill in the details and select the role (**Tutor** or **Student**).
3. Complete registration. You can now log in with your new account!

---

## 📡 API Endpoints Reference

The backend exposes a comprehensive RESTful API. For an interactive view, run the backend and visit:
👉 `http://localhost:5000/swagger/index.html`

Below is the complete endpoint reference:

### Authentication (`/api/auth`)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/register` | Register a new user (Student/Tutor) |
| `POST` | `/api/auth/login` | Login and receive JWT + refresh token |
| `POST` | `/api/auth/refresh` | Refresh an expired access token |
| `POST` | `/api/auth/logout` | Logout and revoke refresh token |
| `POST` | `/api/auth/verify-email` | Verify email address with token |
| `POST` | `/api/auth/resend-verification` | Resend email verification link |
| `POST` | `/api/auth/forgot-password` | Request a password reset email |
| `POST` | `/api/auth/reset-password` | Reset password using token |

### Profile (`/api/profile`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/profile` | Get current user's profile |
| `PATCH` | `/api/profile` | Update profile details |
| `POST` | `/api/profile/picture` | Upload/update profile picture (WebP) |
| `PATCH` | `/api/profile/password` | Change password |
| `PATCH` | `/api/profile/onboarding-complete` | Mark onboarding tour as complete |
| `PATCH` | `/api/profile/onboarding-reset` | Reset onboarding tour status |

### Users Management (`/api/users`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/users` | List all users (Admin, paginated) |
| `GET` | `/api/users/tutors` | List all tutors |
| `POST` | `/api/users/{id}/lock` | Lock a user account |
| `POST` | `/api/users/{id}/unlock` | Unlock a user account |

### Courses (`/api/courses`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/courses` | List all courses (paginated, filterable) |
| `POST` | `/api/courses` | Create a new course |
| `GET` | `/api/courses/{id}` | Get course details |
| `PATCH` | `/api/courses/{id}` | Update a course |
| `DELETE` | `/api/courses/{id}` | Delete a course |
| `POST` | `/api/courses/{id}/enroll` | Enroll in a course |
| `GET` | `/api/courses/enrollments/pending` | List pending enrollment requests (paginated) |
| `GET` | `/api/courses/enrollments/approved` | List approved enrollments (paginated) |
| `PATCH` | `/api/courses/enrollments/{enrollmentId}/review` | Approve/reject an enrollment |
| `POST` | `/api/courses/enrollments/bulk-review` | Bulk approve/reject enrollments |
| `GET` | `/api/courses/enrollments/my` | Get student's own enrollment statuses |

### Announcements (`/api/courses/{courseId}/announcements`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/courses/{courseId}/announcements` | List course announcements |
| `POST` | `/api/courses/{courseId}/announcements` | Create an announcement |
| `DELETE` | `/api/courses/{courseId}/announcements/{announcementId}` | Delete an announcement |

### Course Materials (`/api/courses/{courseId}/materials`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/courses/{courseId}/materials` | List course materials |
| `POST` | `/api/courses/{courseId}/materials` | Upload a course material |
| `GET` | `/api/courses/{courseId}/materials/{materialId}/download` | Download a material file |
| `DELETE` | `/api/courses/{courseId}/materials/{materialId}` | Delete a material |

### Assignments (`/api/courses/{courseId}/assignments`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/courses/{courseId}/assignments` | List course assignments |
| `POST` | `/api/courses/{courseId}/assignments` | Create an assignment |
| `PATCH` | `/api/courses/{courseId}/assignments/{assignmentId}` | Update an assignment |
| `DELETE` | `/api/courses/{courseId}/assignments/{assignmentId}` | Delete an assignment |
| `GET` | `/api/courses/{courseId}/assignments/{assignmentId}/reference` | Download reference file |
| `POST` | `/api/courses/{courseId}/assignments/{assignmentId}/submissions` | Submit assignment work |
| `GET` | `/api/courses/{courseId}/assignments/{assignmentId}/submissions` | List all submissions |
| `DELETE` | `/api/courses/{courseId}/assignments/{assignmentId}/submissions/{submissionId}` | Delete a submission |
| `GET` | `/api/courses/{courseId}/assignments/{assignmentId}/submissions/{submissionId}/download` | Download a submission |
| `GET` | `/api/courses/{courseId}/assignments/{assignmentId}/submissions/download-all` | Bulk download all submissions (ZIP) |
| `POST` | `/api/courses/{courseId}/assignments/{assignmentId}/submissions/{submissionId}/review` | Grade/review a submission |

### Question Bank (`/api/courses/{courseId}/question-bank`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/courses/{courseId}/question-bank` | List all questions |
| `POST` | `/api/courses/{courseId}/question-bank` | Create a question |
| `GET` | `/api/courses/{courseId}/question-bank/counts` | Get question count by type |
| `PATCH` | `/api/courses/{courseId}/question-bank/{questionId}` | Update a question |
| `DELETE` | `/api/courses/{courseId}/question-bank/{questionId}` | Delete a question |
| `PATCH` | `/api/courses/{courseId}/question-bank/reorder` | Reorder questions |
| `POST` | `/api/courses/{courseId}/question-bank/{questionId}/options` | Add an option to a question |
| `PATCH` | `/api/courses/{courseId}/question-bank/{questionId}/options/{optionId}` | Update an option |
| `DELETE` | `/api/courses/{courseId}/question-bank/{questionId}/options/{optionId}` | Delete an option |
| `POST` | `/api/courses/{courseId}/question-bank/{questionId}/image` | Upload question image |
| `DELETE` | `/api/courses/{courseId}/question-bank/{questionId}/image` | Delete question image |

### Exams (`/api/courses/{courseId}/exams` & `/api/exams`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/courses/{courseId}/exams` | List exams for a course |
| `POST` | `/api/courses/{courseId}/exams` | Create an exam |
| `GET` | `/api/exams/{examId}` | Get exam details |
| `PATCH` | `/api/exams/{examId}` | Update an exam |
| `DELETE` | `/api/exams/{examId}` | Delete an exam |
| `PATCH` | `/api/exams/{examId}/publish` | Publish an exam |

### Exam Attempts (`/api`)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/exams/{examId}/start` | Start an exam attempt (creates secure token) |
| `PATCH` | `/api/exam-attempts/{attemptId}/answers` | Save an answer |
| `POST` | `/api/exam-attempts/{attemptId}/submit` | Submit the exam |
| `POST` | `/api/exam-attempts/{attemptId}/force-submit` | Force-submit (timeout/violation) |

### Exam Results (`/api`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/exam-attempts/{attemptId}/result` | Get attempt result details |
| `GET` | `/api/exams/{examId}/my-attempts` | Student's own attempts |
| `GET` | `/api/exams/{examId}/attempts` | All student attempts (Tutor/Admin) |
| `POST` | `/api/exam-attempts/{attemptId}/grade-short-answers` | Grade short answer questions |
| `POST` | `/api/exams/{examId}/release-results` | Release results to all students |
| `GET` | `/api/exams/{examId}/export` | Export results as CSV |

### Re-Attempt Requests (`/api/reattempt-requests`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/reattempt-requests` | List all requests (Tutor/Admin, paginated) |
| `POST` | `/api/reattempt-requests` | Submit a re-attempt/re-open request |
| `GET` | `/api/reattempt-requests/can-reopen` | Check if student can request re-open |
| `GET` | `/api/reattempt-requests/mine` | Get student's own requests |
| `PATCH` | `/api/reattempt-requests/{requestId}/review` | Approve/reject with optional extension |

### Violations (`/api/violations`)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/violations/batch` | Report violations in batch |
| `GET` | `/api/attempts/{attemptId}/violations` | Get violations for an attempt |
| `GET` | `/api/exams/{examId}/violations` | Get all violations for an exam |

### Monitoring (`/api/monitoring`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/attempts/{attemptId}/timeline` | Get session timeline for an attempt |
| `GET` | `/api/attempts/{attemptId}/violations/summary` | Get violation summary |
| `GET` | `/api/monitoring/tutor/dashboard` | Tutor monitoring dashboard data |
| `GET` | `/api/monitoring/admin/dashboard` | Admin monitoring dashboard data |

### Grades (`/api`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/courses/{courseId}/grades` | Get all grades for a course |
| `GET` | `/api/courses/{courseId}/grades/my` | Student's own grades for a course |
| `GET` | `/api/my-grades` | Student's grades across all courses |
| `PATCH` | `/api/grades/{gradeId}` | Update a grade record |
| `POST` | `/api/courses/{courseId}/grades/publish` | Bulk publish grades |
| `GET` | `/api/courses/{courseId}/grades/export` | Export grades as CSV |

### Notifications (`/api/notifications`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/notifications` | List user's notifications |
| `GET` | `/api/notifications/unread-count` | Get unread notification count |
| `PATCH` | `/api/notifications/{id}/read` | Mark a notification as read |
| `PATCH` | `/api/notifications/mark-all-read` | Mark all notifications as read |
| `DELETE` | `/api/notifications` | Clear all notifications |

### Attendance (`/api/attendance`)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/attendance/checks` | Create a new attendance check |
| `PUT` | `/api/attendance/checks/{id}/end` | End an attendance check |
| `POST` | `/api/attendance/checks/{id}/manual/{studentId}` | Manually mark attendance |
| `GET` | `/api/attendance/checks/{id}` | Get attendance check details |
| `GET` | `/api/attendance/checks/{id}/current-qr` | Get current active QR code |
| `GET` | `/api/attendance/courses/{courseId}/history` | Course attendance history |
| `GET` | `/api/attendance/all` | All attendance records (Admin) |
| `POST` | `/api/attendance/checks/{id}/scan` | Student scans QR code |
| `GET` | `/api/attendance/my-history` | Student's own attendance history |

### Chat (`/api/chat`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/chat/inbox` | Get chat inbox (conversations list) |
| `GET` | `/api/chat/conversations/{conversationId}/messages` | Get messages in a conversation |
| `GET` | `/api/chat/users` | Search users to start a chat |
| `GET` | `/api/chat/conversation-id` | Get/create conversation with a user |

### Calendar (`/api/calendar`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/calendar/events` | Get all calendar events |
| `POST` | `/api/calendar/events/custom` | Create a custom event |
| `PUT` | `/api/calendar/events/custom/{eventId}` | Update a custom event |
| `DELETE` | `/api/calendar/events/custom/{eventId}` | Delete a custom event |

### Payments (`/api/payment`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/payment/history` | Get payment history |
| `GET` | `/api/payment/pending` | Get pending payments |
| `POST` | `/api/payment/checkout` | Create Stripe checkout session |

### Stripe Webhook (`/api/webhooks/stripe`)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/webhooks/stripe` | Handles Stripe webhook events (anonymous) |

### AI Assistant (`/api/ai`)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/ai/chat` | Send a message to the AI assistant |

### Static Files (`/uploads`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/uploads/profile-pictures/{filename}` | Serve a profile picture |
| `GET` | `/uploads/course-materials/{courseId}/{filename}` | Serve a course material file |

---

## 🌿 Git Workflow

To keep the project organized, we follow a structured Git workflow:

- 🌿 **Feature Branches**: Never work directly on `main`. Create a new branch for every feature or fix:
  ```bash
  git checkout -b feature/your-feature-name
  ```
- 🔀 **Pull Requests**: When a feature is complete, push it to GitHub and create a Pull Request for code review before merging.

---

<div align="center">
  <strong>🛡️ SHIELDON — "Integrity You Can Trust" 🛡️</strong>
</div>
