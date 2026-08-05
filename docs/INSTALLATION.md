# 🔧 Installation & Setup Guide (Step-by-Step)

Follow these comprehensive steps in order to properly get **SHIELDON** running on your local machine from scratch. 🚀

---

## 📑 Table of Contents

- [📋 Prerequisites (For Beginners)](#-prerequisites-for-beginners)
- [🔧 Installation Steps](#-installation-steps)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Backend Database Configuration](#2-backend-database-configuration-crucial-step)
  - [3. Initialize and Update the Database](#3-initialize-and-update-the-database)
  - [4. Run the WhatsApp Gateway Microservice](#4-run-the-whatsapp-gateway-microservice-for-phone-verification--otp)
  - [5. Run the Backend API](#5-run-the-backend-api)
  - [6. Run the Frontend Application](#6-run-the-frontend-application)
  - [7. Stripe Payment Setup](#7-stripe-payment-setup-optional)

---

## 📋 Prerequisites (For Beginners)

If you are new to development and want to run this project on your own computer, you need to download and install the following tools first. They are all free! 🆓

1. **Node.js**: Required to run the frontend.
   - Download the **LTS** version from [nodejs.org](https://nodejs.org/).
   - Run the installer and follow the default steps.
2. **.NET 9 SDK**: The engine that runs the backend.
   - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0).
   - Look for the ".NET SDK" installer for your operating system.
3. **SQL Server**: The database where all data will be stored.
   - Download **SQL Server Express** from [Microsoft](https://www.microsoft.com/sql-server/sql-server-downloads).
   - Choose the "Basic" installation type.
4. **SSMS (SQL Server Management Studio)**: A visual program to inspect your database.
   - Download from [Microsoft Docs](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).
5. **Git**: A tool to clone the project from GitHub.
   - Download from [git-scm.com](https://git-scm.com/).
6. **Stripe CLI** _(optional, for payment testing)_:
   - Download from [stripe.com/docs/stripe-cli](https://docs.stripe.com/stripe-cli).

---

## 🔧 Installation Steps

### 1. Clone the Repository

This downloads the project files to your computer. 📥

1. Open your terminal (or Command Prompt / PowerShell on Windows).
2. Run this command:
   ```bash
   git clone https://github.com/ahmed-samir-dev/SHIELDON.git
   ```
3. Navigate into the project folder:
   ```bash
   cd SHIELDON
   ```

### 2. Backend Database Configuration (CRUCIAL STEP)

Before running the backend, you must configure it to connect to your local SQL Server.

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Open `SHIELDON.API/appsettings.json` and `SHIELDON.API/appsettings.Development.json` in a text editor.
3. Locate the `"ConnectionStrings"` block. Update the `"DefaultConnection"` string to match your local SQL Server instance name.
   - **How to find your Server Name**: Open SSMS. The connection prompt shows the `Server name` (e.g., `DESKTOP-ABC123\SQLEXPRESS` or `(localdb)\MSSQLLocalDB`).
   - **Update the string**: Replace the Server part. Use double backslashes `\\` for escaping in JSON.

   _Example:_
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_PC_NAME\\SQLEXPRESS;Database=SHIELDON_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
   }
   ```

4. Save the file(s).

### 3. Initialize and Update the Database

Now we tell Entity Framework to build the tables in your SQL Server.

1. Keep your terminal in the `backend` folder (NOT inside `SHIELDON.API`).
2. Ensure the EF Core CLI tools are installed globally:
   ```bash
   dotnet tool install -g dotnet-ef
   ```
   (If it says already installed, that's perfect!)
3. Apply all migrations:
   ```bash
   dotnet ef database update --project SHIELDON.Infrastructure --startup-project SHIELDON.API
   ```
4. **Verification**: Open SSMS, connect to your server, expand "Databases", and you should see `SHIELDON_DB` with all tables created!

### 4. Run the WhatsApp Gateway Microservice (For Phone Verification & OTP)

The WhatsApp Gateway is a lightweight Node.js microservice running on port 3001 that sends 6-digit OTP verification codes via WhatsApp.

1. Open a **new terminal window** and navigate to the gateway directory:
   ```bash
   cd backend/whatsapp-gateway
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the gateway server:
   ```bash
   npm start
   ```
4. **First Time Pairing**:
   - The terminal will display a **WhatsApp QR code**.
   - Open WhatsApp on your mobile phone → **Linked Devices** → **Link a Device** and scan the terminal QR code.
   - Once linked, it will output `WhatsApp connected! Gateway is ready to send OTP messages.`.
   - Your session is saved locally — you do **not** need to scan the QR code again on future restarts!
5. Keep this terminal window open.

### 5. Run the Backend API

1. Open a **new terminal window** and navigate into the API project:
   ```bash
   cd backend/SHIELDON.API
   ```
2. Start the server:
   ```bash
   dotnet run
   ```
   (Or use `dotnet watch run` for automatic hot-reloading)
3. The backend is now running! Visit the live API documentation at:
   👉 `http://localhost:5000/swagger`
4. Keep this terminal window open.

### 6. Run the Frontend Application

1. Open another **separate terminal window** (leave the backend & gateway running!).
2. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```
3. Install all dependencies:
   ```bash
   npm install
   ```
   (This may take a couple of minutes)
4. Start the Angular dev server:
   ```bash
   npm start
   ```
5. Wait until compilation is successful.
6. Open your browser and navigate to:
   👉 `http://localhost:4201`

---

## 🎉 Congratulations! You are now running SHIELDON on your local machine!

### 7. Stripe Payment Setup

To enable the online payment gateway, you need a Stripe account and the Stripe CLI.

#### Step A: Create a Stripe Account & Get API Keys
1. Go to [stripe.com](https://stripe.com) and create a **free** account.
2. After logging in, make sure you are in **Test mode** (toggle in the top-right of the dashboard).
3. Navigate to [Developers → API Keys](https://dashboard.stripe.com/test/apikeys).
4. You will see two keys:
   - **Publishable key** — starts with `pk_test_...`
   - **Secret key** — starts with `sk_test_...` (click "Reveal test key" to see it)
5. Copy both keys — you'll need them in the next step.

#### Step B: Install the Stripe CLI
The Stripe CLI is a command-line tool that forwards payment events from Stripe's servers to your local machine.

**Option 1 — Download manually (recommended for beginners):**
1. Go to [Stripe CLI releases](https://github.com/stripe/stripe-cli/releases).
2. Download the latest `.zip` file for your OS (e.g., `stripe_X.X.X_windows_x86_64.zip`).
3. Extract the `.zip` and place the `stripe.exe` file somewhere accessible (e.g., inside a `stripe_cli` folder in your project root).

**Option 2 — Install via package manager:**
```bash
# Windows (Scoop)
scoop install stripe

# macOS (Homebrew)
brew install stripe/stripe-cli/stripe
```

4. Verify the installation:
   ```bash
   stripe --version
   ```

5. Log in to your Stripe account from the CLI:
   ```bash
   stripe login
   ```
   This will open your browser to authenticate. Follow the instructions and press Enter when done.

#### Step C: Configure Backend
1. Open `backend/SHIELDON.API/appsettings.json`.
2. Locate the `"Stripe"` section and fill in your keys:
   ```json
   "Stripe": {
     "SecretKey": "sk_test_YOUR_SECRET_KEY",
     "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
     "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET"
   }
   ```
   > You'll get the `WebhookSecret` in the next step — leave it blank for now.

#### Step D: Run Stripe CLI for Webhooks
The Stripe CLI forwards webhook events (like `checkout.session.completed`) to your local backend so payments are processed correctly.

1. Open a **new terminal** and navigate to your project root:
   ```bash
   cd path/to/SHIELDON
   ```
2. Run the following command to start listening for Stripe events:

   **If using the bundled CLI in the project:**
   ```bash
   .\stripe_cli\stripe.exe listen --forward-to localhost:5000/api/webhooks/stripe
   ```

   **If installed globally:**
   ```bash
   stripe listen --forward-to localhost:5000/api/webhooks/stripe
   ```

3. The CLI will output a **Webhook signing secret** like this:
   ```
   > Ready! Your webhook signing secret is whsec_abc123...
   ```
4. **Copy this `whsec_...` value** and paste it into `appsettings.json` → `Stripe.WebhookSecret`.
5. **Restart the backend** after updating the secret.
6. **Keep this terminal open** while testing payments — it must be running to receive Stripe events.

#### Step E: Test Payments

Use Stripe's official test card numbers to simulate different payment scenarios. No real money is charged.

##### ✅ Success Cards

| Card Number | Scenario |
|:---|:---|
| `4242 4242 4242 4242` | Standard successful payment |
| `4000 0025 0000 3155` | Requires 3D Secure (two-step authentication) |

##### ❌ Failure / Decline Cards

| Card Number | Scenario Simulated |
|:---|:---|
| `4000 0000 0000 0002` | Generic decline |
| `4000 0000 0000 9995` | Insufficient funds |
| `4000 0000 0000 0069` | Card expired |
| `4000 0000 0000 0127` | Incorrect CVC |
| `4000 0000 0000 0119` | Processing error |

**For all test cards, use:**
- **Expiry:** Any future date (e.g., `12/30`)
- **CVC:** Any 3 digits (e.g., `123`)
- **Name / ZIP:** Any values
