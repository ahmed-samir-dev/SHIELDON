# Changelog

All notable changes to the SHIELDON project will be documented in this file.

## [v1.1.0-phone-verification] - 2026-08-04

### Added - WhatsApp OTP Microservice & Phone Verification
- **WhatsApp Gateway Microservice**: Standalone Node.js microservice (`@whiskeysockets/baileys`) running on port 3001 for zero-cost WhatsApp OTP message delivery.
- **Phone Verification Flow**: 6-digit WhatsApp OTP validation with 2-minute resend cooldown, Egyptian phone number normalization (`010`/`011`/`012`/`015` → `+20`), auto-advancing 6-cell OTP input modal in Angular, and 1-phone-per-account uniqueness enforcement in EF Core.
- **Google OAuth 2.0 Integration**: Passwordless social sign-in and registration via Google account.
- **Live Real-Time Leaderboard**: Course Top-10 ranking with SignalR real-time updates, neon podium (Gold/Silver/Bronze), delta rank badges, dense-rank tie handling, and student own-rank card.
- **Red Flag Question Bookmarking**: Students can mark questions with a Red Flag in the Exam Engine for later review. State is persisted to SQL Server database, displayed in the Question Navigator with filter tabs (All/Answered/Unanswered/Flagged), and summarized before final submission with full Light & Dark theme support.

## [v1.0.0-graduation] - 2026-05-24

### Added - Final Core Infrastructure
- **SHIELDON AI Assistant**: Integrated Gemini Pro API through the backend, enabling contextual chat and support directly within the application without exposing API keys.
- **Real-Time Chat & Messaging**: SignalR-powered many-to-many messaging, contact lists, online presence tracking, and typing indicators.
- **Dynamic QR Attendance System**: 7-second rotating TOTP QR codes for secure, proximity-based student attendance tracking by Tutors.
- **Premium Themes & i18n**: System-wide Dark/Light mode engine and full English/Arabic (RTL) internationalization utilizing Lingva.
- **Shepherd.js Onboarding**: Guided, role-based interactive UI tours for first-time login users.

### Added - E-Commerce & Financials
- **Stripe Integration**: Online course payments via Stripe Checkout.
- **Payment History**: User payment history panels and webhook-driven automatic enrollment approval upon successful payment.

### Added - Core Learning Management (LMS)
- **Role-Based Authentication**: Secure JWT flow separating Admin, Tutor, and Student permissions.
- **Email Systems**: SMTP-backed email verification and password reset functionality.
- **Course Management**: Complete CRUD, materials uploading (file sharing), and student enrollments.
- **Assignments**: Task lifecycle with due dates and bulk ZIP export capabilities.
- **Notifications**: Real-time in-app alerts and database-persisted notifications.
- **Calendar & Schedule View**: Interactive events, deadlines, and schedule visualization.

### Added - Advanced Evaluation & Anti-Cheat Engine
- **Exam Management**: Question banks, exam publishing, timer tracking, and automated grading schemas.
- **Anti-Cheating Engine**: Native browser-based integrity system enforcing limits on:
  - Copy/Paste (Clipboard blocking)
  - Window Blur / Lose Focus (Visibility API)
  - Fullscreen Exit / Resize
  - Tab Switching
  - Mouse movement anomalies
  - Right-click Context Menus
- **Live Monitoring Dashboard**: Admin/Tutor socket-driven dashboards showing real-time student presence and violation timeline telemetry.
- **Re-Attempt System**: Workflow for students to request exam resets, subject to Tutor approval.

### Added - Updates & Enhancements
- **Enhanced Dashboards**: Separated Top Violation Types and Violations by Course charts (with exact percentages).
- **Data Export**: Complete CSV export functionality for comprehensive raw metrics instead of just screenshots.
- **Score Normalization**: System-wide synchronization of Strike Severities (Minor=0.5, Medium=1.0, Critical=1.0) with an elegant horizontal progress bar UI.
- **Presence Tracking**: Restored `PresenceLog` and `HeartbeatMonitorBackgroundService` for real-time tracking.
- **UI Stabilizations**: Fixed Angular Change Detection freezes caused by `lucide-angular` and ensured zero-latency widget rendering via SignalR `NgZone`.

### Changed
- Converted the entire frontend to use Angular 21 Standalone Components.
- Upgraded the backend from legacy paradigms to .NET 9 Clean Architecture.
- Migrated all reporting charts to Apache ECharts for superior performance.

### Security
- Protected all configuration secrets via `appsettings.json` ignored by Git.
- Ensured EF Core queries prevent SQL Injection natively.
- Enforced single-session validation for exams.
