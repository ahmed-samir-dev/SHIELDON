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
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-10-512BD4?logo=dotnet&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-Auth-000000?logo=jsonwebtokens&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript&logoColor=white)

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21 (Standalone Components), TypeScript, SCSS |
| Backend | .NET 10 ASP.NET Core Web API, C# |
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
| 0.2 | Backend .NET Solution Scaffold | ⬜ Pending |
| 0.3 | Database Initialization | ⬜ Pending |
| 0.4 | Angular Frontend Scaffold | ⬜ Pending |
| 0.5 | Design System Verification | ⬜ Pending |
| 0.6 | Landing Page | ⬜ Pending |
| 0.7 | Global Loading System | ⬜ Pending |
| 0.8 | Device Guard + Placeholder Assets | ⬜ Pending |

### Phase 1 — Authentication & User Management
| Stage | Feature | Status |
|-------|---------|--------|
| 1.1 | Auth Domain Entities + Database | ⬜ Pending |
| 1.2 | F1: Secure Login & Role-Based Redirect | ⬜ Pending |
| 1.3 | F2: Email Verification | ⬜ Pending |
| 1.4 | F3: Password Reset Via Email | ⬜ Pending |
| 1.5 | F4: Profile Management | ⬜ Pending |

### Phase 2 — Core Learning Management System
| Stage | Feature | Status |
|-------|---------|--------|
| 2.1 | LMS Domain Entities + Database | ⬜ Pending |
| 2.2 | F5: Course Management & Enrollment | ⬜ Pending |
| 2.3 | F6: File Sharing (Course Materials) | ⬜ Pending |
| 2.4 | F7: Announcements | ⬜ Pending |
| 2.5 | F8: Notifications System | ⬜ Pending |

### Phase 3 — Examination Management System
| Stage | Feature | Status |
|-------|---------|--------|
| 3.1 | Exam Domain Entities + Database | ⬜ Pending |
| 3.2 | F9: Exam Management & Re-Attempt Requests | ⬜ Pending |
| 3.3 | F10: Question Bank | ⬜ Pending |
| 3.4 | F11/F12: Question Randomization + Timed Exam Engine | ⬜ Pending |
| 3.5 | F13: Secure Exam Token | ⬜ Pending |
| 3.6 | F14: Exam Results | ⬜ Pending |

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

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
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
# 4. Run the API
cd SHIELDON.API
dotnet run
# API available at: https://localhost:7001
# Swagger UI at: https://localhost:7001/swagger
```

### Frontend Setup

```bash
cd frontend
npm install
# Update src/environments/environment.ts if API port differs
ng serve
# App available at: http://localhost:4200
```

---

## API Endpoints Reference

> Will be updated after each confirmed stage.

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| — | — | — | *Coming in Stage 1.2* |

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

---

## Team Members

> *[Add team member names and roles here]*

---

*SHIELDON — "Integrity You Can Trust"*
