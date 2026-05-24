# 🛡️ SHIELDON - Backend

<div align="center">
  <img src="https://img.shields.io/badge/.NET_9-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Entity_Framework-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt="EF Core" />
  <img src="https://img.shields.io/badge/SQL_Server_2022-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/Stripe-635BFF?style=for-the-badge&logo=stripe&logoColor=white" alt="Stripe" />
</div>

> 🔧 The core API, business logic, and database orchestration for the SHIELDON Learning Management System & Anti-Cheating Engine.
> Built with .NET 9.

---

## 📑 Table of Contents

- [🔭 Project Overview](#-project-overview)
- [⚙️ Technology Stack](#️-technology-stack)
- [📋 Prerequisites](#-prerequisites-for-beginners)
- [🔧 Installation & Setup](#-installation--setup-step-by-step-guide)
- [🏛️ Architecture & Layers](#️-architecture--layers)
- [🚀 Feature List (F1 – F30)](#-comprehensive-feature-list-f1--f30)
- [📡 API Endpoints Reference](#-api-endpoints-reference)

---

## 🔭 Project Overview

This directory contains the backend application for the **SHIELDON** platform. It provides the RESTful API endpoints, business logic orchestration, and database persistence required to power the entire system.

The backend is responsible for:
- Secure JWT authentication, refresh tokens, and role management
- Course, enrollment, and assignment management
- Orchestrating the Exam Engine logic, secure tokens, and auto-grading
- Persisting and analyzing anti-cheat violation logs
- Serving analytical data for tutor and admin dashboards
- Email notifications (verified via [Mailtrap](https://mailtrap.io), production via **Google Gmail SMTP**)
- Online payment processing via Stripe
- AI assistant proxy (Google Gemini API)
- Dynamic QR attendance tracking

---

## ⚙️ Technology Stack

| Technology | Purpose |
|---|---|
| **.NET 9 ASP.NET Core** | Web API framework |
| **C#** | Language (Nullable reference types enabled) |
| **Clean Architecture** | Vertical Slice Hybrid approach |
| **Entity Framework Core 9** | ORM (Code-First migrations) |
| **Microsoft SQL Server 2022** | Relational database |
| **JWT Bearer Tokens** | Authentication (Access + Refresh) |
| **MailKit / MimeKit** | Email delivery (Mailtrap for testing, Gmail SMTP for production) |
| **FluentValidation** | Request validation |
| **AutoMapper** | Object mapping |
| **Stripe.net** | Payment processing & webhooks |
| **Google Gemini API** | AI assistant backend proxy |
| **Serilog** | Structured logging |

---

## 📋 Prerequisites (For Beginners)

If you have zero previous technical background and want to run the backend part on your device, you need to install these tools first: 

1. **.NET 9 SDK**: The software development kit required to build and run .NET applications.
   - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0).
   - Follow the default installation steps.
2. **SQL Server**: The database where all users, courses, and exam data will be stored.
   - Download **SQL Server Express** (free) from [Microsoft](https://www.microsoft.com/sql-server/sql-server-downloads).
   - Choose the "Basic" installation type.
3. **SSMS (SQL Server Management Studio)**: A visual program to inspect your database.
   - Download from [Microsoft Docs](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).

---

## 🔧 Installation & Setup (Step-by-Step Guide)

Follow these comprehensive steps in order to properly get the backend running on your device from scratch. 

### 1. Database Configuration (CRUCIAL STEP)

**Before running the backend, you must configure it to connect to your local SQL Server.**

1. Open your terminal and navigate to the backend directory:
   ```bash
   cd path/to/SHIELDON/backend
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

4. 💾 Save the file(s).

### 2. Initialize and Update the Database

Now we tell Entity Framework to build the tables in your SQL Server. 🏗️

1. Keep your terminal in the `backend` folder (NOT inside `SHIELDON.API`).
2. Ensure the EF Core CLI tools are installed globally:
   ```bash
   dotnet tool install -g dotnet-ef
   ```
   _(If it says already installed, that's perfect!)_
3. Apply all migrations:
   ```bash
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```
4. **Verification**: Open SSMS, connect to your server, expand "Databases", and you should see `SHIELDON_DB` with all tables created!

### 3. Run the Backend API

1. Navigate into the API startup project:
   ```bash
   cd SHIELDON.API
   ```
2. Start the server:
   ```bash
   dotnet run
   ```
   _(Or use `dotnet watch run` for hot-reloading)_
3. The backend is now running! Visit the live API docs at:
   👉 `http://localhost:5000/swagger`
4. Keep this terminal window open if you plan to run the frontend simultaneously.

---

## 🏛️ Architecture & Layers

The backend follows **Clean Architecture + Vertical Slice Hybrid** principles to keep the code organized and independent:

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
| **SHIELDON.API** | The entry point. Contains thin HTTP controllers that receive requests and delegate to the application layer. No business logic lives here. |
| **SHIELDON.Application** | Business logic organized by feature slices (Use Cases, DTOs, Validators, Interfaces). |
| **SHIELDON.Infrastructure** | External implementations — database persistence (EF Core), email sending, file storage, payment gateway. |
| **SHIELDON.Domain** | The heart of the system. Contains C# entities, enums, and constants. Zero external dependencies. |
| **SHIELDON.Tests** | Unit tests and integration tests to ensure correctness and prevent regressions. |

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
| F17 | **Anti-Cheating Engine** | Browser-native exam integrity system |
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

---

## 📡 API Endpoints Reference

The backend exposes a comprehensive RESTful API. For an interactive view, run the backend and visit:
👉 `http://localhost:5000/swagger/index.html`

> 📋 For the full endpoint table with all 100+ endpoints, see the root [README.md](../README.md#-api-endpoints-reference).

### Summary of API Controllers

| Controller | Base Route | Endpoints |
|---|---|---|
| Auth | `/api/auth` | register, login, refresh, logout, verify-email, forgot/reset password |
| Profile | `/api/profile` | get, update, picture upload, change password, onboarding |
| Users | `/api/users` | list all, list tutors, lock/unlock |
| Courses | `/api/courses` | CRUD, enroll, enrollment management (pending/approved/bulk-review) |
| Announcements | `/api/courses/{id}/announcements` | list, create, delete |
| Materials | `/api/courses/{id}/materials` | list, upload, download, delete |
| Assignments | `/api/courses/{id}/assignments` | CRUD, submissions, review, bulk download |
| Question Bank | `/api/courses/{id}/question-bank` | CRUD, options, images, reorder, counts |
| Exams | `/api/courses/{id}/exams` & `/api/exams` | CRUD, publish |
| Exam Attempts | `/api/exams/{id}/start` | start, save answer, submit, force-submit |
| Exam Results | `/api/exam-attempts/{id}/result` | results, attempts, grading, release, export |
| Reattempt | `/api/reattempt-requests` | submit, list, review, can-reopen, mine |
| Violations | `/api/violations` | batch report, per-attempt, per-exam |
| Monitoring | `/api/monitoring` | timeline, violation summary, tutor/admin dashboards |
| Grades | `/api/courses/{id}/grades` | list, my grades, update, publish, export |
| Notifications | `/api/notifications` | list, unread count, mark read, clear all |
| Attendance | `/api/attendance` | create check, end, scan QR, manual mark, history |
| Chat | `/api/chat` | inbox, messages, users, conversation-id |
| Calendar | `/api/calendar` | events, custom CRUD |
| Payment | `/api/payment` | history, pending, checkout |
| Stripe Webhook | `/api/webhooks/stripe` | webhook handler |
| AI | `/api/ai` | chat |
| Files | `/uploads` | profile pictures, course materials |

---

<div align="center">
  <strong>🛡️ SHIELDON Backend — "Integrity You Can Trust" 🛡️</strong>
</div>
