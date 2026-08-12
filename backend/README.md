# SHIELDON - Backend

<div align="center">
  <img src="https://img.shields.io/badge/.NET_9-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Entity_Framework-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt="EF Core 9" />
  <img src="https://img.shields.io/badge/SQL_Server_2022-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/SignalR-512BD4?style=for-the-badge&logo=.net&logoColor=white" alt="SignalR" />
  <img src="https://img.shields.io/badge/WhatsApp_Gateway-25D366?style=for-the-badge&logo=whatsapp&logoColor=white" alt="WhatsApp Gateway" />
  <img src="https://img.shields.io/badge/Stripe-635BFF?style=for-the-badge&logo=stripe&logoColor=white" alt="Stripe" />
  <img src="https://img.shields.io/badge/Google_OAuth-4285F4?style=for-the-badge&logo=google&logoColor=white" alt="Google OAuth" />
</div>

> 🔧 The Web API framework, business logic orchestration, and database persistence layer for the **SHIELDON** LMS & Anti-Cheating Engine.
> Includes a dedicated Node.js Microservice for zero-cost WhatsApp OTP delivery.

---

## 📑 Table of Contents

- [🔭 Project Overview](#-project-overview)
- [⚙️ Technology Stack](#️-technology-stack)
- [🏛️ Architecture & Microservice Layout](#️-architecture--microservice-layout)
- [🚀 Comprehensive Feature List (F1 – F35)](#-comprehensive-feature-list-f1--f35)
- [📡 API Controllers & SignalR Hubs](#-api-controllers--signalr-hubs)
- [🔧 Setup & Database Migration](#-setup--database-migration)
- [🤝 Contributing & Guidelines](#-contributing--guidelines)

---

## 🔭 Project Overview

This directory contains the backend for **SHIELDON**, organized using **Clean Architecture** with a **Vertical Slice Hybrid** approach in .NET 9, plus a Node.js microservice for WhatsApp integration.

Key backend responsibilities:

- **Authentication & Security**: ASP.NET Core Identity, JWT access/refresh tokens, Google OAuth 2.0, single active session enforcement (`SecurityHub`), and IP audit trail logging.
- **WhatsApp OTP Verification**: `IOtpService` integration with a Node.js Baileys microservice for zero-cost 6-digit WhatsApp OTP delivery.
- **LMS Engine**: Full CRUD for courses, enrollments, materials, announcements, assignments, grading, attendance, and calendar events.
- **Exam Integrity Engine**: Cryptographic question randomization, attempt tokens, violation batch ingestion, severity intelligence scoring, and auto-grading.
- **Real-Time Communication**: SignalR hubs for WebSocket chat, presence tracking, typing indicators, live Top-10 leaderboard updates, session displacement, and QR attendance.
- **Integrations**: Stripe payment checkout & webhooks, Google Gemini AI proxy, and MailKit email notifications (Mailtrap / Gmail SMTP).

---

## ⚙️ Technology Stack

### Backend (.NET 9)

| Technology                    | Purpose                                                                             |
| ----------------------------- | ----------------------------------------------------------------------------------- |
| **.NET 9 ASP.NET Core**       | Web API framework                                                                   |
| **C#**                        | Language (Nullable reference types enabled)                                         |
| **Entity Framework Core 9**   | Code-First ORM & database migrations                                                |
| **Microsoft SQL Server 2022** | Relational database storage                                                         |
| **SignalR**                   | Real-time WebSockets (Chat, Presence, Leaderboard, Security, Attendance, Dashboard) |
| **ASP.NET Core Identity**     | User authentication & role management                                               |
| **Google OAuth 2.0**          | Passwordless social authentication                                                  |
| **MailKit / MimeKit**         | Email dispatch (Mailtrap for dev, Gmail SMTP for prod)                              |
| **FluentValidation**          | Request validation layer                                                            |
| **Stripe.net**                | Payment processing & webhook listener                                               |
| **Google Gemini API**         | AI assistant backend proxy                                                          |
| **Serilog**                   | Structured logging                                                                  |
| **Swashbuckle / Swagger**     | Interactive OpenAPI documentation                                                   |

### WhatsApp Gateway Microservice (Node.js)

| Technology                  | Purpose                                          |
| --------------------------- | ------------------------------------------------ |
| **Node.js 18+**             | Runtime environment (`backend/whatsapp-gateway`) |
| **@whiskeysockets/baileys** | Self-hosted WhatsApp Web socket library          |
| **Express.js**              | Internal HTTP API on port 3001                   |
| **qrcode-terminal**         | QR code pairing for first-time setup             |

---

## 🏛️ Architecture & Microservice Layout

```
  ┌──────────────────────────────────────────────────────┐
  │             Angular 21 Frontend (SPA)                │  ← Port 4201
  └─────────────────────────┬────────────────────────────┘
                            │ HTTP / SignalR WebSocket
  ┌─────────────────────────▼────────────────────────────┐
  │                   SHIELDON.API                       │  ← Port 5000 (Thin HTTP Controllers)
  ├──────────────────────────────────────────────────────┤
  │              SHIELDON.Application                    │  ← Use Cases, DTOs, Validators, IOtpService
  ├──────────────────────────────────────────────────────┤
  │             SHIELDON.Infrastructure                  │  ← DB, Email, File Storage, WhatsApp Caller
  ├──────────────────────────────────────────────────────┤
  │                SHIELDON.Domain                       │  ← Entities & Core Business Rules
  ├──────────────────────────────────────────────────────┤
  │                SHIELDON.Tests                        │  ← Unit & Integration Tests
  └─────────────────────────┬────────────────────────────┘
                            │ Internal HTTP (localhost only)
  ┌─────────────────────────▼────────────────────────────┐
  │       WhatsApp Gateway Microservice (Node.js)        │  ← Port 3001 (Baileys / WhatsApp Web)
  └──────────────────────────────────────────────────────┘
```

| Layer                       | Responsibility                                                                                                    |
| --------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| **SHIELDON.API**            | Entry point. Contains HTTP controllers and SignalR Hub endpoints.                                                 |
| **SHIELDON.Application**    | Core business use cases, DTOs, FluentValidation rules, and service interfaces.                                    |
| **SHIELDON.Infrastructure** | External services: EF Core DB Context, Stripe, Email, Storage, Gemini AI, and `WhatsAppGatewayOtpService`.        |
| **SHIELDON.Domain**         | Core entities (`User`, `Course`, `Exam`, `ExamAttempt`, `Violation`, `ChatMessage`, etc.) with zero dependencies. |
| **SHIELDON.Tests**          | Unit and integration test suites.                                                                                 |

---

## 🚀 Comprehensive Feature List (F1 – F35)

| #   | Feature                             | Backend Implementation Details                                                                    |
| --- | ----------------------------------- | ------------------------------------------------------------------------------------------------- |
| F1  | **Secure Login & Role Redirect**    | JWT access/refresh tokens, BCrypt password hashing                                                |
| F2  | **Google OAuth 2.0 Login**          | Google token verification and automatic user registration                                         |
| F3  | **Email Verification**              | Verification token generation and email dispatch                                                  |
| F4  | **Password Reset via Email**        | Secure password reset token lifecycle                                                             |
| F5  | **WhatsApp OTP Phone Verification** | 6-digit OTP delivery via Node.js Baileys microservice, unique phone constraint, 2-min cooldown    |
| F6  | **Profile Management**              | Avatar WebP file handling, password change, tour reset                                            |
| F7  | **Public Registration**             | Student or Tutor user creation                                                                    |
| F8  | **Course Management & Enrollment**  | Paginated enrollment queries, bulk review endpoints                                               |
| F9  | **File Sharing (Course Materials)** | File storage service, secure download stream                                                      |
| F10 | **Announcements**                   | Priority pinning logic and course feed retrieval                                                  |
| F11 | **Assignment System**               | Submission handling, review/grading, ZIP bulk export                                              |
| F12 | **Notifications**                   | In-app notification store and email triggers                                                      |
| F13 | **Exam Management**                 | CRUD, scheduling, publish state Machine                                                           |
| F14 | **Re-Attempt & Re-Open Requests**   | Student request handling, tutor time-extension approvals                                          |
| F15 | **Question Bank**                   | MCQ, True/False, Short Answer authoring & image uploads                                           |
| F16 | **Exam Engine + Secure Token**      | Attempt token generation, cryptographic question randomization                                    |
| F17 | **Exam Results & Auto-Grading**     | Auto-grading engine and manual short-answer grading                                               |
| F18 | **Gradebook Panel**                 | Grade calculation, CSV export generation                                                          |
| F19 | **Anti-Cheating Engine**            | Batch violation ingestion, score normalization, 3-strike force-submit                             |
| F20 | **Session Timeline**                | Event log timeline generation per attempt                                                         |
| F21 | **Violation Analytics**             | Aggregated density statistics endpoint for scatter plot                                           |
| F22 | **Tutor Dashboard**                 | Real-time monitoring feed and exam summary statistics                                             |
| F23 | **Admin Dashboard**                 | System-wide statistics and user account management                                                |
| F24 | **SHIELDON AI Assistant**           | Secure backend proxy to Google Gemini API                                                         |
| F25 | **Real-Time Chat System**           | SignalR `ChatHub`, 1-on-1/Group messaging, WebRTC signaling, Voice notes, Link preview, Reactions |
| F26 | **Onboarding Tours**                | Tour completion state persistence                                                                 |
| F27 | **Analytics Dashboard**             | ECharts statistical payload endpoints                                                             |
| F28 | **Dynamic QR Attendance**           | SignalR `AttendanceHub`, 15s rotating QR code validation                                          |
| F29 | **Calendar View**                   | System event aggregator endpoint                                                                  |
| F30 | **Online Payments (Stripe)**        | Stripe Checkout Session creation & webhook signature verification                                 |
| F31 | **Theme Settings**                  | User theme preference persistence                                                                 |
| F32 | **Localization (i18n)**             | Internationalized response messages                                                               |
| F33 | **Mobile Guard**                    | Device user-agent inspection                                                                      |
| F34 | **Single Active Session**           | SignalR `SecurityHub` token revocation and displacement broadcast                                 |
| F35 | **Live Course Leaderboard**         | SignalR `LeaderboardHub` Top-10 ranking computation & tie-breaker engine                          |

---

## 📡 API Controllers & SignalR Hubs

Interactive OpenAPI documentation is available when running the API:
👉 `http://localhost:5000/swagger`

### 26 API Controllers (146 REST Endpoints)

`AuthController`, `ProfileController`, `UsersController`, `CoursesController`, `AnnouncementsController`, `MaterialsController`, `AssignmentsController`, `QuestionBankController`, `ExamsController`, `ExamAttemptsController`, `ExamResultsController`, `ReattemptRequestsController`, `ViolationsController`, `MonitoringController`, `GradesController`, `NotificationsController`, `AttendanceController`, `ChatController`, `CalendarController`, `LeaderboardController`, `AuditTrailController`, `PaymentController`, `StripeWebhookController`, `AiController`, `FilesController`.

### 5 Real-Time SignalR Hubs

1. **`ChatHub`** (`/hubs/chat`): Message delivery, typing indicators, read receipts, reactions, deletions, group updates.
2. **`LeaderboardHub`** (`/hubs/leaderboard`): Real-time Top-10 leaderboard score broadcasts.
3. **`SecurityHub`** (`/hubs/security`): Single-session displacement events.
4. **`AttendanceHub`** (`/hubs/attendance`): 15-second dynamic QR code refreshing.
5. **`DashboardHub`** (`/hubs/dashboard`): Live admin & tutor monitoring statistics.

---

## 🔧 Setup & Database Migration

> 📖 For step-by-step beginner instructions, read [docs/INSTALLATION.md](../docs/INSTALLATION.md).

1. **Configure Connection String**: Update `SHIELDON.API/appsettings.json` with your SQL Server instance name.
2. **Apply EF Core Migrations**:
   ```bash
   cd backend
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```
3. **Start WhatsApp Gateway Microservice**:
   ```bash
   cd backend/whatsapp-gateway
   npm install
   npm start
   ```
4. **Run API**:
   ```bash
   cd backend/SHIELDON.API
   dotnet run
   ```

---

## 🤝 Contributing & Guidelines

Please review [docs/CONTRIBUTING.md](../docs/CONTRIBUTING.md) before submitting pull requests.

---

<div align="center">
  <strong>🛡️ SHIELDON Backend - "Integrity You Can Trust" 🛡️</strong>
</div>
