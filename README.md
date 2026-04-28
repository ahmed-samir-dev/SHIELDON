# SHIELDON — Integrity You Can Trust

> A full-stack Learning Management System with a built-in browser-native Anti-Cheating Engine.
> Built as a graduation project — no external exam-locking software required.

---

## Project Overview

**SHIELDON** eliminates the dependency on third-party exam security tools (Safe Exam Browser, LockDown Browser).
The Anti-Cheating Engine is built **directly into the browser** using standard Web APIs:
- Fullscreen API
- Visibility API (tab switching detection)
- Keyboard event capture (shortcut blocking)
- Window resize/minimize detection
- Mouse pattern analysis

All violations are recorded in real-time and displayed in a **Session Timeline** and **Violation Timeline** — giving tutors full visibility into student behavior during and after the exam.

---

## Technology Stack

![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-9-512BD4?logo=dotnet&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-Auth-000000?logo=jsonwebtokens&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript&logoColor=white)

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21 (Standalone Components), TypeScript, SCSS |
| Backend | .NET 9 ASP.NET Core Web API, C# |
| Database | Microsoft SQL Server 2022, EF Core Code-First |
| Auth | JWT Bearer Tokens (Access + Refresh) |
| Icons | Lucide Icons (exclusively) |
| UI Libraries | SweetAlert2, ngx-toastr, Chart.js, canvas-confetti, Shepherd.js |

---

## Architecture

**Clean Architecture + Vertical Slice Hybrid:**

```
SHIELDON.sln
├── SHIELDON.Domain           → Entities, Enums, Constants (zero external dependencies)
├── SHIELDON.Application      → Feature slices: Commands, Handlers, DTOs, Validators
├── SHIELDON.Infrastructure   → EF Core, Email, File Storage (implements Application interfaces)
└── SHIELDON.API              → Thin HTTP controllers — no business logic
```

```
frontend/src/app/
├── core/        → Guards, Interceptors, Auth Service, Token Service
├── shared/      → Reusable components, directives, pipes
├── features/    → One folder per feature (vertical slice end-to-end)
├── layouts/     → PublicLayout (horizontal navbar), DashboardLayout (vertical sidebar)
└── assets/      → Design tokens, global styles, images
```

---

## AI-Assisted Development Workflow

This project enforces a strict **AI Model Split** for all feature development:
1. **Backend Phase (Steps 1-8):** Database changes, Domain Entities, Application logic, and API Controllers are built and tested.
2. **Model Switch Pause:** The AI explicitly stops and asks the user to switch the AI Model to **Gemini 3.1 Pro (High)**.
3. **Frontend Phase (Steps 9-12):** Angular services, standalone components, and UI/UX are built natively by Gemini 3.1 Pro. The frontend is never started until the backend is fully confirmed and the model is switched.

---

## System Roles

| Role | Access |
|------|--------|
| **Admin** | Full system: courses, users, all exams, all violations |
| **Tutor** | Assigned courses: create exams, upload materials, monitor violations |
| **Student** | Enrolled courses: take monitored exams, view own results |

---

## Feature Implementation Status

### Project Setup
| Stage | Feature | Status |
|-------|---------|--------|
| 0.1 | Repository & Folder Structure | ✅ Complete |
| 0.2 | Backend .NET Solution Scaffold | ✅ Complete |
| 0.3 | Database Initialization | ✅ Complete |
| 0.4 | Angular Frontend Scaffold | ✅ Complete |
| 0.5 | Design System Verification | ✅ Complete |
| 0.6 | Landing Page | ✅ Complete |
| 0.7 | Global Loading System | ✅ Complete |
| 0.8 | Device Guard + Placeholder Assets | ✅ Complete |

### Phase 1 — Authentication & User Management
| Stage | Feature | Status |
|-------|---------|--------|
| 1.1 | Auth Domain Entities + Database | ✅ Complete |
| 1.2 | F1: Secure Login & Role-Based Redirect | ✅ Complete |
| 1.3 | F2: Email Verification | ✅ Complete |
| 1.4 | F3: Password Reset Via Email | ✅ Complete |
| 1.5 | F4: Profile Management | ✅ Complete |

### Phase 2 — Core Learning Management System
| Stage | Feature | Status |
|-------|---------|--------|
| 2.1 | LMS Domain Entities + Database | ✅ Complete |
| 2.2 | F5: Course Management & Enrollment | ✅ Complete |
| 2.3 | F6: File Sharing (Course Materials) | ✅ Complete |
| 2.4 | F7: Announcements | ✅ Complete |
| 2.4b | F6b: Assignment Management System | ✅ Complete |
| 2.5 | F8: Notifications & Advanced Enrollment (Bulk Review, Filter/Paging) | ✅ Complete |

### Phase 3 — Examination Management System
| Stage | Feature | Status |
|-------|---------|--------|
| 3.1 | Exam & Grade Domain Entities + Database | ✅ Complete |
| 3.2 | F9: Exam Management + Exam Notifications (in-app & email) | ✅ Complete |
| 3.3 Backend | F10: Re-Attempt Requests — Backend | ✅ Complete |
| 3.4 | F11: Question Bank Management (MCQ / True-False / Short Answer) | ✅ Complete |
| 3.5 | F12/F13: Question Randomization + Timed Exam Engine + Secure Token | ⬜ Pending |
| 3.6 | F14: Exam Results & Auto-Grading | ⬜ Pending |
| 3.7 | F10 Frontend: Re-Attempt Requests — Frontend (backend done in 3.3) | ⬜ Pending |
| 3.8 | F15: Grade Management Panel (Student + Tutor/Admin, weighted grades) | ⬜ Pending |
| 3.9 | Phase 3 Integration & Polish | ⬜ Pending |

> **Why this order?** Publishing an exam requires at least 1 question (enforced by the API). So Question Bank (3.4) must come before the Exam Engine (3.5). The "Request Re-attempt" button belongs on the student result page (3.6), so Re-Attempt Frontend (3.7) comes last.

### Testing Stages
| Stage | Feature | Status |
|-------|---------|--------|
| T.1 | Backend Unit Tests (xUnit) | ⬜ Pending |
| T.2 | Backend Integration Tests | ⬜ Pending |
| T.3 | Frontend Unit Tests (Jest) | ⬜ Pending |

### Phase 4 — Anti-Cheating Engine
| Stage | Feature | Status |
|-------|---------|--------|
| 4.1 | Pre-Exam Rules Acknowledgment | ⬜ Pending |
| 4.2 | Fullscreen Enforcement | ⬜ Pending |
| 4.3 | Tab & Focus Detection | ⬜ Pending |
| 4.4 | Keyboard Shortcut Blocking | ⬜ Pending |
| 4.5 | Window Resize / Minimize / Split Detection | ⬜ Pending |
| 4.6 | Mouse Monitoring | ⬜ Pending |
| 4.7 | Violation Intelligence Layer | ⬜ Pending |
| 4.8 | Warning System + Force-Submit | ⬜ Pending |
| 4.9 | Backend Violation Persistence & API | ⬜ Pending |
| 4.10 | Monitoring Continuity on Reconnect | ⬜ Pending |

### Phase 5 — Monitoring & Dashboards
| Stage | Feature | Status |
|-------|---------|--------|
| 5.1 | F16: Exam Presence Tracking | ⬜ Pending |
| 5.2 | F17: Session Timeline View | ⬜ Pending |
| 5.3 | F18: Violation Timeline | ⬜ Pending |
| 5.4 | F19: Manual Review | ⬜ Pending |
| 5.5 | F20: Tutor Monitoring Dashboard | ⬜ Pending |
| 5.6 | F21: Admin Dashboard | ⬜ Pending |

### Final
| Stage | Feature | Status |
|-------|---------|--------|
| F.1 | Shepherd.js Onboarding Tours | ⬜ Pending |
| F.2 | README.md Final Documentation | ⬜ Pending |
| F.3 | GitHub Cleanup & Release Tag v1.0.0-graduation | ⬜ Pending |

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 24+](https://nodejs.org)
- [SQL Server 2022](https://www.microsoft.com/sql-server/sql-server-downloads)
- [Angular CLI 21](https://angular.io/cli) — `npm install -g @angular/cli@latest`
- [Git](https://git-scm.com)
- SQL Server Management Studio (SSMS) — for database inspection

### Clone the Repository

```bash
git clone https://github.com/[your-username]/shieldon-lms.git
cd shieldon-lms
```

### Backend Setup

```bash
cd backend
# 1. Copy the template and fill in your real values
cp docs/SECRETS_TEMPLATE.md .  # Read setup guide
# 2. Create appsettings.Development.json with your connection string, JWT key, SMTP creds
# 3. Apply database migrations
dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
# 4. Run the API (Watch Mode)
cd SHIELDON.API
dotnet watch run
# API available at: http://localhost:5000
# Swagger UI at: http://localhost:5000/swagger
```

### Frontend Setup

```bash
cd frontend
npm install
# Update src/environments/environment.ts if API port differs
ng serve --port 4201
# App available at: http://localhost:4201
```

---

## API Endpoints Reference

> Will be updated after each confirmed stage.

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | No | Authenticate user & return tokens |
| POST | `/api/auth/refresh` | No | Rotate Refresh & Access tokens |
| POST | `/api/auth/logout` | Yes | Revoke active refresh token |
| POST | `/api/auth/verify-email` | No | Validate email verification token |
| POST | `/api/auth/resend-verification` | No | Resend verification email |
| POST | `/api/auth/forgot-password` | No | Initiate password reset flow |
| POST | `/api/auth/reset-password` | No | Commit password reset |
| GET | `/api/profile` | Yes | Get authenticated user profile |
| PATCH | `/api/profile` | Yes | Update text profile info |
| POST | `/api/profile/picture` | Yes | Upload/resize WebP profile picture |
| GET | `/uploads/profile-pictures/*` | No | Securely fetch profile picture |

---

## Git Workflow

```
main       → Production-ready only
develop    → Integration branch
feature/*  → Individual features
fix/*      → Bug fixes
```

**Commit format:** `feat(auth): implement secure login with JWT and role-based redirect`

---

## Version History

| Version | Stage | Date | What Was Added |
|---------|-------|------|----------------|
| 0.1.0 | Stage 0.1 | 2026-04-09 | Repository structure, .gitignore, README |
| 0.5.0 | Stage 0.5 | 2026-04-09 | .NET Solution, DB, Angular Scaffold, Design System |
| 0.8.0 | Stage 0.8 | 2026-04-10 | Landing Page, Global Loading, Device Guard |
| 1.1.0 | Stage 1.1 | 2026-04-10 | Auth Domain Entities & Expansion |
| 1.2.0 | Stage 1.2 | 2026-04-10 | Secure Login & JWT Implementation |
| 1.3.0 | Stage 1.3 | 2026-04-10 | Email Verification & Mailtrap Setup |
| 1.4.0 | Stage 1.4 | 2026-04-10 | Password Reset (Forgot/Reset Flow) |
| 1.5.0 | Stage 1.5 | 2026-04-10 | Profile Management & Dashboard Shell |
| 2.4.0 | Stage 2.4 | 2026-04-19 | Course Materials, Announcements, Tabbed Hub UI |
| 2.4b-plan | Stage 2.4b | 2026-04-19 | Student Assignments — planned, pending approval |
| 2.5.0 | Stage 2.5 | 2026-04-21 | In-App Notifications, Email Templates, Advanced Enrollment (Paging, Filtering) |
| 3.0-plan | Phase 3 | 2026-04-26 | Phase 3 plan revised: 9 stages — Exam Mgmt, Notifications, Re-Attempt, Question Bank, Randomization, Token, Results, Grade Panel, Integration |

---

## Team Members

> *[Add team member names and roles here]*

---

*SHIELDON — "Integrity You Can Trust"*
