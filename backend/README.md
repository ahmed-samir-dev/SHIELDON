# SHIELDON - Backend

<div align="center">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt=".NET" />
  <img src="https://img.shields.io/badge/Entity_Framework-5C2D91?style=for-the-badge&logo=.net&logoColor=white" alt="EF Core" />
  <img src="https://img.shields.io/badge/SQL_Server-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
</div>

> The core API, business logic, and database orchestration for the SHIELDON Learning Management System & Anti-Cheating Engine.
> Built with .NET 9.

---

## Project Overview

This directory contains the backend application for the **SHIELDON** platform. It provides the RESTful API endpoints, business logic orchestration, and database persistence required to power the system.

The backend is responsible for:
- Secure JWT authentication and role management.
- Orchestrating the Exam Engine logic and auto-grading.
- Persisting and analyzing anti-cheat violation logs.
- Serving analytical data for dashboards.

---

## Technology Stack

- **Framework**: .NET 9 ASP.NET Core Web API
- **Language**: C# (Nullable reference types enabled)
- **Architecture**: Clean Architecture + Vertical Slice Hybrid
- **ORM**: Entity Framework Core 9 (Code-First)
- **Database**: Microsoft SQL Server
- **Authentication**: JWT Bearer Tokens (Access + Refresh)
- **Mail System**: MailKit / MimeKit for SMTP emails
- **Validation**: FluentValidation
- **Mapping**: AutoMapper
- **Logging**: Serilog with structured sinks

---

## Prerequisites (For Beginners)

If you have zero previous technical background and want to run the backend part on your device, you need to install these tools first:

1. **.NET 9 SDK**: This is the software development kit required to build and run .NET applications.
   - Download it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0).
   - Follow the default installation steps.
2. **SQL Server**: This is the database where all users, courses, and exam data will be stored.
   - Download **SQL Server Express** (free) from [Microsoft](https://www.microsoft.com/sql-server/sql-server-downloads).
   - Choose the "Basic" installation type.
3. **SSMS (SQL Server Management Studio)**: This is a visual program to look at your database.
   - Download it from [Microsoft Docs](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).

---

## Installation & Setup (Step-by-Step Guide)

Follow these comprehensive steps in order to properly get the backend running on your device from scratch.

### 1. Database Configuration (CRUCIAL STEP)
Before running the backend, you must configure it to connect to your local SQL Server.

1. Open your terminal and navigate to the backend directory:
   ```bash
   cd path/to/SHIELDON/backend
   ```
2. Open the file `SHIELDON.API/appsettings.json` and `SHIELDON.API/appsettings.Development.json` in a text editor (like VS Code or Notepad).
3. Locate the `"ConnectionStrings"` block. You must update the `"DefaultConnection"` string to match your local SQL Server instance name.
   - **How to find your Server Name**: Open SQL Server Management Studio (SSMS). The prompt that asks you to connect will show the `Server name` (e.g., `DESKTOP-ABC123\SQLEXPRESS` or `(localdb)\MSSQLLocalDB`).
   - **Update the string**: Replace the Server part of the connection string. Make sure you use double backslashes `\\` for escaping in JSON.
   
   *Example of a correct connection string for SQL Express:*
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_PC_NAME\\SQLEXPRESS;Database=SHIELDON_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
   }
   ```
4. Save the file(s).

### 2. Initialize and Update the Database
Now we need to tell Entity Framework (the ORM) to build the tables in your SQL Server.

1. Keep your terminal open in the `backend` folder (NOT inside `SHIELDON.API`).
2. First, ensure you have the EF Core CLI tools installed globally on your machine. Run:
   ```bash
   dotnet tool install -g dotnet-ef
   ```
   *(If it says it is already installed, that's perfect!)*
3. Now, run the magical command to apply all migrations and build your database schema from scratch:
   ```bash
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```
4. **Verification**: Open SSMS, connect to your server, expand "Databases", and you should now see `SHIELDON_DB` with all the tables created!

### 3. Run the Backend API
1. Navigate into the API startup project folder:
   ```bash
   cd SHIELDON.API
   ```
2. Start the server:
   ```bash
   dotnet run
   ```
   *(Alternatively, if you want hot-reloading while making changes, use `dotnet watch run`)*
3. The backend is now actively running! Open your browser and go to the live API documentation at:
   👉 `http://localhost:5000/swagger`
4. Keep this terminal window open if you plan to run the frontend simultaneously.

## Architecture & Layers

The backend follows **Clean Architecture** principles to keep the code organized and independent:

- **SHIELDON.Domain**: The heart of the system. Contains C# entities representing database tables, enums, and constants. It has zero external dependencies.
- **SHIELDON.Application**: Contains the business logic and use cases organized by feature slices. This is where commands, handlers, DTOs, and request validators live.
- **SHIELDON.Infrastructure**: Handles implementation details like database persistence (EF Core), file storage, and email sending.
- **SHIELDON.API**: The entry point. Contains thin HTTP controllers that receive requests and delegate them to the application layer. No business logic lives here.

---

## API Endpoints Reference

The backend exposes a comprehensive RESTful API for all system functions (Authentication, Courses, Exams, Chat, Payments, etc.). 

For a complete, interactive list of all API endpoints and their required payloads, please run the backend locally and visit the automatically generated Swagger documentation at:
👉 `http://localhost:5000/swagger/index.html`
