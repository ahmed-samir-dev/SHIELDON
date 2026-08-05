# 🛡️ SHIELDON - Frontend

<div align="center">
  <img src="https://img.shields.io/badge/Angular_21-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular 21" />
  <img src="https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/SCSS-CC6699?style=for-the-badge&logo=sass&logoColor=white" alt="SCSS" />
  <img src="https://img.shields.io/badge/WebRTC-333333?style=for-the-badge&logo=webrtc&logoColor=white" alt="WebRTC" />
  <img src="https://img.shields.io/badge/Lucide_Icons-F54E00?style=for-the-badge&logo=lucide&logoColor=white" alt="Lucide Icons" />
</div>

> 🖥️ The modern SPA user interface for the **SHIELDON** Learning Management System & Anti-Cheating Engine.
> Built with **Angular 21** using Standalone Components, Signals, and browser-native APIs.

---

## 📑 Table of Contents

- [🔭 Project Overview](#-project-overview)
- [⚙️ Technology Stack](#️-technology-stack)
- [🏛️ Architecture & Folder Structure](#️-architecture--folder-structure)
- [🚀 Comprehensive Feature List (F1 – F35)](#-comprehensive-feature-list-f1--f35)
- [🛡️ Anti-Cheating Engine (Sub-Features)](#️-anti-cheating-engine--sub-features)
- [🔧 Setup & Running Locally](#-setup--running-locally)
- [🤝 Contributing & Guidelines](#-contributing--guidelines)

---

## 🔭 Project Overview

This directory contains the single-page application (SPA) frontend for **SHIELDON**. It provides a responsive, role-tailored interface for **Students**, **Tutors**, and **Administrators**.

Key frontend responsibilities:
- **LMS Workflows**: Courses, enrollment management, course materials, announcements, assignments, grades, and calendar.
- **Browser-Native Anti-Cheating Engine**: Full exam monitoring without plugins, extensions, or external software downloads.
- **Real-Time Communication**: SignalR WebSocket chat, instant typing indicators, delivery receipts, link preview auto-extraction, audio voice note recording, and peer-to-peer WebRTC video calls with a global ringtone overlay.
- **Real-Time Leaderboard**: Live Top-10 course ranking podium with SignalR updates, tie handling, and personal rank tracking.
- **Security & Single Active Session**: Real-time enforcement via `SecurityHub` with 7-second grace countdown modal on session displacement.
- **Phone Verification**: 6-cell auto-advancing WhatsApp OTP verification input.
- **Analytics & Dashboards**: Interactive ECharts visualization for course grades, tutor monitoring, and violation density.
- **Accessibility & Design System**: Dark/Light mode toggle via CSS Custom Properties, and full English / Arabic (RTL) internationalization.

---

## ⚙️ Technology Stack

| Technology | Purpose |
|---|---|
| **Angular 21** | Modern SPA framework (Standalone Components, Signals, Reactive Forms) |
| **TypeScript** | Strongly-typed application logic |
| **SCSS** | CSS custom properties (design tokens) & dark/light theme system |
| **@microsoft/signalr** | Real-time WebSocket connection (chat, presence, leaderboard, session security) |
| **WebRTC API** | Peer-to-peer 1-on-1 video call system |
| **Apache ECharts** | Charts & visual data analytics (via `ngx-echarts`) |
| **ngx-translate** | Internationalization (EN / AR with full RTL layout support) |
| **Lucide Icons** | Modern icon set |
| **SweetAlert2** | Custom dialogs & confirmation modals |
| **ngx-toastr** | Toast notification alerts |
| **Shepherd.js** | Guided interactive onboarding tours for first-time users |
| **canvas-confetti** | Result celebration visual effects |
| **Stripe.js** | Client-side payment checkout integration |

---

## 🏛️ Architecture & Folder Structure

The frontend follows a **vertical slice feature organization** for maintainability:

```
src/app/
├── core/                    ← Singletons, services, guards, and interceptors
│   ├── guards/              ← Auth, Role, and Exam guards
│   ├── interceptors/        ← JWT token injection & global HTTP error handler
│   ├── models/              ← DTOs and TypeScript models
│   └── services/            ← AuthService, ChatService, LinkPreviewService, AntiCheatService
│
├── shared/                  ← Reusable UI elements & utilities
│   ├── components/          ← Navbar, Sidebar, Global Call Overlay, Security Countdown Overlay
│   ├── directives/          ← UI helper directives
│   └── pipes/               ← Date, time, and currency pipes
│
├── layouts/                 ← Layout wrapper templates
│   ├── public-layout/       ← Top navigation bar (Landing, Login, Register)
│   └── dashboard-layout/    ← Collapsible sidebar navigation (Authenticated App)
│
└── features/                ← Feature modules
    ├── auth/                ← Login, Register, Google OAuth, Forgot/Reset Password
    ├── courses/             ← Course list, Enrollment, Materials, Announcements, Assignments, Exams
    ├── exam-engine/         ← Anti-cheat exam player, token validator, questions navigator
    ├── monitoring/          ← Tutor live monitoring, Session Timeline, Violation Density chart
    ├── grades/              ← Gradebook, weighted grade calculator, CSV export
    ├── admin/               ← System-wide admin panel, User lock/unlock management, Audit logs
    ├── chat/                ← Real-time messaging, WebRTC call modal, Voice recorder, Link preview
    ├── attendance/          ← Dynamic QR scanner & attendance log
    ├── calendar/            ← Unified schedule view
    ├── payment/             ← Stripe payment checkout & transaction history
    ├── profile/             ← Profile picture WebP upload, WhatsApp OTP verification modal
    └── public/              ← Landing page & mobile guard screen
```

---

## 🚀 Comprehensive Feature List (F1 – F35)

| # | Feature | Frontend Implementation Details |
|---|---|---|
| F1 | **Secure Login & Role Redirect** | JWT authentication token storage, auto-login, role-based routing |
| F2 | **Google OAuth 2.0 Login** | Passwordless social login via Google Account |
| F3 | **Email Verification** | Verification token verification screen |
| F4 | **Password Reset via Email** | Forgot-password request and password reset workflow |
| F5 | **Phone Verification (WhatsApp OTP)** | 6-cell auto-advancing OTP input modal with 2-minute cooldown timer |
| F6 | **Profile Management** | WebP avatar upload, change password, reset onboarding tour |
| F7 | **Public Registration** | Student vs Tutor registration selection |
| F8 | **Course Management & Enrollment** | Paginated grid, search/filter, enrollment status cards |
| F9 | **File Sharing (Course Materials)** | Material upload modal, file category badges, direct download |
| F10 | **Announcements** | Priority pinning banner and interactive course feed |
| F11 | **Assignment Management** | File submission drag-and-drop, grade feedback review |
| F12 | **Notifications** | In-app notification bell drawer with unread counter badge |
| F13 | **Exam Management** | Exam scheduling cards, publish state badges |
| F14 | **Re-Attempt & Re-Open Requests** | Student request form and tutor extension approval modal |
| F15 | **Question Bank Management** | MCQ, True/False, and Short Answer question authoring |
| F16 | **Exam Engine + Secure Token** | Countdown timer, Red Flag question bookmarking, auto-submit |
| F17 | **Exam Results & Auto-Grading** | Confetti celebration, question breakdown review |
| F18 | **Grade Management Panel** | Weighted grade calculation, CSV export button |
| F19 | **Anti-Cheating Engine** | Browser-native monitoring layer (12 sub-features) |
| F20 | **Session Timeline View** | Per-attempt vertical timeline with SignalR presence indicators |
| F21 | **Violation Density Analytics** | ECharts sticky Bubble/Scatter plot mapping severity over time |
| F22 | **Tutor Monitoring Dashboard** | Live exam violation stream and real-time monitoring grid |
| F23 | **Admin Dashboard & Sidebar** | System-wide statistics and responsive collapsible sidebar |
| F24 | **SHIELDON AI Assistant** | Gemini-powered chat widget (auto-disabled during active exams) |
| F25 | **Real-Time Chat System** | SignalR chat, WebRTC video calls, voice notes, link previews, reactions, reply/forward |
| F26 | **Shepherd.js Onboarding Tours** | Interactive step-by-step tour for new users |
| F27 | **Tutor & Global Analytics** | Interactive ECharts performance analytics |
| F28 | **Dynamic QR Attendance** | QR scanner interface and history breakdown |
| F29 | **Calendar & Schedule View** | Month/Week calendar with exams, assignments, and custom events |
| F30 | **Online Payment (Stripe)** | Stripe Checkout redirect and payment history list |
| F31 | **Dark / Light Mode** | Single-click theme toggle powered by CSS custom properties |
| F32 | **English / Arabic (i18n)** | Language switcher with full RTL document layout switching |
| F33 | **Mobile Guard** | Device detection overlay blocking mobile devices from taking exams |
| F34 | **Single Active Session Enforcement** | Real-time `SecurityHub` SignalR connection listener with 7s warning modal |
| F35 | **Live Course Leaderboard** | Real-time podium ranking with SignalR broadcasts and rank badges |

---

## 🛡️ Anti-Cheating Engine — Sub-Features

Built entirely with standard Web APIs (no extensions required):

1. **Pre-Exam Rules Acknowledgment Modal**: Terms acceptance before starting exam.
2. **Fullscreen Exit Enforcement**: Triggers critical violation on fullscreen exit.
3. **Tab Switching & Focus Loss**: Detects window blur and tab switching.
4. **Keyboard Shortcut Blocking**: Disables `Ctrl+C`, `Ctrl+V`, `F12`, `Alt+Tab`, `PrintScreen`, etc.
5. **Window Resize / Split-Screen Detection**: Monitors viewport boundary alterations.
6. **Mouse Pattern Monitoring**: Anomaly detection on rapid mouse cursor exits.
7. **Selection Blocking**: Prevents mouse text selection inside the exam player.
8. **Right-Click Blocking**: Context menu disabled.
9. **Violation Intelligence Layer**: Severity scoring & anti-flooding cooldowns.
10. **Action Debouncer & Score Normalization**: 500ms aggregation window preventing cascading violations.
11. **Warning Progress Bar & Force-Submit**: 3-strike visual progress bar ending in auto-submit.
12. **Monitoring Continuity**: Anti-cheat state persists across page refreshes.

---

## 🔧 Setup & Running Locally

> ⚠️ **Prerequisite:** The backend API must be running. See [docs/INSTALLATION.md](../docs/INSTALLATION.md) for full setup instructions.

1. **Navigate to the frontend directory**:
   ```bash
   cd frontend
   ```
2. **Install dependencies**:
   ```bash
   npm install
   ```
3. **Start the development server**:
   ```bash
   npm start
   ```
4. **Open application in browser**:
   👉 `http://localhost:4201`

---

## 🤝 Contributing & Guidelines

Please read [docs/CONTRIBUTING.md](../docs/CONTRIBUTING.md) before submitting pull requests.

---

<div align="center">
  <strong>🛡️ SHIELDON Frontend — "Integrity You Can Trust" 🛡️</strong>
</div>
