# 🏆 Horizon Universal Architecture & Excellence Blueprint

This document defines the "Golden Rules" and architectural standards for all projects within the Horizon platform. It is intended for both human developers and AI agents to ensure consistency, scalability, and high code quality across the entire ecosystem.

---

## 🏗️ Architectural Pillars

### 1. The "Single Source of Truth" API
- **Tooling**: Use **NSwag** or **OpenAPI** to auto-generate TypeScript clients.
- **Models & Structure**: The auto-generated client should include all **Response Models** and **Request Structures**. Never manually define these in the frontend.
- **Rule**: Whenever the backend DTOs change, re-run the client generator.
- **Benefit**: Zero "type mismatch" bugs.

### 2. Standardized Error Handling
- **Backend**: Use a global `ExceptionMiddleware`. No `try-catch` blocks in controllers unless for very specific logic.
- **Responses**: Always return `ProblemDetails` (RFC 7807).
- **Frontend**: Use a central notification system (e.g., `react-hot-toast`) to display these errors.

### 3. Container-First & Infrastructure-as-Code
- **Docker**: Every core dependency (API, Web, DB, Cache) must be in `docker-compose.yml`.
- **Environment**: Use `.env` files for secrets. Use `render.yaml` or similar for cloud infrastructure definition.
- **Rules**: Local dev must be "Plug & Play" with a single helper script (`dev.ps1`) that handles dynamic port allocation and service startup.

### 4. Background Job & Multi-Channel Delivery
- **Offloading**: Never perform slow operations (Email, external API sync, heavy processing) in the request-response cycle. Use **BullMQ** (for Node.js/NestJS) or **Hangfire** (for .NET).
- **Reliability**: Jobs should be retriable and traceable.
- **Fallback**: Implement multi-channel defaults (e.g., WebSocket for real-time, Email for fallback).

### 5. Resilient Session Management
- **UX Requirement**: Users should never be kicked out due to expired short-lived tokens.
- **Pattern**: Implement a 401 Interceptor that triggers an automatic renewal (refresh token) and retries the original request seamlessly.

### 6. Universal State & Caching
- **Standard**: Always use a robust data-fetching library (e.g., `@tanstack/react-query`) for caching and state synchronization.
- **Benefit**: Ensures a "snappy" UI with built-in optimistic updates and automated background refetching.
- **Parity**: All frontends (Web, Mobile) MUST adopt the same caching logic.

### 7. Real-time Communication & Presence
- **Technology**: Use **Socket.IO** (NestJS) or **SignalR** (.NET) for bidirectional, real-time communication.
- **Pattern**: Implement room-based/hub-based communication for scoped updates.
- **Mobile Integration**: Use singleton instances for persistent connections across screens.
- **Fallback**: Always ensure REST APIs are available as fallback for critical operations.

### 8. Observability & Health Monitoring
- **Structured Logging**: All logs must be structured (JSON format with contextual properties). Use **Seq** for visual log search.
- **Health Checks**: Implement `/health` endpoints for orchestration and monitoring.
- **Transient Fault Handling**: Implement retries and circuit breakers for infrastructure dependencies (DB, Cache, Storage).
- **Persistence Strategy**: All infrastructure data (DB, Cache, Logs) must persist across restarts via Docker volumes.

### 9. Pluggable Storage Abstraction
- **Abstraction**: Applications must interact with storage via an interface (e.g., `IStorageService`) rather than direct filesystem calls.
- **Hybrid Support**: The architecture should support multiple providers (Local Disk, S3, Cloudinary) switchable via configuration.
- **Path Resolution**: Use a centralized resolver to handle transitions between relative local paths and absolute production URLs.

###  Ten. Implementation Excellence & Patterns
- **Standardized Onboarding**: Complex flows must be broken into discrete, verifiable steps (e.g., Identity Verification -> Resource Allocation).
- **Tooling Automation**: Repetitive developer tasks (setup, verification, seeding) MUST be scripted (e.g., `dev.ps1`).
- **Dynamic Infrastructure**: The `dev.ps1` script handles environmental conflicts by automatically re-mapping ports and performing **Self-Healing** (clearing zombie services/orphans) before launch.

---

## 🚀 DevOps Workflow Patterns

### Northern Workflow (Build & Test)
- **Goal**: Code quality, formatting, and logical correctness.
- **Tools**: VS Code, PowerShell, GitHub Actions, **Prettier**, **ESLint**.
- **Rule**: Never merge if formatting checks, linting, or tests fail.
- **Pre-Commit Checklist**:
  ```bash
  # Frontend (TypeScript/React)
  npm run lint          # Check for linting errors
  npm run lint -- --fix # Auto-fix linting errors
  npm run build         # Ensure build succeeds
  
  # Backend (.NET)
  dotnet build          # Ensure build succeeds
  dotnet test           # Run all tests
  ```

### Southern Workflow (Docker & Deploy)
- **Goal**: Environment parity and deployment reliability.
- **Rules**: A feature is only "Done" when it passes health checks in the container mesh. All infrastructure MUST be ephemeral-ready.

---

## 🌿 Git & Collaboration

### Git Tagging & Semantic Versioning
- **Versioning Standard**: Follow **Semantic Versioning** (SemVer): `MAJOR.MINOR.PATCH`.
- **Automated Management**: Use automation tools (e.g., **Google's Release Please**) to manage versions and changelogs.
- **Rule**: Never manually edit `CHANGELOG.md` files managed by automation.

### Commit & PR Strategy
- **Atomic Commits**: Each commit should represent a single logical change. Commits should be kept short and broken into smaller commits by features. If changes are related or dependent, they should be committed together.
- **Conventional Commits**: Use the `type(scope): description` format (feat, fix, chore, etc.).
  - **Types**: feat, fix, chore, refactor, docs, style, test, perf, ci, build
  - **Examples**: 
    - `feat(ui): add dark mode support`
    - `fix(api): resolve file upload timeout issue`
    - `chore: update dependencies`
- **Squash Merge**: Default for feature branches to keep history clean.
- **Pre-Commit Requirements**:
  1. Run linting and auto-fix: `npm run lint -- --fix` (Frontend)
  2. Ensure builds succeed: `npm run build` or `dotnet build`
  3. Stage changes: `git add .`
  4. Commit with conventional format: `git commit -m "type(scope): description"`
  5. Push to remote: `git push`

---

## 🛡️ Security & Performance Standards

### Security Headers
Every project must implement:
- **CSP (Content-Security-Policy)**
- **X-Frame-Options: DENY**
- **X-Content-Type-Options: nosniff**
- **Referrer-Policy: no-referrer**
- **Rate Limiting** (IP-based)

---

## 📜 Naming Conventions & Style

### TypeScript / React
- **Components**: PascalCase (e.g., `FileList`, `Dashboard`)
- **Files**: Match component name (e.g., `FileList.tsx`)
- **Hooks**: Start with `use` (e.g., `useTheme`, `useAuth`)
- **Types/Interfaces**: PascalCase (e.g., `FileItemDto`, `UserProfile`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `API_BASE_URL`, `MAX_FILE_SIZE`)
- **Functions/Variables**: camelCase (e.g., `handleSubmit`, `isLoading`)

### C# / .NET
- **Namespaces**: Use file-scoped namespaces (C#) and consistent directory structures.
- **Interfaces**: Start with `I` (e.g., `IStorageService`, `IRepository`).
- **Async**: Methods must end in `Async` (e.g., `GetFileAsync`, `SaveAsync`).
- **Classes**: PascalCase (e.g., `FileRepository`, `UserService`)
- **Private Fields**: _camelCase with underscore prefix (e.g., `_dbContext`, `_logger`)
- **Properties**: PascalCase (e.g., `FileName`, `CreatedDate`)

### General Rules
- **No `any` types**: Always use proper TypeScript types
- **Descriptive names**: Use meaningful, self-documenting names
- **Avoid abbreviations**: Unless widely understood (e.g., `id`, `url`, `api`)
- **Consistency**: Follow existing patterns in the codebase

---

## 📖 Architecture Decision Records (ADR)
Projects should document project-specific ADRs separately. Global platform choices include:
- **DB**: Prefer production-grade relational databases (Postgres) for parity.
- **Observability**: Structured Logging (Seq) + Centralized Log Dashboard.
- **Caching**: Distributed caching (Redis) for horizontally scalable services.

---

## ✅ Gold Standard Verification

### Frontend (TypeScript/React)
1. **Zero-Error Build**: `npm run build` succeeds without errors
2. **Zero `any` Types**: No `any` types in codebase (except auto-generated files)
3. **Linting Passes**: `npm run lint` returns 0 errors
4. **Formatted Code**: Code formatted with Prettier (auto-fix with `npm run lint -- --fix`)
5. **Type Safety**: All props, state, and API responses properly typed

### Backend (.NET)
1. **Zero-Error Build**: `dotnet build` succeeds without errors
2. **Tests Pass**: `dotnet test` all tests passing
3. **No Warnings**: Build produces no warnings
4. **Proper DI**: All dependencies injected via constructor
5. **CQRS Pattern**: Commands and Queries properly separated

### Infrastructure
1. **Stable Infrastructure**: Health checks green for all mesh services
2. **Docker Compose**: All services start successfully with `docker-compose up`
3. **Environment Parity**: Local dev matches production architecture
4. **Logs Structured**: All logs in JSON format with context

### General
1. **Standardized Formatting**: Ruleset-compliant project-wide
2. **Conventional Commits**: All commits follow `type(scope): description` format
3. **Documentation**: README and architecture docs up to date
4. **Horizon Guardian Compliance**: The codebase is audit-ready for the **Horizon Guardian** engine

---

## 🤖 Future Agent Instructions

### Before Starting Work
1. **Read the Blueprint**: Always check this file first
2. **Check Steering Files**: Review `.kiro/steering/` for project-specific rules
3. **Understand the Stack**: Identify if working on Frontend (React/TS) or Backend (.NET)

### During Development
1. **Follow Patterns**: Use existing code patterns as reference
2. **Type Everything**: No `any` types - use proper TypeScript types
3. **Modularize First**: If a file exceeds 200 lines, extract logic
4. **Audit the Chain**: Ensure changes propagate from Entity → DTO → Handler → API → Client
5. **Test Locally**: Run builds and tests before committing

### Before Committing
1. **Run Linting** (Frontend):
   ```bash
   cd FileManagementSystem.Web
   npm run lint -- --fix
   ```
2. **Check Build** (Frontend):
   ```bash
   npm run build
   ```
3. **Check Build** (Backend):
   ```bash
   dotnet build
   ```
4. **Commit with Convention**:
   ```bash
   git add .
   git commit -m "type(scope): description"
   git push
   ```

### Code Quality Checklist
- ✅ No `any` types
- ✅ Proper error handling
- ✅ Follows naming conventions
- ✅ Uses dependency injection
- ✅ Implements proper validation
- ✅ Passes linting and builds
- ✅ Committed with conventional format

---
*Created February 2026.*
*This document powers the Horizon Platform Excellence standards.*
