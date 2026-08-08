# SHIELDON - Integrity You Can Trust

<div align="center">
  <img src="https://img.shields.io/badge/.NET_9-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Angular_21-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular 21" />
  <img src="https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/SCSS-CC6699?style=for-the-badge&logo=sass&logoColor=white" alt="SCSS" />
  <img src="https://img.shields.io/badge/SQL_Server_2022-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/SignalR-512BD4?style=for-the-badge&logo=.net&logoColor=white" alt="SignalR" />
  <img src="https://img.shields.io/badge/WhatsApp_Gateway-25D366?style=for-the-badge&logo=whatsapp&logoColor=white" alt="WhatsApp Gateway" />
  <img src="https://img.shields.io/badge/WebRTC-333333?style=for-the-badge&logo=webrtc&logoColor=white" alt="WebRTC" />
  <img src="https://img.shields.io/badge/Stripe-635BFF?style=for-the-badge&logo=stripe&logoColor=white" alt="Stripe" />
  <img src="https://img.shields.io/badge/Google_OAuth-4285F4?style=for-the-badge&logo=google&logoColor=white" alt="Google OAuth" />
  <img src="https://img.shields.io/badge/Lucide_Icons-F54E00?style=for-the-badge&logo=lucide&logoColor=white" alt="Lucide Icons" />
</div>

> A full-stack E-Learning Management System (LMS) with a built-in browser-native Anti-Cheating Engine with no external exam-locking software required.

---

## 📑 Table of Contents

- [🔭 Project Overview](#-project-overview)
- [⚙️ Technology Stack](#️-technology-stack)
- [🏛️ Architecture](#️-architecture)
- [🧪 Testing Layer & Quality Assurance](#-testing-layer--quality-assurance)
- [👥 System Roles](#-system-roles)
- [🚀 Comprehensive Feature List (F1 – F35)](#-comprehensive-feature-list-f1--f35)
- [📋 Prerequisites (For Beginners)](#-prerequisites-for-beginners)
- [🔧 Installation & Setup (Step-by-Step Guide)](#-installation--setup-step-by-step-guide)
- [🧪 How to Test (Demo Accounts)](#-how-to-test-demo-accounts)
- [📡 API Endpoints Reference](#-api-endpoints-reference)
- [🤝 Contributing & Git Workflow](#-contributing--git-workflow)

---

## 🔭 Project Overview

**SHIELDON** is a comprehensive educational platform that combines a modern Learning Management System with a robust, browser-based Exam Integrity System.

Most traditional LMS platforms depend on external software like Safe Exam Browser (SEB) or LockDown Browser to enforce exam security, requiring students to download and install external applications to avoid cheating. **SHIELDON eliminates this dependency entirely** by building the Anti-Cheating Engine directly into the web platform using standard Web APIs.

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

### Database
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
| **Isolation** | Runs as a completely separate process - if it restarts, the .NET API stays unaffected. |
| **Communication** | Called exclusively by `SHIELDON.Infrastructure` via `IOtpService` → `WhatsAppGatewayOtpService`. |
| **Clean Architecture Compliance** | The .NET core has zero knowledge of Node.js or Baileys - only the `IOtpService` interface is referenced. Swapping to Meta Cloud API or Twilio requires changing only one line in `DependencyInjection.cs`. |
| **Security** | Port 3001 is internal-only and never exposed to the internet or the frontend directly. |

---

## 🧪 Testing Layer & Quality Assurance

> 📖 **Full Testing Strategy Guide**: [docs/TESTING.md](docs/TESTING.md)

SHIELDON implements a robust **100% Pass Rate** testing suite for both Backend (.NET 9) and Frontend (Angular 21 + Vitest), covering unit logic and end-to-end API integration testing across all platform module clusters.

### 📊 Executive Test Metrics Summary

| Category | Total Test Files | Total Executed Tests | Passed | Failed | Pass Rate |
|---|---|---|---|---|---|
| 🔐 **Backend Unit Tests** | 14 test classes | 32 tests | 32 | 0 | **100%** |
| 🌐 **Backend Integration Tests** | 10 test classes | 13 tests | 13 | 0 | **100%** |
| 🎨 **Frontend Unit Tests** | 26 spec files | 35 tests | 35 | 0 | **100%** |
| 🏆 **TOTAL COMBINED SUITE** | **50 test files** | **80 tests** | **80** | **0** | **100%** |

<details>
<summary><b>🔍 Expand to view complete test suite breakdown & execution guide</b></summary>
<br>

#### 1. Backend Test Breakdown (`SHIELDON.Tests`)

| # | Test File | Test Type | Module / Domain Area Covered | Test Count | Result |
|---|---|---|---|---|---|
| 1 | `AuthServiceTests.cs` | **Unit Test** | Module 1: Registration, bcrypt password hashing, login token generation, lockout counters | 5 | `[x]` **PASSED** |
| 2 | `RegistrationIntegrationTests.cs` | **Integration Test** | Module 1: End-to-end `POST /api/auth/register` API endpoint (201 Created vs 400 BadRequest) | 2 | `[x]` **PASSED** |
| 3 | `ProfileServiceTests.cs` | **Unit Test** | Module 1: Profile fetching, user identity validation | 2 | `[x]` **PASSED** |
| 4 | `ProfileIntegrationTests.cs` | **Integration Test** | Module 1: End-to-end `GET /api/profile` & `PUT /api/profile` user identity API pipeline | 2 | `[x]` **PASSED** |
| 5 | `CourseServiceTests.cs` | **Unit Test** | Module 2: Course CRUD, unique `CourseCode` generation, assigned tutor checks | 3 | `[x]` **PASSED** |
| 6 | `CourseIntegrationTests.cs` | **Integration Test** | Module 2: End-to-end `GET /api/courses` authorization API pipeline | 1 | `[x]` **PASSED** |
| 7 | `AnnouncementServiceTests.cs` | **Unit Test** | Module 2: Announcement creation, priority levels ('Normal'/'Important'), tutor RBAC | 3 | `[x]` **PASSED** |
| 8 | `AssignmentServiceTests.cs` | **Unit Test** | Module 2: Assignment fetching, non-existent course error handling | 2 | `[x]` **PASSED** |
| 9 | `MaterialServiceTests.cs` | **Unit Test** | Module 2: Material upload limits, MIME security checks, deletion RBAC | 2 | `[x]` **PASSED** |
| 10 | `ExamServiceTests.cs` | **Unit Test** | Module 3: Exam creation, query parameter filtering, exam retrieval | 2 | `[x]` **PASSED** |
| 11 | `ExamIntegrationTests.cs` | **Integration Test** | Module 3: End-to-end `GET /api/courses/{id}/exams` authorization API pipeline | 1 | `[x]` **PASSED** |
| 12 | `ExamResultServiceTests.cs` | **Unit Test** | Module 3: Attempt result fetching, Not Found handling, student cross-attempt security | 2 | `[x]` **PASSED** |
| 13 | `ViolationServiceTests.cs` | **Unit Test** | Module 4: Anti-Cheat violation batch logging, attempt ownership check, DB persistence | 2 | `[x]` **PASSED** |
| 14 | `ViolationIntegrationTests.cs` | **Integration Test** | Module 4: End-to-end `POST /api/violations/attempt/{id}/log` Anti-Cheat API endpoint | 2 | `[x]` **PASSED** |
| 15 | `MonitoringServiceTests.cs` | **Unit Test** | Module 5: ProcessHeartbeat timeline aggregation, active session querying | 2 | `[x]` **PASSED** |
| 16 | `MonitoringIntegrationTests.cs` | **Integration Test** | Module 5: End-to-end `POST /api/monitoring/heartbeat` live proctoring API pipeline | 2 | `[x]` **PASSED** |
| 17 | `ChatServiceTests.cs` | **Unit Test** | Module 6: Real-Time Chat empty inbox handling, 1-on-1 DM conversation creation | 2 | `[x]` **PASSED** |
| 18 | `ChatIntegrationTests.cs` | **Integration Test** | Module 6: End-to-end `GET /api/chat/conversations` direct messaging API pipeline | 1 | `[x]` **PASSED** |
| 19 | `LeaderboardServiceTests.cs` | **Unit Test** | Module 7: Non-existent course handling, hidden leaderboard student filtering | 2 | `[x]` **PASSED** |
| 20 | `LeaderboardIntegrationTests.cs` | **Integration Test** | Module 7: End-to-end `GET /api/leaderboard/course/{id}` API pipeline | 1 | `[x]` **PASSED** |
| 21 | `AttendanceServiceTests.cs` | **Unit Test** | Module 7: Dynamic 30s QR check creation, secret key generation, tutor check deactivation | 2 | `[x]` **PASSED** |
| 22 | `AttendanceIntegrationTests.cs` | **Integration Test** | Module 7: End-to-end `GET /api/attendance/courses/{id}/active-session` API pipeline | 1 | `[x]` **PASSED** |
| 23 | `PaymentServiceTests.cs` | **Unit Test** | Module 7: Stripe payment history, paid course transaction filtering | 2 | `[x]` **PASSED** |
| 24 | `PaymentIntegrationTests.cs` | **Integration Test** | Module 7: End-to-end `GET /api/payments/history` Stripe integration API pipeline | 1 | `[x]` **PASSED** |
| 25 | `CalendarServiceTests.cs` | **Unit Test** | Module 7: User calendar event querying and deadline sync | 1 | `[x]` **PASSED** |

#### 2. Frontend Test Breakdown (`frontend`)

> **Architecture Note**: All component specs are co-located right next to their implementation files within their respective **Vertical Slice** feature folders.

| # | Spec File | Test Type | Vertical Slice / Location | Test Count | Result |
|---|---|---|---|---|---|
| 1 | `theme.service.spec.ts` | **Unit Test (Service)** | `src/app/core/services/` — Light/Dark theme switching, `data-theme` attribute | 3 | `[x]` **PASSED** |
| 2 | `language.service.spec.ts` | **Unit Test (Service)** | `src/app/core/services/` — English/Arabic i18n switching, `dir="rtl"` / `dir="ltr"` | 3 | `[x]` **PASSED** |
| 3 | `exam-device.guard.spec.ts` | **Unit Test (Guard)** | `src/app/core/guards/` — Viewport width check (`< 1024px`), redirect bypass | 1 | `[x]` **PASSED** |
| 4 | `auth.service.spec.ts` | **Unit Test (Service)** | `src/app/core/services/` — Reactive Angular signals state, token storage, logout | 2 | `[x]` **PASSED** |
| 5 | `login.spec.ts` | **Unit Test (Component)** | `src/app/features/auth/login/` — Login form controls, validation state | 4 | `[x]` **PASSED** |
| 6 | `register.spec.ts` | **Unit Test (Component)** | `src/app/features/auth/register/` — Register form controls, userType selection | 2 | `[x]` **PASSED** |
| 7 | `verify-email.spec.ts` | **Unit Test (Component)** | `src/app/features/auth/verify-email/` — Verify email view & translation resolution | 1 | `[x]` **PASSED** |
| 8 | `landing.spec.ts` | **Unit Test (Component)** | `src/app/features/public/landing/` — Hero animations, IntersectionObserver | 1 | `[x]` **PASSED** |
| 9 | `mobile-blocked.spec.ts` | **Unit Test (Component)** | `src/app/features/public/mobile-blocked/` — Device blocked redirect view creation | 1 | `[x]` **PASSED** |
| 10 | `my-grades.spec.ts` | **Unit Test (Component)** | `src/app/features/grades/my-grades/` — Student gradebook view & GradeService | 1 | `[x]` **PASSED** |
| 11 | `course-grades.spec.ts` | **Unit Test (Component)** | `src/app/features/grades/course-grades/` — Tutor course gradebook management | 1 | `[x]` **PASSED** |
| 12 | `exam-result-page.spec.ts` | **Unit Test (Component)** | `src/app/features/exams/exam-result-page/` — Student exam score result ring view | 1 | `[x]` **PASSED** |
| 13 | `tutor-results-panel.spec.ts` | **Unit Test (Component)** | `src/app/features/exams/tutor-results-panel/` — Tutor exam results panel component | 1 | `[x]` **PASSED** |
| 14 | `global-progress-bar.spec.ts` | **Unit Test (Component)** | `src/app/shared/components/global-progress-bar/` — Progress bar UI directive | 1 | `[x]` **PASSED** |
| 15 | `public-layout.spec.ts` | **Unit Test (Component)** | `src/app/layouts/public-layout/` — Navbar layout wrapper & i18n controls | 1 | `[x]` **PASSED** |
| 16 | `app.spec.ts` | **Unit Test (Component)** | `src/app/` — Root Angular App component instantiation | 1 | `[x]` **PASSED** |
| 17 | `global-call-overlay.spec.ts` | **Unit Test (Component)** | `src/app/shared/components/global-call-overlay/` — Global web RTC call overlay | 1 | `[x]` **PASSED** |

#### 3. Execution Commands

- **Backend Test Runner**:
  ```bash
  cd backend
  dotnet test
  ```
- **Frontend Test Runner**:
  ```bash
  cd frontend
  npx vitest run
  ```

</details>

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
| F1 | **Secure Login & Role-Based Redirect** | JWT authentication, refresh tokens, single-session enforcement, logout confirmation |
| F2 | **Google OAuth 2.0 Login** | Passwordless social login - sign in instantly with any Google account |
| F3 | **Email Verification** | SMTP integration (Mailtrap / Gmail), verification tokens |
| F4 | **Password Reset Via Email** | Forgot-password workflow with secure reset links |
| F5 | **Phone Verification via WhatsApp OTP** | 6-digit WhatsApp OTP via self-hosted Node.js microservice gateway. Unique phone per account enforcement, 2-minute resend cooldown, 6-cell auto-advancing OTP modal input |
| F6 | **Profile Management** | WebP avatar upload, edit profile, change password, reset tour guide, phone number management |
| F7 | **Public Registration** | Student or Tutor role selection during sign-up |
| F8 | **Course Management & Enrollment** | Full CRUD, Admin-only hard delete with 3 safeguards (Smart Gate for paid/exam records, course code typing confirm, permanent `CourseDeleteAuditLog`), paginated enrollment & Removed/Dropped Students Panel, bulk review, enroll/kick/drop |
| F9 | **File Sharing (Course Materials)** | Upload, download, and manage course resources |
| F10 | **Announcements** | Post, feed, priority pinning, and manual drag-and-drop re-ordering for course announcements |
| F11 | **Assignment Management System** | Task lifecycle, file submissions, ZIP bulk export, review & grading |
| F12 | **Notifications** | In-app and email notifications for all key system events |
| F13 | **Exam Management & Notifications** | CRUD, publish workflow, scheduling, deadline management, reminders |
| F14 | **Re-Attempt & Re-Open Requests** | Students request re-attempts/re-opens, tutors approve with configurable extensions (24h/48h/72h/custom) |
| F15 | **Question Bank Management** | Centralized course-level question bank (MCQ, True/False, Short Answer) with image support |
| F16 | **Exam Engine + Secure Token** | Countdown timer, Red Flag question bookmarking, question navigator with filter tabs, auto-submit on timeout, cryptographic question randomization |
| F17 | **Exam Results & Auto-Grading** | Confetti animation, per-question review, manual grading for short answers |
| F18 | **Grade Management Panel** | Bulk publish, CSV export, weighted grade calculation |
| F19 | **Anti-Cheating Engine** | Browser-native exam integrity system - no plugins or extensions required. <details><summary>See 12 sub-features ▼</summary><br>**1. Pre-Exam Rules Acknowledgment Modal** - Students must read and accept exam integrity rules before starting<br>**2. Fullscreen Exit Enforcement** - Logs a critical violation if the student attempts to exit fullscreen mode<br>**3. Tab Switching & Focus Loss** - Triggers violations if the student switches tabs or clicks outside the exam window<br>**4. Keyboard Shortcut Blocking** - Blocks Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+A, Ctrl+P, Ctrl+F, Ctrl+U, F12, Ctrl+Shift+I/J, Esc, Alt+Tab<br>**5. Window Resize / Minimize / Split Detection** - Detects split-screen, window resizing, and minimize attempts<br>**6. Mouse Monitoring (Pattern Analysis)** - Tracks mouse movement patterns for anomaly detection<br>**7. Selection by Mouse Blocking** - Prevents text selection via mouse during exams<br>**8. Right-Click Context Menu Blocking** - Disables right-click to prevent copy/paste/inspect operations<br>**9. Violation Intelligence Layer** - Severity scoring per violation type + cooldown periods to prevent duplicate flooding<br>**10. Action Debouncer & Score Normalization** - 500ms aggregation window preventing cascading violations (e.g. ALT+TAB) and unified decimal scoring (Minor=0.5, Medium=1.0, Critical=1.0)<br>**11. Warning System & Force-Submit** - 3-strike escalation (displayed on an elegant horizontal progress bar) - warnings → final warning → auto force-submit<br>**12. Monitoring Continuity on Reconnect** - Anti-cheat resumes seamlessly if the student disconnects or refreshes</details> |
| F20 | **Session Timeline View** | Per-attempt vertical timeline with live Presence Tracking (connection status) |
| F21 | **Violation Density Analytics** | Advanced sticky Bubble/Scatter Chart mapping violation frequency against severity over time |
| F22 | **Tutor Monitoring Dashboard** | Live overview of ongoing exams and violations with CSV data export |
| F23 | **Admin Dashboard & Global Layout** | System-wide admin panel with responsive Collapsible Sidebar and dynamic data grids |
| F24 | **SHIELDON AI Assistant** | Gemini-powered chatbot with backend proxy, automatically blocked during exams |
| F25 | **Real-Time Chat System** | Built with SignalR (WebSockets), WebRTC, and browser-native APIs. <details><summary>See 12 sub-features ▼</summary><br>**1. File Attachments & Uploads** - Send images, documents, and audio files (max 10 MB). Images render as constrained thumbnail previews (250×250px). Documents display with a styled download link.<br>**2. Voice Notes** - Record audio messages (up to 5 minutes) directly from the chat composer via HTML5 MediaRecorder. Renders as a full-width horizontal audio player bubble.<br>**3. Delivery Receipts** - Three-state read receipts: single gray tick (Sent), double gray tick (Delivered), double blue tick (Read). Updated in real-time via SignalR callbacks.<br>**4. WebRTC 1-on-1 Video Calls** - Peer-to-peer WebRTC video calls with a global ringtone overlay that persists across SPA page navigation. Strict media track teardown on hang-up, rejection, or logout.<br>**5. Group Chat Management** - Full group lifecycle: create (Admin/Tutor only), rename, add/remove members, and permanently delete (Group Admin/creator only, with cascade delete).<br>**6. Contacts Filtration** - Real-time inbox filtering by status (All / Online / Offline) and by role (Admin / Tutor / Student) powered by the SignalR PresenceTracker.<br>**7. Last Seen Tracking** - Shows relative time (e.g. "2 hours ago") for recently offline users, or an absolute date/time stamp for users offline more than 24 hours.<br>**8. Real-Time Typing Indicators** - 3-dot pulsating animation in the active chat window plus an italic typing... prompt in the sidebar inbox row. Auto-clears after 2.5s of inactivity.<br>**9. Message Reactions** - Emoji reaction picker with optimistic UI updates. Aggregated reaction pill chips (e.g. 👍 3). Reaction-details modal with per-emoji tabs showing user avatars and counts.<br>**10. Message Deletion** - Any user may delete their own messages. Group Admins may delete any member's message. Replaced with a dashed "deleted" bubble for all participants.<br>**11. Reply & Forward** - Inline reply with a quoted message block and click-to-scroll highlight animation. Forward sends messages to multiple conversations with double-curved arrow icon.<br>**12. Link Thumbnail Preview Loading** - Rich link preview cards (Open Graph image thumbnail, page title, description snippet, domain pill) fetched asynchronously for external URLs shared in chat messages.</details> |
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

## 📋 Prerequisites (For Beginners)

If you are new to development and want to run this project on your own computer, you need to download and install the following tools first. They are all free!

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

Follow these comprehensive steps in order to properly get the project running on your device from scratch. 

<details>
<summary>Click to expand complete Installation & Setup Guide</summary>

### 1. Clone the Repository

This downloads the project files to your computer. 

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

### 4. Run the WhatsApp Gateway Microservice (For Phone Verification & OTP)

The WhatsApp Gateway is a lightweight Node.js microservice running on port 3001 that sends 6-digit OTP verification codes via WhatsApp.

1. Open a **new terminal window** and navigate to the gateway directory:
   ```bash
   cd backend/whatsapp-gateway
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the gateway server:
   ```bash
   npm start
   ```
4. **First Time Pairing**:
   - The terminal will display a **WhatsApp QR code**.
   - Open WhatsApp on your mobile phone → **Linked Devices** → **Link a Device** and scan the terminal QR code.
   - Once linked, it will output `WhatsApp connected! Gateway is ready to send OTP messages.`.
   - Your session is saved locally - you do **not** need to scan the QR code again on future restarts!
5. Keep this terminal window open.

### 5. Run the Backend API

1. Open a **new terminal window** and navigate into the API project:
   ```bash
   cd backend/SHIELDON.API
   ```
2. Start the server:
   ```bash
   dotnet run
   ```
   (Or use `dotnet watch run` for automatic hot-reloading)
3. The backend is now running! Visit the live API documentation at:
   👉 `http://localhost:5000/swagger`
4. Keep this terminal window open.

### 6. Run the Frontend Application

1. Open another **separate terminal window** (leave the backend & gateway running!).
2. Navigate to the frontend directory:
   ```bash
   cd frontend
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
6. Open your browser and navigate to:
   👉 `http://localhost:4201`

---

## 🎉 Congratulations! You are now running SHIELDON on your local machine!

> For full step-by-step documentation, see the dedicated [docs/INSTALLATION.md](docs/INSTALLATION.md) guide.

---

### 7. Stripe Payment Setup

To enable the online payment gateway, you need a Stripe account and the Stripe CLI.

#### Step A: Create a Stripe Account & Get API Keys
1. Go to [stripe.com](https://stripe.com) and create a **free** account.
2. After logging in, make sure you are in **Test mode** (toggle in the top-right of the dashboard).
3. Navigate to [Developers → API Keys](https://dashboard.stripe.com/test/apikeys).
4. You will see two keys:
   - **Publishable key** - starts with `pk_test_...`
   - **Secret key** - starts with `sk_test_...` (click "Reveal test key" to see it)
5. Copy both keys - you'll need them in the next step.

#### Step B: Install the Stripe CLI
The Stripe CLI is a command-line tool that forwards payment events from Stripe's servers to your local machine.

**Option 1 - Download manually (recommended for beginners):**
1. Go to [Stripe CLI releases](https://github.com/stripe/stripe-cli/releases).
2. Download the latest `.zip` file for your OS (e.g., `stripe_X.X.X_windows_x86_64.zip`).
3. Extract the `.zip` and place the `stripe.exe` file somewhere accessible (e.g., inside a `stripe_cli` folder in your project root).

**Option 2 - Install via package manager:**
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
   > You'll get the `WebhookSecret` in the next step - leave it blank for now.

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
6. **Keep this terminal open** while testing payments - it must be running to receive Stripe events.

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

The SHIELDON API surfaces **160+ API interaction points** - **146 REST endpoints** across **26 controllers** + **14 SignalR real-time events** across **5 hubs** - all fully documented and explorable via Swagger.


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
| `DELETE` | `/api/courses/{id}` | Hard-delete a course (Admin only; blocked if paid/exam records exist; writes `CourseDeleteAuditLog`) |
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
- `feat:` - New feature added
- `fix:` - Bug fix
- `docs:` - Documentation updates
- `style:` - Code formatting, missing semicolons, CSS adjustments (no logic change)
- `refactor:` - Code restructuring without adding features or fixing bugs
- `test:` - Adding or updating automated tests
- `chore:` - Maintenance tasks, dependency updates, configuration changes

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

> 📖 For full detailed instructions, check out the official [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) guide and [docs/TESTING.md](docs/TESTING.md) testing strategy guide.

---

<div align="center">
  <strong> SHIELDON - "Integrity You Can Trust" </strong>
</div>
