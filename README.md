# 🛡️ SHIELDON - Integrity You Can Trust

<div align="center">
  <img src="https://img.shields.io/badge/.NET_9-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Angular_21-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular 21" />
  <img src="https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/SQL_Server_2022-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/SignalR-512BD4?style=for-the-badge&logo=.net&logoColor=white" alt="SignalR" />
  <img src="https://img.shields.io/badge/WhatsApp-25D366?style=for-the-badge&logo=whatsapp&logoColor=white" alt="WhatsApp Gateway" />
  <img src="https://img.shields.io/badge/WebRTC-333333?style=for-the-badge&logo=webrtc&logoColor=white" alt="WebRTC" />
  <img src="https://img.shields.io/badge/Stripe-635BFF?style=for-the-badge&logo=stripe&logoColor=white" alt="Stripe" />
  <img src="https://img.shields.io/badge/Google_OAuth-4285F4?style=for-the-badge&logo=google&logoColor=white" alt="Google OAuth" />
</div>

> 🎓 A full-stack Learning Management System (LMS) with a built-in browser-native Anti-Cheating Engine.
> Built as a graduation project — no external exam-locking software required.

---

## 📑 Table of Contents

- [🔭 Project Overview](#-project-overview)
- [⚙️ Technology Stack](#️-technology-stack)
- [🏛️ Architecture](#️-architecture)
- [👥 System Roles](#-system-roles)
- [🚀 Comprehensive Feature List (F1 – F35)](#-comprehensive-feature-list-f1--f35)
- [🔧 Installation & Setup Guide](#-installation--setup-guide)
- [🧪 How to Test (Demo Accounts)](#-how-to-test-demo-accounts)
- [📡 API Endpoints Reference](#-api-endpoints-reference)
- [🤝 Contributing & Git Workflow](#-contributing--git-workflow)

---

## 🔭 Project Overview

**SHIELDON** is a comprehensive educational platform that combines a modern Learning Management System with a robust, browser-based Exam Integrity System.

Most traditional LMS platforms depend on external software (like Safe Exam Browser or LockDown Browser) to enforce exam security, requiring students to download and install applications. **SHIELDON eliminates this dependency entirely** by building the Anti-Cheating Engine directly into the web platform using standard Web APIs.

---

## ⚙️ Technology Stack

### Frontend
| Technology | Purpose |
|---|---|
| **Angular 21** | Framework (Standalone Components, signals, reactive forms) |
| **TypeScript** | Language |
| **SCSS** | Styling with CSS Custom Properties & dark/light theme system |
| **SignalR Client** | Real-time WebSocket communication |
| **WebRTC** | Peer-to-peer video calls in the chat system |
| **Apache ECharts** | Charts & analytics (via ngx-echarts) |
| **ngx-translate** | Internationalization with full Arabic (RTL) support |
| **SweetAlert2** | Beautiful modal dialogs |
| **ngx-toastr** | Toast notifications |
| **Shepherd.js** | Guided onboarding tours |
| **canvas-confetti** | Celebration effects |
| **Lucide Icons** | Icon library |

### Backend (.NET)
| Technology | Purpose |
|---|---|
| **.NET 9 ASP.NET Core** | Web API framework |
| **C#** | Language |
| **Entity Framework Core 9** | ORM (Code-First migrations) |
| **SignalR** | Real-time WebSocket hub (chat, presence tracking, leaderboard) |
| **ASP.NET Core Identity / JWT** | Authentication with access + refresh tokens |
| **Google OAuth 2.0** | Passwordless social login via Google account |
| **MailKit / MimeKit** | Email delivery ([Mailtrap](https://mailtrap.io) for dev, Gmail SMTP for production) |
| **FluentValidation** | Request validation layer |
| **Stripe.net** | Payment processing |
| **Google Gemini API** | AI assistant (backend proxy) |
| **BCrypt.Net** | Password hashing |
| **Serilog** | Structured request logging |
| **Swashbuckle / Swagger** | Interactive API documentation |

### WhatsApp Gateway Microservice (Node.js)
| Technology | Purpose |
|---|---|
| **Node.js 18+** | Runtime for the WhatsApp gateway microservice |
| **@whiskeysockets/baileys** | Self-hosted WhatsApp Web socket library (OTP delivery, zero-cost) |
| **Express.js** | Lightweight HTTP server exposing internal gateway endpoints |
| **qrcode-terminal** | Renders QR code in terminal for first-time WhatsApp pairing |
| **pino** | High-performance JSON logger |

### 💽 Database
| Technology | Purpose |
|---|---|
| **Microsoft SQL Server 2022** | Primary relational database |

---

## 🏛️ Architecture

SHIELDON follows a **Clean Architecture + Vertical Slice Hybrid** approach for its .NET backend, with a dedicated **Node.js Microservice** for WhatsApp OTP delivery:

```
  ┌──────────────────────────────────────────────────────┐
  │             Angular 21 Frontend (SPA)                │  ← Port 4201
  └─────────────────────────┬────────────────────────────┘
                            │ HTTP / SignalR WebSocket
  ┌─────────────────────────▼────────────────────────────┐
  │                   SHIELDON.API                       │  ← Port 5000  (Thin HTTP Controllers)
  ├──────────────────────────────────────────────────────┤
  │              SHIELDON.Application                    │  ← Use Cases, DTOs, Validators, Interfaces
  ├──────────────────────────────────────────────────────┤
  │             SHIELDON.Infrastructure                  │  ← DB, Email, File Storage, WhatsApp Caller
  ├──────────────────────────────────────────────────────┤
  │                SHIELDON.Domain                       │  ← Entities & Core Business Rules
  ├──────────────────────────────────────────────────────┤
  │                SHIELDON.Tests                        │  ← Unit & Integration Tests
  └──────────────────────────────────────────────────────┘
                            │ Internal HTTP (localhost only)
  ┌─────────────────────────▼────────────────────────────┐
  │       WhatsApp Gateway Microservice (Node.js)        │  ← Port 3001  (Baileys / WhatsApp Web)
  └──────────────────────────────────────────────────────┘
```

### Clean Architecture Layers (.NET)

| Layer | Responsibility |
|---|---|
| **SHIELDON.API** | Thin HTTP controllers that receive requests and delegate to the application layer. |
| **SHIELDON.Application** | Business logic organized by feature slices (Use Cases, DTOs, FluentValidation validators). Defines `IOtpService` abstraction. |
| **SHIELDON.Infrastructure** | External implementations: EF Core database, email service, file storage, payment gateway (Stripe), Gemini AI, and `WhatsAppGatewayOtpService` HTTP caller. |
| **SHIELDON.Domain** | Core business entities and enums (independent of any framework or infrastructure). |
| **SHIELDON.Tests** | Unit and integration tests to ensure correctness and prevent regressions. |

### WhatsApp Gateway Microservice (Node.js)

| Aspect | Detail |
|---|---|
| **Isolation** | Runs as a completely separate process — if it restarts, the .NET API stays unaffected. |
| **Communication** | Called exclusively by `SHIELDON.Infrastructure` via `IOtpService` → `WhatsAppGatewayOtpService`. |
| **Clean Architecture Compliance** | The .NET core has zero knowledge of Node.js or Baileys — only the `IOtpService` interface is referenced. Swapping to Meta Cloud API or Twilio requires changing only one line in `DependencyInjection.cs`. |
| **Security** | Port 3001 is internal-only and never exposed to the internet or the frontend directly. |

---

## 👥 System Roles

| Role | Description |
|---|---|
| **Admin** | Full system access. Manages courses, users, all exams, analytics, and violations system-wide. |
| **Tutor** | Manages assigned courses. Creates exams, uploads materials, posts announcements, monitors exam violations, and tracks attendance. |
| **Student** | Accesses enrolled courses, downloads materials, submits assignments, takes exams under anti-cheat monitoring, and makes payments. |

---

## 🚀 Comprehensive Feature List (F1 – F35)

| # | Feature | Details |
|---|---|---|
| F1 | **Secure Login & Role-Based Redirect** | JWT authentication, refresh tokens, single-session enforcement |
| F2 | **Google OAuth 2.0 Login** | Passwordless social login — sign in instantly with any Google account |
| F3 | **Email Verification** | SMTP integration (Mailtrap / Gmail), verification tokens |
| F4 | **Password Reset Via Email** | Forgot-password workflow with secure reset links |
| F5 | **Phone Verification via WhatsApp OTP** | 6-digit WhatsApp OTP via self-hosted Node.js gateway. Unique phone per account enforcement, 2-minute resend cooldown, 6-cell auto-advancing OTP input |
| F6 | **Profile Management** | WebP avatar upload, edit profile, change password, reset tour guide, phone number management |
| F7 | **Public Registration** | Student or Tutor role selection during sign-up |
| F8 | **Course Management & Enrollment** | Full CRUD, paginated enrollment, bulk review, enroll/drop, search & filter |
| F9 | **File Sharing (Course Materials)** | Upload, download, and manage course resources |
| F10 | **Announcements** | Post, feed, priority pinning for courses |
| F11 | **Assignment Management System** | Task lifecycle, file submissions, ZIP bulk export, review & grading |
| F12 | **Notifications** | In-app and email notifications for all key system events |
| F13 | **Exam Management & Notifications** | CRUD, publish workflow, scheduling, deadline management, reminders |
| F14 | **Re-Attempt & Re-Open Requests** | Students request re-attempts/re-opens, tutors approve with configurable extensions (24h/48h/72h/custom) |
| F15 | **Question Bank Management** | Centralized course-level question bank (MCQ, True/False, Short Answer) with image support |
| F16 | **Exam Engine + Secure Token** | Countdown timer, Red Flag question bookmarking, question navigator with filter tabs, auto-submit on timeout, cryptographic question randomization |
| F17 | **Exam Results & Auto-Grading** | Confetti animation, per-question review, manual grading for short answers |
| F18 | **Grade Management Panel** | Bulk publish, CSV export, weighted grade calculation |
| F19 | **Anti-Cheating Engine** | Browser-native exam integrity system — no plugins or extensions required. <details><summary>See 12 sub-features ▼</summary><br>**1. Pre-Exam Rules Acknowledgment Modal** — Students must read and accept exam integrity rules before starting<br>**2. Fullscreen Exit Enforcement** — Logs a critical violation if the student attempts to exit fullscreen mode<br>**3. Tab Switching & Focus Loss** — Triggers violations if the student switches tabs or clicks outside the exam window<br>**4. Keyboard Shortcut Blocking** — Blocks Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+A, Ctrl+P, Ctrl+F, Ctrl+U, F12, Ctrl+Shift+I/J, Esc, Alt+Tab<br>**5. Window Resize / Minimize / Split Detection** — Detects split-screen, window resizing, and minimize attempts<br>**6. Mouse Monitoring (Pattern Analysis)** — Tracks mouse movement patterns for anomaly detection<br>**7. Selection by Mouse Blocking** — Prevents text selection via mouse during exams<br>**8. Right-Click Context Menu Blocking** — Disables right-click to prevent copy/paste/inspect operations<br>**9. Violation Intelligence Layer** — Severity scoring per violation type + cooldown periods to prevent duplicate flooding<br>**10. Action Debouncer & Score Normalization** — 500ms aggregation window preventing cascading violations (e.g. ALT+TAB) and unified decimal scoring (Minor=0.5, Medium=1.0, Critical=1.0)<br>**11. Warning System & Force-Submit** — 3-strike escalation (displayed on an elegant horizontal progress bar) — warnings → final warning → auto force-submit<br>**12. Monitoring Continuity on Reconnect** — Anti-cheat resumes seamlessly if the student disconnects or refreshes</details> |
| F20 | **Session Timeline View** | Per-attempt vertical timeline with live Presence Tracking (connection status) |
| F21 | **Violation Density Analytics** | Advanced sticky Bubble/Scatter Chart mapping violation frequency against severity over time |
| F22 | **Tutor Monitoring Dashboard** | Live overview of ongoing exams and violations with CSV data export |
| F23 | **Admin Dashboard & Global Layout** | System-wide admin panel with responsive Collapsible Sidebar and dynamic data grids |
| F24 | **SHIELDON AI Assistant** | Gemini-powered chatbot with backend proxy, automatically blocked during exams |
| F25 | **Real-Time Chat System** | Built with SignalR (WebSockets), WebRTC, and browser-native APIs. <details><summary>See 11 sub-features ▼</summary><br>**1. File Attachments & Uploads** — Send images, documents, and audio files (max 10 MB). Images render as constrained thumbnail previews (250×250px). Documents display with a styled download link.<br>**2. Voice Notes** — Record audio messages (up to 5 minutes) directly from the chat composer via HTML5 MediaRecorder. Renders as a full-width horizontal audio player bubble.<br>**3. Delivery Receipts** — Three-state read receipts: single gray tick (Sent), double gray tick (Delivered), double blue tick (Read). Updated in real-time via SignalR callbacks.<br>**4. WebRTC 1-on-1 Video Calls** — Peer-to-peer WebRTC video calls with a global ringtone overlay that persists across SPA page navigation. Strict media track teardown on hang-up, rejection, or logout.<br>**5. Group Chat Management** — Full group lifecycle: create (Admin/Tutor only), rename, add/remove members, and permanently delete (Group Admin/creator only, with cascade delete).<br>**6. Contacts Filtration** — Real-time inbox filtering by status (All / Online / Offline) and by role (Admin / Tutor / Student) powered by the SignalR PresenceTracker.<br>**7. Last Seen Tracking** — Shows relative time (e.g. "2 hours ago") for recently offline users, or an absolute date/time stamp for users offline more than 24 hours.<br>**8. Real-Time Typing Indicators** — 3-dot pulsating animation in the active chat window plus an italic typing... prompt in the sidebar inbox row. Auto-clears after 2.5s of inactivity.<br>**9. Message Reactions** — Emoji reaction picker with optimistic UI updates. Aggregated reaction pill chips (e.g. 👍 3). Reaction-details modal with per-emoji tabs showing user avatars and counts.<br>**10. Message Deletion** — Any user may delete their own messages. Group Admins may delete any member's message. Replaced with a dashed "deleted" bubble for all participants.<br>**11. Reply & Forward** — Inline reply with a quoted message block and click-to-scroll highlight animation. Forward sends messages to multiple conversations with double-curved arrow icon.</details> |
| F26 | **Shepherd.js Onboarding Tours** | Role-based guided tours for first-time users |
| F27 | **Tutor & Global Analytics Dashboard** | Course-level and system-wide analytics with ECharts visualizations |
| F28 | **Dynamic QR Attendance Tracking** | QR code refreshes every 15 seconds, manual override, attendance history |
| F29 | **Calendar & Schedule View** | Unified calendar with exams, assignments, and custom events |
| F30 | **Online Payment Gateway (Stripe)** | Secure checkout, payment history, pending payments, webhook processing |
| F31 | **Dark / Light Mode** | Seamless theme toggle with CSS custom properties |
| F32 | **English / Arabic (i18n)** | Full RTL support with ngx-translate |
| F33 | **Mobile Guard** | Detects and blocks mobile/tablet devices from accessing exam engine |
| F34 | **Single Active Session Enforcement** | Real-time single-session-per-user policy via `SecurityHub` SignalR. Login from a new device instantly revokes old tokens and triggers a 7-second blocking countdown overlay on the displaced session before forced logout. |
| F35 | **Live Real-Time Course Leaderboard** | Top-10 course ranking with SignalR real-time updates, neon podium (Gold/Silver/Bronze), delta rank badges (↑↓=, NEW), tutor-controlled visibility, dense-rank tie handling, and student own-rank card. |

---

## 🔧 Installation & Setup Guide

<details>
<summary>Click to expand complete Step-by-Step Installation Guide</summary>

### 📋 Prerequisites (For Beginners)

If you are new to development and want to run this project on your own computer, you need to download and install the following tools first. They are all free! 🆓

1. **Node.js**: Required to run the frontend ([nodejs.org](https://nodejs.org/)).
2. **.NET 9 SDK**: The engine that runs the backend ([dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0)).
3. **SQL Server**: Database engine ([SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads)).
4. **SSMS**: Database management GUI ([SSMS Download](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)).
5. **Git**: Version control ([git-scm.com](https://git-scm.com/)).
6. **Stripe CLI** *(optional)*: Payment testing ([stripe-cli](https://docs.stripe.com/stripe-cli)).

---

### 🚀 Step-by-Step Setup

1. **Clone Repository**:
   ```bash
   git clone https://github.com/ahmed-samir-dev/SHIELDON.git
   cd SHIELDON
   ```

2. **Configure Database**:
   Update `backend/SHIELDON.API/appsettings.json` connection string to your local SQL Server instance name.

3. **Initialize Database**:
   ```bash
   cd backend
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```

4. **Run WhatsApp Gateway Microservice**:
   ```bash
   cd backend/whatsapp-gateway
   npm install
   npm start
   ```

5. **Run Backend API**:
   ```bash
   cd backend/SHIELDON.API
   dotnet run
   ```

6. **Run Frontend Application**:
   ```bash
   cd frontend
   npm install
   npm start
   ```

7. **Stripe Payment Setup** *(Optional)*:
   Set `Stripe:SecretKey`, `PublishableKey`, and run `stripe listen --forward-to localhost:5000/api/webhooks/stripe`.

> 📖 For full step-by-step instructions, see the dedicated [docs/INSTALLATION.md](docs/INSTALLATION.md) guide.

</details>

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

The SHIELDON API surfaces **160+ API interaction points** — **146 REST endpoints** across **26 controllers** + **14 SignalR real-time events** across **5 hubs** — all fully documented and explorable via Swagger.


<details>
<summary>Click to expand complete API Reference</summary>

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
| `POST` | `/api/auth/google` | Sign in / register via Google OAuth 2.0 |

### Profile & Phone Verification (`/api/profile`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/profile` | Get current user's profile |
| `PATCH` | `/api/profile` | Update profile details (name, etc.) |
| `POST` | `/api/profile/picture` | Upload/update profile picture (WebP) |
| `PATCH` | `/api/profile/password` | Change password |
| `PATCH` | `/api/profile/onboarding-complete` | Mark onboarding tour as complete |
| `PATCH` | `/api/profile/onboarding-reset` | Reset onboarding tour status |
| `PUT` | `/api/profile/phone` | Save or update verified phone number (E.164 format, unique per account) |
| `POST` | `/api/profile/phone/send-otp` | Send a 6-digit OTP via WhatsApp to the saved phone number |
| `POST` | `/api/profile/phone/verify-otp` | Verify the OTP code and mark phone as verified |

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
| `POST` | `/api/chat/conversations` | Create a 1-on-1 or Group conversation |
| `POST` | `/api/chat/conversations/{id}/messages` | Send a message (text, reply, or forward) |
| `POST` | `/api/chat/conversations/{id}/upload` | Upload a file or voice note attachment |
| `POST` | `/api/chat/messages/{id}/react` | Toggle an emoji reaction on a message |
| `DELETE` | `/api/chat/messages/{id}` | Delete a message (own, or any if Group Admin) |
| `POST` | `/api/chat/messages/forward` | Forward a message to multiple conversations |
| `GET` | `/api/chat/conversations/{id}/participants` | List group participants and admin status |
| `PATCH` | `/api/chat/conversations/{id}/rename` | Rename a group (Group Admin only) |
| `POST` | `/api/chat/conversations/{id}/members` | Add member(s) to a group (Group Admin only) |
| `DELETE` | `/api/chat/conversations/{id}/members/{userId}` | Remove a member from a group (Group Admin only) |
| `DELETE` | `/api/chat/conversations/{id}` | Permanently delete a group (Group Admin only) |

### Calendar (`/api/calendar`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/calendar/events` | Get all calendar events |
| `POST` | `/api/calendar/events/custom` | Create a custom event |
| `PUT` | `/api/calendar/events/custom/{eventId}` | Update a custom event |
| `DELETE` | `/api/calendar/events/custom/{eventId}` | Delete a custom event |

### Leaderboard (`/api/courses/{courseId}/leaderboard`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/courses/{courseId}/leaderboard` | Get Top-10 course leaderboard (students see only if visible) |
| `GET` | `/api/courses/{courseId}/leaderboard/settings` | Get leaderboard visibility settings (Tutor/Admin) |
| `PUT` | `/api/courses/{courseId}/leaderboard/settings` | Update leaderboard visibility settings (Tutor/Admin) |
| `POST` | `/api/courses/{courseId}/leaderboard/refresh` | Manually trigger leaderboard recompute & broadcast (Tutor/Admin) |

### IP Audit Trail (`/api/admin` & `/api/users` & `/api/attempts`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/admin/audit-trail` | Paginated system-wide IP audit log with filters (Admin only) |
| `GET` | `/api/users/{userId}/ip-logs` | All IP logs for a specific user (Admin/Tutor) |
| `GET` | `/api/attempts/{attemptId}/ip-logs` | IP logs linked to a specific exam attempt (Admin/Tutor) |

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

### SignalR Hubs (Real-Time WebSocket Events)

#### ChatHub (`ws://.../hubs/chat`)

| Event | Direction | Description |
|---|---|---|
| `ReceiveMessage` | Server → Client | Delivers a new message to all conversation participants |
| `MessageReactionChanged` | Server → Client | Broadcasts updated emoji reaction state for a message |
| `MessageDeleted` | Server → Client | Notifies all participants that a message was soft-deleted |
| `UserIsTyping` | Server → Client | Pushes composing state (`conversationId`, `userName`) |
| `UserIsOffline` | Server → Client | Pushes `lastSeenAt` timestamp when a user disconnects |
| `GroupRenamed` | Server → Client | Notifies all members of a group name change |
| `AddedToGroup` | Server → Client | Notifies a user they were added to a group |
| `RemovedFromGroup` | Server → Client | Notifies a user they were removed from a group |
| `GroupDeleted` | Server → Client | Notifies all members their group conversation was deleted |
| `NotifyTyping` | Client → Server | Client signals composing activity with `conversationId` |

#### LeaderboardHub (`ws://.../hubs/leaderboard`)

| Event | Direction | Description |
|---|---|---|
| `LeaderboardUpdated` | Server → Client | Broadcasts recomputed Top-10 rankings to all subscribed course members |

#### SecurityHub (`ws://.../hubs/security`)

| Event | Direction | Description |
|---|---|---|
| `SessionRevoked` | Server → Client | Forces logout on displaced sessions when a new login is detected |

#### AttendanceHub (`ws://.../hubs/attendance`)

| Event | Direction | Description |
|---|---|---|
| `QrRefreshed` | Server → Client | Broadcasts newly generated QR code every 15 seconds to the tutor's session |

#### DashboardHub (`ws://.../hubs/dashboard`)

| Event | Direction | Description |
|---|---|---|
| `DashboardUpdated` | Server → Client | Pushes live admin/tutor monitoring dashboard stats updates |

</details>


---

## 🤝 Contributing & Git Workflow

We welcome contributions to **SHIELDON**! To ensure high code quality, consistency, and a seamless developer experience, please follow these guidelines when contributing:

### 🔀 Branching Strategy
- 🚀 **`main` Branch**: Production-ready code. Must always compile cleanly, pass tests, and be deployable. **Never push directly to `main`.**
- 🛠️ **`develop` Branch**: Main integration branch for active development.
- 🌿 **Feature & Fix Branches**: Create focused branches off `develop`:
  ```bash
  git checkout develop
  git pull origin develop
  git checkout -b feature/your-feature-name   # For new features
  git checkout -b bugfix/issue-description   # For bug fixes
  git checkout -b docs/update-readme         # For documentation updates
  ```

### 📝 Commit Message Standards (Conventional Commits)
All commit messages should follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:
- `feat:` — New feature added
- `fix:` — Bug fix
- `docs:` — Documentation updates
- `style:` — Code formatting, missing semicolons, CSS adjustments (no logic change)
- `refactor:` — Code restructuring without adding features or fixing bugs
- `test:` — Adding or updating automated tests
- `chore:` — Maintenance tasks, dependency updates, configuration changes

*Example:* `feat(chat): add real-time link preview auto-extraction`

### 💻 Technical Guidelines

#### Backend (.NET 9)
- Adhere strictly to **Clean Architecture** (Domain → Application → Infrastructure → API). Never leak Infrastructure into the Domain layer.
- Keep `Nullable` reference types active and eliminate compiler warnings.
- Annotate all API controller endpoints with Swagger response attributes.
- Test EF Core migrations locally before committing (`dotnet ef database update`).

#### Frontend (Angular 21)
- Use **Standalone Components**, Angular Signals, and modern RxJS patterns.
- Utilize established CSS Custom Properties (`var(--primary-color)`, etc.) for dark/light mode consistency.
- Delegate business logic and HTTP calls to dedicated Angular Services.

### 📬 Submitting a Pull Request (PR)
1. Push your branch to GitHub: `git push origin feature/your-feature-name`
2. Open a Pull Request targeting `develop` (or `main` for release milestones).
3. Fill out the PR description outlining key changes and verification steps.
4. Once reviewed and approved, merge using **Squash and Merge**.

> 📖 For full detailed instructions, check out the official [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) guide.

---

<div align="center">
  <strong> SHIELDON — "Integrity You Can Trust" </strong>
</div>
