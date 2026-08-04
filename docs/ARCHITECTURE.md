# SHIELDON - System Architecture

This document provides a high-level overview of the SHIELDON architecture, outlining the backend design, microservices, and the interaction between the Angular frontend, .NET Web API, and Node.js services.

## 1. High-Level Overview

SHIELDON is a hybrid Clean Architecture + Microservice system:
- **Frontend**: A Single Page Application (SPA) built with Angular 21 (Standalone Components). Runs in the user's browser (Port 4201).
- **Backend Core**: A RESTful API built with ASP.NET Core 9 following Clean Architecture principles (Port 5000).
- **WhatsApp Gateway Microservice**: A self-hosted Node.js microservice (`@whiskeysockets/baileys`) running on Port 3001 that handles zero-cost WhatsApp OTP phone verification.
- **Database**: Microsoft SQL Server 2022, accessed via Entity Framework Core 9 (EF Core) using a Code-First approach.

## 2. System Architecture Diagram

```
  ┌──────────────────────────────────────────────────────┐
  │             Angular 21 Frontend (SPA)                │  ← Port 4201
  └─────────────────────────┬────────────────────────────┘
                            │ HTTP / SignalR WebSocket
  ┌─────────────────────────▼────────────────────────────┐
  │                   SHIELDON.API                       │  ← Port 5000  (Thin HTTP Controllers)
  ├──────────────────────────────────────────────────────┤
  │              SHIELDON.Application                    │  ← Use Cases, DTOs, Validators, Interfaces
  ├──────────────────────────────────────────────────────┤
  │             SHIELDON.Infrastructure                  │  ← DB, Email, File Storage, WhatsApp Gateway Client
  ├──────────────────────────────────────────────────────┤
  │                SHIELDON.Domain                       │  ← Entities & Core Business Rules
  ├──────────────────────────────────────────────────────┤
  │                SHIELDON.Tests                        │  ← Unit & Integration Tests
  └──────────────────────────────────────────────────────┘
                            │ Internal HTTP (localhost:3001)
  ┌─────────────────────────▼────────────────────────────┐
  │       WhatsApp Gateway Microservice (Node.js)        │  ← Port 3001  (Baileys / WhatsApp Web)
  └────────────────└─────────────────────────────────────┘
```

## 3. Backend Design: Clean Architecture + Vertical Slice Hybrid

The backend is strictly organized into layers to separate concerns, making the system highly testable, maintainable, and scalable.

### SHIELDON.Domain (Core)
- **Responsibility**: Contains enterprise-wide logic, Entities, Enums, and custom Exceptions.
- **Dependencies**: None. This is the center of the architecture.
- **Key Concepts**: `User` (with `PhoneVerificationStatus`), `Course`, `Exam`, `ViolationRecord`, `PaymentRecord`, `LeaderboardSetting`, `IpAuditLog`.

### SHIELDON.Application (Use Cases)
- **Responsibility**: Contains application-specific business rules organized by Feature Slices (e.g., `Features/Auth/`, `Features/Users/`, `Features/Exams/`, `Features/Leaderboard/`).
- **Dependencies**: Depends ONLY on `SHIELDON.Domain`.
- **Key Concepts**: DTOs, Validation (`FluentValidation`), Service Interfaces (`IOtpService`, `IProfileService`), and Application logic orchestrators.

### SHIELDON.Infrastructure (Implementation)
- **Responsibility**: Provides concrete implementations for interfaces defined in the Application layer.
- **Dependencies**: Depends on `SHIELDON.Application` and `SHIELDON.Domain`.
- **Key Concepts**: `AppDbContext` (EF Core), MailKit (Email), Stripe Integrations, `WhatsAppGatewayOtpService` (HTTP client for WhatsApp Gateway), SignalR Hub implementations.

### SHIELDON.API (Presentation)
- **Responsibility**: Exposes 146 REST endpoints and 5 SignalR real-time hubs across 26 controllers.
- **Dependencies**: Depends on `SHIELDON.Application` and `SHIELDON.Infrastructure`.
- **Key Concepts**: Controllers, JWT Authentication Middleware, Google OAuth, Swagger documentation.

## 4. WhatsApp Gateway Microservice (Node.js)

- **Purpose**: Provides self-hosted, zero-cost WhatsApp OTP delivery using `@whiskeysockets/baileys`.
- **Isolation**: Runs as an independent process on Port 3001. If the gateway restarts or re-pairs, the main .NET backend stays completely operational.
- **Clean Architecture Compliance**: The .NET core relies solely on the `IOtpService` abstraction. Swapping to Meta Cloud API or Twilio requires changing only one dependency injection line.

## 5. Frontend Design

The Angular frontend follows a modular, feature-based architecture:
- `src/app/core/`: Singleton services, Interceptors (JWT, Language), Guards, and central models.
- `src/app/shared/`: Reusable UI components (OTP Modal, Country Picker, Modals, Navbars) and pipes.
- `src/app/features/`: Feature modules lazy-loaded when accessed (e.g., `auth/`, `profile/`, `courses/`, `exams/`, `monitoring/`, `chat/`).
- `src/app/layouts/`: Structural wrappers defining the shell of the application (Public Layout vs Dashboard Layout).

## 6. Key Integrations

- **Security**: JWT access & refresh tokens handle stateless session management. Single-session enforcement via `SecurityHub` SignalR.
- **Social Login**: Google OAuth 2.0 integration for passwordless authentication.
- **Phone Verification**: 6-digit WhatsApp OTP verification with 2-minute resend cooldown, Egyptian number auto-formatting, and 1-phone-per-account uniqueness enforcement.
- **Real-Time**: SignalR WebSockets power Chat, Presence Tracking, Live Leaderboard, Attendance, and Exam Monitoring subsystems.
- **Payment Gateway**: Stripe handles Course Enrollments securely via backend webhooks.
- **Email Delivery**: SMTP via MailKit handles Verification and Password Reset (Mailtrap / Gmail).
- **AI Engine**: Gemini API is securely proxied through the backend, preventing client-side key exposure.

