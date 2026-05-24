# SHIELDON - Database Schema Overview

This document provides a high-level conceptual overview of the core database entities managed by Microsoft SQL Server and Entity Framework Core 9.

## Core Entities & Relationships

### Users & Identity
- **`User`**: The central identity entity. Contains credentials, role (`Admin`, `Tutor`, `Student`), language preferences, and profile picture paths.
- **Roles**: Determined by a simple string or enum property within the User table, enforcing authorization rules via standard .NET Identity or custom JWT claims.

### Courses & Enrollment
- **`Course`**: Represents a class. Has a Title, Description, and an associated `TutorId` (Foreign Key to `User`).
- **`Enrollment`**: The many-to-many link between `Student` (User) and `Course`. Tracks enrollment status (e.g., Pending, Active, Dropped).
- **`CourseMaterial`**: Files uploaded by Tutors for a specific course.
- **`CourseAnnouncement`**: Text updates posted by Tutors to a course feed.

### Exams & Question Bank
- **`QuestionBank`**: Centralized repository of questions belonging to a Course, allowing reuse across multiple exams.
- **`Exam`**: Represents a test instance within a Course. Contains timing rules, publish state, and anti-cheat strictness settings.
- **`ExamQuestion`**: Many-to-many joining `Exam` and `QuestionBank`.
- **`ExamAttempt`**: Records a student taking an exam. Tracks start time, end time, current score, and submission state (e.g., Auto-Submitted, Completed).

### Anti-Cheating & Telemetry
- **`ViolationRecord`**: Telemetry log recorded when a student triggers the Anti-Cheating Engine (e.g., window blur, clipboard copy, suspicious mouse movement). Linked to a specific `ExamAttempt`.
- **`ReattemptRequest`**: Students who are locked out due to violations can submit a request to the Tutor for an exam reset.

### E-Commerce & Financials
- **`PaymentRecord`**: Logs Stripe transactions for paid courses. Links to a `User`, a `Course`, and contains the Stripe Session/Intent IDs and payment status (e.g., Succeeded, Failed).

### Chat & Presence
- **`ChatMessage`**: Persisted history for the real-time SignalR chat system. Links Sender to Receiver.
- **`AttendanceRecord`**: Logs student check-ins via the rotating Dynamic QR system.

## Data Integrity Rules
- Entity Framework uses **Cascade Deletes** for strong dependencies (e.g., deleting an `Exam` deletes its `ExamQuestions`).
- **Soft Deletes** (IsDeleted flag) are not globally used, but careful business logic prevents deleting courses with active enrollments or paid histories.
- All primary keys are `Guid` types for maximum distribution and obscurity compared to predictable sequential integers.
