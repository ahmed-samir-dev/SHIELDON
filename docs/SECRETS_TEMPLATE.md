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

### Email (Mailtrap.io — Development Only)
1. Go to [mailtrap.io](https://mailtrap.io) → Sign up free
2. Go to **Email Testing → Inboxes → My Inbox**
3. Click **Show Credentials**
4. Copy `SMTP Host`, `Port`, `Username`, and `Password`
5. No real emails are sent — all emails go to Mailtrap inbox for inspection

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
