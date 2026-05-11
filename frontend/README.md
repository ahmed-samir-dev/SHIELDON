# SHIELDON — Frontend

> The user interface for the SHIELDON Learning Management System & Anti-Cheating Engine.
> Built with Angular 21.

---

## Project Overview

This directory contains the frontend application for the **SHIELDON** platform. It provides a modern, responsive, and secure user interface for students, tutors, and administrators. 

The frontend is responsible for:
- Delivering the Learning Management System experience (viewing courses, downloading materials, submitting assignments).
- Enforcing the **Anti-Cheating Engine** rules natively in the browser during exams.
- Displaying rich visual analytics and monitoring dashboards using Apache ECharts.
- Providing an interactive AI assistant for student support.

---

## Technology Stack

- **Framework**: Angular 21 (Standalone Components)
- **Language**: TypeScript (Strict mode enabled)
- **Styling**: Vanilla CSS / SCSS with CSS Custom Properties (Design Tokens)
- **Charts**: Apache ECharts (via ngx-echarts)
- **Icons**: Lucide Icons (Exclusively)
- **UI Feedback**: SweetAlert2 (Modals), ngx-toastr (Toasts)
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

## Installation & Setup

Follow these steps to get the frontend running:

1. Open your terminal (or Command Prompt on Windows).
2. Navigate to the frontend directory of the project:
   ```bash
   cd path/to/SHIELDON/frontend
   ```
3. Install all the required libraries and dependencies:
   ```bash
   npm install
   ```
   *(This may take a minute or two as it downloads the required packages).*
4. Start the frontend development server:
   ```bash
   npm start
   ```
5. Once the terminal says the build is complete, open your web browser and go to:
   ```
   http://localhost:4201
   ```
   *(The port might differ if 4201 is occupied; the terminal will show the exact address).*

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

## Features Implemented

- **Dynamic Landing Page**: Premium SaaS-style layout with glassmorphism effects.
- **Secure Auth Portal**: Role-based access, email verification, and strong password validation.
- **LMS Course Hub**: Tabbed view for accessing course materials and files.
- **Secure Exam Engine**: Split Screen detection, timer countdown, and question navigator.
- **Anti-Cheat Enforcement**: Native browser event tracking (Tab blur, resize, copy/paste blocks).
- **Visual Dashboards**: Interactive charts for monitoring student performance and violations.
- **In-App Notifications**: Real-time feedback for enrollment and exam events.
- **Onboarding Tours**: Guided step-by-step tours for new users.

---

## Git Workflow

- Create a new branch for every feature you build:
  ```bash
  git checkout -b feature/your-feature
  ```
- Push your branch to GitHub and open a Pull Request to merge into `develop` or `main`.
