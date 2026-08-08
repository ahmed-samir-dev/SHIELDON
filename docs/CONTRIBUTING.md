# Contributing to SHIELDON

Thank you for your interest in contributing to **SHIELDON**! 

Whether you are fixing a bug, adding a feature, updating documentation, or proposing new ideas, we appreciate your help in keeping SHIELDON secure, robust, and modern.

---

## 📑 Table of Contents

- [Code of Conduct](#-code-of-conduct)
- [Getting Started](#-getting-started)
- [Git Branching Strategy](#-git-branching-strategy)
- [Commit Message Conventions](#-commit-message-conventions)
- [Development Guidelines](#-development-guidelines)
  - [Backend (.NET 9)](#backend-net-9)
  - [Frontend (Angular 21)](#frontend-angular-21)
  - [WhatsApp Gateway (Node.js)](#whatsapp-gateway-nodejs)
- [Submitting a Pull Request (PR)](#-submitting-a-pull-request-pr)
- [Reporting Issues & Bugs](#-reporting-issues--bugs)

---

## 📜 Code of Conduct

We are committed to providing a welcoming, respectful, and inclusive environment for everyone. Please be polite, constructive, and respectful in all issues, pull requests, and discussions.

---

## 🚀 Getting Started

1. **Fork the Repository**: Click the "Fork" button at the top right of the GitHub repository page.
2. **Clone your Fork**:
   ```bash
   git clone https://github.com/YOUR_USERNAME/SHIELDON.git
   cd SHIELDON
   ```
3. **Set Up Prerequisites**: Follow the step-by-step installation instructions in the main [README.md](../README.md#-prerequisites-for-beginners).

---

## 🔀 Git Branching Strategy

We follow a structured Git workflow to ensure main branch stability:

- **`main`**: Production release branch. Must always compile, pass tests, and be deployable. **Direct pushes are disabled.**
- **`develop`**: Main integration branch for active development.
- **Working Branches**: Always create a feature/fix branch off `develop`:
  - Features: `feature/short-feature-description`
  - Bug Fixes: `bugfix/issue-description`
  - Documentation: `docs/what-changed`
  - Maintenance: `chore/task-name`

```bash
git checkout develop
git pull origin develop
git checkout -b feature/your-feature-name
```

---

## 📝 Commit Message Conventions

We enforce [Conventional Commits](https://www.conventionalcommits.org/). Commit messages must be clear, concise, and formatted as follows:

`<type>(<scope>): <short summary>`

### Allowed Types:
- `feat`: A new feature for the user or system
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Formatting, semi-colons, whitespace, styling adjustments (no code logic change)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `perf`: A code change that improves performance
- `test`: Adding or correcting existing tests
- `chore`: Updating build tasks, package configurations, or dependencies

### Examples:
- `feat(auth): add Google OAuth 2.0 passwordless authentication`
- `fix(exam-engine): prevent cascading violation score on rapid alt-tab`
- `docs(readme): add contributing guidelines section`
- `chore(deps): update Angular to version 21.1`

---

## 💻 Development Guidelines

### Backend (.NET 9)

- **Architecture**: Follow **Clean Architecture** patterns (`Domain` → `Application` → `Infrastructure` → `API`). Never leak Infrastructure or Web frameworks into the Domain entity layer.
- **Nullable Types**: Keep `#nullable enable` on. Address all compiler warnings before pushing.
- **Validation**: Use **FluentValidation** for request models in the Application layer.
- **Swagger Documentation**: Annotate new controller endpoints with proper OpenAPI response types and summaries.
- **Database Migrations**: Generate EF Core migrations locally via terminal:
  ```bash
  dotnet ef migrations add NameOfMigration --project SHIELDON.Infrastructure --startup-project SHIELDON.API
  ```
  Verify and apply using `dotnet ef database update`.

### Frontend (Angular 21)

- **Standalone Components**: Build all new UI using Angular 21 Standalone Components (`standalone: true`). Avoid creating legacy `NgModules`.
- **Reactivity**: Prefer Angular **Signals** (`signal()`, `computed()`, `effect()`) for state management.
- **Styling & Themes**: Use CSS Custom Properties defined in `src/assets/styles/_variables.scss` (`var(--primary-color)`, `var(--card-bg)`, etc.) to ensure seamless Dark & Light theme compatibility.
- **Internationalization**: Use `ngx-translate` pipes and translation keys for all user-facing strings (support both English `en` and Arabic `ar`).
- **Anti-Cheat Integrity**: Do not tamper with or disable Anti-Cheating Engine monitoring hooks during production builds.

### WhatsApp Gateway (Node.js)

- Located in `backend/whatsapp-gateway/`.
- Keep the microservice decoupled from .NET logic. All interaction occurs via HTTP API endpoints on port 3001.

---

## 📬 Submitting a Pull Request (PR)

1. Ensure your local code builds cleanly and passes type checking:
   ```bash
   # Frontend check
   cd frontend
   npx tsc --noEmit
   
   # Backend check
   cd ../backend
   dotnet build
   ```
2. Push your feature branch to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```
3. Navigate to the main repository on GitHub and click **New Pull Request**.
4. Set base branch to `develop`.
5. Fill out the PR template with:
   - Clear summary of what was added/fixed
   - Screenshots / Loom video for UI changes
   - Steps to test
6. Once approved by reviewers, merge via **Squash and Merge**.

---

## 🐛 Reporting Issues & Bugs

If you find a bug or have a feature request:
1. Search existing GitHub Issues to check if it has already been reported.
2. If not, open a new Issue with a descriptive title and detailed steps to reproduce.
3. For security vulnerabilities, please do **not** open a public issue - contact the maintainers directly.

---

<div align="center">
  Thank you for contributing to <strong>SHIELDON</strong>! 
</div>
