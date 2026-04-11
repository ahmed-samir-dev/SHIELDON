# CLAUDE.md — SHIELDON Project Master Reference

> **For AI Agents & Developers:** This is the single source of truth for the SHIELDON project.
> Read this file **completely and in full** before writing any code, generating any file, or making any architecture decision.
> Every section is mandatory context. Do NOT skip any section.
> Do NOT assume knowledge not documented here. If something is unclear, follow what is written here exactly.
> **Version 3.0.0** — Includes all v2 content PLUS: Landing Page, Global Loading System, Image Handling,
> Device Detection, Load Balancing, Edge Cases, Unit & Integration Testing, Secrets Security, Gap Analysis.

---

## TABLE OF CONTENTS

1.  [Project Overview](#1-project-overview)
2.  [Project Identity & Branding](#2-project-identity--branding)
3.  [Technology Stack](#3-technology-stack)
4.  [Architecture Decision & Explanation](#4-architecture-decision--explanation)
5.  [Golden Rules — Non-Negotiable](#5-golden-rules--non-negotiable)
6.  [Mandatory Development Pattern — 12 Steps Per Feature](#6-mandatory-development-pattern--12-steps-per-feature)
7.  [Stage Confirmation Protocol](#7-stage-confirmation-protocol)
8.  [Testing & Verification Rules](#8-testing--verification-rules)
9.  [Error Handling & Learning Mode](#9-error-handling--learning-mode)
10. [Clean Code Standards](#10-clean-code-standards)
11. [Design System & UI Standards](#11-design-system--ui-standards)
12. [Third-Party Libraries & Tools](#12-third-party-libraries--tools)
13. [Development Workflow & Git Strategy](#13-development-workflow--git-strategy)
14. [Stage-by-Stage Implementation Plan](#14-stage-by-stage-implementation-plan)
15. [Master Feature Checklist](#15-master-feature-checklist)
16. [Phase 1 — Authentication & User Management](#16-phase-1--authentication--user-management)
17. [Phase 2 — Core LMS](#17-phase-2--core-learning-management-system)
18. [Phase 3 — Examination Management System](#18-phase-3--examination-management-system)
19. [Phase 4 — Anti-Cheating Engine](#19-phase-4--anti-cheating-engine-fully-restructured)
20. [Phase 5 — Monitoring & Dashboards](#20-phase-5--monitoring--dashboards)
21. [Complete Database Schema Reference](#21-complete-database-schema-reference)
22. [API Conventions & Standards](#22-api-conventions--standards)
23. [README.md Maintenance Rules](#23-readmemd-maintenance-rules)
24. [Sensitive Data & API Key Security](#24-sensitive-data--api-key-security) ← NEW v3
25. [Global Loading Experience](#25-global-loading-experience) ← NEW v3
26. [Landing Page Specification](#26-landing-page-specification) ← NEW v3
27. [Image Handling Strategy](#27-image-handling-strategy) ← NEW v3
28. [Device Detection & Exam Screen Guard](#28-device-detection--exam-screen-guard) ← NEW v3
29. [Load Balancing & Performance Planning](#29-load-balancing--performance-planning) ← NEW v3
30. [Edge Cases & Workflow Scenarios](#30-edge-cases--workflow-scenarios) ← NEW v3
31. [Unit Testing & Integration Testing Guide](#31-unit-testing--integration-testing-guide) ← NEW v3
32. [Implementation Gap Analysis & Completeness Check](#32-implementation-gap-analysis--completeness-check) ← NEW v3

---


## 1. PROJECT OVERVIEW

### What Is SHIELDON?

**SHIELDON** is a full-stack, integrated **Learning Management System (LMS)** with a built-in **Exam Integrity System** powered by a proprietary browser-based **Anti-Cheating Engine**.

SHIELDON is built as a graduation project and is designed to be a fully functional, architecturally clean, and academically defensible system. Every decision made in this project — architectural, technical, and design — must be explainable, justified, and documented.

### The Core Problem SHIELDON Solves

Most traditional LMS platforms (Moodle, Blackboard Learn) depend on **third-party external software** such as Safe Exam Browser (SEB) or LockDown Browser to enforce exam security. These tools require:
- Students to download and install external applications
- IT administration to configure external software
- Dependency on a vendor outside the LMS ecosystem
- Inconsistent experience across devices and operating systems

**SHIELDON eliminates this dependency entirely.**

The Anti-Cheating Engine is built **directly into the browser-based LMS platform**. No external software. No installation. No friction for students. The system enforces full-screen mode, monitors tab switching, detects clipboard usage, blocks keyboard shortcuts, and tracks all suspicious behavior — all natively within the browser using standard Web APIs.

All violations are recorded and displayed in a **Session Timeline** and **Violation Timeline**, allowing tutors to review student behavior in detail during and after the exam.

### System Roles

| Role | Description |
|------|-------------|
| **Admin** | Full system access. Manages courses, users, all exams and violations system-wide. |
| **Tutor** | Manages their assigned courses. Creates exams, uploads materials, monitors violations. |
| **Student** | Enrolled in courses. Takes exams under anti-cheat monitoring. Views own results. |

### Project Phases & Features

| Phase | Name | Features | Count |
|-------|------|----------|-------|
| Phase 1 | Authentication & User Management | F1 (Login), F2 (Email Verify), F3 (Password Reset), F4 (Profile) | 4 |
| Phase 2 | Core Learning Management System | F5 (Courses/Enrollment), F6 (Files), F7 (Announcements), F8 (Notifications) | 4 |
| Phase 3 | Examination Management System | F9 (Exam Mgmt), F10 (Question Bank), F11 (Randomization), F12 (Timer), F13 (Secure Token), F14 (Results) | 6 |
| Phase 4 | Anti-Cheating Engine | F15 (Full Anti-Cheat Engine) | 1 |
| Phase 5 | Monitoring & Dashboards | F16 (Presence Tracking), F17 (Session Timeline), F18 (Violation Timeline), F19 (Manual Review), F20 (Tutor Dashboard), F21 (Admin Dashboard) | 6 |
| **Total** | | | **21 Features** |

---

## 2. PROJECT IDENTITY & BRANDING

### Project Name

**SHIELDON**

- Written in **UPPERCASE** always: **SHIELDON**
- Never abbreviated, never lowercase
- Used consistently across: navigation bar, page titles, UI elements, code namespaces, documentation, README, git repository name, and all references

### Official Slogan

**"Integrity You Can Trust"**

- Displayed in the horizontal navigation bar (auth/public pages), vertical sidebar (dashboard pages), login page hero section, and footer
- Rendered in color `#5E6E7A` (Typography & Slogan Slate Grey)
- Reflects the system's core mission: secure, trustworthy, integrity-driven exam management

### Logo

The SHIELDON logo (file: `logo.png`) consists of:
- A **shield shape** forming the outer boundary, representing protection and security
- An **open book** integrated inside the shield, representing learning and education
- A **gradient** flowing from Primary Blue (`#215DAE`) on the left to Primary Teal (`#1898A1`) on the right
- The text **"SHIELDON"** in bold below, with the same gradient applied
- The slogan **"Integrity You Can Trust"** below the name in Slate Grey (`#5E6E7A`)

**Logo Placement Rules:**
- Horizontal navbar (public/auth pages): logo in top-left corner (icon + "SHIELDON" text)
- Vertical sidebar (dashboard pages): logo + "SHIELDON" + slogan at the top of the sidebar
- Login/landing page: full logo centered or left-aligned in the hero section
- Favicon: use the shield+book icon only (no text)
- Never stretch, distort, recolor, or modify the logo

---

## 3. TECHNOLOGY STACK

### Frontend

| Technology | Choice | Reason |
|-----------|--------|--------|
| Framework | **Angular 21** | Mature, typed, component-based SPA framework |
| Component Style | **Standalone Components (NO NgModule)** | Modern Angular default since v17+ |
| Language | **TypeScript** (strict mode ON) | Type safety, better IDE support, fewer runtime errors |
| Styling | **SCSS** + CSS Custom Properties | Powerful, maintainable, themeable styles |
| HTTP | **Angular HttpClient** | Built-in, reactive, interceptor-compatible |
| Routing | **Angular Router** with Route Guards | Declarative routing with role-based guards |
| State | **Angular Signals** (prefer over RxJS for new code) | Modern, fine-grained reactivity |
| Charts | **Chart.js** + **ng2-charts** wrapper | Free, powerful, well-supported |
| Icons | **Lucide Icons** (lucide.dev) — MIT license | 100% free, tree-shakeable, consistent style. Used EXCLUSIVELY across entire project. No mixing. |
| Modals/Dialogs | **SweetAlert2** | Beautiful, accessible, customizable |
| Toast Notifications | **ngx-toastr** | Angular-native, integrates with Angular 21 |
| Celebrations | **canvas-confetti** | Lightweight confetti/fireworks animations |
| Onboarding Tours | **Shepherd.js** | Modern tour library, best Angular compatibility |
| Password Effect | **Custom Angular Directive** | Animated SVG eye that opens/closes |

### Backend

| Technology | Choice | Reason |
|-----------|--------|--------|
| Framework | **.NET 9 ASP.NET Core Web API** | Latest LTS, high performance, built for APIs |
| Language | **C#** (nullable reference types ON) | Type-safe, modern, excellent tooling |
| Architecture | **Clean Architecture + Vertical Slice** | See Section 4 for full explanation |
| Authentication | **JWT Bearer Tokens** (Access + Refresh Token) | Stateless, scalable, industry standard |
| Authorization | **ASP.NET Core Identity** + **Custom Role Policies** | Fine-grained, role-based access control |
| ORM | **Entity Framework Core 9** (Code-First) | Productive, migrations-based, LINQ support |
| Email | **MailKit / MimeKit** | Robust SMTP email sending |
| Validation | **FluentValidation** | Clean, testable, expressive validation rules |
| Mapping | **AutoMapper** | Clean DTO ↔ Entity mapping |
| Logging | **Serilog** | Structured logging with sinks |
| API Docs | **Swagger / OpenAPI** with JWT support | Auto-generated, testable API documentation |

### Database

| Technology | Choice |
|-----------|--------|
| Engine | **Microsoft SQL Server** (latest stable) |
| ORM | **Entity Framework Core** Code-First with Migrations |
| Naming | PascalCase for all tables and columns |
| Approach | **Code-First ONLY** — never design database manually |

### DevOps & Tooling

| Tool | Purpose |
|------|---------|
| Git + GitHub | Version control (private repository) |
| Visual Studio 2022 | Backend development (C# / .NET) |
| VS Code | Frontend development (Angular / TypeScript) |
| npm | Frontend package management |
| NuGet | Backend package management |
| Postman / Swagger | API testing |
| SQL Server Management Studio (SSMS) | Database inspection and verification |

---

## 4. ARCHITECTURE DECISION & EXPLANATION

> **This section must be understood before any code is written.**
> The architecture is the backbone of the project. Every folder, every class, every file follows this architecture.

### Chosen Architecture: Clean Architecture + Vertical Slice Hybrid

SHIELDON uses a **hybrid approach** combining:
1. **Clean Architecture** for backend layer separation and dependency direction
2. **Vertical Slice Architecture** for feature organization within those layers

### Why This Architecture Fits SHIELDON

**Clean Architecture** ensures:
- The **Domain** (core business rules) is completely independent — zero dependencies on frameworks, databases, or UI
- The **Application** layer contains use cases and orchestrates domain logic
- The **Infrastructure** layer handles databases, email, file storage — all implementation details
- The **API** layer is just the entry point — thin controllers, no business logic

**Vertical Slice** ensures:
- Each feature (Login, Course Management, Anti-Cheat, etc.) owns all its code end-to-end
- Adding a new feature does not require touching multiple unrelated files
- Each slice is independently understandable and testable
- Features cannot accidentally break each other

**Together they give us:**
- The structural integrity of Clean Architecture (dependencies point inward)
- The feature cohesion of Vertical Slice (each feature is self-contained)
- A codebase that is scalable, maintainable, and academically defensible

### Backend Layer Structure

```
Solution: SHIELDON.sln
│
├── SHIELDON.Domain           ← Layer 1: The heart of the system
│   ├── Entities/             ← C# classes representing database tables (User, Course, Exam, etc.)
│   ├── Enums/                ← All enumerations (UserRole, AccountStatus, ViolationType, etc.)
│   ├── Constants/            ← System-wide constants (no magic numbers)
│   └── Exceptions/           ← Domain-specific exceptions
│
├── SHIELDON.Application      ← Layer 2: Business logic and use cases
│   ├── Features/             ← One folder per feature (vertical slice)
│   │   ├── Auth/
│   │   │   ├── Login/        ← LoginCommand, LoginHandler, LoginRequest, LoginResponse
│   │   │   ├── Register/
│   │   │   └── EmailVerify/
│   │   ├── Courses/
│   │   ├── Exams/
│   │   └── AntiCheat/
│   ├── Interfaces/           ← Contracts for Infrastructure (IEmailService, IFileService, etc.)
│   ├── Common/               ← Shared DTOs, base classes, result types
│   └── Mappings/             ← AutoMapper profiles
│
├── SHIELDON.Infrastructure   ← Layer 3: Implementation of external concerns
│   ├── Persistence/
│   │   ├── AppDbContext.cs   ← EF Core DbContext
│   │   ├── Configurations/   ← Entity type configurations (IEntityTypeConfiguration)
│   │   └── Migrations/       ← EF Core generated migrations
│   ├── Services/             ← EmailService, FileService, JwtService implementations
│   └── DependencyInjection.cs ← Registers all Infrastructure services
│
└── SHIELDON.API              ← Layer 4: Entry point (thin controllers only)
    ├── Controllers/          ← HTTP endpoints — NO business logic here
    ├── Middleware/           ← Exception handling, request logging
    ├── Extensions/           ← Service registration helpers
    └── Program.cs            ← Application startup configuration
```

**Dependency Rule (NON-NEGOTIABLE):**
```
API → Application → Domain     (allowed)
API → Infrastructure           (allowed, ONLY for DI registration in Program.cs)
Infrastructure → Application   (allowed, implements interfaces)
Infrastructure → Domain        (allowed)
Domain → nothing               (Domain has ZERO dependencies on other layers)
Application → Infrastructure   (FORBIDDEN — use interfaces/dependency inversion)
```

### Frontend Angular Structure

```
frontend/
└── src/
    └── app/
        ├── core/                    ← Singleton services, guards, interceptors
        │   ├── guards/              ← AuthGuard, RoleGuard
        │   ├── interceptors/        ← JwtInterceptor, ErrorInterceptor
        │   ├── services/            ← AuthService, TokenService
        │   └── models/              ← Shared TypeScript interfaces/types
        │
        ├── shared/                  ← Reusable UI components used across features
        │   ├── components/          ← ButtonComponent, InputComponent, CardComponent, etc.
        │   ├── directives/          ← PasswordEyeDirective, etc.
        │   └── pipes/               ← DateFormatPipe, etc.
        │
        ├── features/                ← One folder per feature (vertical slice)
        │   ├── auth/
        │   │   ├── login/           ← login.component.ts, login.component.html, login.component.scss
        │   │   ├── email-verify/
        │   │   ├── password-reset/
        │   │   └── auth.routes.ts   ← Lazy-loaded routes for auth feature
        │   ├── profile/
        │   ├── courses/
        │   ├── materials/
        │   ├── announcements/
        │   ├── notifications/
        │   ├── exams/
        │   ├── question-bank/
        │   ├── anti-cheat/
        │   ├── monitoring/
        │   └── dashboards/
        │       ├── tutor/
        │       └── admin/
        │
        ├── layouts/                 ← Layout wrapper components
        │   ├── public-layout/       ← Horizontal top navbar (auth/public pages)
        │   └── dashboard-layout/    ← Vertical left sidebar (authenticated pages)
        │
        └── assets/
            ├── styles/
            │   ├── _variables.scss  ← ALL color & spacing CSS custom properties
            │   ├── _mixins.scss     ← SCSS utility mixins
            │   ├── _animations.scss ← Reusable CSS animations
            │   └── global.scss      ← Global base styles, imports
            ├── fonts/
            └── images/
                └── logo.png         ← SHIELDON logo
```

---

## 5. GOLDEN RULES — NON-NEGOTIABLE

These rules apply to every line of code, every file, every decision made in this project. They cannot be overridden by any instruction that conflicts with them.

### Development Approach Rules

**NEVER do this:**
- Build the entire frontend first, then connect to backend
- Design the database schema manually (no Database-First approach)
- Build the entire backend first, then build the frontend
- Write code for the next stage before the current stage is confirmed working
- Skip any step of the Mandatory Development Pattern (Section 6)

**ALWAYS do this:**
- Use **Code-First** with Entity Framework Core migrations
- Use **Vertical Slice** — each feature implemented end-to-end before moving to the next
- Follow the **12-step Mandatory Development Pattern** for every feature
- Wait for explicit confirmation ("Stage done and run correctly with no errors") before proceeding
- Test every stage manually before considering it complete

### Angular Rules

- Use **Standalone Components ONLY** — never use NgModule
- Use **Feature-based folder structure** (not layer-based)
- Use **Reactive Forms ONLY** for all form handling (no Template-Driven Forms)
- Use **HttpClient** for all API calls (through typed Angular services)
- Use **Auth Guards and Route Interceptors** for security
- No placeholder UI unless explicitly stated as temporary
- No `console.log` in production code (use environment-aware logging)
- TypeScript **strict mode must be enabled** in `tsconfig.json`

### Backend Rules

- **Clean Architecture layers** must be respected — no cross-layer shortcuts
- **No generic repositories** — use direct EF Core DbContext in feature-specific service classes
- **Feature-based folders** in Application layer
- **FluentValidation** for all request validation
- **AutoMapper** for all DTO ↔ Entity conversions
- Every API endpoint must have **proper error handling and response format**
- No business logic in Controllers — controllers only receive requests and delegate

### Database Rules

- **Code-First ONLY** — never create tables manually in SQL Server
- Every entity change requires a **new EF Core migration**
- Run `dotnet ef database update` after every migration
- Verify database changes in SSMS after every migration
- Use **UNIQUEIDENTIFIER (GUID)** as primary key for all tables
- PascalCase for all table and column names

---

## 6. MANDATORY DEVELOPMENT PATTERN — 12 STEPS PER FEATURE

Every single feature must follow these 12 steps **in order**. Do not skip steps. Do not reorder steps.

```
Step 1  → Domain Model (Entity)
          Define the C# entity class in SHIELDON.Domain/Entities/
          Add all properties, relationships, and XML doc comments

Step 2  → EF Core Configuration
          Create IEntityTypeConfiguration<TEntity> in SHIELDON.Infrastructure/Persistence/Configurations/
          Configure table name, primary key, column types, indexes, relationships

Step 3  → Migration
          Run: dotnet ef migrations add {FeatureName}Entities --project SHIELDON.Infrastructure --startup-project SHIELDON.API
          Review the generated migration file before applying

Step 4  → Database Update
          Run: dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
          Verify the tables/columns appear correctly in SSMS

Step 5  → Application Layer (DTOs, Interfaces, Validators)
          Create Request DTO (what the API receives)
          Create Response DTO (what the API returns)
          Create FluentValidation validator for the request DTO
          Define any required interfaces in SHIELDON.Application/Interfaces/

Step 6  → Infrastructure Implementation
          Implement any interfaces defined in Step 5
          Write the feature service/handler in SHIELDON.Infrastructure or Application

Step 7  → API Controller + Endpoints
          Create or update the Controller in SHIELDON.API/Controllers/
          Add the endpoint(s) with proper [Authorize], [HttpGet/Post/Put/Patch/Delete]
          Apply correct response type attributes
          Apply FluentValidation

Step 8  → Swagger / Postman Testing (API Only)
          Test every endpoint in Swagger or Postman
          Verify: correct status codes, correct response body, correct error handling
          Verify database updated correctly after mutations

Step 9  → Angular Service
          Create the Angular service in features/{feature-name}/services/
          Add typed methods that call the API endpoints using HttpClient
          Handle errors and map responses to TypeScript models

Step 10 → Angular Component (UI)
          Create the Angular standalone component
          Implement Reactive Form if needed
          Connect to the Angular service
          Apply SHIELDON design system (colors, typography, spacing from _variables.scss)
          Add Lucide icons, SweetAlert2 dialogs, ngx-toastr notifications as appropriate

Step 11 → Manual Testing & Full Verification
          Test API via Swagger/Postman
          Test database state via SSMS
          Test UI behavior in browser
          Complete the Manual Testing Checklist (see Section 8)

Step 12 → Commit & Documentation
          Git commit with descriptive message: feat(scope): description
          Update README.md with what was added
          Move to next feature ONLY after explicit confirmation
```

---

## 7. STAGE CONFIRMATION PROTOCOL

This protocol is **mandatory** and must be followed after every stage.

### After Completing Each Stage, The AI Must:

1. **Stop completely** — do not start the next stage automatically
2. **Display the Manual Testing Checklist** with specific items to test
3. **Explain exactly what to test**, how to test it, and what result to expect
4. **Ask explicitly** for confirmation using this exact phrase:

> "Please test the above checklist and confirm with: **'Stage done and run correctly with no errors'**"

### The Developer Must:

1. Test every item on the checklist
2. Verify the result in browser (frontend), Swagger/Postman (API), and SSMS (database)
3. Only proceed when everything works correctly
4. Respond with: **"Stage done and run correctly with no errors"**
5. If there are errors: describe the error so we can fix it before moving on

### What Happens On Error:

If a stage has errors, follow the Error Handling Protocol in Section 9.
**Do NOT move to the next stage until the current stage is confirmed working.**

### Stage Response Format

Each stage response must include:

```
# Stage X.Y — [Stage Name]

## What We Are Building
[Clear explanation of what this stage implements and why it comes at this point]

## Files to Create / Modify
[Complete list of every file being created or modified]

## [All Code Files — Complete, No Skipping]

## Manual Testing Checklist
[ ] 1. [Specific test item with exact expected result]
[ ] 2. [Specific test item with exact expected result]
[ ] 3. [Specific test item with exact expected result]
...

## How to Test
[Step-by-step instructions: open Swagger / Postman / browser / SSMS]

## Expected Results
[Exactly what should appear for each test]

---
Please test the above checklist and confirm with: **'Stage done and run correctly with no errors'**
```

---

## 8. TESTING & VERIFICATION RULES

At the end of every stage, testing must cover all three layers:

### Layer 1 — API Testing (Swagger or Postman)

For every new endpoint created, verify:
- [ ] Endpoint is visible in Swagger UI at `https://localhost:{port}/swagger`
- [ ] Unauthenticated request returns `401 Unauthorized` (for protected endpoints)
- [ ] Valid request returns correct status code (200, 201, 204)
- [ ] Invalid request returns `400 Bad Request` with error details
- [ ] Wrong role returns `403 Forbidden`
- [ ] Not-found resource returns `404 Not Found`
- [ ] Response body matches the documented response format (success/data/message/errors envelope)
- [ ] JWT token works correctly when included in Authorization header

### Layer 2 — Database Testing (SSMS)

For every entity/migration added, verify in SSMS:
- [ ] New table exists with correct name (PascalCase)
- [ ] All columns exist with correct data types
- [ ] Primary key is UNIQUEIDENTIFIER
- [ ] Foreign key relationships are correctly defined
- [ ] Indexes are applied where specified
- [ ] After a POST request: new record appears in the table
- [ ] After a PATCH/PUT request: record is correctly updated
- [ ] After a DELETE request: record is removed or soft-deleted

### Layer 3 — Frontend Testing (Browser)

For every UI component created, verify in browser (Chrome, 1280px+ width):
- [ ] Component loads without console errors (open DevTools → Console)
- [ ] Form validation works (required fields, format checks)
- [ ] Form submits correctly and calls the API
- [ ] Success feedback appears (ngx-toastr toast or SweetAlert2)
- [ ] Error feedback appears (toast or inline error message)
- [ ] Navigation/routing works correctly after actions
- [ ] SHIELDON design system is applied (correct colors, fonts, spacing)
- [ ] Lucide icons render correctly
- [ ] Page is responsive at 1280px, 1440px, and 1920px widths
- [ ] No placeholder text (e.g., "Lorem ipsum") remains

### Standard Manual Testing Checklist Template

```markdown
## Manual Testing Checklist — Stage X.Y

### API Tests (Swagger / Postman)
[ ] POST /api/{endpoint} with valid data → returns 201 / 200 with expected response
[ ] POST /api/{endpoint} with missing required fields → returns 400 with error details
[ ] POST /api/{endpoint} without JWT token → returns 401 Unauthorized
[ ] POST /api/{endpoint} with wrong role → returns 403 Forbidden

### Database Tests (SSMS)
[ ] Table {TableName} exists and has all correct columns
[ ] Record created/updated/deleted correctly after API call
[ ] Foreign keys and indexes are correct

### Frontend Tests (Browser)
[ ] Page loads at correct route with no console errors
[ ] Form validation shows error messages for invalid input
[ ] Successful action shows toast notification
[ ] Failed action shows error message
[ ] Data displays correctly from API response
[ ] UI matches SHIELDON design system
```

---

## 9. ERROR HANDLING & LEARNING MODE

### When An Error Occurs During Implementation

If any error appears (build error, runtime error, validation error, database error), the following protocol must be followed:

```
Step 1 — STOP
  Do not continue. Do not skip the error. Do not move forward.

Step 2 — EXPLAIN THE ERROR
  Show the exact error message.
  Explain in plain English what the error means.
  Explain WHY this error happened (root cause).

Step 3 — SHOW HOW TO DEBUG IT
  Show where to look (browser DevTools, Visual Studio error list, terminal output, SSMS logs).
  Show how to read the error stack trace.

Step 4 — PROVIDE THE FIX
  Show the exact corrected code.
  Highlight what changed and why.

Step 5 — TEACH HOW TO AVOID IT
  Explain what practice or rule prevents this error in the future.
  Add it to the mental model for future stages.

Step 6 — VERIFY THE FIX
  After applying the fix, re-test the affected functionality.
  Confirm the error is resolved before continuing.
```

### Developer Level Context

**Important:** The developer using this project is a **junior-to-fresh developer** who is new to Angular, ASP.NET Core, and Microsoft SQL Server. This means:

- **Explain everything**, even if it seems obvious
- Never say "you already know how to do X" — always show it fully
- When introducing a new concept, explain it before using it
- When a terminal command is needed, show the exact command, where to run it, and what output to expect
- When a configuration file needs updating, show the entire relevant section (not just the changed line)
- Never say "add the standard configuration" — always show it explicitly
- Use clear section headers in each stage so the developer knows exactly which file to open and what to add

---

## 10. CLEAN CODE STANDARDS

These standards apply to every file in both frontend and backend.

### Naming Rules

| Context | Convention | Example |
|---------|-----------|---------|
| C# Classes | PascalCase | `UserAuthenticationService` |
| C# Methods | PascalCase | `GenerateEmailVerificationToken()` |
| C# Properties | PascalCase | `PasswordHash`, `CreatedAt` |
| C# Variables | camelCase | `hashedPassword`, `userEntity` |
| C# Constants | UPPER_SNAKE_CASE | `MAX_LOGIN_ATTEMPTS` |
| C# Enums | PascalCase (values too) | `AccountStatus.Active` |
| C# Interfaces | Prefix with `I` | `IEmailService`, `ITokenService` |
| TypeScript Classes | PascalCase | `ExamSessionService` |
| TypeScript Methods | camelCase | `startExamSession()`, `reportViolation()` |
| TypeScript Variables | camelCase | `sessionToken`, `violationCount` |
| TypeScript Constants | UPPER_SNAKE_CASE or camelCase | `MAX_VIOLATIONS = 3` |
| Angular Components | kebab-case file, PascalCase class | `exam-taking.component.ts` → `ExamTakingComponent` |
| Angular Services | camelCase file, PascalCase class | `anti-cheat-monitor.service.ts` |
| SCSS Variables | `--kebab-case` (CSS Custom Property) | `--color-primary-blue` |
| Database Tables | PascalCase | `ExamSessions`, `ViolationLogs` |
| Database Columns | PascalCase | `StartedAt`, `ViolationCount` |
| Git Branches | `kebab-case` | `feature/secure-login`, `feature/anti-cheat-engine` |
| Git Commits | Conventional Commits | `feat(auth): implement secure login with JWT` |

### Comment Rules

- **C# XML Doc Comments:** Every public class, every public method, every public property must have `/// <summary>` documentation
- **TypeScript JSDoc:** Every service method, every component method must have `/** */` documentation
- Comments explain **WHY** the code exists, not **WHAT** it does (the code itself shows what)
- Complex logic must have inline explanation comments
- No commented-out dead code — delete it or use version control

### Other Code Quality Rules

1. **No Magic Numbers or Strings:** Always define constants. Example: `MAX_FAILED_LOGIN_ATTEMPTS = 5` not just `5`
2. **Single Responsibility:** Each class/component/service does exactly one thing
3. **No Console Logs in Production:** Use Angular environment checks or Serilog on backend
4. **SOLID Principles:** Apply all five, especially Dependency Inversion (use interfaces)
5. **Error Handling is Mandatory:** Every API call handles errors. Every service method has try-catch. Every validation is explicit
6. **No Empty Catch Blocks:** Never write `catch { }` — always log and handle
7. **DRY (Don't Repeat Yourself):** Extract repeated logic into shared utilities, not copied code

---

## 11. DESIGN SYSTEM & UI STANDARDS

### Color Palette

All colors are defined as CSS Custom Properties in `src/assets/styles/_variables.scss`.
**Never use hardcoded hex values anywhere in the project.** Always reference the CSS variable.

```scss
// ============================================================
// SHIELDON Design System — Color Palette
// File: src/assets/styles/_variables.scss
// Source: Official SHIELDON Brand Identity
// ============================================================

:root {

  // ── Core Brand Colors ─────────────────────────────────────────
  --color-primary-blue:          #215DAE;   // Brand Blue Core — primary CTAs, links, key UI elements
  --color-primary-teal:          #1898A1;   // Brand Teal Core — accents, secondary elements
  --color-text-slogan:           #5E6E7A;   // Typography & Slogan Slate Grey — body text, slogan

  // ── Blue Extended Palette ─────────────────────────────────────
  --color-deep-ocean-blue:       #0B315B;   // Darkest blue — sidebar background, deep contrast areas
  --color-cobalt-blue:           #1E4C8B;   // Dark blue — hover states on primary blue elements
  --color-sky-blue:              #4C97D9;   // Medium blue — secondary buttons, info states
  --color-light-cloud-blue:      #A0CFF4;   // Light blue — highlights, selected states, subtle fills
  --color-cool-water-blue:       #3EA9DC;   // Accent blue — badges, tags, active indicators

  // ── Teal Extended Palette ─────────────────────────────────────
  --color-deep-forest-teal:      #086F70;   // Darkest teal — deep teal UI elements
  --color-muted-ocean-teal:      #127F85;   // Dark teal — hover states on primary teal elements
  --color-light-cyan-teal:       #40C0C6;   // Medium teal — secondary accents, illustrations
  --color-mint-teal:             #ACEAEF;   // Light teal — subtle backgrounds, chips
  --color-aqua-teal:             #00B5B0;   // Vivid teal — highlights, progress bars

  // ── Neutral / UI Colors ───────────────────────────────────────
  --color-medium-grey-blue:      #87949C;   // Medium grey — secondary text, placeholder text
  --color-light-grey-blue:       #B2B9BC;   // Light grey — borders, dividers
  --color-cool-grey-tint:        #DADEE0;   // Subtle grey — input borders, card borders
  --color-mid-neutral-grey:      #818C92;   // Mid grey — disabled text
  --color-light-background:      #EDF0F1;   // Light Interface Hue — page background, alternating rows

  // ── Semantic / Status Colors ──────────────────────────────────
  --color-bright-cyan-accent:    #22D3EE;   // Bright cyan — special highlights, live indicators
  --color-error:                 #EF4444;   // Error / Violation — error states, violation badges
  --color-critical-violation:    #DC2626;   // Critical — force-submit state, critical violations
  --color-warning:               #F59E0B;   // Warning — warning states, medium violations
  --color-soft-warning:          #FBBF24;   // Soft warning — minor violation indicators
  --color-success:               #22C55E;   // Success — pass states, approved status
  --color-strong-success:        #16A34A;   // Strong success — confirmed pass, high scores

  // ── White & Black ─────────────────────────────────────────────
  --color-white:                 #FFFFFF;
  --color-black:                 #000000;

  // ── Typography ────────────────────────────────────────────────
  --font-family-base:            'Inter', 'Segoe UI', system-ui, -apple-system, sans-serif;
  --font-family-heading:         'Inter', 'Segoe UI', system-ui, -apple-system, sans-serif;
  --font-family-mono:            'JetBrains Mono', 'Courier New', monospace;

  --font-size-xs:                11px;
  --font-size-sm:                13px;
  --font-size-base:              15px;
  --font-size-md:                16px;
  --font-size-lg:                18px;
  --font-size-xl:                22px;
  --font-size-2xl:               28px;
  --font-size-3xl:               36px;

  --font-weight-regular:         400;
  --font-weight-medium:          500;
  --font-weight-semibold:        600;
  --font-weight-bold:            700;

  --line-height-tight:           1.2;
  --line-height-base:            1.5;
  --line-height-relaxed:         1.75;

  // ── Spacing Scale ─────────────────────────────────────────────
  --spacing-1:     4px;
  --spacing-2:     8px;
  --spacing-3:     12px;
  --spacing-4:     16px;
  --spacing-5:     20px;
  --spacing-6:     24px;
  --spacing-8:     32px;
  --spacing-10:    40px;
  --spacing-12:    48px;
  --spacing-16:    64px;
  --spacing-20:    80px;

  // ── Border Radius ─────────────────────────────────────────────
  --radius-sm:     6px;
  --radius-md:     10px;
  --radius-lg:     16px;
  --radius-xl:     24px;
  --radius-full:   9999px;

  // ── Shadows ───────────────────────────────────────────────────
  --shadow-xs:     0 1px 2px rgba(0,0,0,0.05);
  --shadow-sm:     0 1px 4px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.04);
  --shadow-md:     0 4px 12px rgba(0,0,0,0.10), 0 2px 4px rgba(0,0,0,0.06);
  --shadow-lg:     0 10px 30px rgba(0,0,0,0.12), 0 4px 8px rgba(0,0,0,0.06);
  --shadow-xl:     0 20px 50px rgba(0,0,0,0.15), 0 8px 16px rgba(0,0,0,0.08);

  // ── Transitions ───────────────────────────────────────────────
  --transition-fast:   150ms ease;
  --transition-base:   250ms ease;
  --transition-slow:   400ms ease;
  --transition-spring: cubic-bezier(0.175, 0.885, 0.32, 1.275);

  // ── Z-Index Scale ─────────────────────────────────────────────
  --z-dropdown:        100;
  --z-sticky:          200;
  --z-fixed:           300;
  --z-modal-backdrop:  400;
  --z-modal:           500;
  --z-toast:           600;
  --z-tooltip:         700;
  --z-exam-overlay:    900;  // Anti-cheat warnings must appear above everything

  // ── Layout Dimensions ─────────────────────────────────────────
  --sidebar-width:           260px;
  --sidebar-collapsed-width: 72px;
  --navbar-height:           64px;
  --content-max-width:       1400px;
}
```

### Typography

- **Font:** Inter (Google Fonts — free, load from CDN in `index.html`)
- **Weights to load:** 400, 500, 600, 700
- **Body text:** `--font-size-base` (15px), color `--color-text-slogan`
- **Headings:** `--font-weight-bold` (700) or `--font-weight-semibold` (600)
- **Secondary text:** `--font-size-sm` (13px), color `--color-medium-grey-blue`
- **Labels:** 11–12px, uppercase, letter-spacing 0.5–1px

### Icons

- **Library:** Lucide Icons (https://lucide.dev) — MIT license, 100% free
- Used **exclusively** across the entire project — no other icon library
- Import only the specific icons used (tree-shakeable)
- Default size: 20px | Small: 16px | Large: 24px
- Stroke width: `1.5` for all icons (consistent look)
- Never use emojis as icons anywhere in the UI

### Navigation Structure

- **Public / Auth pages** (Login, Register, Forgot Password, etc.): **Horizontal top navigation bar**
  - Contains: SHIELDON logo (left), navigation links (center/right), auth buttons
- **Authenticated pages** (all dashboards and feature pages): **Vertical left sidebar navigation**
  - Contains: SHIELDON logo + slogan (top), navigation links (middle), user profile (bottom)
  - Sidebar is collapsible (icon-only mode at `--sidebar-collapsed-width`)

### Responsive Scope

- **In scope:** Desktop, laptop, and large screens (`min-width: 1024px`)
- **Out of scope:** Mobile phones and tablets (do not implement mobile layouts)
- Test at: 1024px, 1280px, 1440px, 1920px

### UI Design Principles

| Principle | Description |
|-----------|-------------|
| **Modern** | Use current design trends: clean lines, ample whitespace, subtle gradients |
| **Animated** | Smooth CSS transitions on hover, card entrances, page transitions — must NOT affect performance |
| **Comfortable** | Soft backgrounds (`--color-light-background`), not harsh white-on-white; easy on the eyes for long sessions |
| **Wow-factor** | Subtle glassmorphism for modals, gradient accents on key CTAs, skeleton loading states |
| **Card-based** | Most content lives in cards with `--shadow-md` and `--radius-lg` |
| **Consistent spacing** | Always use the spacing scale variables — never arbitrary pixel values |

### Component Design Rules

- **Buttons:** Primary = gradient from `--color-primary-blue` to `--color-primary-teal`; hover = darken 10%; active = darken 15%; border-radius `--radius-md`
- **Inputs:** Border `--color-cool-grey-tint`; focus border `--color-primary-blue`; border-radius `--radius-md`; padding `12px 16px`
- **Cards:** Background white; border `1px solid --color-cool-grey-tint`; shadow `--shadow-md`; border-radius `--radius-lg`; padding `--spacing-6`
- **Badges/Tags:** Small, pill-shaped (`--radius-full`), color-coded by status
- **Loading states:** Use skeleton screens (animated grey shimmer) — no bare spinners alone
- **Empty states:** Show a friendly illustration or icon with a descriptive message and a CTA button

### SweetAlert2 Configuration

SweetAlert2 dialogs must use the SHIELDON color scheme:
```typescript
// Shared SweetAlert2 config in shared/utils/swal.config.ts
import Swal from 'sweetalert2';

export const ShieldonSwal = Swal.mixin({
  confirmButtonColor: '#215DAE',    // --color-primary-blue
  cancelButtonColor: '#87949C',     // --color-medium-grey-blue
  iconColor: '#1898A1',             // --color-primary-teal
  borderRadius: '10px',
  customClass: { popup: 'shieldon-swal-popup' }
});
```

### Password Eye Effect

All password input fields across the entire application must use the `PasswordEyeDirective`:
- Shows an SVG eye icon inside the input field (right side)
- When password is **hidden**: eye is closed (eyelids shut) — animated eyelid closing
- When password is **visible**: eye is open — animated eyelid opening
- Smooth `transition: 250ms ease` on the animation
- The eye icon uses `--color-medium-grey-blue` color, darkens to `--color-primary-blue` on hover

---

## 12. THIRD-PARTY LIBRARIES & TOOLS

| Library | Purpose | When to Use |
|---------|---------|-------------|
| **SweetAlert2** | Confirmation dialogs, important pop-ups | All destructive confirmations (delete, force-submit), important alerts |
| **ngx-toastr** | Toast notifications | Success/error/info quick feedback after actions |
| **Chart.js + ng2-charts** | Data visualization | All dashboard charts — violation stats, exam scores, activity trends |
| **canvas-confetti** | Celebration animation | Fire confetti when student passes exam (result reveal), and any other celebration event |
| **Shepherd.js** | First-visit onboarding tour | Guide new users through their dashboard on first login — one tour per role |
| **Lucide Icons** | Icon library | Every icon in the entire UI — exclusively |
| **Inter (Google Fonts)** | Typography | Load in `index.html` from Google Fonts CDN |
| **SweetAlert2** | Modals / dialogs | All modal dialog needs |

### Library Installation Commands

```bash
# Frontend (run from /frontend directory)
npm install sweetalert2
npm install ngx-toastr
npm install chart.js ng2-charts
npm install canvas-confetti
npm install shepherd.js
npm install lucide-angular

# Backend (run from /backend/SHIELDON.API directory)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation.AspNetCore
dotnet add package MailKit
dotnet add package Serilog.AspNetCore
dotnet add package Swashbuckle.AspNetCore
```

---

## 13. DEVELOPMENT WORKFLOW & GIT STRATEGY

### Repository

- **Repository name:** `shieldon-lms` (private on GitHub)
- **Root structure:**
  ```
  shieldon-lms/
  ├── frontend/           ← Angular project
  ├── backend/            ← .NET solution
  ├── docs/               ← Additional documentation, diagrams
  ├── CLAUDE.md           ← This file (project master reference)
  └── README.md           ← Public project documentation
  ```

### Branch Strategy

```
main          ← Production-ready, stable code only
develop       ← Integration branch — all features merge here first
feature/XXX   ← Individual feature branches
fix/XXX       ← Bug fix branches
```

**Branch naming examples:**
- `feature/secure-login`
- `feature/email-verification`
- `feature/course-management`
- `feature/anti-cheat-fullscreen`

### Commit Message Format (Conventional Commits)

```
type(scope): short description

Types:
  feat     ← New feature
  fix      ← Bug fix
  refactor ← Code restructure (no behavior change)
  style    ← CSS/formatting changes
  docs     ← Documentation only
  chore    ← Build config, dependency updates
  test     ← Adding or updating tests

Examples:
  feat(auth): implement secure login with JWT and role-based redirect
  feat(auth): add email verification with 24-hour token expiry
  feat(courses): implement enrollment request with 24-hour rejection cooldown
  feat(anti-cheat): implement fullscreen enforcement with violation logging
  fix(exam): correct timer calculation on late exam start
  docs(readme): update with phase 2 feature documentation
```

### Push Rules

1. **Never push directly to `main`**
2. Push each completed feature to `feature/XXX` branch
3. Merge `feature/XXX` → `develop` after stage confirmation
4. Merge `develop` → `main` after each full Phase is complete and tested

### Per-Stage Git Workflow

```bash
# 1. Create feature branch
git checkout develop
git pull origin develop
git checkout -b feature/stage-name

# 2. Work on the stage — implement all code

# 3. After stage is confirmed working
git add .
git commit -m "feat(scope): descriptive message"
git push origin feature/stage-name

# 4. Merge to develop
git checkout develop
git merge feature/stage-name
git push origin develop
```

---

## 14. STAGE-BY-STAGE IMPLEMENTATION PLAN

> **CRITICAL RULE:** Each stage is one complete, self-contained unit of work.
> Do NOT move to the next stage until the current one is confirmed working.
> The developer confirms with: **"Stage done and run correctly with no errors"**
> Each stage response must STOP after displaying the Manual Testing Checklist.

---

### STAGE 0 — PROJECT SETUP & ENVIRONMENT

**Goal:** Create the complete project skeleton before any feature code. This stage sets up the entire foundation that all future stages build upon.

---

#### Stage 0.1 — Repository & Folder Structure

What we are building: The GitHub repository, root folder structure, `.gitignore`, and initial `README.md`.

Actions:
- Create private GitHub repository named `shieldon-lms`
- Initialize with `main` branch
- Create `develop` branch from `main`
- Create root-level folders: `frontend/`, `backend/`, `docs/`
- Add `.gitignore` files for Angular, .NET, Visual Studio, VS Code, and macOS
- Create initial `README.md` with project name, slogan, tech stack, and placeholder sections
- Place `CLAUDE.md` at root

---

#### Stage 0.2 — Backend Solution Scaffold (.NET 10, Clean Architecture)

What we are building: The .NET solution with all four Clean Architecture project layers, NuGet packages, and initial configuration.

Projects to create:
```
SHIELDON.sln
├── SHIELDON.Domain           (Class Library, net10.0)
├── SHIELDON.Application      (Class Library, net10.0) → references Domain
├── SHIELDON.Infrastructure   (Class Library, net10.0) → references Application, Domain
└── SHIELDON.API              (ASP.NET Core Web API, net10.0) → references Application, Infrastructure
```

NuGet packages to install per project:
```
SHIELDON.Infrastructure:
  Microsoft.EntityFrameworkCore.SqlServer
  Microsoft.EntityFrameworkCore.Tools
  Microsoft.EntityFrameworkCore.Design
  AutoMapper.Extensions.Microsoft.DependencyInjection
  FluentValidation.AspNetCore
  MailKit
  Serilog.AspNetCore

SHIELDON.API:
  Microsoft.AspNetCore.Authentication.JwtBearer
  Swashbuckle.AspNetCore (Swagger)
```

Key files to create:
- `Program.cs` with CORS, JWT auth, Swagger, Serilog configured
- `appsettings.json` with connection string, JWT config, SMTP config placeholders
- `appsettings.Development.json` for dev overrides
- `SHIELDON.Infrastructure/DependencyInjection.cs` (registers services)
- Swagger configured with JWT bearer auth support

---

#### Stage 0.3 — Database Initialization (SQL Server + EF Core)

What we are building: The SQL Server database and EF Core `AppDbContext` with initial setup.

Actions:
- Create SQL Server database: `ShieldonDB`
- Create `AppDbContext` in `SHIELDON.Infrastructure/Persistence/`
- Register DbContext in `Program.cs` with connection string from `appsettings.json`
- Install EF Core CLI tools globally: `dotnet tool install --global dotnet-ef`
- Run initial migration: `dotnet ef migrations add InitialCreate`
- Apply migration: `dotnet ef database update`
- Verify database appears in SSMS

---

#### Stage 0.4 — Angular Frontend Scaffold

What we are building: The Angular 21 standalone project with all packages, folder structure, and configuration.

Commands:
```bash
ng new shieldon-frontend --standalone --style=scss --routing --strict
cd shieldon-frontend
npm install sweetalert2 ngx-toastr chart.js ng2-charts canvas-confetti shepherd.js lucide-angular
```

Actions:
- Create complete folder structure as defined in Section 4
- Create `src/assets/styles/_variables.scss` with full color palette from Section 11
- Create `src/assets/styles/_mixins.scss` with utility mixins
- Create `src/assets/styles/_animations.scss` with reusable animations
- Create `src/assets/styles/global.scss` importing Inter font + all partials
- Update `angular.json` to include global styles
- Configure `src/environments/environment.ts` and `environment.prod.ts` with API base URL
- Configure Angular Router in `app.routes.ts` with lazy-loaded feature modules
- Create `PublicLayoutComponent` (horizontal navbar with SHIELDON branding)
- Create `DashboardLayoutComponent` (vertical sidebar with SHIELDON branding)
- Configure `ngx-toastr` in app config
- Create shared `ButtonComponent`, `InputComponent`, `CardComponent`, `SpinnerComponent`
- Place `logo.png` in `src/assets/images/`

---

#### Stage 0.5 — Design System Verification

What we are building: Verifying that the design system renders correctly before any feature code.

Actions:
- Create a temporary `DesignSystemPreviewComponent` that renders all:
  - Color swatches (all 28 colors from the palette)
  - Typography scales (all font sizes and weights)
  - Button variants (primary, secondary, danger)
  - Input states (default, focused, error)
  - Card component
  - All Lucide icons used in the project
- Verify everything looks correct at 1280px, 1440px, 1920px
- Delete the preview component after verification (or mark it for dev-only)

**Stage 0 Commit Message:**
```
chore(setup): initialize SHIELDON project — Angular 21 standalone, .NET 10 Clean Architecture, SQL Server, design system
```

---

### PHASE 1 STAGES — Authentication & User Management

---

#### Stage 1.1 — Domain Entities & Database: Auth Foundation

Following the 12-step pattern, Steps 1–4.

**Backend only — no frontend work in this stage.**

Entities to define in `SHIELDON.Domain/Entities/`:
- `User` — all fields detailed in Section 16
- `RefreshToken` — token, userId, expiresAt, isRevoked
- `LoginActivityLog` — userId, timestamp, ipAddress, success

EF Core configurations for all three entities.

Migration: `AddAuthEntities`

Seed data: One Admin user with hashed password for immediate testing.

---

#### Stage 1.2 — Feature 1: Secure Login & Role-Based Access

Following the 12-step pattern, Steps 5–12.

Backend: `POST /api/auth/login`, `POST /api/auth/refresh-token`, `POST /api/auth/logout`

Frontend: `/auth/login` route with:
- Email + Password form (Reactive Form)
- Password field with animated eye directive
- Remember Me checkbox
- Role-based redirect on success (Admin/Tutor/Student → their respective dashboard)
- Account locked warning via SweetAlert2
- Invalid credentials error with inline message
- Success redirect with ngx-toastr confirmation

---

#### Stage 1.3 — Feature 2: Email Verification

Backend: `POST /api/auth/send-verification-email`, `POST /api/auth/verify-email`, `POST /api/auth/resend-verification`

Frontend:
- `/auth/verify-email` — pending verification page with resend button
- `/auth/email-confirmed` — success page with animation and link to login

---

#### Stage 1.4 — Feature 3: Password Reset Via Email

Backend: `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`

Frontend:
- `/auth/forgot-password` — email input form
- `/auth/reset-password?token=xxx` — new password + confirm password (both with eye directive)
- SweetAlert2 success modal on completion

---

#### Stage 1.5 — Feature 4: Profile Management

Backend: `GET /api/users/profile`, `PATCH /api/users/profile`, `PATCH /api/users/profile/email`, `PATCH /api/users/profile/password`, `POST /api/users/profile/picture`

Frontend:
- `/profile` with tabs: Account Info, Change Password
- Profile picture upload with preview
- All password fields with eye directive
- ngx-toastr for save confirmations

---

### PHASE 2 STAGES — Core Learning Management System

---

#### Stage 2.1 — Domain Entities: LMS Foundation

Entities: `Course`, `CourseEnrollment`, `CourseMaterial`, `Announcement`, `Notification`

Migration: `AddLMSEntities`

---

#### Stage 2.2 — Feature 5: Course Management & Enrollment

Backend: Full CRUD for courses + enrollment request workflow + bulk actions + cooldown logic

Frontend: Admin course management UI + Student enrollment request + Status tracking

---

#### Stage 2.3 — Feature 6: File Sharing (Course Materials)

Backend: File upload (PDF, DOC, DOCX, PPT, PPTX, JPG, PNG, max 20MB) + external links + secure file serving

Frontend: Drag-and-drop upload + material cards + download/link buttons

---

#### Stage 2.4 — Feature 7: Announcements

Backend: CRUD for announcements + priority handling (Important bypasses notification aggregation)

Frontend: Announcement list with Important announcements pinned + priority visual distinction

---

#### Stage 2.5 — Feature 8: Notifications System

Backend: Notification generation service + aggregation logic + exam reminder scheduler + mark-as-read

Frontend: Bell icon with unread badge + dropdown panel + full notifications page

---

### PHASE 3 STAGES — Examination Management System

---

#### Stage 3.1 — Domain Entities: Exam Foundation

Entities: `Exam`, `Question`, `QuestionOption`, `ExamSession`, `StudentAnswer`, `ExamResult`, `ReAttemptRequest`

Migration: `AddExamEntities`

---

#### Stage 3.2 — Feature 9: Exam Management & Re-Attempt Requests

Backend: Exam CRUD + status computation + re-attempt request workflow + attempt limits

Frontend: Exam cards with status badges + re-attempt request form for students + review panel for Tutor/Admin

---

#### Stage 3.3 — Feature 10: Question Bank

Backend: Question CRUD (MCQ + True/False) + category tagging + correct answers never exposed to students

Frontend: Question bank management + dynamic form for MCQ vs True/False + filter/search

---

#### Stage 3.4 — Feature 11 & 12: Question Randomization + Timed Exam Engine

Backend: Fisher-Yates randomization at session start + server-side timer + auto-submit background service + late start handling

Frontend: Exam taking UI + countdown timer display + auto-save after each answer + in-exam time warnings

---

#### Stage 3.5 — Feature 13: Secure Exam Token

Backend: Cryptographic GUID token per session + one-session-per-student enforcement + token validation middleware

Frontend: Exam start flow with rules → token received → exam wrapper component + token in-memory storage

---

#### Stage 3.6 — Feature 14: Exam Results

Backend: Auto-grading for MCQ and True/False + result visibility control (Immediate/Scheduled/ManualRelease) + result release endpoint

Frontend: Student result page with score ring animation + confetti on pass + Tutor results table with release control

---

### PHASE 4 STAGES — Anti-Cheating Engine

> Full detailed specification for each stage is in Section 19.

- **Stage 4.1** — Pre-Exam Rules Acknowledgment UI
- **Stage 4.2** — Fullscreen Enforcement (Fullscreen API)
- **Stage 4.3** — Tab & Focus Detection (visibilitychange + blur events)
- **Stage 4.4** — Keyboard Shortcut Blocking (keydown capture phase)
- **Stage 4.5** — Window Resize / Minimize / Split Detection
- **Stage 4.6** — Mouse Monitoring (abnormal pattern detection)
- **Stage 4.7** — Violation Intelligence Layer (severity classification + cooldown)
- **Stage 4.8** — Warning System + Violation Accumulation + Force-Submit
- **Stage 4.9** — Backend Violation Persistence & API Endpoints
- **Stage 4.10** — Monitoring Continuity on Reconnect

---

### PHASE 5 STAGES — Monitoring & Dashboards

- **Stage 5.1** — Feature 16: Exam Presence Tracking
- **Stage 5.2** — Feature 17: Session Timeline View
- **Stage 5.3** — Feature 18: Violation Timeline
- **Stage 5.4** — Feature 19: Manual Review
- **Stage 5.5** — Feature 20: Tutor Monitoring Dashboard
- **Stage 5.6** — Feature 21: Admin Dashboard

---

### FINAL STAGES

- **Stage F.1** — Shepherd.js Onboarding Tours (per role)
- **Stage F.2** — README.md Final Documentation
- **Stage F.3** — Final GitHub Cleanup & Release Tag `v1.0.0-graduation`

---

## 15. MASTER FEATURE CHECKLIST

> Update this checklist as each stage is confirmed complete.
> A stage is complete ONLY when confirmed with "Stage done and run correctly with no errors."

### Project Setup
- [x] **Stage 0.1** — Repository & folder structure
- [x] **Stage 0.2** — Backend solution scaffold (.NET 9, Clean Architecture)
- [x] **Stage 0.3** — Database initialization (SQL Server + EF Core + AppDbContext)
- [x] **Stage 0.4** — Angular frontend scaffold (Angular 21 standalone + all packages)
- [x] **Stage 0.5** — Design system verification

### Phase 1 — Authentication & User Management
- [x] **Stage 1.1** — Auth domain entities + database migration
- [x] **Stage 1.2** — F1: Secure Login & Role-Based Redirect
- [x] **Stage 1.3** — F2: Email Verification
- [x] **Stage 1.4** — F3: Password Reset Via Email
- [x] **Stage 1.5** — F4: Profile Management
- [x] **Stage 1.6** — Fx: Public Registration (Student/Tutor)

### Phase 2 — Core LMS
- [ ] **Stage 2.1** — LMS domain entities + database migration
- [ ] **Stage 2.2** — F5: Course Management & Enrollment
- [ ] **Stage 2.3** — F6: File Sharing (Course Materials)
- [ ] **Stage 2.4** — F7: Announcements
- [ ] **Stage 2.5** — F8: Notifications System

### Phase 3 — Examination Management
- [ ] **Stage 3.1** — Exam domain entities + database migration
- [ ] **Stage 3.2** — F9: Exam Management & Re-Attempt Requests
- [ ] **Stage 3.3** — F10: Question Bank
- [ ] **Stage 3.4** — F11/F12: Question Randomization + Timed Exam Engine
- [ ] **Stage 3.5** — F13: Secure Exam Token
- [ ] **Stage 3.6** — F14: Exam Results

### Phase 4 — Anti-Cheating Engine
- [ ] **Stage 4.1** — Pre-Exam Rules Acknowledgment
- [ ] **Stage 4.2** — Fullscreen Enforcement
- [ ] **Stage 4.3** — Tab & Focus Detection
- [ ] **Stage 4.4** — Keyboard Shortcut Blocking
- [ ] **Stage 4.5** — Window Resize / Minimize / Split Detection
- [ ] **Stage 4.6** — Mouse Monitoring
- [ ] **Stage 4.7** — Violation Intelligence Layer (Severity + Cooldown)
- [ ] **Stage 4.8** — Warning System + Violation Accumulation + Force-Submit
- [ ] **Stage 4.9** — Backend Violation Persistence & API Endpoints
- [ ] **Stage 4.10** — Monitoring Continuity on Reconnect

### Phase 5 — Monitoring & Dashboards
- [ ] **Stage 5.1** — F16: Exam Presence Tracking
- [ ] **Stage 5.2** — F17: Session Timeline View
- [ ] **Stage 5.3** — F18: Violation Timeline
- [ ] **Stage 5.4** — F19: Manual Review
- [ ] **Stage 5.5** — F20: Tutor Monitoring Dashboard
- [ ] **Stage 5.6** — F21: Admin Dashboard

### Final
- [ ] **Stage F.1** — Shepherd.js Onboarding Tours
- [ ] **Stage F.2** — README.md Final Documentation
- [ ] **Stage F.3** — Final GitHub Cleanup & Release Tag

---

## 16. PHASE 1 — AUTHENTICATION & USER MANAGEMENT

### Feature 1 — Secure Login & Roles

**Purpose:** Allow users to securely access the system and grant permissions based on their assigned role (Admin, Tutor, or Student).

**User Entity Fields (SHIELDON.Domain/Entities/User.cs):**
```
Id                    GUID (primary key)
FirstName             string (required, max 100)
LastName              string (required, max 100)
Email                 string (required, unique, max 255)
PasswordHash          string (BCrypt hash, never plain text)
Role                  enum: Admin | Tutor | Student
AccountStatus         enum: Active | Unverified | Locked | Disabled
StudentId             string? (null if not student; unique auto-generated)
TutorId               string? (null if not tutor; unique auto-generated)
AdminId               string? (null if not admin; unique auto-generated)
ProfilePicturePath    string? (relative path to stored image)
FailedLoginAttempts   int (default 0)
LockedAt              DateTime? (when account was locked)
LastLoginAt           DateTime? (last successful login)
EmailVerifiedAt       DateTime? (when email was verified)
CreatedAt             DateTime (auto-set on creation, UTC)
```

**Login Business Logic (Step by Step):**
1. Validate email and password are not empty (FluentValidation)
2. Find user by email → if not found: return generic error ("Invalid credentials" — do NOT reveal email existence)
3. Check AccountStatus:
   - Disabled → "Your account has been disabled. Contact support."
   - Locked → "Your account is locked. Reset your password to unlock it."
   - Unverified → "Please verify your email address before logging in."
4. Verify password using BCrypt hash comparison
5. If incorrect password:
   - Increment `FailedLoginAttempts`
   - If `FailedLoginAttempts >= 5`: set `AccountStatus = Locked`, set `LockedAt = now`, send lockout email
   - Return: "Invalid credentials"
6. If correct password:
   - Reset `FailedLoginAttempts` to 0
   - Set `LastLoginAt = now`
   - Invalidate any existing active RefreshToken for this user
   - Generate new JWT Access Token (15 min expiry) with claims: userId, email, role
   - Generate new Refresh Token (7 days expiry), store in `RefreshTokens` table
   - Create `LoginActivityLog` record
   - Return: `{ accessToken, refreshToken, role, expiresIn, userId, fullName }`

**Role-Based Redirect:**
- Admin → `/admin/dashboard`
- Tutor → `/tutor/dashboard`
- Student → `/student/dashboard`

**JWT Configuration:**
```json
"JwtSettings": {
  "SecretKey": "your-secret-key-minimum-32-characters-long",
  "Issuer": "SHIELDON",
  "Audience": "SHIELDON-Users",
  "AccessTokenExpiryMinutes": 15,
  "RefreshTokenExpiryDays": 7
}
```

**API Endpoints:**
- `POST /api/auth/login` — public endpoint
- `POST /api/auth/refresh-token` — public endpoint (body: `{ refreshToken }`)
- `POST /api/auth/logout` — protected endpoint (invalidates refresh token)

---

### Feature 2 — Email Verification

**Purpose:** Ensure each user account is associated with a valid email before granting full access.

**Token Generation:**
- Cryptographically secure random string (64 hex characters)
- Stored as hash in `EmailVerificationTokens` table (or as a field on User)
- 24-hour expiry from generation time
- Single-use: invalidated immediately after successful verification

**Verification Flow:**
1. Account created → `AccountStatus = Unverified` → system sends verification email
2. Email contains: `{frontendUrl}/auth/verify-email?token={token}`
3. User clicks → frontend sends token to `POST /api/auth/verify-email`
4. Backend validates token (exists, not expired, not already used)
5. If valid: `AccountStatus = Active`, `EmailVerifiedAt = now`, token invalidated
6. If expired: inform user, offer resend option
7. Frontend redirects to `/auth/email-confirmed` with success animation

**Email Template:** SHIELDON branded HTML email with:
- SHIELDON logo at top
- Friendly greeting
- Clear CTA button ("Verify My Email") in `#215DAE` blue
- Token expiry notice ("This link expires in 24 hours")
- Note that if they didn't create this account, they can ignore the email

**API Endpoints:**
- `POST /api/auth/verify-email` body: `{ token: string }`
- `POST /api/auth/resend-verification` body: `{ email: string }`

---

### Feature 3 — Password Reset Via Email

**Purpose:** Allow users with forgotten passwords to securely reset via email.

**Reset Token:** Cryptographically secure, 1-hour expiry, single-use, invalidated after use.

**Business Rules:**
- Always respond "If an account exists with this email, you will receive a reset email" (prevent email enumeration)
- Reset link format: `{frontendUrl}/auth/reset-password?token={token}`
- Password must meet policy: minimum 8 chars, 1 uppercase, 1 number, 1 special character
- After successful reset: invalidate ALL active sessions (refresh tokens) for the user
- If account was Locked: password reset also unlocks the account (`AccountStatus = Active`)
- Record password reset timestamp in security log

**API Endpoints:**
- `POST /api/auth/forgot-password` body: `{ email: string }`
- `POST /api/auth/reset-password` body: `{ token: string, newPassword: string, confirmPassword: string }`

---

### Feature 4 — Profile Management

**Purpose:** Allow authenticated users to view and manage their personal account information and security settings.

**Sub-Feature 4.1 — Account Information View & Edit:**
- View: Full Name, Email, Role (read-only), AccountStatus (read-only), CreatedAt (read-only), role-specific ID (read-only)
- Edit: FirstName, LastName, ProfilePicture
- Email change: marks account as Unverified → sends re-verification email → restricts features until verified
- Validates: email uniqueness, name not empty

**Sub-Feature 4.2 — Change Password:**
- Requires: Current Password (verified first), New Password, Confirm New Password
- Validates: current password correct, new passwords match, policy compliance
- Prevents: reusing the same password
- After change: invalidate other active sessions, send security notification email

**Sub-Feature 4.3 — Profile Picture Upload:**
- Accepted formats: JPG, PNG, WebP (validated server-side by MIME type)
- Maximum size: 2MB
- Storage path: `/backend/SHIELDON.API/Storage/Uploads/profile-pictures/{userId}.webp` (optimized via ImageSharp)
- Old picture deleted when new one uploaded

**Sub-Feature 4.4 — Security Activity Log:**
- Log entries for: Email Change, Password Change, Profile Update, Picture Upload
- Each entry: event type, timestamp, IP address
- Stored in `UserActivityLogs` table

**API Endpoints:**
- `GET /api/users/profile` — get own profile
- `PATCH /api/users/profile` — update name (firstName, lastName)
- `PATCH /api/users/profile/email` — change email (triggers re-verification)
- `PATCH /api/users/profile/password` — change password
- `POST /api/users/profile/picture` — upload/replace profile picture

---

## 17. PHASE 2 — CORE LEARNING MANAGEMENT SYSTEM

### Feature 5 — Course Management & Enrollment

**Purpose:** Allow Admins to create and manage courses, assign tutors, and enable students to request enrollment.

**Course Entity:** `Id`, `Title`, `CourseCode` (unique), `Description`, `AssignedTutorId`, `IsActive`, `CreatedAt`

**CourseEnrollment Entity:** `Id`, `StudentId`, `CourseId`, `Status` (Pending/Approved/Rejected), `RejectionCount`, `CooldownUntil` (nullable), `RequestedAt`, `ReviewedAt`, `ReviewedById`

**Admin Operations:** Create course (CourseCode must be unique), Assign tutor from list, Edit (Title/Description/Tutor), Delete (with confirmation)

**Student Enrollment Rules:**
1. Student submits enrollment request → Status = Pending
2. Cannot submit if already enrolled OR has a Pending request for same course
3. After 2 consecutive rejections → 24-hour cooldown
4. Cooldown prevents new requests; system shows "You can submit a new request in {X} hours"
5. After cooldown expires → student can try again
6. Maximum total rejections: system may define limit (3 total = blocked permanently from that course)

**Enrollment Management (Admin/Tutor):**
- View all Pending requests with: Student Name, Student ID, Email, Request Date
- Approve single / Reject single with reason
- Bulk approve selected / Bulk reject selected
- All status changes trigger notifications to student

**Access Rules:**
- Admins: see all courses and all enrollment requests
- Tutors: see only their assigned courses and enrollment requests for those courses
- Students: see all courses (to browse), see their own enrollment status

---

### Feature 6 — File Sharing (Course Materials)

**Purpose:** Allow tutors to upload and share course materials with enrolled students.

**CourseMaterial Entity:** `Id`, `CourseId`, `Title`, `Description`, `MaterialType` (File|Link), `FilePath`, `ExternalUrl`, `UploadedByUserId`, `CreatedAt`, `UpdatedAt`

**File Upload Rules:**
- Allowed: PDF, DOC, DOCX, PPT, PPTX, JPG, PNG
- Max size: 20MB per file
- Validate MIME type server-side (not just file extension)
- Storage: `/backend/SHIELDON.API/Storage/Uploads/course-materials/{courseId}/{uniqueFileName}`
- Files served through API controller endpoint (never expose raw server path to client)
- Secure download: only enrolled students can download

**External Links:** Google Drive, YouTube, any URL; system stores the URL; optionally verifies URL is reachable

**Access Control:**
- Only assigned Tutor (or Admin) can upload/edit/delete materials
- Tutors can only manage materials in their assigned courses
- Only enrolled students can view and download materials

---

### Feature 7 — Announcements

**Purpose:** Allow tutors and admins to create course announcements for enrolled students.

**Announcement Entity:** `Id`, `CourseId`, `Title`, `Content`, `Priority` (Normal|Important), `AttachmentType` (None|File|Link), `AttachmentPath`, `AttachmentUrl`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`

**Priority Rules:**
- Important announcements: pinned at top of list, visually highlighted (accent border + badge)
- Important announcements: bypass notification aggregation → immediate notification delivery
- Normal announcements: standard date-descending order

**Display:**
- Latest first (descending by `CreatedAt`), with Important ones always at top
- Each announcement shows: title, preview text, date, priority badge, attachment icon
- Click to expand full content

---

### Feature 8 — Notifications System

**Purpose:** Keep all users informed about relevant events in their courses and exams.

**Notification Entity:** `Id`, `RecipientUserId`, `Title`, `Message`, `Type`, `IsRead`, `ReadAt`, `RelatedCourseId`, `RelatedExamId`, `CreatedAt`

**Notification Types:**
```
AnnouncementCreated | AnnouncementUpdated
EnrollmentApproved | EnrollmentRejected
MaterialUploaded
ExamCreated | ExamUpdated
ExamReminder24h | ExamReminder1h
ResultReleased
ReAttemptApproved | ReAttemptRejected
```

**Aggregation Rules:**
- Normal events of the same type within 5-minute window → single grouped notification
- Example: 5 material uploads → "5 new materials added to [Course Name]"
- Exception: `Priority = Important` announcements → always immediate, never aggregated

**Exam Reminders:**
- Background service runs every minute
- Sends 24h reminder: if exam start is between 23h55m and 24h05m from now
- Sends 1h reminder: if exam start is between 55m and 65m from now
- Idempotent check: never send the same reminder type twice for the same exam+student

**API Endpoints:**
- `GET /api/notifications?page=1&pageSize=20` — paginated list for current user
- `PATCH /api/notifications/{id}/read` — mark one as read
- `PATCH /api/notifications/read-all` — mark all as read
- `GET /api/notifications/unread-count` — unread count (for badge)

**Frontend:**
- Bell icon in sidebar with red badge showing unread count
- Dropdown: latest 10 notifications with click-to-navigate behavior
- Full notifications page with pagination and filter (all/unread)
- Poll every 30 seconds for new notifications

---

## 18. PHASE 3 — EXAMINATION MANAGEMENT SYSTEM

### Feature 9 — Exam Management

**Purpose:** Allow tutors and admins to create, schedule, and manage exams within a course.

**Exam Entity:** `Id`, `CourseId`, `Title`, `Description`, `ExamType` (Midterm|Final|Quiz), `TotalMarks`, `DurationMinutes`, `StartDateTime`, `EndDateTime`, `MaxViolationsAllowed` (default 3), `ResultVisibility` (Immediate|Scheduled|ManualRelease), `ResultReleaseAt`, `CreatedByUserId`, `CreatedAt`

**Exam Status (Computed — not stored in DB):**
- `Upcoming` → `now < StartDateTime`
- `Available` → `StartDateTime <= now <= EndDateTime`
- `Expired` → `now > EndDateTime`

**Re-Attempt Request Entity:** `Id`, `StudentId`, `ExamId`, `Justification`, `Status` (Pending|Approved|Rejected), `RequestedAt`, `ReviewedAt`, `ReviewedById`

**Attempt Rules:**
- Default: 1 attempt per student per exam
- Re-attempt request must include a justification reason
- If approved: grants exactly 1 additional attempt (max total = 2)
- 2nd attempt generates a DIFFERENT random question set
- After 2 consecutive rejections: 24-hour cooldown on new requests

**Notifications triggered by exam events:**
- Exam created → notify all enrolled students
- Exam updated → notify all enrolled students
- Result released → notify all students who took the exam
- Re-attempt approved/rejected → notify the requesting student

---

### Feature 10 — Question Bank

**Purpose:** Allow tutors and admins to create and manage exam questions within a course.

**Question Entity:** `Id`, `CourseId`, `QuestionTitle`, `QuestionText`, `QuestionType` (MCQ|TrueFalse), `Marks`, `Category`, `CreatedByUserId`, `CreatedAt`

**QuestionOption Entity:** `Id`, `QuestionId`, `OptionText`, `IsCorrect`

**MCQ Rules:**
- Minimum 2 answer options (typically 4)
- Exactly 1 option marked as `IsCorrect = true`
- System validates exactly one correct answer on creation/update

**True/False Rules:**
- Always exactly 2 options: "True" and "False"
- One marked as correct

**Security Rules:**
- `IsCorrect` field is NEVER included in any student-facing API response
- Correct answers are only evaluated on the server during grading (never sent to client)
- Question categories are visible only to Tutor/Admin, never to students

**Operations:**
- Create, Edit, Delete questions
- Filter by: Question Type, Category
- Search by: keyword in question text
- Prevent deletion if question is already used in a submitted exam attempt

---

### Feature 11 — Question Randomization

**Purpose:** Generate a unique, fair, randomized question set for each student exam attempt.

**Randomization Process (server-side):**
1. Retrieve all questions for the exam's course matching the exam's category filter
2. Apply Fisher-Yates shuffle algorithm to question order
3. For each MCQ question: independently shuffle the answer options order
4. Determine the required number of questions:
   - If more available than needed: pick the required number after shuffling
   - If fewer available than needed: use all available questions (or block exam start if 0)
5. Store the generated set as JSON in `ExamSession.QuestionSetJson`: `[{ questionId, optionOrder: [optId1, optId2...] }, ...]`
6. On reconnect: return the SAME stored question set (no re-randomization for same attempt)

**Guarantees:**
- No duplicate questions within one attempt
- The same question may appear for different students (but in different order)
- Answer option order differs per student

---

### Feature 12 — Timed Exam Engine

**Purpose:** Control exam timing, enforce duration, and handle auto-submission.

**Timer Logic:**
```
Session start: StartedAt = server UTC now
ExpiresAt = MIN(
  StartedAt + DurationMinutes,
  Exam.EndDateTime
)
Remaining = ExpiresAt - server UTC now (in seconds)
```

**Late Start Handling:**
- If student starts exam after `StartDateTime`:
  - Remaining time = `MIN(DurationMinutes, EndDateTime - now)` in minutes
  - If remaining < full duration: show SweetAlert2 warning: "You have only {X} minutes remaining for this exam. The full duration is {Y} minutes. Do you want to proceed?"
  - Student can proceed or cancel

**Auto-Submit Background Service:**
- Runs every 60 seconds
- Finds all `Active` exam sessions where `ExpiresAt <= now`
- For each: grade saved answers, create ExamResult, set session status to `AutoExpired`
- This runs independently of the frontend

**In-Exam Time Warnings (Frontend):**
- At 50% of time elapsed: ngx-toastr info toast: "You have used half of your exam time."
- At 5 minutes remaining: ngx-toastr warning toast (persistent, not auto-dismissed): "Only 5 minutes remaining!"
- At 2 minutes remaining: ngx-toastr error toast (persistent): "2 minutes remaining — your exam will auto-submit soon!"

**Timer Display (Frontend):**
- Prominent countdown timer showing HH:MM:SS
- Color changes: Green (>50% time left) → Orange (5–10 min left) → Red (<5 min left)
- Timer animates last 5 minutes (subtle pulse effect)

**Session Continuity:**
- On page refresh: frontend requests `GET /api/sessions/{token}` → backend returns remaining time (server-calculated)
- Frontend timer resets to server-returned remaining seconds (never client-side timer)

---

### Feature 13 — Secure Exam Token

**Purpose:** Ensure each exam attempt is uniquely and securely tied to one session, preventing duplicates or unauthorized access.

**ExamSession Entity:** `Id`, `Token` (GUID — unique, indexed), `StudentId`, `ExamId`, `Status` (Active|Submitted|ForceSubmitted|Expired|AutoExpired), `QuestionSetJson`, `ViolationCount`, `IsFirstViolationWarned`, `StartedAt`, `ExpiresAt`, `SubmittedAt`, `SubmissionType` (Manual|AutoExpired|ForceSubmitted), `ForceSubmittedAt`, `LastHeartbeatAt`

**Token Generation:**
- Generated using `Guid.NewGuid()` — cryptographically random
- Stored in `ExamSessions` table with a unique index
- Returned to client ONCE on session start — client stores in Angular service (NOT localStorage)

**Session Validation (Applied to Every Exam API Call):**
1. Extract token from `X-Exam-Session-Token` request header
2. Find session in database by token
3. Validate: exists, `Status = Active`, `StudentId = currentUserId`, `ExpiresAt > now`
4. If any validation fails: return `403 Forbidden`

**One Session Per Student Per Exam:**
- Before creating a new session: check if student already has an `Active` session for this exam
- If yes: return the existing session (resume, don't create new)
- If no: create a new session

---

### Feature 14 — Exam Results

**Purpose:** Grade submitted exams and present results with controlled visibility.

**ExamResult Entity:** `Id`, `ExamSessionId`, `StudentId`, `ExamId`, `TotalMarks`, `ScoreObtained`, `Percentage`, `IsForceSubmitted`, `IsReleased`, `ReleasedAt`, `SubmittedAt`, `GradedAt`

**StudentAnswer Entity:** `Id`, `ExamSessionId`, `QuestionId`, `SelectedOptionId`, `SavedAt`

**Grading Logic:**
```
For each question in the exam session's question set:
  Find StudentAnswer where QuestionId = question.Id and ExamSessionId = session.Id
  If answer exists AND SelectedOptionId.IsCorrect = true:
    scoreObtained += question.Marks
  Else:
    scoreObtained += 0 (no negative marking)
percentage = (scoreObtained / totalMarks) * 100
```

**Result Visibility Control:**
- `Immediate`: result visible to student right after submission
- `Scheduled`: result visible after `ResultReleaseAt` datetime (auto-released by background service)
- `ManualRelease`: Tutor/Admin must manually click "Release Results" button

**Student Result Display:**
- Score obtained / Total marks
- Percentage (e.g., 85%)
- Animated circular progress ring (fills up to percentage)
- If percentage >= pass threshold (e.g., 50%): fire canvas-confetti celebration
- Submission type badge: "Manual", "Auto-Expired", "Force-Submitted (Suspicious)"

**Multiple Attempts:**
- Store result per ExamSession separately
- When displaying final result to student: show highest score between approved attempts
- Tutor/Admin can see all attempt results

---

## 19. PHASE 4 — ANTI-CHEATING ENGINE (FULLY RESTRUCTURED)

> This section is the most critical phase. It is fully restructured for maximum clarity.
> Each sub-feature is a self-contained, implementable unit.
> Implement stages 4.1 through 4.10 in order. Do not combine stages.

### Architecture Overview

The Anti-Cheating Engine has two layers working together:

**Layer 1 — Frontend: `AntiCheatMonitorService` (Angular)**
A singleton Angular service attached to the exam session. It listens to browser DOM events, detects violations, and communicates with the backend in real-time.

**Layer 2 — Backend: Violation Persistence & Enforcement (.NET API)**
Receives violation reports from the frontend, stores them in the database, applies business rules (cooldown, counter, force-submit enforcement), and returns enforcement decisions.

### Anti-Cheat Domain Entities

**ViolationLog:**
```
Id                  GUID (PK)
ExamSessionId       GUID (FK → ExamSessions)
ViolationType       string (enum value)
Severity            string ('Minor' | 'Medium' | 'Critical')
Timestamp           DateTime (UTC)
EventDetail         string? (optional extra context)
IsFirstViolation    bool (true = warning only, not counted toward limit)
```

**ExamSession additions (added in this phase):**
```
ViolationCount          int (default 0) — number of counted violations (excludes first warning)
IsFirstViolationWarned  bool (default false)
ForceSubmittedAt        DateTime? (when force-submit was triggered)
```

### Violation Severity Classification

| Violation Type | Severity | Examples |
|---------------|---------|---------|
| `AbnormalMouseActivity` | Minor | Unusual cursor movement patterns |
| `ClipboardCopy` | Medium | Ctrl+C during exam |
| `ClipboardPaste` | Medium | Ctrl+V during exam |
| `RestrictedShortcut` | Medium | Ctrl+A, Ctrl+S, F12, Ctrl+Shift+I |
| `WindowResize` | Medium | Browser window resized while in exam |
| `WindowMinimize` | Medium | Window minimized |
| `SplitScreen` | Medium | Window reduced to <75% screen width/height |
| `FullScreenExit` | Critical | Exited fullscreen mode |
| `TabSwitch` | Critical | Switched browser tabs |
| `FocusLoss` | Critical | Alt+Tab or clicking outside browser |
| `BrowserClose` | Critical | Browser/tab closed unexpectedly |

---

### Stage 4.1 — Pre-Exam Rules Acknowledgment

**What:** Before an exam starts, display a mandatory rules modal. Student must accept to proceed.

**Angular Component: `ExamRulesAcknowledgmentComponent`**

Trigger: Student clicks "Start Exam" button on the exam card.

Display this as a SweetAlert2 modal (or custom full-screen overlay) with:
```
Title: "Before You Begin — Please Read"

Exam Rules:
1. You must remain in full-screen mode for the entire exam duration.
2. Switching browser tabs or windows is not allowed.
3. Minimizing or resizing the browser window is not allowed.
4. Copy and paste (Ctrl+C / Ctrl+V) are disabled during the exam.
5. Keyboard shortcuts (Ctrl+A, Ctrl+S, F12, developer tools) are blocked.
6. Your session is monitored in real-time. All suspicious activities are recorded.
7. The first detected violation will result in a warning.
8. After {maxViolations} violations, your exam will be automatically submitted.
9. The exam timer will auto-submit when time expires.

[ Cancel — go back ] [ I Understand, Start Exam ]
```

**Behavior:**
- "Cancel" → navigate back to exam list page
- "I Understand, Start Exam" → call `POST /api/sessions/start` → receive session token → enter exam interface

**No backend changes in this stage.** The rules modal is purely a UI gate before session creation.

---

### Stage 4.2 — Fullscreen Enforcement

**What:** Force the exam into browser fullscreen mode. Detect and log exit events.

**Browser APIs Used:**
- `document.documentElement.requestFullscreen()` — request fullscreen
- `document.exitFullscreen()` — exit fullscreen (for cleanup)
- `document.fullscreenElement` — check if currently in fullscreen (null = not in fullscreen)
- `document.addEventListener('fullscreenchange', handler)` — detect changes

**Angular Service: `AntiCheatMonitorService` — `initFullscreen()` method:**

```typescript
async initFullscreen(): Promise<boolean> {
  try {
    // Request browser fullscreen mode
    await document.documentElement.requestFullscreen();
    this.startFullscreenMonitoring();
    return true; // fullscreen granted
  } catch (error) {
    // Browser denied fullscreen — exam cannot start
    this.showError('Full-screen mode is required to take this exam. Please allow full-screen and try again.');
    return false; // fullscreen denied
  }
}

private startFullscreenMonitoring(): void {
  document.addEventListener('fullscreenchange', this.onFullscreenChange.bind(this));
}

private onFullscreenChange(): void {
  if (document.fullscreenElement === null) {
    // Student exited fullscreen
    this.processViolation({
      type: 'FullScreenExit',
      severity: 'Critical',
      detail: 'Student exited full-screen mode'
    });
    this.showReturnToFullscreenOverlay(); // Show overlay asking to return
  }
}

showReturnToFullscreenOverlay(): void {
  // Display a non-dismissible overlay: "You have exited full-screen. Please return."
  // With button: "Return to Full-Screen"
  // Button calls: document.documentElement.requestFullscreen()
}
```

**Exam wrapper behavior:**
- Fullscreen entered IMMEDIATELY after rules acknowledgment, BEFORE exam questions load
- If user refuses fullscreen → exam blocked with SweetAlert2 error
- On fullscreen exit: overlay appears within 1 second (the `fullscreenchange` event delay)
- Overlay does NOT block question saving — auto-save continues in background

**No backend changes in this stage.** Violations are queued locally and sent in Stage 4.9.

---

### Stage 4.3 — Tab & Focus Detection

**What:** Detect when the student switches browser tabs or moves focus away from the exam window.

**Browser APIs Used:**
- `document.visibilityState` — 'visible' | 'hidden' | 'prerender'
- `document.addEventListener('visibilitychange', handler)` — fires when tab visibility changes
- `window.addEventListener('blur', handler)` — fires when window loses focus (Alt+Tab, taskbar, etc.)
- `window.addEventListener('focus', handler)` — fires when window regains focus

**Angular Service: `AntiCheatMonitorService` — `startTabFocusMonitoring()` method:**

```typescript
private isFocusEventHandled = false; // Deduplication flag

startTabFocusMonitoring(): void {

  // Monitor tab visibility (handles tab switching in same browser)
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      this.isFocusEventHandled = true;
      this.processViolation({
        type: 'TabSwitch',
        severity: 'Critical',
        detail: 'Browser tab became hidden'
      });
    } else {
      // Tab visible again — log re-entry (presence event, not a violation)
      this.isFocusEventHandled = false;
    }
  });

  // Monitor window focus (handles Alt+Tab, clicking outside browser)
  window.addEventListener('blur', () => {
    // Delay 100ms to avoid double-counting with visibilitychange
    setTimeout(() => {
      if (!this.isFocusEventHandled && document.visibilityState === 'visible') {
        // This is a window blur NOT caused by tab switching (e.g., Alt+Tab, taskbar)
        this.processViolation({
          type: 'FocusLoss',
          severity: 'Critical',
          detail: 'Browser window lost focus'
        });
      }
    }, 100);
  });

  window.addEventListener('focus', () => {
    this.isFocusEventHandled = false;
    // Log re-entry as a presence event (not a violation)
    this.logPresenceEvent('WindowFocusRegained');
  });
}
```

**Important Notes:**
- Both `visibilitychange` and `blur` can fire for the same Alt+Tab event — the `isFocusEventHandled` flag prevents double-counting
- The 100ms delay on the blur handler ensures `visibilitychange` has time to set the flag first
- Focus regain events are logged as presence events (for the Session Timeline) but never as violations

---

### Stage 4.4 — Keyboard Shortcut Blocking

**What:** Block and log keyboard shortcuts that could assist cheating.

**Browser API Used:**
- `document.addEventListener('keydown', handler, true)` — `true` = capture phase (intercepts BEFORE other handlers)

**Angular Service: `AntiCheatMonitorService` — `startShortcutBlocking()` method:**

```typescript
startShortcutBlocking(): void {
  document.addEventListener('keydown', this.handleKeyDown.bind(this), true);
  document.addEventListener('contextmenu', this.handleContextMenu.bind(this));
}

private handleKeyDown(event: KeyboardEvent): void {
  const ctrl = event.ctrlKey || event.metaKey; // metaKey = Cmd on Mac

  // === Clipboard shortcuts ===
  if (ctrl && event.key === 'c') {
    event.preventDefault();
    event.stopPropagation();
    this.processViolation({ type: 'ClipboardCopy', severity: 'Medium', detail: 'Ctrl+C pressed' });
    return;
  }
  if (ctrl && event.key === 'v') {
    event.preventDefault();
    event.stopPropagation();
    this.processViolation({ type: 'ClipboardPaste', severity: 'Medium', detail: 'Ctrl+V pressed' });
    return;
  }

  // === Restricted shortcuts (log but do NOT count as violation — just block + minor log) ===
  const blockedCombos = [
    { ctrl: true, key: 'a' },    // Select All
    { ctrl: true, key: 's' },    // Save
    { ctrl: true, key: 'u' },    // View Source
    { ctrl: true, key: 'p' },    // Print
    { ctrl: true, key: 'f' },    // Find in page
    { ctrl: true, key: 'g' },    // Find next
  ];
  if (ctrl && blockedCombos.some(c => c.key === event.key.toLowerCase())) {
    event.preventDefault();
    event.stopPropagation();
    this.processViolation({ type: 'RestrictedShortcut', severity: 'Medium', detail: `Ctrl+${event.key.toUpperCase()} pressed` });
    return;
  }

  // === Developer Tools shortcuts ===
  if (
    event.key === 'F12' ||
    (ctrl && event.shiftKey && ['i', 'j', 'c'].includes(event.key.toLowerCase()))
  ) {
    event.preventDefault();
    event.stopPropagation();
    this.processViolation({ type: 'RestrictedShortcut', severity: 'Medium', detail: 'Developer tools shortcut detected' });
    return;
  }
}

private handleContextMenu(event: MouseEvent): void {
  // Block right-click context menu (prevents inspect element from right-click)
  event.preventDefault();
  // No violation logged for right-click alone — too common accidentally
}
```

---

### Stage 4.5 — Window Resize / Minimize / Split Detection

**What:** Detect and log attempts to resize, minimize, or split-screen the browser window during the exam.

**Browser API Used:**
- `window.addEventListener('resize', handler)` — fires when window size changes
- `window.screen.width` / `window.screen.height` — screen total dimensions
- `window.innerWidth` / `window.innerHeight` — current window dimensions
- `document.fullscreenElement` — check if still in fullscreen mode

**Angular Service: `AntiCheatMonitorService` — `startWindowBehaviorMonitoring()` method:**

```typescript
private lastWindowWidth = window.innerWidth;
private lastWindowHeight = window.innerHeight;

startWindowBehaviorMonitoring(): void {
  window.addEventListener('resize', this.handleWindowResize.bind(this));
}

private handleWindowResize(): void {
  const newWidth = window.innerWidth;
  const newHeight = window.innerHeight;
  const screenWidth = window.screen.width;
  const screenHeight = window.screen.height;

  // Case 1: Minimize (window collapsed to taskbar)
  if (newWidth === 0 || newHeight === 0) {
    this.processViolation({
      type: 'WindowMinimize',
      severity: 'Medium',
      detail: `Window minimized. Size: ${newWidth}x${newHeight}`
    });
  }
  // Case 2: Split-screen or significantly reduced window
  else if (newWidth < screenWidth * 0.75 || newHeight < screenHeight * 0.75) {
    this.processViolation({
      type: 'SplitScreen',
      severity: 'Medium',
      detail: `Window reduced to ${newWidth}x${newHeight} (screen: ${screenWidth}x${screenHeight})`
    });
  }
  // Case 3: Any resize while supposedly in fullscreen (indicates bypass attempt)
  else if (document.fullscreenElement !== null) {
    // True fullscreen should prevent resize — if resize fires while in fullscreen, something unusual happened
    this.processViolation({
      type: 'WindowResize',
      severity: 'Medium',
      detail: `Resize event during fullscreen: ${newWidth}x${newHeight}`
    });
  }

  // Update tracked dimensions
  this.lastWindowWidth = newWidth;
  this.lastWindowHeight = newHeight;
}
```

---

### Stage 4.6 — Mouse Monitoring

**What:** Detect statistically abnormal mouse movement patterns that may indicate automated tools or bots.

**Note:** Mouse monitoring generates Minor violations only. It should not trigger on normal exam usage.

**Angular Service: `AntiCheatMonitorService` — `startMouseMonitoring()` method:**

```typescript
private mouseMovements: Array<{ x: number; y: number; timestamp: number }> = [];
private readonly MOUSE_BUFFER_SIZE = 20;     // Keep last 20 movements
private readonly MOUSE_CHECK_INTERVAL = 5000; // Check every 5 seconds
private readonly ABNORMAL_SPEED_THRESHOLD = 1000; // pixels per 50ms

startMouseMonitoring(): void {
  document.addEventListener('mousemove', this.recordMouseMovement.bind(this));

  // Analyze movement patterns periodically
  setInterval(() => {
    this.analyzeMousePattern();
  }, this.MOUSE_CHECK_INTERVAL);
}

private recordMouseMovement(event: MouseEvent): void {
  this.mouseMovements.push({
    x: event.clientX,
    y: event.clientY,
    timestamp: Date.now()
  });
  // Keep only last N movements (circular buffer)
  if (this.mouseMovements.length > this.MOUSE_BUFFER_SIZE) {
    this.mouseMovements.shift();
  }
}

private analyzeMousePattern(): void {
  if (this.mouseMovements.length < 5) return; // Not enough data

  let abnormalMoveCount = 0;
  for (let i = 1; i < this.mouseMovements.length; i++) {
    const prev = this.mouseMovements[i - 1];
    const curr = this.mouseMovements[i];
    const timeDelta = curr.timestamp - prev.timestamp; // ms
    const distance = Math.sqrt(
      Math.pow(curr.x - prev.x, 2) + Math.pow(curr.y - prev.y, 2)
    );
    // Speed check: if moved ABNORMAL_SPEED_THRESHOLD pixels in less than 50ms
    if (timeDelta < 50 && distance > this.ABNORMAL_SPEED_THRESHOLD) {
      abnormalMoveCount++;
    }
  }

  // Only flag if more than 30% of recent movements are abnormal
  if (abnormalMoveCount > this.mouseMovements.length * 0.3) {
    this.processViolation({
      type: 'AbnormalMouseActivity',
      severity: 'Minor',
      detail: `${abnormalMoveCount} abnormal movements detected in last ${this.MOUSE_BUFFER_SIZE} samples`
    });
  }

  // Reset buffer after analysis
  this.mouseMovements = [];
}
```

---

### Stage 4.7 — Violation Intelligence Layer (Severity + Cooldown)

**What:** The central hub that ALL violation emitters route through. Applies deduplication cooldown and the first-violation warning rule.

**Angular Service: `AntiCheatMonitorService` — `processViolation()` method:**

This is the most important method in the anti-cheat system. All stages call this.

```typescript
// State tracked per exam session
private violationCount = 0;
private isFirstViolationWarned = false;
private maxViolationsAllowed = 3; // Loaded from exam config
private cooldownMap = new Map<string, number>(); // violationType → last timestamp (ms)
private readonly COOLDOWN_MS = 3000; // 3 seconds cooldown per violation type

processViolation(violation: {
  type: ViolationType;
  severity: ViolationSeverity;
  detail?: string;
}): void {

  // === STEP 1: Cooldown Check ===
  // Prevent the same violation from being logged repeatedly within 3 seconds
  const now = Date.now();
  const lastOccurrence = this.cooldownMap.get(violation.type) ?? 0;
  if (now - lastOccurrence < this.COOLDOWN_MS) {
    // This is a duplicate within the cooldown window — IGNORE COMPLETELY
    return;
  }
  this.cooldownMap.set(violation.type, now);

  // === STEP 2: First Violation Warning Rule ===
  // The VERY FIRST violation detected (of ANY type) is treated as a warning only.
  // It is still logged to the backend (with isFirstViolation = true)
  // but it does NOT increment the violationCount.
  if (!this.isFirstViolationWarned) {
    this.isFirstViolationWarned = true;
    this.showWarningMessage(violation.type, true); // isFirstWarning = true
    this.reportViolationToBackend({ ...violation, isFirstViolation: true });
    return; // Do NOT proceed to accumulation
  }

  // === STEP 3: Accumulation ===
  // All subsequent violations (after the first warning) increment the counter
  this.violationCount++;
  this.showWarningMessage(violation.type, false); // isFirstWarning = false
  this.reportViolationToBackend({ ...violation, isFirstViolation: false });

  // === STEP 4: Enforcement Check ===
  if (this.violationCount >= this.maxViolationsAllowed) {
    this.triggerForceSubmit();
  }
}
```

**Contextual Warning Messages:**

| Violation Type | Warning Message |
|---------------|----------------|
| `FullScreenExit` | "Please return to full-screen mode immediately." |
| `TabSwitch` | "Tab switching is not allowed during the exam." |
| `FocusLoss` | "Please keep the exam window in focus." |
| `ClipboardCopy` | "Copying content is not allowed during the exam." |
| `ClipboardPaste` | "Pasting content is not allowed during the exam." |
| `RestrictedShortcut` | "This keyboard shortcut is not allowed during the exam." |
| `WindowResize` | "Please maintain the exam in full-screen mode." |
| `WindowMinimize` | "Minimizing the window is not allowed during the exam." |
| `SplitScreen` | "Split-screen mode is not allowed during the exam." |
| `AbnormalMouseActivity` | "Unusual activity detected." |

---

### Stage 4.8 — Warning System + Violation Accumulation + Force-Submit

**What:** The user-facing warning UI and the force-submit flow when the violation limit is exceeded.

**Angular — `showWarningMessage()` method:**

```typescript
showWarningMessage(violationType: ViolationType, isFirstWarning: boolean): void {
  if (isFirstWarning) {
    // First warning: prominent but not panic-inducing
    // Show persistent ngx-toastr warning (type: 'warning')
    this.toastr.warning(
      this.getViolationMessage(violationType),
      'Warning — First Notice',
      { timeOut: 8000, closeButton: true, disableTimeOut: false }
    );
  } else {
    // Subsequent violations: show count
    const remaining = this.maxViolationsAllowed - this.violationCount;
    this.toastr.error(
      `${this.getViolationMessage(violationType)} You have ${remaining} warning(s) remaining before your exam is submitted.`,
      `Violation ${this.violationCount} of ${this.maxViolationsAllowed}`,
      { timeOut: 10000, closeButton: true }
    );
  }
}
```

**Angular — `triggerForceSubmit()` method:**

```typescript
async triggerForceSubmit(): Promise<void> {
  // 1. Stop all monitoring immediately
  this.stopAllMonitoring();

  // 2. Show non-dismissible SweetAlert2 modal
  await Swal.fire({
    title: 'Exam Terminated',
    html: 'Your exam has been automatically submitted due to repeated violations of the exam integrity rules.<br><br>Your answers up to this point have been saved.',
    icon: 'error',
    allowOutsideClick: false,
    allowEscapeKey: false,
    allowEnterKey: false,
    showConfirmButton: true,
    confirmButtonText: 'View Result',
    confirmButtonColor: '#215DAE'
  });

  // 3. Submit to backend with ForceSubmitted type
  try {
    await this.examService.forceSubmitExam(this.sessionToken, 'ForceSubmitted');
  } catch (error) {
    // Even if API call fails, navigate away from exam
    console.error('Force submit API call failed:', error);
  }

  // 4. Exit fullscreen
  if (document.fullscreenElement) {
    await document.exitFullscreen();
  }

  // 5. Navigate away
  this.router.navigate(['/student/courses']);
}
```

---

### Stage 4.9 — Backend Violation Persistence & API Endpoints

**What:** All backend endpoints for violation management.

**API Endpoint: `POST /api/sessions/{token}/violations`**

Request body:
```json
{
  "violationType": "TabSwitch",
  "severity": "Critical",
  "timestamp": "2025-01-15T10:30:45Z",
  "eventDetail": "Browser tab became hidden",
  "isFirstViolation": false
}
```

Backend logic:
1. Validate session token (must be Active, must belong to requesting student)
2. Cooldown check: if same `violationType` was logged within last 3 seconds for this session → return `{ ignored: true }`
3. Create `ViolationLog` record
4. If `isFirstViolation = false`: increment `ExamSession.ViolationCount`
5. Check if `ViolationCount >= MaxViolationsAllowed`:
   - If yes: set `Status = ForceSubmitted`, `ForceSubmittedAt = now`, trigger grading
   - Return `{ shouldForceSubmit: true, violationCount, maxAllowed }`
6. Return: `{ ignored: false, shouldForceSubmit: false, violationCount, maxAllowed }`

Response body:
```json
{
  "success": true,
  "data": {
    "ignored": false,
    "shouldForceSubmit": false,
    "violationCount": 2,
    "maxAllowed": 3,
    "message": "Violation recorded. 1 warning remaining."
  }
}
```

**Additional API Endpoints:**
- `GET /api/sessions/{sessionId}/violations` — list all violations (Tutor/Admin only)
- `GET /api/sessions/{token}/state` — restore session state on reconnect (questions, remaining time, violationCount, isFirstViolationWarned)

---

### Stage 4.10 — Monitoring Continuity on Reconnect

**What:** Ensure the anti-cheat engine resumes correctly after page refresh, connection loss, or tab restore.

**Frontend — Session Recovery Flow:**

When student navigates to the exam page (after refresh):
1. Angular component calls `GET /api/sessions/{token}`
2. If session is `Active`:
   - Restore exam state from backend response:
     ```
     questionSet, remainingSeconds, violationCount,
     isFirstViolationWarned, savedAnswers, maxViolationsAllowed
     ```
   - Re-initialize `AntiCheatMonitorService` with restored state:
     - `this.violationCount = response.violationCount`
     - `this.isFirstViolationWarned = response.isFirstViolationWarned`
   - Re-attach ALL event listeners:
     - `startFullscreenMonitoring()`
     - `startTabFocusMonitoring()`
     - `startShortcutBlocking()`
     - `startWindowBehaviorMonitoring()`
     - `startMouseMonitoring()`
   - Request fullscreen again
   - Show ngx-toastr info: "Session restored. Monitoring continues."
3. If session is `ForceSubmitted` or `Expired`:
   - Show SweetAlert2 info: "This exam session has ended."
   - Navigate to results or course page

**Backend — Session State Endpoint:**

`GET /api/sessions/{token}` returns:
```json
{
  "success": true,
  "data": {
    "sessionId": "...",
    "examId": "...",
    "status": "Active",
    "remainingSeconds": 1847,
    "questionSet": [ ... ],
    "savedAnswers": [ ... ],
    "violationCount": 1,
    "maxViolationsAllowed": 3,
    "isFirstViolationWarned": true
  }
}
```

**Heartbeat (Presence Tracking):**
- Frontend sends `POST /api/sessions/{token}/heartbeat` every 30 seconds
- Backend updates `ExamSession.LastHeartbeatAt`
- If no heartbeat for 90 seconds: backend background service marks the session's presence as "disconnected" in `PresenceLogs`

---

## 20. PHASE 5 — MONITORING & DASHBOARDS

### Feature 16 — Exam Presence Tracking

**Purpose:** Track student presence and activity throughout the exam session for post-exam review.

**PresenceLog Entity:** `Id`, `ExamSessionId`, `StudentId`, `EventType`, `Timestamp`

**EventType values:**
```
ExamStarted | PageRefreshed | Disconnected | Reconnected | HeartbeatReceived |
ExamSubmitted | ForceSubmitted | AutoExpired | UnexpectedExit
```

**Implementation:**
- Every significant session event creates a `PresenceLog` entry
- Heartbeat received every 30 seconds → `HeartbeatReceived` log entry
- If heartbeat not received for 90 seconds → `Disconnected` entry (created by background service)
- When student reconnects → `Reconnected` entry

**Access:** Tutor (their courses only) and Admin (all courses). Students cannot see presence logs.

---

### Feature 17 — Session Timeline View

**Purpose:** Provide a chronological visualization of ALL events during a student's exam session.

**Route:** `/tutor/courses/{courseId}/exams/{examId}/sessions/{sessionId}/timeline`

**Timeline Events (combined from multiple tables):**
- `PresenceLogs` → session lifecycle events
- `ViolationLogs` → all detected violations
- System events (auto-submit, force-submit decision)

**Visual Design:**
- Vertical timeline with line connecting events
- Each event: Lucide icon (appropriate to type) + colored dot + timestamp + description
- Color coding:
  - Green (`--color-success`): normal events (started, submitted, reconnected)
  - Yellow (`--color-warning`): minor/medium violations, warnings
  - Orange: medium violations
  - Red (`--color-critical-violation`): critical violations, force-submit

**Filtering:** Filter by event category (Normal | Warning | Violation | Critical)

---

### Feature 18 — Violation Timeline

**Purpose:** Focused view of all violations for a session, with summary statistics.

**Route:** `/tutor/courses/{courseId}/exams/{examId}/sessions/{sessionId}/violations`

**Display:**
1. **Summary Cards Row:**
   - Total Violations: `{count}`
   - Critical Violations: `{count}` (in red)
   - Medium Violations: `{count}` (in orange)
   - Minor Violations: `{count}` (in yellow)
   - Submission Type: Manual | Auto-Expired | Force-Submitted (color-coded badge)

2. **Violations-Over-Time Chart (Chart.js):**
   - X-axis: time during exam (in minutes)
   - Y-axis: cumulative violation count
   - Color-coded bars by severity (Red=Critical, Orange=Medium, Yellow=Minor)

3. **Violation List Table:**
   - Columns: Timestamp | Type | Severity Badge | Detail | Is First Warning
   - Sorted chronologically

---

### Feature 19 — Manual Review

**Purpose:** Allow tutors and admins to review suspicious sessions and make decisions.

**Route:** `/tutor/courses/{courseId}/exams/{examId}/sessions/{sessionId}/review`

**Trigger:** Any `ForceSubmitted` session, or any session manually flagged by Tutor/Admin.

**Review Page Layout:**

Left panel:
- Student info (name, ID, email)
- Session summary (start time, end time, duration, submission type)
- Current result (score, if released)

Right panel:
- Session Timeline (embedded from Feature 17)
- Violation Timeline summary (embedded from Feature 18)

Bottom action bar (SweetAlert2 confirmation for each):
- "Accept Result" → mark as `Reviewed + Accepted` — score stands
- "Mark as Cheating" → mark as `MarkedAsCheating` — score zeroed
- "Approve Re-Attempt" → grant one additional attempt + notify student
- "Add Review Notes" → free-text notes saved to `ReviewDecision.Notes`

**ReviewDecision Entity:** `Id`, `ExamSessionId`, `ReviewerId`, `Decision` (Accepted|MarkedAsCheating|ReAttemptGranted), `Notes`, `ReviewedAt`

**Student Appeal:**
- Student can submit an appeal for ForceSubmitted sessions within 24 hours
- Appeal includes a reason text
- Tutor/Admin reviews appeal alongside session data
- Appeal status is visible to student

---

### Feature 20 — Tutor Monitoring Dashboard

**Purpose:** Provide tutors a real-time command center to monitor live exam sessions.

**Route:** `/tutor/dashboard`

**Dashboard Sections:**

**Section 1 — Active Exams Panel:**
For each currently active exam across all assigned courses:
- Exam title + course name
- `{n}` students in progress | `{n}` submitted | `{n}` force-submitted | `{n}` not started
- "View Details" → expand to student list

**Section 2 — Student Status Grid (Live — polls every 15 seconds):**
For each enrolled student:
| Student Name | Student ID | Status Badge | Violations | Actions |
|-------------|-----------|-------------|-----------|---------|
| Ahmed Ali | S2023001 | In Progress | 1 | [Review] [Flag] |

Status Badge colors:
- Not Started → grey
- In Progress → green (with pulse animation)
- Disconnected → orange (with pulse)
- Submitted → blue
- Force-Submitted → red

**Section 3 — Risk Highlights:**
- Students with `ViolationCount >= 2` → highlighted in orange background
- Students with `Status = ForceSubmitted` → highlighted in red background
- Sorted by violation count descending

**Section 4 — Quick Actions (per student row):**
- Open Session Timeline (in modal)
- Open Manual Review page
- Approve Re-Attempt (opens confirmation dialog)

**Section 5 — Charts (Chart.js):**
- Violation distribution by type (doughnut chart)
- Violations over time for active exam (line chart, live-updating)

---

### Feature 21 — Admin Dashboard

**Purpose:** System-wide monitoring and management across all courses, tutors, and exams.

**Route:** `/admin/dashboard`

**Dashboard Sections:**

**Section 1 — System Overview Cards (KPI Row):**
- Total Courses Active
- Total Ongoing Exams Now
- Total Students Enrolled (all courses)
- Total Violations Today
- Total Suspicious Submissions (Force-Submitted) Today

**Section 2 — Global Exam Monitor:**
Table of all currently active exam sessions across all courses:
- Filterable by: Course, Tutor, Exam, Date
- Each row: Exam name | Course | Tutor | Students in progress | Violations | Action (view/review)

**Section 3 — Analytics Charts (Chart.js):**
- Most frequent violation types — horizontal bar chart
- Exams with highest violation rates — ranked list
- User activity trends over past 30 days — line chart
- Suspicious submission rate — gauge or percentage card

**Section 4 — System Health Panel:**
- Concurrent active sessions count (live)
- Failed/interrupted sessions today
- Average exam completion time
- Background service status (exam reminders, auto-submit)

**Section 5 — Notification Center:**
- System-wide important alerts inline in dashboard
- Critical events: "Exam X has 15 force-submitted sessions" → shows in red alert

**Section 6 — Bulk Actions:**
- Select multiple ForceSubmitted sessions → "Bulk: Mark All as Reviewed"
- Select multiple pending re-attempt requests → "Bulk: Approve All" or "Bulk: Reject All"

**Access:** Admin role ONLY. All data is system-wide (not filtered by course).

---

## 21. COMPLETE DATABASE SCHEMA REFERENCE

All tables use UNIQUEIDENTIFIER (GUID) as primary key. All column names in PascalCase.
All timestamps in UTC using DATETIME2.

```sql
-- ═══════════════════════════════════════════════════════════════
-- PHASE 1: Authentication & User Management
-- ═══════════════════════════════════════════════════════════════

Users
  Id                    UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  FirstName             NVARCHAR(100)     NOT NULL
  LastName              NVARCHAR(100)     NOT NULL
  Email                 NVARCHAR(255)     NOT NULL  UNIQUE
  PasswordHash          NVARCHAR(512)     NOT NULL
  Role                  NVARCHAR(20)      NOT NULL  -- 'Admin' | 'Tutor' | 'Student'
  AccountStatus         NVARCHAR(20)      NOT NULL  DEFAULT 'Unverified'  -- 'Active' | 'Unverified' | 'Locked' | 'Disabled'
  StudentId             NVARCHAR(50)      NULL      UNIQUE
  TutorId               NVARCHAR(50)      NULL      UNIQUE
  AdminId               NVARCHAR(50)      NULL      UNIQUE
  ProfilePicturePath    NVARCHAR(500)     NULL
  FailedLoginAttempts   INT               NOT NULL  DEFAULT 0
  LockedAt              DATETIME2         NULL
  LastLoginAt           DATETIME2         NULL
  EmailVerifiedAt       DATETIME2         NULL
  CreatedAt             DATETIME2         NOT NULL  DEFAULT GETUTCDATE()

RefreshTokens
  Id          UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  UserId      UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  Token       NVARCHAR(512)     NOT NULL  UNIQUE
  ExpiresAt   DATETIME2         NOT NULL
  IsRevoked   BIT               NOT NULL  DEFAULT 0
  CreatedAt   DATETIME2         NOT NULL  DEFAULT GETUTCDATE()

LoginActivityLogs
  Id          UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  UserId      UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  IpAddress   NVARCHAR(50)      NULL
  IsSuccess   BIT               NOT NULL  DEFAULT 1
  CreatedAt   DATETIME2         NOT NULL  DEFAULT GETUTCDATE()

UserActivityLogs
  Id          UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  UserId      UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  EventType   NVARCHAR(50)      NOT NULL  -- 'EmailChange' | 'PasswordChange' | 'ProfileUpdate' | 'PictureUpload'
  IpAddress   NVARCHAR(50)      NULL
  CreatedAt   DATETIME2         NOT NULL  DEFAULT GETUTCDATE()

-- ═══════════════════════════════════════════════════════════════
-- PHASE 2: Core LMS
-- ═══════════════════════════════════════════════════════════════

Courses
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  Title             NVARCHAR(255)     NOT NULL
  CourseCode        NVARCHAR(50)      NOT NULL  UNIQUE
  Description       NVARCHAR(2000)    NULL
  AssignedTutorId   UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  IsActive          BIT               NOT NULL  DEFAULT 1
  CreatedAt         DATETIME2         NOT NULL  DEFAULT GETUTCDATE()

CourseEnrollments
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  StudentId         UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  CourseId          UNIQUEIDENTIFIER  NOT NULL  FK → Courses(Id)
  Status            NVARCHAR(10)      NOT NULL  -- 'Pending' | 'Approved' | 'Rejected'
  RejectionCount    INT               NOT NULL  DEFAULT 0
  CooldownUntil     DATETIME2         NULL
  RequestedAt       DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
  ReviewedAt        DATETIME2         NULL
  ReviewedById      UNIQUEIDENTIFIER  NULL      FK → Users(Id)
  UNIQUE (StudentId, CourseId)  -- One enrollment record per student per course

CourseMaterials
  Id                  UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  CourseId            UNIQUEIDENTIFIER  NOT NULL  FK → Courses(Id)
  Title               NVARCHAR(255)     NOT NULL
  Description         NVARCHAR(1000)    NULL
  MaterialType        NVARCHAR(10)      NOT NULL  -- 'File' | 'Link'
  FilePath            NVARCHAR(500)     NULL
  ExternalUrl         NVARCHAR(1000)    NULL
  UploadedByUserId    UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  CreatedAt           DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
  UpdatedAt           DATETIME2         NULL

Announcements
  Id                  UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  CourseId            UNIQUEIDENTIFIER  NOT NULL  FK → Courses(Id)
  Title               NVARCHAR(255)     NOT NULL
  Content             NVARCHAR(MAX)     NOT NULL
  Priority            NVARCHAR(10)      NOT NULL  DEFAULT 'Normal'  -- 'Normal' | 'Important'
  AttachmentType      NVARCHAR(10)      NOT NULL  DEFAULT 'None'    -- 'None' | 'File' | 'Link'
  AttachmentPath      NVARCHAR(500)     NULL
  AttachmentUrl       NVARCHAR(1000)    NULL
  CreatedByUserId     UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  CreatedAt           DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
  UpdatedAt           DATETIME2         NULL

Notifications
  Id                  UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  RecipientUserId     UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  Title               NVARCHAR(255)     NOT NULL
  Message             NVARCHAR(1000)    NOT NULL
  Type                NVARCHAR(50)      NOT NULL
  IsRead              BIT               NOT NULL  DEFAULT 0
  ReadAt              DATETIME2         NULL
  RelatedCourseId     UNIQUEIDENTIFIER  NULL      FK → Courses(Id)
  RelatedExamId       UNIQUEIDENTIFIER  NULL      FK → Exams(Id)
  CreatedAt           DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
  INDEX IX_Notifications_RecipientUserId (RecipientUserId)
  INDEX IX_Notifications_IsRead (IsRead)

-- ═══════════════════════════════════════════════════════════════
-- PHASE 3: Examination Management
-- ═══════════════════════════════════════════════════════════════

Exams
  Id                    UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  CourseId              UNIQUEIDENTIFIER  NOT NULL  FK → Courses(Id)
  Title                 NVARCHAR(255)     NOT NULL
  Description           NVARCHAR(2000)    NULL
  ExamType              NVARCHAR(20)      NOT NULL  -- 'Midterm' | 'Final' | 'Quiz'
  TotalMarks            INT               NOT NULL
  DurationMinutes       INT               NOT NULL
  StartDateTime         DATETIME2         NOT NULL
  EndDateTime           DATETIME2         NOT NULL
  MaxViolationsAllowed  INT               NOT NULL  DEFAULT 3
  ResultVisibility      NVARCHAR(20)      NOT NULL  DEFAULT 'ManualRelease'  -- 'Immediate' | 'Scheduled' | 'ManualRelease'
  ResultReleaseAt       DATETIME2         NULL
  CreatedByUserId       UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  CreatedAt             DATETIME2         NOT NULL  DEFAULT GETUTCDATE()

Questions
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  CourseId          UNIQUEIDENTIFIER  NOT NULL  FK → Courses(Id)
  QuestionTitle     NVARCHAR(255)     NULL
  QuestionText      NVARCHAR(MAX)     NOT NULL
  QuestionType      NVARCHAR(20)      NOT NULL  -- 'MCQ' | 'TrueFalse'
  Marks             INT               NOT NULL
  Category          NVARCHAR(100)     NULL
  CreatedByUserId   UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  CreatedAt         DATETIME2         NOT NULL  DEFAULT GETUTCDATE()

QuestionOptions
  Id            UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  QuestionId    UNIQUEIDENTIFIER  NOT NULL  FK → Questions(Id)  ON DELETE CASCADE
  OptionText    NVARCHAR(500)     NOT NULL
  IsCorrect     BIT               NOT NULL  DEFAULT 0

ExamSessions
  Id                      UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  Token                   UNIQUEIDENTIFIER  NOT NULL  UNIQUE  -- Indexed for fast lookup
  StudentId               UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  ExamId                  UNIQUEIDENTIFIER  NOT NULL  FK → Exams(Id)
  Status                  NVARCHAR(20)      NOT NULL  DEFAULT 'Active'  -- 'Active' | 'Submitted' | 'ForceSubmitted' | 'Expired' | 'AutoExpired'
  QuestionSetJson         NVARCHAR(MAX)     NULL      -- Serialized question+option order
  ViolationCount          INT               NOT NULL  DEFAULT 0
  IsFirstViolationWarned  BIT               NOT NULL  DEFAULT 0
  StartedAt               DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
  ExpiresAt               DATETIME2         NOT NULL
  SubmittedAt             DATETIME2         NULL
  SubmissionType          NVARCHAR(20)      NULL      -- 'Manual' | 'AutoExpired' | 'ForceSubmitted'
  ForceSubmittedAt        DATETIME2         NULL
  LastHeartbeatAt         DATETIME2         NULL
  INDEX IX_ExamSessions_Token (Token)
  INDEX IX_ExamSessions_StudentId_ExamId (StudentId, ExamId)

StudentAnswers
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  ExamSessionId     UNIQUEIDENTIFIER  NOT NULL  FK → ExamSessions(Id)
  QuestionId        UNIQUEIDENTIFIER  NOT NULL  FK → Questions(Id)
  SelectedOptionId  UNIQUEIDENTIFIER  NULL      FK → QuestionOptions(Id)
  SavedAt           DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
  UNIQUE (ExamSessionId, QuestionId)  -- One answer per question per session

ExamResults
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  ExamSessionId     UNIQUEIDENTIFIER  NOT NULL  FK → ExamSessions(Id)  UNIQUE
  StudentId         UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  ExamId            UNIQUEIDENTIFIER  NOT NULL  FK → Exams(Id)
  TotalMarks        INT               NOT NULL
  ScoreObtained     INT               NOT NULL
  Percentage        DECIMAL(5,2)      NOT NULL
  IsForceSubmitted  BIT               NOT NULL  DEFAULT 0
  IsReleased        BIT               NOT NULL  DEFAULT 0
  ReleasedAt        DATETIME2         NULL
  SubmittedAt       DATETIME2         NOT NULL
  GradedAt          DATETIME2         NOT NULL

ReAttemptRequests
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  StudentId         UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  ExamId            UNIQUEIDENTIFIER  NOT NULL  FK → Exams(Id)
  Justification     NVARCHAR(2000)    NOT NULL
  Status            NVARCHAR(10)      NOT NULL  DEFAULT 'Pending'  -- 'Pending' | 'Approved' | 'Rejected'
  RequestedAt       DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
  ReviewedAt        DATETIME2         NULL
  ReviewedById      UNIQUEIDENTIFIER  NULL      FK → Users(Id)

-- ═══════════════════════════════════════════════════════════════
-- PHASE 4: Anti-Cheating Engine
-- ═══════════════════════════════════════════════════════════════

ViolationLogs
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  ExamSessionId     UNIQUEIDENTIFIER  NOT NULL  FK → ExamSessions(Id)
  ViolationType     NVARCHAR(50)      NOT NULL  -- ViolationType enum value
  Severity          NVARCHAR(10)      NOT NULL  -- 'Minor' | 'Medium' | 'Critical'
  Timestamp         DATETIME2         NOT NULL
  EventDetail       NVARCHAR(500)     NULL
  IsFirstViolation  BIT               NOT NULL  DEFAULT 0
  INDEX IX_ViolationLogs_ExamSessionId (ExamSessionId)

-- ═══════════════════════════════════════════════════════════════
-- PHASE 5: Monitoring & Dashboards
-- ═══════════════════════════════════════════════════════════════

PresenceLogs
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  ExamSessionId     UNIQUEIDENTIFIER  NOT NULL  FK → ExamSessions(Id)
  StudentId         UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  EventType         NVARCHAR(30)      NOT NULL  -- PresenceEventType enum value
  Timestamp         DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
  INDEX IX_PresenceLogs_ExamSessionId (ExamSessionId)

ReviewDecisions
  Id                UNIQUEIDENTIFIER  NOT NULL  PRIMARY KEY  DEFAULT NEWID()
  ExamSessionId     UNIQUEIDENTIFIER  NOT NULL  FK → ExamSessions(Id)
  ReviewerId        UNIQUEIDENTIFIER  NOT NULL  FK → Users(Id)
  Decision          NVARCHAR(30)      NOT NULL  -- 'Accepted' | 'MarkedAsCheating' | 'ReAttemptGranted'
  Notes             NVARCHAR(2000)    NULL
  ReviewedAt        DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
```

---

## 22. API CONVENTIONS & STANDARDS

### Base URL

- **Development:** `https://localhost:7001/api`
- **Production:** `https://api.shieldon.com/api` (placeholder)

### Authentication Headers

All protected endpoints require:
```http
Authorization: Bearer {accessToken}
```

Exam session endpoints additionally require:
```http
X-Exam-Session-Token: {sessionToken}
```

### Unified Response Envelope

Every API response — success or error — uses this format:

**Success:**
```json
{
  "success": true,
  "data": { "...": "..." },
  "message": "Operation completed successfully.",
  "errors": []
}
```

**Error:**
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed.",
  "errors": ["Email is required.", "Password must be at least 8 characters."]
}
```

**Pagination response (for lists):**
```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 8
  },
  "message": null,
  "errors": []
}
```

### HTTP Status Codes

| Code | When to Use |
|------|------------|
| 200 OK | Successful GET, PATCH, PUT |
| 201 Created | Successful POST that creates a new resource |
| 204 No Content | Successful DELETE |
| 400 Bad Request | Validation failure, malformed request |
| 401 Unauthorized | Missing or invalid JWT token |
| 403 Forbidden | Valid JWT but insufficient role/permissions |
| 404 Not Found | Resource does not exist |
| 409 Conflict | Duplicate resource (email, course code) |
| 429 Too Many Requests | Rate limit hit (login attempts, enrollment cooldown) |
| 500 Internal Server Error | Unhandled exception (log server-side, return generic message to client) |

### Endpoint Naming Convention

```
POST   /api/auth/login
POST   /api/auth/refresh-token
POST   /api/auth/logout
POST   /api/auth/forgot-password
POST   /api/auth/reset-password
POST   /api/auth/verify-email
POST   /api/auth/resend-verification

GET    /api/users/profile
PATCH  /api/users/profile
PATCH  /api/users/profile/email
PATCH  /api/users/profile/password
POST   /api/users/profile/picture

GET    /api/courses
POST   /api/courses
GET    /api/courses/{courseId}
PATCH  /api/courses/{courseId}
DELETE /api/courses/{courseId}
GET    /api/courses/{courseId}/materials
POST   /api/courses/{courseId}/materials
GET    /api/courses/{courseId}/announcements
POST   /api/courses/{courseId}/announcements
GET    /api/courses/{courseId}/exams
POST   /api/courses/{courseId}/exams
GET    /api/courses/{courseId}/questions
POST   /api/courses/{courseId}/questions

GET    /api/enrollments
POST   /api/enrollments
PATCH  /api/enrollments/{id}
PATCH  /api/enrollments/bulk

GET    /api/materials/{id}
PATCH  /api/materials/{id}
DELETE /api/materials/{id}

GET    /api/announcements/{id}
PATCH  /api/announcements/{id}
DELETE /api/announcements/{id}

GET    /api/notifications
PATCH  /api/notifications/{id}/read
PATCH  /api/notifications/read-all
GET    /api/notifications/unread-count

GET    /api/exams/{examId}
PATCH  /api/exams/{examId}
DELETE /api/exams/{examId}
GET    /api/exams/{examId}/results

POST   /api/sessions/start          -- Start new exam session
GET    /api/sessions/{token}        -- Restore session state
POST   /api/sessions/{token}/heartbeat
PATCH  /api/sessions/{token}/answers -- Save answer(s)
POST   /api/sessions/{token}/submit  -- Manual submit
POST   /api/sessions/{token}/violations -- Report violation

GET    /api/sessions/{sessionId}/timeline
GET    /api/sessions/{sessionId}/violations
POST   /api/sessions/{sessionId}/review

GET    /api/reattempt-requests
POST   /api/reattempt-requests
PATCH  /api/reattempt-requests/{id}

GET    /api/questions/{id}
PATCH  /api/questions/{id}
DELETE /api/questions/{id}
```

### Controller Naming

| Controller | File | Base Route |
|-----------|------|-----------|
| `AuthController` | `AuthController.cs` | `/api/auth` |
| `UsersController` | `UsersController.cs` | `/api/users` |
| `CoursesController` | `CoursesController.cs` | `/api/courses` |
| `EnrollmentsController` | `EnrollmentsController.cs` | `/api/enrollments` |
| `MaterialsController` | `MaterialsController.cs` | `/api/materials` |
| `AnnouncementsController` | `AnnouncementsController.cs` | `/api/announcements` |
| `NotificationsController` | `NotificationsController.cs` | `/api/notifications` |
| `ExamsController` | `ExamsController.cs` | `/api/exams` |
| `QuestionsController` | `QuestionsController.cs` | `/api/questions` |
| `ExamSessionsController` | `ExamSessionsController.cs` | `/api/sessions` |
| `ReAttemptRequestsController` | `ReAttemptRequestsController.cs` | `/api/reattempt-requests` |

---

## 23. README.md MAINTENANCE RULES

The `README.md` at the root of the repository must be updated after every confirmed stage.

### Required Sections in README.md

```markdown
# SHIELDON — Integrity You Can Trust

## Project Overview
[What SHIELDON is, the problem it solves, the 5 phases]

## Technology Stack
[Badges for Angular, .NET, SQL Server, EF Core, JWT]

## Architecture
[Brief explanation of Clean Architecture + Vertical Slice]
[ASCII folder structure diagram]

## Getting Started

### Prerequisites
[List: .NET 10 SDK, Node.js 20+, SQL Server, Angular CLI 21, Git]

### Backend Setup
1. Clone repository
2. Update connection string in appsettings.Development.json
3. Run: dotnet ef database update
4. Run: dotnet run

### Frontend Setup
1. cd frontend
2. npm install
3. Update API URL in environment.ts
4. ng serve

## Feature Implementation Status
[Table with all 21 features + setup stages, checkboxes updated per confirmed stage]

## API Endpoints Reference
[Full list of all implemented endpoints, updated each stage]

## Database Schema
[Brief ERD description or link to schema file]

## Git Workflow
[Branch strategy, commit convention]

## Team Members
[Names and roles]

## Version History
[Changelog: what was added in each stage]
```

### Update Timing

- Update README.md as **Step 12 of every stage** (after git commit, before stage confirmation)
- Add the completed feature to the Feature Implementation Status table
- Add new API endpoints to the Endpoints Reference section
- Update the Version History with the stage name and date

---

---

## NEW SECTIONS ADDED IN VERSION 3.0.0

The following sections are new additions to v3. They are inserted after the existing sections (1–23 from v2).
All previous sections (1–23) remain fully intact and unchanged below.

---

## 24. SENSITIVE DATA & API KEY SECURITY

> **CRITICAL:** This must be followed strictly. Any security breach means failing the graduation review.

### The Core Rule

**NEVER put any of the following in any file committed to GitHub:**
- Database connection strings
- JWT secret keys
- SMTP credentials (email password)
- Any API keys or passwords
- Any private keys or certificates

### Backend: ASP.NET Core Configuration

**`appsettings.Development.json`** — gitignored, contains real secrets:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ShieldonDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-minimum-32-chars-long-here",
    "Issuer": "SHIELDON",
    "Audience": "SHIELDON-Users",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUser": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromName": "SHIELDON Platform",
    "FromEmail": "noreply@shieldon.com"
  }
}
```

**`appsettings.json`** — committed to git (structure only, no real values):
```json
{
  "ConnectionStrings": { "DefaultConnection": "SET_IN_DEVELOPMENT_SECRETS" },
  "JwtSettings": {
    "SecretKey": "SET_IN_DEVELOPMENT_SECRETS",
    "Issuer": "SHIELDON",
    "Audience": "SHIELDON-Users",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "EmailSettings": {
    "SmtpHost": "SET_IN_DEVELOPMENT_SECRETS",
    "SmtpPort": 587,
    "SmtpUser": "SET_IN_DEVELOPMENT_SECRETS",
    "SmtpPassword": "SET_IN_DEVELOPMENT_SECRETS",
    "FromName": "SHIELDON Platform",
    "FromEmail": "noreply@shieldon.com"
  }
}
```

**Alternative: .NET User Secrets (for development):**
```bash
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet user-secrets set "EmailSettings:SmtpPassword" "your-smtp-password"
```

### .gitignore Critical Entries

```gitignore
# SECRETS — NEVER COMMIT
appsettings.Development.json
appsettings.Production.json
secrets.json
.env
.env.local
.env.development
```

### GitHub Push Pre-Check

Before every push:
```bash
git ls-files | grep appsettings.Development
# Must return NOTHING. If it shows the file:
git rm --cached backend/SHIELDON.API/appsettings.Development.json
```

### Team Member Setup

Create `docs/SECRETS_TEMPLATE.md` (safe to commit):
```markdown
# Required Secrets Setup Guide

## Backend (appsettings.Development.json)
- ConnectionStrings:DefaultConnection — your local SQL Server connection string
- JwtSettings:SecretKey — shared team secret (ask team lead, share via WhatsApp/Notion)
- EmailSettings:SmtpUser — Gmail address
- EmailSettings:SmtpPassword — Gmail App Password (NOT your real Gmail password)
  How to get: Google Account → Security → 2FA → App Passwords
```

### Frontend: Angular Environment Files

```typescript
// src/environments/environment.ts — safe to commit (no secrets)
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:7001/api',
  appName: 'SHIELDON',
  appSlogan: 'Integrity You Can Trust'
};
```
**Never put API keys in Angular environment files.** If a frontend API key is needed, proxy through the backend.

---

## 25. GLOBAL LOADING EXPERIENCE

> Every API call, route change, and heavy operation shows a loading indicator.
> Loading must feel polished: no flicker, no UI blocking, smooth transitions.

### Loading Types

| Type | When Used |
|------|----------|
| **Top Progress Bar** (3px slim) | Route changes + all API calls (global) |
| **Skeleton Screens** | Initial data load for lists and cards |
| **Button Inline Spinner** | Form submit button loading state |
| **Overlay Spinner** | File upload, heavy operations |

### `LoadingService` (core/services/)

```typescript
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private loadingCount = signal(0);
  private readonly MIN_DISPLAY_MS = 300; // Prevent flicker
  private hideTimeout: ReturnType<typeof setTimeout> | null = null;
  readonly isLoading = signal(false);

  startLoading(): void {
    this.loadingCount.update(count => count + 1);
    if (this.hideTimeout) { clearTimeout(this.hideTimeout); this.hideTimeout = null; }
    this.isLoading.set(true);
  }

  stopLoading(): void {
    this.loadingCount.update(count => Math.max(0, count - 1));
    if (this.loadingCount() === 0) {
      this.hideTimeout = setTimeout(() => this.isLoading.set(false), this.MIN_DISPLAY_MS);
    }
  }
}
```

### `LoadingInterceptor` (core/interceptors/)

```typescript
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);
  if (req.url.includes('/heartbeat')) return next(req); // Skip heartbeat
  loadingService.startLoading();
  return next(req).pipe(finalize(() => loadingService.stopLoading()));
};
```

### `GlobalProgressBarComponent`

A 3px slim bar at the very top of the page, gradient from `--color-primary-blue` to `--color-primary-teal`, z-index `--z-loading-bar` (800). Appears when `isLoading = true`, fades out when false. Shimmer effect on the leading edge.

```scss
.global-progress-bar {
  position: fixed;
  top: 0; left: 0;
  height: var(--loading-bar-height);
  background: linear-gradient(90deg, var(--color-primary-blue), var(--color-primary-teal));
  z-index: var(--z-loading-bar);
  border-radius: 0 var(--radius-full) var(--radius-full) 0;
  transition: width var(--transition-base), opacity var(--transition-fast);
}
```

### Route Change Loading

```typescript
// In AppComponent or layout
this.router.events.subscribe(event => {
  if (event instanceof NavigationStart) this.loadingService.startLoading();
  if (event instanceof NavigationEnd
   || event instanceof NavigationCancel
   || event instanceof NavigationError) this.loadingService.stopLoading();
});
```

### Skeleton Screens

```scss
@keyframes skeleton-shimmer {
  0%   { background-position: -400px 0; }
  100% { background-position: 400px 0; }
}
.skeleton {
  background: linear-gradient(90deg, var(--skeleton-base-color) 25%, var(--skeleton-shine-color) 50%, var(--skeleton-base-color) 75%);
  background-size: 800px 100%;
  animation: skeleton-shimmer 1.5s ease-in-out infinite;
  border-radius: var(--radius-md);
}
.skeleton-text   { height: 16px; margin-bottom: 8px; }
.skeleton-title  { height: 24px; width: 60%; }
.skeleton-card   { height: 120px; border-radius: var(--radius-lg); }
.skeleton-avatar { width: 48px; height: 48px; border-radius: var(--radius-full); }
```

### Loading States Per Page Type

- **List Pages:** Show 3–5 skeleton cards → replace with content → show empty-state if empty
- **Detail Pages:** Show skeleton matching page structure
- **Form Submit:** Button shows inline spinner + "Saving..." + disabled state
- **File Upload:** Progress overlay with percentage

### Error & Timeout Handling

```typescript
this.http.get('/api/courses').pipe(
  timeout(30000),
  catchError(err => {
    if (err instanceof TimeoutError) {
      this.toastr.error('Request timed out. Please check your connection.', 'Timeout');
    }
    throw err;
  })
);
```

If initial page-load data fails: show error state component with a "Try Again" button. Never leave the user staring at a skeleton that never resolves.

---

## 26. LANDING PAGE SPECIFICATION

> The first page every visitor sees. Must be professional and guide to login.

### Route: `/` (root, public, no auth required)

Uses `PublicLayoutComponent` (horizontal navbar).

### Page Structure

```
[Horizontal Navbar — fixed top]
  Logo (left) | Home · Features · About (nav links) | Login button (right CTA)

[Hero Section — full-width gradient background]
  Left: Headline + subheadline + CTA buttons
  Right: Hero illustration (SVG)

[Stats Bar — 3 key stats]
  "21 Features" | "3 User Roles" | "Browser-Native Security"

[Features Section — 3 cards]
  LMS Management | Secure Exam Delivery | Real-Time Monitoring

[How It Works — 3 steps]
  1. Enroll in courses  2. Take exams securely  3. Review results

[Why SHIELDON — comparison vs traditional LMS]

[CTA Section]
  "Ready to take exams with integrity?"  → Login button

[Footer]
  Logo + slogan + copyright
```

### Hero Section Content

```
Background: gradient --color-deep-ocean-blue → --color-primary-teal
Headline: "Secure Learning, Trusted Exams"
Subheadline: "SHIELDON is an integrated LMS with a built-in Anti-Cheating Engine —
              no external software needed."
CTA Primary: "Get Started" → /auth/login
CTA Secondary: "Learn More" → scrolls to #features
```

### Features Cards

```
Card 1: BookOpen icon | "Complete LMS" | Courses, materials, announcements
Card 2: Shield icon   | "Built-in Anti-Cheating" | No external software required
Card 3: BarChart2 icon| "Real-Time Monitoring" | Session timelines, violation logs
```

### Navbar Links (Smooth Scroll)

- **SHIELDON Logo** → scrolls to top
- **Home** → scrolls to #hero
- **Features** → scrolls to #features
- **About** → scrolls to #why-shieldon
- **Login** button → navigates to `/auth/login`

### Animations

- Hero section: fade-in + slide-up on load
- Feature cards: staggered fade-in on scroll (Intersection Observer)
- Stats: count-up animation when scrolled into view
- CTA button: subtle pulse animation

Always include:
```scss
@media (prefers-reduced-motion: reduce) {
  * { animation: none !important; transition: none !important; }
}
```

### Logo Usage on Landing Page

```html
<!-- In landing page hero -->
<img src="assets/images/logo.png" alt="SHIELDON — Integrity You Can Trust"
     class="landing-logo" loading="eager" (error)="onLogoError($event)" />
<!-- In navbar -->
<img src="assets/images/logo-horizontal.png" alt="SHIELDON"
     class="navbar-logo" loading="eager" />
```

---

## 27. IMAGE HANDLING STRATEGY

> All images must display correctly or show a graceful fallback. Never show a broken image icon.

### Image Categories

| Category | Source | Fallback |
|----------|--------|---------|
| Logo | `assets/images/logo*.png` (local) | Text "SHIELDON" in brand colors |
| User Profile Pictures | Backend URL: `{apiBaseUrl}/uploads/profile-pictures/{filename}` | `assets/images/placeholders/avatar-placeholder.svg` |
| Course Images | Backend URL or none | `assets/images/placeholders/course-placeholder.svg` |
| Illustrations | `assets/images/illustrations/*.svg` (local SVG) | N/A — SVGs always render |
| Material Files | File-type icon from Lucide (no thumbnails) | N/A |

### `ImageWithFallbackComponent`

```typescript
@Component({
  selector: 'app-image-with-fallback',
  standalone: true,
  template: `<img [src]="imageSrc" [alt]="alt" [class]="cssClass"
                  (error)="handleImageError()" (load)="handleImageLoad()" />`
})
export class ImageWithFallbackComponent implements OnInit {
  @Input() src!: string;
  @Input() fallbackSrc = 'assets/images/placeholders/avatar-placeholder.svg';
  @Input() alt = '';
  @Input() cssClass = '';

  imageSrc = '';

  ngOnInit(): void { this.imageSrc = this.src || this.fallbackSrc; }

  handleImageError(): void {
    if (this.imageSrc !== this.fallbackSrc) this.imageSrc = this.fallbackSrc;
  }
  handleImageLoad(): void { /* optional: remove skeleton */ }
}
```

### Placeholder SVGs

**`avatar-placeholder.svg`** — Generic person silhouette in `#EDF0F1` background with `#B2B9BC` silhouette.

**`course-placeholder.svg`** — Book/course icon in `#EDF0F1` background with `#DADEE0` lines.

Create these SVG files at `src/assets/images/placeholders/`.

### Backend: Secure File Serving

Files are NEVER served directly from disk. Always through a controller:
```csharp
[HttpGet("uploads/{category}/{filename}")]
public async Task<IActionResult> ServeFile(string category, string filename)
{
    // 1. Validate requesting user has permission
    // 2. Construct safe file path (prevent path traversal: ../../../etc/passwd)
    // 3. Return file with correct Content-Type
}
```

### Email Template Images

Embed logo as **base64 inline** in HTML emails (email clients block external image URLs):
```html
<img src="data:image/png;base64,{base64LogoBytes}" alt="SHIELDON" width="160" />
```

### Logo File Assets

- `src/assets/images/logo.png` — Full logo (hero/landing/sidebar)
- `src/assets/images/logo-horizontal.png` — Navbar version (icon + text, no slogan)
- `src/assets/images/logo-icon.png` — Icon only (favicon)
- `src/favicon.ico` — Generated from logo-icon.png

---

## 28. DEVICE DETECTION & EXAM SCREEN GUARD

> Exam pages are ONLY supported on desktop/laptop (min-width: 1024px).
> Mobile and tablet users attempting to take exams must be blocked with a clear message.

### `DeviceGuardService` (core/services/)

```typescript
@Injectable({ providedIn: 'root' })
export class DeviceGuardService {
  private readonly MIN_EXAM_WIDTH = 1024;

  isScreenSupportedForExam(): boolean {
    return window.innerWidth >= this.MIN_EXAM_WIDTH;
  }

  isMobileOrTablet(): boolean {
    const mobileRegex = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i;
    return mobileRegex.test(navigator.userAgent) || window.screen.width < this.MIN_EXAM_WIDTH;
  }

  getUnsupportedReason(): string {
    if (window.innerWidth < this.MIN_EXAM_WIDTH)
      return `Your screen width (${window.innerWidth}px) is too small. Minimum: ${this.MIN_EXAM_WIDTH}px.`;
    if (this.isMobileOrTablet())
      return 'Mobile phones and tablets are not supported for exam-taking.';
    return 'Your device is not supported for this exam.';
  }
}
```

### `ExamScreenGuard` (core/guards/)

```typescript
export const examScreenGuard: CanActivateFn = (route, state) => {
  const deviceGuard = inject(DeviceGuardService);
  const router = inject(Router);
  if (!deviceGuard.isScreenSupportedForExam()) {
    router.navigate(['/exam-device-error'], {
      queryParams: { reason: deviceGuard.getUnsupportedReason() }
    });
    return false;
  }
  return true;
};

// Apply to exam routes:
{ path: 'exams/:examId/take', component: ExamTakingComponent,
  canActivate: [authGuard, examScreenGuard] }
```

### `ExamDeviceErrorComponent` (route: `/exam-device-error`)

Display:
- Large shield+X icon (Lucide)
- Heading: "Exam Not Available on This Device"
- Message: "SHIELDON exams require a desktop or laptop computer with minimum 1024px screen width to ensure exam integrity. [Reason from query param]"
- "Please switch to a desktop or laptop to take this exam. Your exam access remains available until the exam window closes."
- "Go Back" button

### Runtime Screen Resize During Exam

If window is resized below 1024px during an active exam:
- Caught by WindowResize detection in Stage 4.5 (anti-cheat)
- Show non-dismissible overlay: "Your screen is too small for exam integrity monitoring. Please maximize your window to continue."
- Violation logged; timer continues; overlay blocks interaction until window restored

---

## 29. LOAD BALANCING & PERFORMANCE PLANNING

### Stateless Design (Load-Balancer Ready)

SHIELDON is designed stateless — no in-memory session state per user:
- All user identity is in the **JWT token** (sent with every request)
- Exam sessions are in the **database** (not server memory)
- Any server in a cluster can handle any user's request without coordination

For graduation defense: *"SHIELDON uses stateless JWT authentication so any server instance can handle any request. Horizontal scaling behind a load balancer is possible without code changes since no user state is stored in server memory."*

### Rate Limiting (.NET 7+ Built-in)

```csharp
// Program.cs
builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("login", o => {
        o.PermitLimit = 10; o.Window = TimeSpan.FromMinutes(1); o.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("api", o => {
        o.PermitLimit = 100; o.Window = TimeSpan.FromMinutes(1); o.QueueLimit = 5;
    });
    options.AddFixedWindowLimiter("violations", o => {
        o.PermitLimit = 30; o.Window = TimeSpan.FromMinutes(1); o.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

// Apply to controllers:
[EnableRateLimiting("login")]
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request) { ... }
```

### Database Connection Resilience

```csharp
options.UseSqlServer(connectionString, sqlOptions => {
    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
    sqlOptions.CommandTimeout(30);
});
```

### Memory Caching & Response Compression

```csharp
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });
app.UseResponseCompression(); // Before UseRouting
```

### Query Optimization Rules

```csharp
// ALWAYS: AsNoTracking() for read-only queries
var courses = await _context.Courses.AsNoTracking().Where(c => c.IsActive).ToListAsync();

// ALWAYS: Pagination on all list endpoints
var paged = await _context.Courses.AsNoTracking().Skip((page-1) * size).Take(size).ToListAsync();

// ALWAYS: Project only needed fields
var names = await _context.Courses.AsNoTracking().Select(c => new { c.Id, c.Title }).ToListAsync();
```

---

## 30. EDGE CASES & WORKFLOW SCENARIOS

### Authentication Edge Cases

| Scenario | Expected Behavior |
|----------|-----------------|
| Email doesn't exist | "Invalid credentials" (do NOT reveal) |
| Wrong password, 5th attempt | Lock account + send email |
| Login on Locked account | "Account locked. Reset password to unlock." |
| Login on Disabled account | "Account disabled. Contact support." |
| Login on Unverified account | "Please verify email first." |
| JWT expired (15 min) | Frontend interceptor auto-calls refresh-token |
| Refresh token expired (7 days) | Clear tokens → redirect to login |
| Login on two devices | New login invalidates previous refresh token |
| Password reset on locked account | Resets password AND unlocks account |
| Reset link used twice | "Link already used" |
| Reset link expired | "Link expired. Request a new one." |
| Change email while verification pending | Cancel old verification, send new one |

### Course & Enrollment Edge Cases

| Scenario | Expected Behavior |
|----------|-----------------|
| Submit enrollment while request pending | "You already have a pending request" |
| Already enrolled tries to request | "You are already enrolled" |
| Request during 24h cooldown | "Please wait {X} hours" |
| Admin deletes course with students | Soft-delete, preserve all historical data |
| Admin reassigns tutor during exam | Effective after exam ends |
| Tutor accesses another tutor's course | 403 Forbidden |
| Bulk approve with some already processed | Process only pending ones |

### Exam Edge Cases

| Scenario | Expected Behavior |
|----------|-----------------|
| Open exam exactly at end time | "Exam has ended" — blocked |
| Timer expires on question 3 | Auto-submit all saved answers |
| Refresh during exam | Restore from last auto-save |
| Network disconnects | Timer continues server-side; restore on reconnect |
| Reconnect after exam expires | "Time expired. Answers submitted." |
| Open exam in two tabs | Return existing session (no duplicate) |
| Mobile tries to take exam | ExamScreenGuard blocks; device error page shown |
| Zero questions available | "Exam not ready: No questions available" |
| Fewer questions than exam requires | Use all available questions |
| Auto-grading fails | Log error; mark as "PendingGrade"; alert admin |

### Anti-Cheat Edge Cases

| Scenario | Expected Behavior |
|----------|-----------------|
| Fullscreen denied by browser | Block exam; "Please allow full-screen" |
| F11 key pressed (OS toggle) | Same as fullscreenchange — violation logged |
| Rapid violation flood (same type, 3s) | Cooldown discards duplicates |
| First violation: student immediately returns | Warning logged but count stays 0 |
| Force-submit and manual submit race | First one wins; second ignored |
| Browser crash | Background service auto-submits on expiry |
| Screen reader / accessibility tools | Do NOT block — no cheating indicator |
| Mac user presses Cmd instead of Ctrl | Handle both ctrlKey and metaKey |
| Developer tools via browser menu | Cannot prevent; blur event logs it |

### File Upload Edge Cases

| Scenario | Expected Behavior |
|----------|-----------------|
| File > 20MB | "File size exceeds 20MB limit" |
| Wrong MIME type | "Invalid file type" |
| Same filename uploaded twice | Store as `{courseId}/{timestamp}_{filename}` |
| Student downloads without enrollment | 403 Forbidden |
| File deleted from disk, DB record exists | 404 with helpful message; log orphan |

### Notification Edge Cases

| Scenario | Expected Behavior |
|----------|-----------------|
| Exam reminder after student submitted | Don't send — check submission first |
| Student unenrolled, notifications remain | Show "Course no longer available" on link |
| Important announcement created + updated < 5min | Both send immediately (no aggregation) |

---

## 31. UNIT TESTING & INTEGRATION TESTING GUIDE

> The developer is new to testing. This section explains everything from scratch.

### What Is Testing? (Beginner Explanation)

**Unit Testing** = Testing ONE small piece of code in complete isolation.
- Example: Testing the function `calculateScore(answers, questions)` returns the right number.
- You isolate this function from everything else (no database, no network).
- Tool: **xUnit** (C#) and **Jest** (TypeScript/Angular)

**Integration Testing** = Testing how multiple parts work TOGETHER.
- Example: Send a real HTTP POST to `/api/auth/login` and verify the response contains a JWT token.
- No mocking — uses a real (in-memory) version of the API.
- Tool: **WebApplicationFactory** (.NET)

**Why test?** (For graduation defense)
1. Catch bugs early before users find them
2. Document exactly what the code is supposed to do
3. Prove technical maturity in the graduation review

**How much?** A focused set — 3–5 unit tests per major service + 2–3 integration tests per feature.

### Backend Unit Tests (xUnit)

```csharp
// SHIELDON.Tests/Unit/Auth/LoginServiceTests.cs
public class LoginServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly LoginService _loginService;

    public LoginServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _loginService = new LoginService(_userRepositoryMock.Object, _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // ARRANGE — Set up the scenario
        var fakeUser = new User {
            Email = "ahmed@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            AccountStatus = AccountStatus.Active,
            Role = UserRole.Student
        };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("ahmed@test.com")).ReturnsAsync(fakeUser);
        _tokenServiceMock.Setup(s => s.GenerateAccessToken(It.IsAny<User>())).Returns("fake.jwt.token");

        // ACT — Call the method
        var result = await _loginService.LoginAsync(new LoginRequest { Email = "ahmed@test.com", Password = "Test@1234" });

        // ASSERT — Check the result
        result.IsSuccess.Should().BeTrue("valid credentials should produce successful login");
        result.Data.AccessToken.Should().NotBeNullOrEmpty("successful login should return access token");
    }

    [Fact]
    public async Task Login_WithLockedAccount_ShouldReturnLockedError()
    {
        var lockedUser = new User { Email = "locked@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            AccountStatus = AccountStatus.Locked };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("locked@test.com")).ReturnsAsync(lockedUser);

        var result = await _loginService.LoginAsync(new LoginRequest { Email = "locked@test.com", Password = "Test@1234" });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("locked");
    }
}
```

```csharp
// SHIELDON.Tests/Unit/Exams/ExamGradingServiceTests.cs
public class ExamGradingServiceTests
{
    private readonly ExamGradingService _gradingService = new ExamGradingService();

    [Fact]
    public void GradeAnswers_WhenAllCorrect_ShouldReturnFullMarks()
    {
        var questionId = Guid.NewGuid();
        var correctOptionId = Guid.NewGuid();
        var questions = new List<Question> {
            new Question { Id = questionId, Marks = 5,
              Options = new List<QuestionOption> {
                  new QuestionOption { Id = correctOptionId, IsCorrect = true },
                  new QuestionOption { Id = Guid.NewGuid(), IsCorrect = false }
              }}
        };
        var answers = new List<StudentAnswer> {
            new StudentAnswer { QuestionId = questionId, SelectedOptionId = correctOptionId }
        };

        var result = _gradingService.CalculateScore(questions, answers);

        result.ScoreObtained.Should().Be(5);
        result.Percentage.Should().Be(100);
    }

    [Fact]
    public void GradeAnswers_WhenNoAnswers_ShouldReturnZeroScore()
    {
        var questions = new List<Question> { new Question { Marks = 10, Options = new() } };
        var result = _gradingService.CalculateScore(questions, new List<StudentAnswer>());
        result.ScoreObtained.Should().Be(0);
        result.Percentage.Should().Be(0);
    }
}
```

### Backend Integration Tests (WebApplicationFactory)

```csharp
// SHIELDON.Tests/Integration/Auth/LoginIntegrationTests.cs
public class LoginIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LoginIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(); // Real API, in-memory
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        var loginRequest = new { email = "admin@shieldon.com", password = "Admin@Test123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        body!.Success.Should().BeTrue();
        body.Data!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithEmptyEmail_Returns400WithValidationError()
    {
        var loginRequest = new { email = "", password = "SomePass@1" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body!.Errors.Should().NotBeEmpty();
    }
}
```

### Frontend Unit Tests (Jest)

```typescript
// login.component.spec.ts
describe('LoginComponent', () => {
  let component: LoginComponent;
  let authServiceMock: jest.Mocked<AuthService>;
  let routerMock: jest.Mocked<Router>;

  beforeEach(async () => {
    authServiceMock = { login: jest.fn() } as any;
    routerMock = { navigate: jest.fn() } as any;

    await TestBed.configureTestingModule({
      imports: [LoginComponent, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => { expect(component).toBeTruthy(); });
  it('should have invalid form when empty', () => { expect(component.loginForm.valid).toBeFalsy(); });
  it('should be valid with correct input', () => {
    component.loginForm.patchValue({ email: 'test@test.com', password: 'Test@1234' });
    expect(component.loginForm.valid).toBeTruthy();
  });
  it('should navigate to student dashboard on success', () => {
    authServiceMock.login.mockReturnValue(of({ success: true, data: { role: 'Student', accessToken: 'token', refreshToken: 'refresh' } }));
    component.loginForm.patchValue({ email: 'student@test.com', password: 'Test@1234' });
    component.onSubmit();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/student/dashboard']);
  });
});
```

### How to Run Tests

```bash
# Backend (from /backend)
dotnet test
dotnet test --logger "console;verbosity=detailed"

# Frontend (from /frontend)
ng test --no-watch
ng test --code-coverage
```

### Graduation Defense Statement

> "We implemented a focused testing strategy covering the most critical business logic.
> For the backend, we used xUnit with Moq and FluentAssertions for unit tests covering
> authentication (valid login, locked accounts), exam grading (full marks, zero score),
> and violation threshold logic. For integration testing, we used WebApplicationFactory
> to test the login endpoint end-to-end with real HTTP requests.
> For the frontend, we used Jest to test the login component form validation and routing behavior.
> While we didn't target 100% coverage, the tests validate the core rules that must not break."

---

## 32. IMPLEMENTATION GAP ANALYSIS & COMPLETENESS CHECK

### Confirmed Complete

- [x] All 21 features specified with detailed requirements
- [x] Landing page specification (Section 26)
- [x] Authentication with all edge cases
- [x] Anti-Cheating Engine fully restructured (10 stages)
- [x] Complete database schema (Section 30 of original + 20 of v2)
- [x] Full API endpoint reference
- [x] Clean Architecture with folder structures
- [x] Design system with full color palette
- [x] Git strategy with .gitignore and secrets management
- [x] Global loading experience (Section 25)
- [x] Image handling with fallbacks (Section 27)
- [x] Device detection for exams (Section 28)
- [x] Rate limiting and performance (Section 29)
- [x] Testing guide — beginner level (Section 31)
- [x] Stage confirmation protocol
- [x] Sensitive data security (Section 24)

### Items Requiring Attention During Development

1. **Admin User Seeding:** First Admin cannot register publicly. Seed in EF Core `OnModelCreating()` with credentials stored in `appsettings.Development.json` (gitignored).

2. **Email Provider:** Use Mailtrap.io for development (free sandbox, no real emails sent). Switch to real SMTP for production. Document in `SECRETS_TEMPLATE.md`.

3. **File Storage:** Currently local disk (`wwwroot/uploads/`). Document as known improvement area for production (Azure Blob / AWS S3).

4. **Background Service Registration:** `ExamAutoSubmitService` and `ReminderEmailService` must be registered as `IHostedService` in `Program.cs`. Handle startup gracefully.

5. **CORS Configuration:** Allow `http://localhost:4200` in development. Never use `AllowAnyOrigin()` in production. Update for real frontend URL.

6. **Profile Picture in Login Response:** Include `profilePictureUrl` in login response so navbar can immediately show the user's avatar without a second API call.

7. **Exam Timer Drift Prevention:** Browser `setInterval` drifts under CPU load. Always recalculate remaining time from `GET /api/sessions/{token}` on reconnect, never rely solely on browser ticks.

8. **Pagination Required Everywhere:** Every list endpoint MUST support pagination. Default page size: 20. Never return all records unbounded.

9. **Announcement "Edited" Indicator:** If `UpdatedAt ≠ null`, display "Edited X minutes ago" in the UI.

10. **JWT Token on Account Disable:** If admin disables a user, their JWT is valid for up to 15 minutes. For graduation: acceptable. Document as known limitation.

### Additional Stages (From Gap Analysis)

| Stage | When | What |
|-------|------|------|
| Stage 0.6 | After 0.5 | Landing page |
| Stage 0.7 | After 0.6 | Global loading system |
| Stage 0.8 | After 0.7 | Device guard + placeholder assets |
| Stage T.1 | After Phase 3 | Backend unit tests |
| Stage T.2 | After T.1 | Backend integration tests |
| Stage T.3 | After T.2 | Frontend unit tests |


---

*End of CLAUDE.md — SHIELDON Project Master Reference*
*Version: 3.0.0 — Complete with Landing Page, Global Loading, Image Handling, Device Guard,*
*Load Balancing, Edge Cases, Unit/Integration Testing, Secrets Security, Gap Analysis*
*Read this file completely before any implementation*
