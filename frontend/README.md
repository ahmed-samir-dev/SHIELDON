# SHIELDON - Frontend

<div align="center">
  <img src="https://img.shields.io/badge/Angular-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular" />
  <img src="https://img.shields.io/badge/TypeScript-007ACC?style=for-the-badge&logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/Sass-CC6699?style=for-the-badge&logo=sass&logoColor=white" alt="Sass" />
</div>

> The user interface for the SHIELDON Learning Management System & Anti-Cheating Engine.
> Built with Angular 21.

---

## Project Overview

This directory contains the frontend application for the **SHIELDON** platform. It provides a modern, responsive, and secure user interface for students, tutors, and administrators. 

The frontend is responsible for:
- 📚 Delivering the Learning Management System experience (viewing courses, downloading materials, submitting assignments).
- 🛡️ Enforcing the **Anti-Cheating Engine** rules natively in the browser during exams.
- 📈 Displaying rich visual analytics and monitoring dashboards using Apache ECharts.
- 🤖 Providing an interactive AI assistant for student support.

---

## Technology Stack

- 🅰️ **Framework**: Angular 21 (Standalone Components)
- 🔷 **Language**: TypeScript (Strict mode enabled)
- 🎨 **Styling**: Vanilla CSS / SCSS with CSS Custom Properties (Design Tokens)
- 📊 **Charts**: Apache ECharts (via ngx-echarts)
- 🧩 **UI / UX**: Lucide Icons, SweetAlert2, ngx-toastr, canvas-confetti, Shepherd.js
- **Animations**: canvas-confetti (for results), CSS keyframes
- **Guided Tours**: Shepherd.js

---

## Prerequisites (For Beginners)

If you have zero previous technical background and want to run the frontend part on your device, you need to install these tools first:

1. **Node.js**: This is the environment that allows the frontend code to run.
   - Download the **LTS** version from [nodejs.org](https://nodejs.org/).
   - Follow the default installation steps.
2. **Git**: Required to download the project files (if you haven't already).
   - Download from [git-scm.com](https://git-scm.com/).

---

## Installation & Setup (Step-by-Step Guide)

> ⚠️ **CRITICAL REQUIREMENT:** The SHIELDON Frontend **cannot function without the Backend API**. You MUST complete the backend database setup and have the backend API running before you can log in or use the frontend. See the root `README.md` for full database configuration steps.

Follow these comprehensive steps to get the frontend running:

### 1. Ensure Backend is Running
1. Verify that your SQL Server is running.
2. Verify that the `.NET 9` backend is actively running (usually on `http://localhost:5000`).
3. **Keep the backend terminal open** in the background.

### 2. Run the Frontend Application
1. Open a **new, completely separate** terminal window (or Command Prompt / PowerShell).
2. Navigate to the frontend directory of the project:
   ```bash
   cd path/to/SHIELDON/frontend
   ```
3. Install all the required Node modules and dependencies:
   ```bash
   npm install
   ```
   *(This may take a couple of minutes depending on your internet connection as it downloads Angular and all required UI libraries).*
4. Start the Angular development server:
   ```bash
   npm start
   ```
5. Wait until the terminal says "Compiled successfully".
6. Finally, open your web browser and go to:
   👉 `http://localhost:4201`
   *(Note: The port is explicitly set to 4201 to avoid conflicts).*

**Congratulations! The frontend is now running and talking to your local backend.**

---

## Architecture & Folder Structure

The frontend follows a feature-based organization (Vertical Slice) to keep code maintainable:

```
src/app/
├── core/                    ← Singleton services, guards, and interceptors
│   ├── guards/              ← Auth and Role guards
│   ├── interceptors/        ← JWT and Error interceptors
│   └── services/            ← AuthService, TokenService
│
├── shared/                  ← Reusable UI components used across features
│   ├── components/          ← Buttons, Inputs, Custom Cards
│   ├── directives/          ← Password eye toggle, etc.
│   └── pipes/               ← Date formatting, etc.
│
├── layouts/                 ← Layout wrapper components
│   ├── public-layout/       ← Horizontal top navbar (for login, landing, etc.)
│   └── dashboard-layout/    ← Vertical sidebar (for authenticated users)
│
└── features/                ← One folder per feature slice
    ├── auth/                ← Login, Registration, Password Reset
    ├── courses/             ← Course cards, Enrollment, Materials
    ├── exams/               ← Exam taking interface and countdowns
    ├── monitoring/          ← Violation timelines and session logs
    └── dashboards/          ← Analytics screens for Tutors and Admins
```

---

## 🚀 Comprehensive Feature List (F1 - F31)

- **F1: Secure Login & Role-Based Redirect** (JWT, single-session enforcement)
- **F2: Email Verification** (SMTP integration, verification tokens)
- **F3: Password Reset Via Email** (forgot-password workflow)
- **F4: Profile Management** (WebP avatar upload, edit profile, change password and reset tour guide)
- **F5: Public Registration** (Student or Tutor role selection)
- **F6: Course Management & Enrollment** (CRUD, bulk review, enroll/drop)
- **F7: File Sharing** (Course Materials)
- **F8: Announcements** (post, feed, priority pinning)
- **F9: Assignment Management System** (task lifecycle, ZIP bulk export)
- **F10: Notifications** (In-app and email) & Advanced Enrollment Polish
- **F11: Exam Management & Notifications** (CRUD, publish workflow, reminders)
- **F12: Re-Attempt Requests**
- **F13: Question Bank Management** (Centralized course-level bank)
- **F14/F15: Exam Engine + Secure Token** (countdown, navigator, auto-submit)
- **F16: Exam Results & Auto-Grading** (confetti, per-question review)
- **F17: Grade Management Panel** (bulk publish, CSV export)
- **F18: Anti-Cheating Engine** contains:
  - **18-1:** Pre-exam rules acknowledgment modal
  - **18-2:** Keyboard shortcut blocking (CTRL+C, ... etc)
  - **18-3:** Window resize / minimize / split detection
  - **18-4:** Mouse monitoring (pattern analysis)
  - **18-5:** Selection by mouse blocking
  - **18-6:** Blocking Right click options to do operations
  - **18-7:** Violation intelligence layer (severity + cooldown)
  - **18-8:** Warning system and force-submit (3-strike escalation)
  - **18-9:** Monitoring continuity on reconnect
- **F19: Session timeline view**
- **F20: Violation timeline view**
- **F21: Tutor monitoring dashboard**
- **F22: Admin dashboard and users management**
- **F23: SHIELDON AI Assistant** (Gemini-powered chatbot, backend proxy, blocked during exams)
- **F24: Shepherd.js onboarding tours** (Role-based guided tours)
- **F25: SHIELDON AI Assistant** *(Integrated)*
- **F26: Real-time Chat System**
- **F27: Dynamic QR Attendance Tracking**
- **F28: Calendar & Schedule View**
- **F29: Online Payment Gateway**
- **F30: Dark / Light mode**
- **F31: English / Arabic**

---

## Git Workflow

- Create a new branch for every feature you build:
  ```bash
  git checkout -b feature/your-feature
  ```
- Push your branch to GitHub and open a Pull Request to merge into `develop` or `main`.
