# SHIELDON - Testing Strategy & Quality Assurance Guide

Welcome to the **SHIELDON Testing & QA Documentation**. This guide outlines our testing philosophy, framework architecture, test execution instructions, and complete inventory of unit and integration tests across both backend (.NET 9) and frontend (Angular 21 + Vitest) systems.

---

## 📑 Table of Contents

- [🎯 Testing Philosophy](#-testing-philosophy)
- [📊 Executive Suite Statistics](#-executive-suite-statistics)
- [🔐 Backend Testing Framework (.NET 9)](#-backend-testing-framework-net-9)
  - [Unit Testing Architecture](#unit-testing-architecture)
  - [Integration Testing Architecture (`WebApplicationFactory`)](#integration-testing-architecture-webapplicationfactory)
  - [Running Backend Tests](#running-backend-tests)
- [🎨 Frontend Testing Framework (Angular 21 + Vitest)](#-frontend-testing-framework-angular-21--vitest)
  - [Vertical Slice Co-location](#vertical-slice-co-location)
  - [Vitest Configuration & Component Inlining](#vitest-configuration--component-inlining)
  - [Running Frontend Tests](#running-frontend-tests)
- [📋 Complete Test Suite Breakdown (50 Files / 80 Tests)](#-complete-test-suite-breakdown-50-files--80-tests)
  - [Backend Unit & Integration Tests (24 Files)](#backend-unit--integration-tests-24-files)
  - [Frontend Spec Files (26 Files)](#frontend-spec-files-26-files)
- [📈 Code Coverage & CI/CD Integration](#-code-coverage--cicd-integration)

---

## 🎯 Testing Philosophy

SHIELDON enforces a **100% Pass Rate Standard** across all active codebases prior to merging pull requests. Our testing strategy follows two core architectural principles:

1. **Clean Architecture Isolation (Backend)**: Domain logic and Application service handlers are isolated from database infrastructure using mock repositories (`Moq`) and In-Memory EF Core databases (`DbContextFixture`). API controllers are validated using `WebApplicationFactory` for true HTTP request pipeline testing.
2. **Vertical Feature Slice Co-location (Frontend)**: Every component, service, directive, and guard spec file (`*.spec.ts`) is co-located right next to its implementation inside its feature folder (e.g., `src/app/features/auth/login/login.spec.ts`), keeping test context tightly bound to domain features.

---

## 📊 Executive Suite Statistics

| Metric             | Backend (.NET 9)                    | Frontend (Angular 21) | Total Combined Suite         |
| ------------------ | ----------------------------------- | --------------------- | ---------------------------- |
| **Test Files**     | 24 files (14 Unit + 10 Integration) | 26 spec files         | **50 Test Files**            |
| **Executed Tests** | 45 tests (32 Unit + 13 Integration) | 35 tests              | **80 Executed Tests**        |
| **Pass Rate**      | **100%**                            | **100%**              | **100% PASSED (0 Failures)** |

---

## 🔐 Backend Testing Framework (.NET 9)

### Unit Testing Architecture

Backend unit tests target `SHIELDON.Application` services and business logic handlers.

- **Test Framework**: `xUnit` 2.8.2
- **Assertions**: `FluentAssertions`
- **Mocking**: `Moq`
- **Data Builders**: Fluent builders (`UserBuilder.cs`, `CourseBuilder.cs`, `ExamBuilder.cs`) facilitate consistent test fixture creation.

### Integration Testing Architecture (`WebApplicationFactory`)

Integration tests utilize `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>` to launch an in-memory test server executing full HTTP endpoint pipelines:

- Automatically replaces SQL Server registration with an isolated In-Memory database per test run (`CustomWebApplicationFactory.cs`).
- Tests end-to-end authorization, middleware execution, validation filter pipeline, and response status codes.

### Running Backend Tests

Run all unit and integration tests from the repository root or `backend` folder:

```bash
cd backend
dotnet test
```

To run a specific test class:

```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

---

## 🎨 Frontend Testing Framework (Angular 21 + Vitest)

### Vertical Slice Co-location

Angular 21 standalone components and services are tested using `Vitest` and `JSDOM`. Every feature folder contains its implementation and corresponding `.spec.ts` test:

```text
src/app/features/auth/login/
├── login.ts
├── login.html
├── login.scss
└── login.spec.ts
```

### Vitest Configuration & Component Inlining

Angular standalone component templates (`templateUrl`) and styles (`styleUrl`) are automatically transformed into inline strings during Vitest JIT execution via custom Vite transform plugins defined in `vitest.config.ts`. Global browser APIs (`IntersectionObserver`, `ResizeObserver`) are mocked in `src/test-setup.ts`.

### Running Frontend Tests

Run all frontend unit tests once:

```bash
cd frontend
npx vitest run
```

Run tests in interactive watch mode during development:

```bash
cd frontend
npx vitest watch
```

---

## 📋 Complete Test Suite Breakdown (50 Files / 80 Tests)

### Backend Unit & Integration Tests (24 Files)

| #   | Test File                         | Type            | Domain Area / Module Covered                                            | Executed Tests |
| --- | --------------------------------- | --------------- | ----------------------------------------------------------------------- | -------------- |
| 1   | `AuthServiceTests.cs`             | **Unit**        | Module 1: Registration, bcrypt password hashing, login token generation | 5              |
| 2   | `RegistrationIntegrationTests.cs` | **Integration** | Module 1: `POST /api/auth/register` API endpoint pipeline               | 2              |
| 3   | `ProfileServiceTests.cs`          | **Unit**        | Module 1: Profile fetching, user identity validation                    | 2              |
| 4   | `ProfileIntegrationTests.cs`      | **Integration** | Module 1: `GET /api/profile` & `PUT /api/profile` API pipeline          | 2              |
| 5   | `CourseServiceTests.cs`           | **Unit**        | Module 2: Course CRUD, unique code generation                           | 3              |
| 6   | `CourseIntegrationTests.cs`       | **Integration** | Module 2: `GET /api/courses` authorization API pipeline                 | 1              |
| 7   | `AnnouncementServiceTests.cs`     | **Unit**        | Module 2: Announcement priority levels, tutor RBAC                      | 3              |
| 8   | `AssignmentServiceTests.cs`       | **Unit**        | Module 2: Assignment fetching & course validation                       | 2              |
| 9   | `MaterialServiceTests.cs`         | **Unit**        | Module 2: Material upload limits & security checks                      | 2              |
| 10  | `ExamServiceTests.cs`             | **Unit**        | Module 3: Exam creation & query filtering                               | 2              |
| 11  | `ExamIntegrationTests.cs`         | **Integration** | Module 3: `GET /api/courses/{id}/exams` API pipeline                    | 1              |
| 12  | `ExamResultServiceTests.cs`       | **Unit**        | Module 3: Attempt result fetching & student security                    | 2              |
| 13  | `ViolationServiceTests.cs`        | **Unit**        | Module 4: Anti-Cheat violation batch logging & DB persistence           | 2              |
| 14  | `ViolationIntegrationTests.cs`    | **Integration** | Module 4: `POST /api/violations/attempt/{id}/log` API pipeline          | 2              |
| 15  | `MonitoringServiceTests.cs`       | **Unit**        | Module 5: ProcessHeartbeat timeline aggregation                         | 2              |
| 16  | `MonitoringIntegrationTests.cs`   | **Integration** | Module 5: `POST /api/monitoring/heartbeat` live proctoring API          | 2              |
| 17  | `ChatServiceTests.cs`             | **Unit**        | Module 6: Real-Time Chat empty inbox & 1-on-1 DM creation               | 2              |
| 18  | `ChatIntegrationTests.cs`         | **Integration** | Module 6: `GET /api/chat/conversations` API pipeline                    | 1              |
| 19  | `LeaderboardServiceTests.cs`      | **Unit**        | Module 7: Hidden student filtering & course leaderboard                 | 2              |
| 20  | `LeaderboardIntegrationTests.cs`  | **Integration** | Module 7: `GET /api/leaderboard/course/{id}` API pipeline               | 1              |
| 21  | `AttendanceServiceTests.cs`       | **Unit**        | Module 7: Dynamic 30s QR check creation & tutor controls                | 2              |
| 22  | `AttendanceIntegrationTests.cs`   | **Integration** | Module 7: `GET /api/attendance/courses/{id}/active-session` API         | 1              |
| 23  | `PaymentServiceTests.cs`          | **Unit**        | Module 7: Stripe payment history & transaction filtering                | 2              |
| 24  | `PaymentIntegrationTests.cs`      | **Integration** | Module 7: `GET /api/payments/history` Stripe API pipeline               | 1              |
| 25  | `CalendarServiceTests.cs`         | **Unit**        | Module 7: User calendar event querying and deadline sync                | 1              |

### Frontend Spec Files (26 Files)

| #   | Spec File                     | Type     | Feature Slice / Component Location                                | Executed Tests |
| --- | ----------------------------- | -------- | ----------------------------------------------------------------- | -------------- |
| 1   | `theme.service.spec.ts`       | **Unit** | `src/app/core/services/` - Theme switching (`data-theme`)         | 3              |
| 2   | `language.service.spec.ts`    | **Unit** | `src/app/core/services/` - English/Arabic i18n (`dir="rtl"`)      | 3              |
| 3   | `exam-device.guard.spec.ts`   | **Unit** | `src/app/core/guards/` - Viewport width check (`< 1024px`)        | 1              |
| 4   | `auth.service.spec.ts`        | **Unit** | `src/app/core/services/` - Reactive signals & auth token state    | 2              |
| 5   | `login.spec.ts`               | **Unit** | `src/app/features/auth/login/` - Login form validation            | 4              |
| 6   | `register.spec.ts`            | **Unit** | `src/app/features/auth/register/` - Register form controls        | 2              |
| 7   | `verify-email.spec.ts`        | **Unit** | `src/app/features/auth/verify-email/` - Verify email view         | 1              |
| 8   | `landing.spec.ts`             | **Unit** | `src/app/features/public/landing/` - Hero view animations         | 1              |
| 9   | `mobile-blocked.spec.ts`      | **Unit** | `src/app/features/public/mobile-blocked/` - Blocked view          | 1              |
| 10  | `my-grades.spec.ts`           | **Unit** | `src/app/features/grades/my-grades/` - Student gradebook          | 1              |
| 11  | `course-grades.spec.ts`       | **Unit** | `src/app/features/grades/course-grades/` - Tutor gradebook        | 1              |
| 12  | `exam-result-page.spec.ts`    | **Unit** | `src/app/features/exams/exam-result-page/` - Student score ring   | 1              |
| 13  | `tutor-results-panel.spec.ts` | **Unit** | `src/app/features/exams/tutor-results-panel/` - Results panel     | 1              |
| 14  | `global-progress-bar.spec.ts` | **Unit** | `src/app/shared/components/global-progress-bar/` - Progress UI    | 1              |
| 15  | `public-layout.spec.ts`       | **Unit** | `src/app/layouts/public-layout/` - Public layout navbar           | 1              |
| 16  | `dashboard-layout.spec.ts`    | **Unit** | `src/app/layouts/dashboard-layout/` - Sidebar & socket            | 1              |
| 17  | `app.spec.ts`                 | **Unit** | `src/app/` - Root Angular App instantiation                       | 1              |
| 18  | `exam-engine.spec.ts`         | **Unit** | `src/app/features/courses/exam-engine/` - Exam engine view        | 1              |
| 19  | `exam-attempt.spec.ts`        | **Unit** | `src/app/features/courses/services/` - Exam token service         | 1              |
| 20  | `exam-result.spec.ts`         | **Unit** | `src/app/features/exams/services/` - Grading service              | 1              |
| 21  | `global-call-overlay.spec.ts` | **Unit** | `src/app/shared/components/global-call-overlay/` - WebRTC overlay | 1              |
| 22  | `password-eye.spec.ts`        | **Unit** | `src/app/shared/directives/` - Password visibility directive      | 1              |
| 23  | `button.spec.ts`              | **Unit** | `src/app/shared/components/button/` - Shared button component     | 1              |
| 24  | `card.spec.ts`                | **Unit** | `src/app/shared/components/card/` - Shared card component         | 1              |
| 25  | `input.spec.ts`               | **Unit** | `src/app/shared/components/input/` - Shared input component       | 1              |
| 26  | `spinner.spec.ts`             | **Unit** | `src/app/shared/components/spinner/` - Shared spinner component   | 1              |

---

## 📈 Code Coverage & CI/CD Integration

### Generating Code Coverage Reports

- **Frontend (Vitest)**:

  ```bash
  cd frontend
  npx vitest --coverage
  ```

  Coverage reports are generated under `frontend/coverage/index.html`.

- **Backend (.NET 9)**:
  ```bash
  cd backend
  dotnet test --collect:"XPlat Code Coverage"
  ```
  Coverage XML files are outputted into `SHIELDON.Tests/TestResults/`.

---

<div align="center">
  <strong>SHIELDON Testing & Quality Assurance Layer</strong>
</div>
