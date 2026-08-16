# SHIELDON - QA Testing Guide

Welcome to the **SHIELDON QA Testing Documentation**. This guide outlines our testing philosophy, framework architecture, test execution instructions, and complete inventory of unit, security, integration, performance load, microservice, and frontend specs across the entire SHIELDON platform.

---

## 📑 Table of Contents

- [🎯 Testing Philosophy](#-testing-philosophy)
- [📊 Executive Suite Statistics](#-executive-suite-statistics)
- [🔐 Backend Testing Framework (.NET 9)](#-backend-testing-framework-net-9)
  - [Unit Testing Architecture](#unit-testing-architecture)
  - [Security Testing Architecture](#security-testing-architecture)
  - [Integration Testing Architecture (`WebApplicationFactory`)](#integration-testing-architecture-webapplicationfactory)
  - [Performance & Load Testing (NBomber)](#performance--load-testing-nbomber)
  - [Running Backend Tests](#running-backend-tests)
- [🎨 Frontend Testing Framework (Angular 21 + Vitest)](#-frontend-testing-framework-angular-21--vitest)
  - [Vertical Slice Co-location](#vertical-slice-co-location)
  - [Frontend Security Specs & Interceptors](#frontend-security-specs--interceptors)
  - [Vitest Configuration & Component Inlining](#vitest-configuration--component-inlining)
  - [Lighthouse CI Performance Configuration](#lighthouse-ci-performance-configuration)
  - [Running Frontend Tests](#running-frontend-tests)
- [📱 WhatsApp Gateway Testing (Node.js)](#-whatsapp-gateway-testing-nodejs)
- [📋 Complete Test Suite Inventory (275 Total Tests)](#-complete-test-suite-inventory-275-total-tests)
  - [Backend Unit Testing (14 Files / 75 Tests)](#backend-unit-testing-14-files--75-tests)
  - [Backend Integration Testing (10 Files / 13 Tests)](#backend-integration-testing-10-files--13-tests)
  - [Backend Security Testing (14 Files / 60 Tests)](#backend-security-testing-14-files--60-tests)
  - [Backend Performance Testing (1 File / 3 Tests)](#backend-performance-testing-1-file--3-tests)
  - [Frontend Unit Testing (26 Files / 35 Tests)](#frontend-unit-testing-26-files--35-tests)
  - [Frontend Security Testing (5 Files / 15 Tests)](#frontend-security-testing-5-files--15-tests)
  - [WhatsApp Gateway Testing (1 File / 12 Tests)](#whatsapp-gateway-testing-1-file--12-tests)
- [📈 Code Coverage & CI/CD Integration](#-code-coverage--cicd-integration)
- [📦 Dependency Audits & Secret Scanning](#-dependency-audits--secret-scanning)

---

## 🎯 Testing Philosophy

SHIELDON enforces a **100% Pass Rate Standard** across all active codebases prior to deployment. Our testing strategy covers:

1. **Clean Architecture Isolation (Backend)**: Domain logic and Application service handlers are isolated from database infrastructure using mock repositories (`Moq`) and In-Memory EF Core databases (`DbContextFixture`). API controllers are validated using `WebApplicationFactory` for true HTTP request pipeline testing.
2. **Dedicated Security Test Suite (`SHIELDON.Tests/Security/`)**: 60 specialized security tests covering Auth brute-force, JWT validation, XSS sanitization, RBAC rules, exam session token lifecycle, Anti-Cheat violation batch integrity, Stripe webhook signatures, AI proxy isolation, and HTTP security headers.
3. **High-Scale Load Testing (NBomber)**: Automated performance load scenarios injecting up to 500 Virtual Users (VU) to validate p95 latency thresholds and server throughput.
4. **Vertical Feature Slice Co-location (Frontend)**: Every component, service, directive, and guard spec file (`*.spec.ts`) is co-located right next to its implementation inside its feature folder (e.g., `src/app/features/auth/login/login.spec.ts`), keeping test context tightly bound to domain features.
5. **Microservice Isolation**: Standalone Node.js test runner suite (`node --test`) validating phone number and OTP regex rules in the WhatsApp Gateway.

---

## 📊 Executive Suite Statistics

| Category / Component                 | Test Files      | Total Executed Tests | Passed  | Failed | Pass Rate       |
| ------------------------------------ | --------------- | -------------------- | ------- | ------ | --------------- |
| 🧪 **Backend Unit Testing**          | 14 test classes | 75 tests             | 75      | 0      | **100%**        |
| 🌐 **Backend Integration Testing**   | 10 test classes | 13 tests             | 13      | 0      | **100%**        |
| 🛡️ **Backend Security Testing**      | 14 test classes | 60 tests             | 60      | 0      | **100%**        |
| ⚡ **Backend Performance Load**      | 1 test class    | 65 tests (13 scenarios × 5 VU tiers) | 65 | 0 | **100%** |
| 🎨 **Frontend Unit Testing**         | 26 spec files   | 35 tests             | 35      | 0      | **100%**        |
| 🛡️ **Frontend Security Testing**     | 5 spec files    | 15 tests             | 15      | 0      | **100%**        |
| 📱 **WhatsApp Gateway Microservice** | 1 test file     | 12 tests             | 12      | 0      | **100%**        |
| 🏆 **TOTAL COMBINED SYSTEM SUITE**   | **71 files**    | **275 tests**        | **275** | **0**  | **100% PASSED** |

---

## 🔐 Backend Testing Framework (.NET 9)

### Unit Testing Architecture

Backend unit tests target `SHIELDON.Application` services and business logic handlers.

- **Test Framework**: `xUnit` 2.8.2
- **Assertions**: `FluentAssertions`
- **Mocking**: `Moq`
- **Data Builders**: Fluent builders (`UserBuilder.cs`, `CourseBuilder.cs`, `ExamBuilder.cs`) facilitate consistent test fixture creation.

### Security Testing Architecture

Backend security tests live in `SHIELDON.Tests/Security/` and cover:

- **Auth**: Account lockout after 5 failed attempts, password hashing verification, Google OAuth input safety.
- **Courses**: Announcement XSS content sanitization via `SanitizationHelper`, material download RBAC.
- **Exams**: Token tampering, submission state immutability, result release timing guards.
- **Anti-Cheat**: Batch violation deduplication, attempt ownership validation, heartbeat idempotency.
- **Monitoring**: Tutor scope isolation for live proctoring dashboards.
- **Chat**: Participant isolation and message content sanitization.
- **Attendance**: Dynamic 30-second QR token rotation and tutor ownership check.
- **Integrations**: Stripe webhook header verification and AI proxy authentication.
- **API Hardening**: `X-Content-Type-Options: nosniff`, `X-Frame-Options`, and CORS origin restriction.

### Integration Testing Architecture (`WebApplicationFactory`)

Integration tests utilize `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>` to launch an in-memory test server executing full HTTP endpoint pipelines:

- Automatically replaces SQL Server registration with an isolated In-Memory database per test run (`CustomWebApplicationFactory.cs`).
- Tests end-to-end authorization, middleware execution, validation filter pipeline, and response status codes.

### Performance & Load Testing (NBomber)

Performance load testing is powered by **NBomber 5.5** in `PerformanceLoadTests.cs`, executing 65 tests across **13 business-critical API scenarios** and **5 Virtual User (VU) scale tiers** (100, 500, 1,000, 5,000, and 10,000 VUs):

- **Auth Login & Token Refresh**: Evaluates authentication pipeline throughput under high concurrent login and session refresh traffic.
- **Course & Exam Listings**: Tests read response times during simultaneous course catalog and exam schedule queries.
- **Anti-Cheat Flood & Heartbeat**: Validates Anti-Cheat engine resilience under rapid batch violation ingestion and live student heartbeat telemetry.
- **Leaderboard, Grades & Admin Dashboard**: Measures throughput for real-time leaderboards, gradebook audits, and cached admin KPI monitoring dashboards.
- **Profile & Assignments**: Validates steady-state profile reads and deadline-surge assignment listing requests under load.

### Running Backend Tests

Run all unit, security, integration, and performance tests:

```bash
cd backend
dotnet test bin/TestRelease/SHIELDON.Tests.dll
```

To run a specific test class:

```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

To run only the Security test suite:

```bash
dotnet test --filter "FullyQualifiedName~Security"
```

To run NBomber Performance Load tests:

```bash
dotnet test --filter "FullyQualifiedName~Performance"
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
├── login.spec.ts
└── login-security.spec.ts
```

### Frontend Security Specs & Interceptors

- `auth-interceptor.security.spec.ts`: Validates `Authorization: Bearer <token>` injection on authenticated requests, auth endpoint bypass, 401 refresh token flow, and redirect to `/login` on refresh failure.
- `login-security.spec.ts`: Validates email format rules and form control constraints.
- `anti-cheat.security.spec.ts`: Validates strike state reset and clean state initialization on exam start.
- `chat-security.spec.ts`: Validates `DOMPurify` HTML sanitization for script tags, event handlers (`onerror`), and `javascript:` links.
- `payment-security.spec.ts`: Validates URL domain validation for Stripe checkout redirects.

### Vitest Configuration & Component Inlining

Angular standalone component templates (`templateUrl`) and styles (`styleUrl`) are automatically transformed into inline strings during Vitest JIT execution via custom Vite transform plugins defined in `vitest.config.ts`. Global browser APIs (`IntersectionObserver`, `ResizeObserver`) are mocked in `src/test-setup.ts`.

### Lighthouse CI Performance Configuration

Frontend performance standards are enforced via `lighthouserc.js` located in `frontend/lighthouserc.js`:

```javascript
module.exports = {
  ci: {
    collect: {
      url: ["http://localhost:4201/", "http://localhost:4201/login"],
      numberOfRuns: 1,
    },
    assert: {
      assertions: {
        "categories:performance": ["error", { minScore: 0.9 }],
        "categories:accessibility": ["error", { minScore: 0.9 }],
        "categories:best-practices": ["error", { minScore: 0.9 }],
        "categories:seo": ["error", { minScore: 0.9 }],
      },
    },
  },
};
```

### Running Frontend Tests

Run all frontend unit and security tests:

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

## 📱 WhatsApp Gateway Testing (Node.js)

The self-hosted WhatsApp Gateway microservice uses Node.js's built-in test runner (`node --test`):

```bash
cd backend/whatsapp-gateway
npm test
```

Validates:

- E.164 international phone number format (`^\+[1-9]\d{6,14}$`).
- 6-digit numeric OTP code format (`^\d{6}$`).

---

## 📋 Complete Test Suite Inventory (275 Total Tests)

### Backend Unit Testing (14 Files / 75 Tests)

| #   | Test File                     | Module / Domain Area Covered                                            | Test Count |
| --- | ----------------------------- | ----------------------------------------------------------------------- | ---------- |
| 1   | `AuthServiceTests.cs`         | Module 1: Registration, bcrypt password hashing, login token generation | 5          |
| 2   | `ProfileServiceTests.cs`      | Module 1: Profile fetching, user identity validation                    | 2          |
| 3   | `CourseServiceTests.cs`       | Module 2: Course CRUD, unique code generation                           | 3          |
| 4   | `AnnouncementServiceTests.cs` | Module 2: Announcement priority levels, tutor RBAC                      | 3          |
| 5   | `AssignmentServiceTests.cs`   | Module 2: Assignment fetching & course validation                       | 2          |
| 6   | `MaterialServiceTests.cs`     | Module 2: Material upload limits & security checks                      | 2          |
| 7   | `ExamServiceTests.cs`         | Module 3: Exam creation & query filtering                               | 2          |
| 8   | `ExamResultServiceTests.cs`   | Module 3: Attempt result fetching & student security                    | 2          |
| 9   | `ViolationServiceTests.cs`    | Module 4: Anti-Cheat violation batch logging & DB persistence           | 2          |
| 10  | `MonitoringServiceTests.cs`   | Module 5: ProcessHeartbeat timeline aggregation                         | 2          |
| 11  | `ChatServiceTests.cs`         | Module 6: Real-Time Chat empty inbox & 1-on-1 DM creation               | 2          |
| 12  | `LeaderboardServiceTests.cs`  | Module 7: Hidden student filtering & course leaderboard                 | 2          |
| 13  | `AttendanceServiceTests.cs`   | Module 7: Dynamic 30s QR check creation & tutor controls                | 2          |
| 14  | `PaymentServiceTests.cs`      | Module 7: Stripe payment history & transaction filtering                | 2          |
| 15  | `CalendarServiceTests.cs`     | Module 7: User calendar event querying and deadline sync                | 1          |

### Backend Integration Testing (10 Files / 13 Tests)

| #   | Test File                         | Domain Area / API Pipeline                                      | Test Count |
| --- | --------------------------------- | --------------------------------------------------------------- | ---------- |
| 1   | `RegistrationIntegrationTests.cs` | Module 1: `POST /api/auth/register` API endpoint pipeline       | 2          |
| 2   | `ProfileIntegrationTests.cs`      | Module 1: `GET /api/profile` & `PUT /api/profile` API pipeline  | 2          |
| 3   | `CourseIntegrationTests.cs`       | Module 2: `GET /api/courses` authorization API pipeline         | 1          |
| 4   | `ExamIntegrationTests.cs`         | Module 3: `GET /api/courses/{id}/exams` API pipeline            | 1          |
| 5   | `ViolationIntegrationTests.cs`    | Module 4: `POST /api/violations/attempt/{id}/log` API pipeline  | 2          |
| 6   | `MonitoringIntegrationTests.cs`   | Module 5: `POST /api/monitoring/heartbeat` live proctoring API  | 2          |
| 7   | `ChatIntegrationTests.cs`         | Module 6: `GET /api/chat/conversations` API pipeline            | 1          |
| 8   | `LeaderboardIntegrationTests.cs`  | Module 7: `GET /api/leaderboard/course/{id}` API pipeline       | 1          |
| 9   | `AttendanceIntegrationTests.cs`   | Module 7: `GET /api/attendance/courses/{id}/active-session` API | 1          |
| 10  | `PaymentIntegrationTests.cs`      | Module 7: `GET /api/payments/history` Stripe API pipeline       | 1          |

### Backend Security Testing (14 Files / 60 Tests)

| #   | Test File                       | Security Domain Covered                                                                | Test Count |
| --- | ------------------------------- | -------------------------------------------------------------------------------------- | ---------- |
| 1   | `AuthSecurityTests.cs`          | Module 1: Brute-force lockout (5 attempts), token invalidation, enumeration protection | 10         |
| 2   | `GoogleOAuthSecurityTests.cs`   | Module 1: Empty/malformed token rejection, locked account SSO blocking                 | 4          |
| 3   | `CoursesRBACTests.cs`           | Module 2: Tutor course assignment RBAC, enrollment cooldown, material access checks    | 6          |
| 4   | `CourseInjectionTests.cs`       | Module 2: Announcement XSS HTML sanitization, large payload safety                     | 5          |
| 5   | `ExamTokenSecurityTests.cs`     | Module 3: Exam session token validation, answer immutability, expiry check             | 5          |
| 6   | `ExamResultAccessTests.cs`      | Module 3: Result release visibility, cross-student result isolation                    | 5          |
| 7   | `ViolationSecurityTests.cs`     | Module 4: Batch deduplication, forged attempt ID rejection, heartbeat idempotency      | 7          |
| 8   | `MonitoringRBACTests.cs`        | Module 5: Tutor live proctoring scope isolation, heartbeat student identity check      | 5          |
| 9   | `ChatAccessTests.cs`            | Module 6: Participant conversation isolation, DM canonical GUID pairing                | 6          |
| 10  | `AttendanceSecurityTests.cs`    | Module 7: Dynamic 30s QR token rotation, expired QR rejection, tutor RBAC              | 7          |
| 11  | `LeaderboardSecurityTests.cs`   | Module 7: Student hidden leaderboard filtering, admin vs tutor access controls         | 4          |
| 12  | `StripeWebhookSecurityTests.cs` | Module 7: Stripe signature header enforcement, forged signature 400 rejection          | 4          |
| 13  | `AIProxySecurityTests.cs`       | Module 7: AI Proxy unauthenticated 401 rejection, OPTIONS preflight handling           | 3          |
| 14  | `SecurityHubTests.cs`           | Module 7: Unauthenticated SignalR hub negotiation 401 rejection (all 5 hubs)           | 5          |
| ✦   | `APIHardeningTests.cs`          | API Layer: `nosniff`, `X-Frame-Options`, CORS origin wildcard guard                    | 6          |

### Backend Performance Testing (1 File / 65 Tests)

| #   | Test File                 | Load Scenario Coverage                                                 | Test Count |
| --- | ------------------------- | ---------------------------------------------------------------------- | ---------- |
| 1   | `PerformanceLoadTests.cs` | 13 API Scenarios × 5 VU Tiers (100, 500, 1,000, 5,000, 10,000 VUs)      | 65         |

### Frontend Unit Testing (26 Files / 35 Tests)

| #   | Spec File                     | Feature Slice / Component Location                                | Test Count |
| --- | ----------------------------- | ----------------------------------------------------------------- | ---------- |
| 1   | `theme.service.spec.ts`       | `src/app/core/services/` - Theme switching (`data-theme`)         | 3          |
| 2   | `language.service.spec.ts`    | `src/app/core/services/` - English/Arabic i18n (`dir="rtl"`)      | 3          |
| 3   | `exam-device.guard.spec.ts`   | `src/app/core/guards/` - Viewport width check (`< 1024px`)        | 1          |
| 4   | `auth.service.spec.ts`        | `src/app/core/services/` - Reactive signals & auth token state    | 2          |
| 5   | `login.spec.ts`               | `src/app/features/auth/login/` - Login form validation            | 4          |
| 6   | `register.spec.ts`            | `src/app/features/auth/register/` - Register form controls        | 2          |
| 7   | `verify-email.spec.ts`        | `src/app/features/auth/verify-email/` - Verify email view         | 1          |
| 8   | `landing.spec.ts`             | `src/app/features/public/landing/` - Hero view animations         | 1          |
| 9   | `mobile-blocked.spec.ts`      | `src/app/features/public/mobile-blocked/` - Blocked view          | 1          |
| 10  | `my-grades.spec.ts`           | `src/app/features/grades/my-grades/` - Student gradebook          | 1          |
| 11  | `course-grades.spec.ts`       | `src/app/features/grades/course-grades/` - Tutor gradebook        | 1          |
| 12  | `exam-result-page.spec.ts`    | `src/app/features/exams/exam-result-page/` - Student score ring   | 1          |
| 13  | `tutor-results-panel.spec.ts` | `src/app/features/exams/tutor-results-panel/` - Results panel     | 1          |
| 14  | `global-progress-bar.spec.ts` | `src/app/shared/components/global-progress-bar/` - Progress UI    | 1          |
| 15  | `public-layout.spec.ts`       | `src/app/layouts/public-layout/` - Public layout navbar           | 1          |
| 16  | `dashboard-layout.spec.ts`    | `src/app/layouts/dashboard-layout/` - Sidebar & socket            | 1          |
| 17  | `app.spec.ts`                 | `src/app/` - Root Angular App instantiation                       | 1          |
| 18  | `exam-engine.spec.ts`         | `src/app/features/courses/exam-engine/` - Exam engine view        | 1          |
| 19  | `exam-attempt.spec.ts`        | `src/app/features/courses/services/` - Exam token service         | 1          |
| 20  | `exam-result.spec.ts`         | `src/app/features/exams/services/` - Grading service              | 1          |
| 21  | `global-call-overlay.spec.ts` | `src/app/shared/components/global-call-overlay/` - WebRTC overlay | 1          |
| 22  | `password-eye.spec.ts`        | `src/app/shared/directives/` - Password visibility directive      | 1          |
| 23  | `button.spec.ts`              | `src/app/shared/components/button/` - Shared button component     | 1          |
| 24  | `card.spec.ts`                | `src/app/shared/components/card/` - Shared card component         | 1          |
| 25  | `input.spec.ts`               | `src/app/shared/components/input/` - Shared input component       | 1          |
| 26  | `spinner.spec.ts`             | `src/app/shared/components/spinner/` - Shared spinner component   | 1          |

### Frontend Security Testing (5 Files / 15 Tests)

| #   | Spec File                           | Security Domain Covered                                                     | Test Count |
| --- | ----------------------------------- | --------------------------------------------------------------------------- | ---------- |
| 1   | `auth-interceptor.security.spec.ts` | Bearer token injection, 401 refresh flow, logout redirect                   | 4          |
| 2   | `login-security.spec.ts`            | Email format rules and form control constraints                             | 3          |
| 3   | `anti-cheat.security.spec.ts`       | Strike score reset & clean state initialization on exam start               | 3          |
| 4   | `chat-security.spec.ts`             | DOMPurify HTML sanitization for XSS script tags, onerror, javascript: links | 3          |
| 5   | `payment-security.spec.ts`          | Stripe checkout URL domain & HTTPS validation                               | 2          |

### WhatsApp Gateway Testing (1 File / 12 Tests)

| #   | Test File                  | Validation Covered                                                | Test Count |
| --- | -------------------------- | ----------------------------------------------------------------- | ---------- |
| 1   | `gateway.security.test.js` | E.164 phone number regex & 6-digit numeric OTP code format checks | 12         |

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

## 📦 Dependency Audits & Secret Scanning

- **NuGet Security**: `dotnet list package --vulnerable` executed. `MailKit` updated to `4.16.0` to resolve known package advisories.
- **npm Security**: `npm audit fix` executed in `frontend/` to clean package dependencies.
- **Secret Audit**: Clean `git grep` secret audit confirming zero committed production keys or connection strings.

---

<div align="center">
  <strong>SHIELDON QA Testing Layer</strong>
</div>
