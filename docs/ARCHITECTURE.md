# SHIELDON - System Architecture

This document provides a high-level overview of the SHIELDON architecture, specifically outlining the backend design and the interaction between the Angular frontend and the .NET Web API.

## 1. High-Level Overview

SHIELDON is a standard client-server application:
- **Frontend**: A Single Page Application (SPA) built with Angular 21 (Standalone Components). It runs in the user's browser.
- **Backend**: A RESTful API built with ASP.NET Core 9. It processes business logic, validates data, and interfaces with the database.
- **Database**: Microsoft SQL Server 2022, accessed via Entity Framework Core 9 (EF Core) using a Code-First approach.

## 2. Backend Design: Clean Architecture + Vertical Slice Hybrid

The backend is strictly organized into layers to separate concerns, making the system highly testable, maintainable, and scalable.

### SHIELDON.Domain (Core)
- **Responsibility**: Contains enterprise-wide logic, Entities, Enums, and custom Exceptions.
- **Dependencies**: None. This is the center of the architecture.
- **Key Concepts**: `User`, `Course`, `Exam`, `ViolationRecord`, `PaymentRecord`.

### SHIELDON.Application (Use Cases)
- **Responsibility**: Contains application-specific business rules. It implements the "Vertical Slice" pattern organized by Features (e.g., `Features/Exams/`, `Features/Users/`).
- **Dependencies**: Depends ONLY on `SHIELDON.Domain`.
- **Key Concepts**: DTOs, Validation (`FluentValidation`), Service Interfaces, and Application logic orchestrators.

### SHIELDON.Infrastructure (Implementation)
- **Responsibility**: Provides concrete implementations for the interfaces defined in the Application layer.
- **Dependencies**: Depends on `SHIELDON.Application` and `SHIELDON.Domain`.
- **Key Concepts**: `ApplicationDbContext` (EF Core), MailKit (Email), Stripe Integrations, Lingva API (Translation), SignalR Hub implementations.

### SHIELDON.API (Presentation)
- **Responsibility**: Exposes the system via HTTP REST endpoints and SignalR WebSockets.
- **Dependencies**: Depends on `SHIELDON.Application` and `SHIELDON.Infrastructure`.
- **Key Concepts**: Controllers, JWT Authentication Middleware, Swagger documentation.

## 3. Frontend Design

The Angular frontend follows a modular, feature-based architecture:
- `src/app/core/`: Singleton services, Interceptors (JWT, Language), Guards, and central models.
- `src/app/shared/`: Reusable UI components (Modals, Navbars, UI Cards) and pipes.
- `src/app/features/`: The actual business views, lazy-loaded when accessed (e.g., `auth/`, `courses/`, `exams/`, `monitoring/`).
- `src/app/layouts/`: Structural wrappers defining the shell of the application (Public Layout vs Dashboard Layout).

## 4. Key Integrations

- **Security**: JWT tokens handle stateless session management.
- **Real-Time**: SignalR WebSockets power the Chat, Notification, and Live Exam Monitoring subsystems.
- **Payment Gateway**: Stripe handles Course Enrollments securely via backend webhooks.
- **Email Delivery**: SMTP via MailKit handles Verification and Password Reset.
- **AI Engine**: Gemini Pro is securely proxied through the backend, meaning API keys are never exposed to the Angular client.
