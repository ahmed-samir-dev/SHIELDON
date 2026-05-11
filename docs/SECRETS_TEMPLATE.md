# Required Secrets Setup Guide

This file is safe to commit. It documents what secrets each team member must configure locally.
**Never put real values in this file.**

---

## Backend: `appsettings.Development.json`

Create this file at: `backend/SHIELDON.API/appsettings.Development.json`
This file is **gitignored** — it will never be committed.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ShieldonDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "ask-team-lead-for-this-minimum-32-characters-long",
    "Issuer": "SHIELDON",
    "Audience": "SHIELDON-Users",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "EmailSettings": {
    "SmtpHost": "sandbox.smtp.mailtrap.io",
    "SmtpPort": 587,
    "SmtpUser": "your-mailtrap-username",
    "SmtpPassword": "your-mailtrap-password",
    "FromName": "SHIELDON Platform",
    "FromEmail": "noreply@shieldon.com"
  },
  "GeminiSettings": {
    "ApiKey": "your-gemini-api-key-here"
  },
  "AdminSeed": {
    "Email": "admin@shieldon.com",
    "Password": "Admin@Shieldon2025!",
    "FirstName": "System",
    "LastName": "Administrator"
  }
}
```

---

## How to Get Each Secret

### SQL Server Connection String

- If using Windows Authentication (Trusted_Connection): `Server=localhost;Database=ShieldonDB;Trusted_Connection=True;TrustServerCertificate=True;`
- If using SQL Server Authentication: `Server=localhost;Database=ShieldonDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;`

### JWT Secret Key

- Must be **minimum 32 characters** long
- Ask your team lead, or generate: open any online GUID generator and combine two GUIDs
- Example (do NOT use this): `SHIELDON-JWT-Secret-Key-For-Dev-Only-2025!`

### Email Option A: Mailtrap (Development Only)

1. Go to [mailtrap.io](https://mailtrap.io) → Sign up free.
2. Go to **Email Testing → Inboxes → My Inbox**.
3. Click **Show Credentials** and copy the SMTP details.
4. This captures emails without sending them to real users.

### Email Option B: Gmail (For Real Emails)

To send real emails using a Gmail account:

1. Set `SmtpHost` to `smtp.gmail.com`
2. Set `SmtpPort` to `587`
3. Set `SmtpUser` to your Gmail address.
4. Set `SmtpPassword` to a **Google App Password** (NOT your normal password).
   - _How to get one_: Go to Google Account → Security → 2-Step Verification → App passwords (at the bottom). Generate one for "Mail".
5. Set `FromEmail` to match your Gmail address.

### Gemini API Key (For AI Assistant)

1. Go to [Google AI Studio](https://aistudio.google.com/)
2. Sign in with your Google account
3. Click **Create API Key**
4. Copy the generated key and paste it in your configuration

---

## Alternative: .NET User Secrets (Recommended)

Instead of creating `appsettings.Development.json`, you can use .NET's built-in secret manager:

```bash
cd backend/SHIELDON.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ShieldonDB;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "JwtSettings:SecretKey" "your-32-char-minimum-secret-key"
dotnet user-secrets set "EmailSettings:SmtpHost" "sandbox.smtp.mailtrap.io"
dotnet user-secrets set "EmailSettings:SmtpPort" "587"
dotnet user-secrets set "EmailSettings:SmtpUser" "your-mailtrap-user"
dotnet user-secrets set "EmailSettings:SmtpPassword" "your-mailtrap-password"
dotnet user-secrets set "GeminiSettings:ApiKey" "your-gemini-api-key"
dotnet user-secrets set "AdminSeed:Password" "Admin@Shieldon2025!"
```

---

## Pre-Push Security Check

Before every `git push`, run:

```bash
git ls-files | grep appsettings.Development
```

**This must return NOTHING.** If it shows the file, remove it:

```bash
git rm --cached backend/SHIELDON.API/appsettings.Development.json
```
