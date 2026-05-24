# Contributing to SHIELDON

Welcome to the SHIELDON development team! Please review the guidelines below before making changes to the repository.

## Git Workflow

We follow a strict Git branching model to ensure stability in production.

1. **`main` Branch**: This is the production-ready code. It must ALWAYS compile, pass tests, and be deployable. **Never push directly to `main`.**
2. **`develop` Branch**: This is the main integration branch for active development. All new features are merged here first before a release is created.

### Creating a Feature
1. Ensure your local `develop` branch is up to date:
   ```bash
   git checkout develop
   git pull origin develop
   ```
2. Create a new branch off of `develop`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
   *(Use `bugfix/` for bug fixes, and `docs/` for documentation updates).*

### Committing Changes
We follow the Conventional Commits specification. Your commit messages must be clear and prefixed appropriately:
- `feat:` for a new feature.
- `fix:` for a bug fix.
- `docs:` for documentation changes.
- `style:` for formatting, missing semi colons, etc (no code change).
- `chore:` for updating build tasks, package manager configs, etc.

*Example:* `feat(exams): add auto-grading logic to submission endpoint`

### Opening a Pull Request
1. Push your branch to the remote repository.
2. Open a Pull Request targeting the `develop` branch (or `main` if releasing a graduation version).
3. Ensure GitHub Copilot (or human reviewers) review and approve the PR.
4. Squash and merge when approved.

## Backend Guidelines (.NET)
- Use **Clean Architecture**. Do not reference Infrastructure inside the Domain layer.
- Use `Nullable` reference types. Address all compiler warnings.
- All new controllers must have Swagger documentation attributes.
- EF Core migrations must be generated locally and committed. Do not run `Update-Database` in production directly (use migration bundles or CI/CD).

## Frontend Guidelines (Angular)
- Use **Standalone Components**. Do not create new `NgModules` unless interfacing with a legacy library.
- Adhere to the established CSS Custom Properties (Variables) defined in `src/assets/styles/_variables.scss` for theming (Dark/Light mode).
- Keep components focused. Delegate heavy logic to Services.
- Do not bypass the Anti-Cheating Engine mechanisms while testing unless explicitly mocking the environment.
