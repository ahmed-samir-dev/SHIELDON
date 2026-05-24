# 🛡️ SHIELDON - Frontend

<div align="center">
  <img src="https://img.shields.io/badge/Angular_21-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular 21" />
  <img src="https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/Sass-CC6699?style=for-the-badge&logo=sass&logoColor=white" alt="Sass" />
</div>

> 🖥️ The user interface for the SHIELDON Learning Management System & Anti-Cheating Engine.
> Built with Angular 21.

---

## 📑 Table of Contents

- [🔭 Project Overview](#-project-overview)
- [⚙️ Technology Stack](#️-technology-stack)
- [📋 Prerequisites](#-prerequisites-for-beginners)
- [🔧 Installation & Setup](#-installation--setup-step-by-step-guide)
- [🏛️ Architecture & Folder Structure](#️-architecture--folder-structure)
- [🚀 Feature List (F1 – F30)](#-comprehensive-feature-list-f1--f30)
- [🛡️ Anti-Cheating Engine](#️-anti-cheating-engine--sub-features-f17)
- [🌿 Git Workflow](#-git-workflow)

---

## 🔭 Project Overview

This directory contains the frontend application for the **SHIELDON** platform. It provides a modern, responsive, and secure user interface for students, tutors, and administrators.

The frontend is responsible for:
- 📚 Delivering the Learning Management System experience (courses, materials, assignments, exams)
- 🛡️ Enforcing the **Anti-Cheating Engine** rules natively in the browser during exams
- 📈 Displaying rich visual analytics and monitoring dashboards using Apache ECharts
- 🤖 Providing an interactive AI assistant for student support
- 💬 Real-time chat system between users
- 📱 Dynamic QR attendance scanning
- 📅 Unified calendar with exams, assignments, and custom events
- 💳 Online payment interface via Stripe Checkout
- 🌓 Dark / Light mode with CSS custom properties
- 🌍 Full English / Arabic (RTL) internationalization

---

## ⚙️ Technology Stack

| Technology | Purpose |
|---|---|
| 🅰️ **Angular 21** | Framework (Standalone Components) |
| 🔷 **TypeScript** | Language (Strict mode enabled) |
| 🎨 **SCSS** | Styling with CSS Custom Properties (Design Tokens) |
| 📊 **Apache ECharts** | Charts & analytics (via ngx-echarts) |
| 🧩 **Lucide Icons** | Icon library |
| 🍬 **SweetAlert2** | Beautiful modal dialogs |
| 🔔 **ngx-toastr** | Toast notifications |
| 🎉 **canvas-confetti** | Celebration effects (exam results) |
| 🧭 **Shepherd.js** | Guided onboarding tours |
| 🌍 **ngx-translate** | Internationalization (EN / AR with RTL) |
| 💳 **Stripe.js** | Client-side payment integration |

---

## 📋 Prerequisites (For Beginners)

If you have zero previous technical background and want to run the frontend part on your device, you need to install these tools first: 🆓

1. 🟢 **Node.js**: The environment that allows the frontend code to run.
   - Download the **LTS** version from [nodejs.org](https://nodejs.org/).
   - Follow the default installation steps.
2. 🌿 **Git**: Required to download the project files (if you haven't already).
   - Download from [git-scm.com](https://git-scm.com/).

---

## 🔧 Installation & Setup (Step-by-Step Guide)

> ⚠️ **CRITICAL REQUIREMENT:** The SHIELDON Frontend **cannot function without the Backend API**. You MUST complete the backend database setup and have the backend API running before you can log in or use the frontend. See the root [README.md](../README.md) for full database configuration steps.

Follow these steps to get the frontend running: 🚀

### 1. Ensure Backend is Running

1. ✅ Verify that your SQL Server is running.
2. ✅ Verify that the `.NET 9` backend is actively running (usually on `http://localhost:5000`).
3. 🚨 **Keep the backend terminal open** in the background.

### 2. Run the Frontend Application

1. Open a **new, completely separate** terminal window (or Command Prompt / PowerShell).
2. Navigate to the frontend directory of the project:
   ```bash
   cd path/to/SHIELDON/frontend
   ```
3. Install all required dependencies:
   ```bash
   npm install
   ```
   _(This may take a couple of minutes ⏳ as it downloads Angular and all required UI libraries)._
4. Start the Angular development server:
   ```bash
   npm start
   ```
5. Wait until the terminal says "Compiled successfully" ✅.
6. Open your web browser and go to:
   👉 `http://localhost:4201`
   _(Note: The port is explicitly set to 4201 to avoid conflicts)._

🎉 **Congratulations! The frontend is now running and talking to your local backend.**

---

## 🏛️ Architecture & Folder Structure

The frontend follows a **feature-based organization (Vertical Slice)** to keep code maintainable:

```
src/app/
├── core/                    ← Singleton services, guards, and interceptors
│   ├── guards/              ← Auth and Role guards
│   ├── interceptors/        ← JWT and Error interceptors
│   ├── models/              ← TypeScript interfaces and DTOs
│   └── services/            ← AuthService, TokenService, LanguageService
│
├── shared/                  ← Reusable UI components used across features
│   ├── components/          ← Navbar, Sidebar, Shared UI elements
│   ├── directives/          ← Password eye toggle, etc.
│   └── pipes/               ← Date formatting, etc.
│
├── layouts/                 ← Layout wrapper components
│   ├── public-layout/       ← Horizontal top navbar (for login, landing, etc.)
│   └── dashboard-layout/    ← Vertical sidebar (for authenticated users)
│
└── features/                ← One folder per feature slice
    ├── auth/                ← Login, Registration, Password Reset
    ├── courses/             ← Course cards, Enrollment, Materials, Exams
    ├── exams/               ← Exam results, Re-attempt requests
    ├── anti-cheat/          ← Anti-Cheating Engine service & overlay
    ├── monitoring/          ← Violation timelines and session logs
    ├── grades/              ← Grade management and student grades
    ├── admin/               ← Admin dashboard, Users management
    ├── chat/                ← Real-time messaging
    ├── attendance/          ← QR attendance scanning & history
    ├── calendar/            ← Calendar & schedule view
    ├── payment/             ← Stripe checkout & payment history
    ├── profile/             ← Profile management & avatar
    └── public/              ← Landing page, mobile guard
```

---

## 🚀 Comprehensive Feature List (F1 – F30)

| # | Feature | Details |
|---|---|---|
| F1 | 🔐 **Secure Login & Role-Based Redirect** | JWT authentication, refresh tokens, single-session enforcement |
| F2 | 📧 **Email Verification** | SMTP integration (Mailtrap / Gmail), verification tokens |
| F3 | 🔑 **Password Reset Via Email** | Forgot-password workflow with secure reset links |
| F4 | 👤 **Profile Management** | WebP avatar upload, edit profile, change password, reset tour guide |
| F5 | 📝 **Public Registration** | Student or Tutor role selection during sign-up |
| F6 | 📚 **Course Management & Enrollment** | Full CRUD, paginated enrollment, bulk review, enroll/drop, search & filter |
| F7 | 📁 **File Sharing (Course Materials)** | Upload, download, and manage course resources |
| F8 | 📢 **Announcements** | Post, feed, priority pinning for courses |
| F9 | 📋 **Assignment Management System** | Task lifecycle, file submissions, ZIP bulk export, review & grading |
| F10 | 🔔 **Notifications** | In-app and email notifications for all key system events |
| F11 | 🗓️ **Exam Management & Notifications** | CRUD, publish workflow, scheduling, deadline management, reminders |
| F12 | 🔄 **Re-Attempt & Re-Open Requests** | Students request re-attempts/re-opens, tutors approve with configurable extensions (24h/48h/72h/custom) |
| F13 | 🏦 **Question Bank Management** | Centralized course-level question bank (MCQ, True/False, Short Answer) with image support |
| F14 | ⚡ **Exam Engine + Secure Token** | Countdown timer, question navigator, auto-submit on timeout, cryptographic question randomization |
| F15 | 📊 **Exam Results & Auto-Grading** | Confetti animation, per-question review, manual grading for short answers |
| F16 | 📈 **Grade Management Panel** | Bulk publish, CSV export, weighted grade calculation |
| F17 | 🛡️ **Anti-Cheating Engine** | Browser-native exam integrity system (see sub-features below) |
| F18 | 📉 **Session Timeline View** | Per-attempt timeline of all student activity during an exam |
| F19 | ⚠️ **Violation Timeline View** | Detailed violation log with types, timestamps, and severity |
| F20 | 📡 **Tutor Monitoring Dashboard** | Live overview of ongoing exams and violations for assigned courses |
| F21 | 🏢 **Admin Dashboard & Users Management** | System-wide admin panel, user lock/unlock, tutor listing, user search |
| F22 | 🤖 **SHIELDON AI Assistant** | Gemini-powered chatbot with backend proxy, automatically blocked during exams |
| F23 | 🧭 **Shepherd.js Onboarding Tours** | Role-based guided tours for first-time users |
| F24 | 📊 **Tutor & Global Analytics Dashboard** | Course-level and system-wide analytics with ECharts visualizations |
| F25 | 📱 **Dynamic QR Attendance Tracking** | QR code refreshes every 15 seconds, manual override, attendance history |
| F26 | 📅 **Calendar & Schedule View** | Unified calendar with exams, assignments, and custom events |
| F27 | 💳 **Online Payment Gateway (Stripe)** | Secure checkout, payment history, pending payments, webhook processing |
| F28 | 🌓 **Dark / Light Mode** | Seamless theme toggle with CSS custom properties |
| F29 | 🌍 **English / Arabic (i18n)** | Full RTL support with ngx-translate |
| F30 | 📱 **Mobile Guard** | Detects and blocks mobile/tablet devices from accessing exam engine |

### 🛡️ Anti-Cheating Engine — Sub-Features (F17)

The Anti-Cheating Engine is built entirely with browser Web APIs — no plugins or extensions required:

| # | Sub-Feature | Description |
|---|---|---|
| 17-1 | 📜 **Pre-Exam Rules Acknowledgment Modal** | Students must read and accept exam integrity rules before starting |
| 17-2 | ⌨️ **Keyboard Shortcut Blocking** | Blocks Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+A, Ctrl+P, Ctrl+F, Ctrl+U, F12, Ctrl+Shift+I/J, Esc, Alt+Tab |
| 17-3 | 📐 **Window Resize / Minimize / Split Detection** | Detects split-screen, window resizing, and minimize attempts |
| 17-4 | 🖱️ **Mouse Monitoring (Pattern Analysis)** | Tracks mouse movement patterns for anomaly detection |
| 17-5 | 🚫 **Selection by Mouse Blocking** | Prevents text selection via mouse during exams |
| 17-6 | 🖱️ **Right-Click Context Menu Blocking** | Disables right-click to prevent copy/paste/inspect operations |
| 17-7 | 🧠 **Violation Intelligence Layer** | Severity scoring per violation type + cooldown periods to prevent duplicate flooding |
| 17-8 | ⚠️ **Warning System & Force-Submit** | 3-strike escalation — warnings → final warning → auto force-submit |
| 17-9 | 🔄 **Monitoring Continuity on Reconnect** | Anti-cheat resumes seamlessly if the student reconnects or refreshes |

---

## 🌿 Git Workflow

- 🌿 **Feature Branches**: Never work directly on `main`. Create a new branch for every feature:
  ```bash
  git checkout -b feature/your-feature-name
  ```
- 🔀 **Pull Requests**: When a feature is complete, push it to GitHub and create a Pull Request for code review before merging.

---

<div align="center">
  <strong>🛡️ SHIELDON Frontend — "Integrity You Can Trust" 🛡️</strong>
</div>
