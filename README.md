# Weekly Plan Tracker

A full-stack web application for agile teams to plan, track, and review weekly work. Teams can manage backlogs, assign tasks, track progress, and analyse delivery across categories.

🌐 **Live App:** https://weekly-plan-tracker-teal.vercel.app
🔗 **API:** https://wpt-api.onrender.com

---

## Tech Stack

| Layer    | Technology                           |
| -------- | ------------------------------------ |
| Frontend | Angular 17+, TypeScript, SCSS        |
| Backend  | ASP.NET Core 10, C#                  |
| Database | Neon (PostgreSQL)                    |
| ORM      | Entity Framework Core 9 + Npgsql     |
| Hosting  | Vercel (frontend) + Render (backend) |
| CI/CD    | GitHub Actions                       |
| Testing  | xUnit, WebApplicationFactory         |

---

## Project Structure

```
weekly-plan-tracker/
├── .github/
│   └── workflows/
│       ├── backend-ci.yml        # Build & test on every push/PR
│       ├── backend-cd.yml        # Build & test on push to dev
│       ├── frontend-ci.yml       # Build on every push/PR
│       └── frontend-cd.yml       # Build on push to dev
│
├── backend/
│   ├── Dockerfile                        # Docker image for Render deployment
│   ├── WeeklyPlanTracker.Api/            # ASP.NET Core Web API
│   │   ├── Controllers/                  # 8 REST controllers
│   │   │   ├── TeamMembersController.cs
│   │   │   ├── BacklogController.cs
│   │   │   ├── PlanningWeeksController.cs
│   │   │   ├── MemberPlansController.cs
│   │   │   ├── ProgressController.cs
│   │   │   ├── ResetController.cs
│   │   │   └── RestoreController.cs
│   │   └── Program.cs
│   │
│   ├── WeeklyPlanTracker.Core/           # Domain models, DTOs, interfaces
│   │   ├── Entities/                     # TeamMember, BacklogItem, PlanningWeek, etc.
│   │   ├── DTOs/                         # Request/response shapes
│   │   ├── Interfaces/                   # Service contracts
│   │   └── Enums/                        # Category, WeekState, ProgressStatus
│   │
│   ├── WeeklyPlanTracker.Infrastructure/ # EF Core, services, migrations
│   │   ├── Data/AppDbContext.cs
│   │   ├── Migrations/
│   │   └── Services/                     # Business logic implementations
│   │
│   └── WeeklyPlanTracker.Tests/          # xUnit integration & unit tests (108 tests)
│
└── frontend/
    └── weekly-plan-tracker-web/          # Angular SPA
        └── src/app/
            ├── core/                     # Services, models, guards, interceptors
            ├── features/                 # Feature modules
            │   ├── setup/                # First-time team setup
            │   ├── hub/                  # Main dashboard
            │   ├── backlog/              # Backlog management
            │   ├── planning/             # Week setup, plan-my-work, review & freeze
            │   ├── progress/             # Team dashboard, update progress, drill views
            │   ├── past-weeks/           # Historical week review
            │   └── team/                 # Team member management
            └── shared/                   # Reusable components, pipes
```

---

## Features

### Planning Workflow

The app follows a structured weekly planning cycle:

1. **Setup** — Create team members, designate a Team Lead
2. **Week Setup** — Create a planning week, set category allocations (Client Focused / Tech Debt / R&D), invite participants
3. **Plan My Work** — Each member claims backlog items and commits hours
4. **Review & Freeze** — Team Lead reviews plans, freezes the week when all members are ready
5. **Track Progress** — Members submit daily progress updates per task
6. **Team Dashboard** — Real-time visibility into team delivery with drill-down by category, member, and task
7. **Past Weeks** — Historical review of completed planning cycles

### Backlog Management

- Create and manage backlog items with categories (Client Focused, Tech Debt, R&D)
- Estimated effort tracking
- Status lifecycle: Available → Assigned → InProgress → Completed → Archived

### Progress Tracking

- Per-task progress updates with hours completed and status
- Team-level and member-level progress dashboards
- Drill-down views by category, member, and task
- History tracking per task assignment

### Data Management

- Full backup and restore via `/api/restore`
- Reset endpoint for demo/testing purposes

---

## Getting Started (Local Development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`

### Backend

```bash
cd backend
dotnet restore WeeklyPlanTracker.sln
dotnet run --project WeeklyPlanTracker.Api
```

API runs at `http://localhost:5020`
Swagger UI available at `http://localhost:5020/swagger`

The local database (`weeklyplanner.db`) is created automatically on first run via EF migrations using SQLite.

### Frontend

```bash
cd frontend/weekly-plan-tracker-web
npm install
ng serve
```

App runs at `http://localhost:4200`

### Running Tests

```bash
cd backend
dotnet test WeeklyPlanTracker.sln --verbosity normal
```

108 tests total — 60 unit tests (services) + 48 controller integration tests.

---

## API Reference

| Method | Endpoint                                            | Description                                        |
| ------ | --------------------------------------------------- | -------------------------------------------------- |
| GET    | `/api/team-members`                                 | List all team members                              |
| POST   | `/api/team-members`                                 | Create team member                                 |
| PATCH  | `/api/team-members/{id}/make-lead`                  | Promote to Team Lead                               |
| GET    | `/api/backlog`                                      | List backlog items (filterable by status/category) |
| POST   | `/api/backlog`                                      | Create backlog item                                |
| GET    | `/api/planning-weeks`                               | List all planning weeks                            |
| POST   | `/api/planning-weeks`                               | Create planning week                               |
| PATCH  | `/api/planning-weeks/{id}/open`                     | Open week for planning                             |
| PATCH  | `/api/planning-weeks/{id}/freeze`                   | Freeze week                                        |
| PATCH  | `/api/planning-weeks/{id}/finish`                   | Mark week complete                                 |
| GET    | `/api/member-plans/{weekId}/{memberId}`             | Get member's plan                                  |
| POST   | `/api/member-plans/{weekId}/{memberId}/assignments` | Claim a backlog item                               |
| GET    | `/api/progress/{weekId}`                            | Team progress for a week                           |
| POST   | `/api/progress/assignments/{id}`                    | Submit progress update                             |
| GET    | `/api/restore/backup`                               | Export full data backup                            |
| POST   | `/api/restore`                                      | Restore from backup                                |
| DELETE | `/api/reset`                                        | Reset all data                                     |

---

## Deployment

### Architecture

| Layer    | Service                      | URL                                         |
| -------- | ---------------------------- | ------------------------------------------- |
| Frontend | Vercel                       | https://weekly-plan-tracker-teal.vercel.app |
| Backend  | Render (Docker, Free tier)   | https://wpt-api.onrender.com                |
| Database | Neon (PostgreSQL, Free tier) | ap-southeast-1.aws.neon.tech                |

### CI/CD Pipeline

| Workflow    | Trigger                                       | Action                             |
| ----------- | --------------------------------------------- | ---------------------------------- |
| Backend CI  | Push/PR to `main` or `dev` (backend changes)  | Build + Test                       |
| Backend CD  | Push to `dev` (backend changes)               | Build + Test (Render auto-deploys) |
| Frontend CI | Push/PR to `main` or `dev` (frontend changes) | Build                              |
| Frontend CD | Push to `main` (frontend changes)             | Build (Vercel auto-deploys)        |

### Render Setup (Backend)

- **Language**: Docker
- **Branch**: `dev`
- **Root Directory**: `backend`
- **Environment Variable**:
  ```
  ConnectionStrings__DefaultConnection = Host=...neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=VerifyFull;
  ```

### Vercel Setup (Frontend)

- **Root Directory**: `frontend/weekly-plan-tracker-web`
- **Framework**: Angular
- **Output Directory**: `dist/weekly-plan-tracker-web/browser`
- **Production Branch**: `main`

---

## Environment Configuration

**Local** (`appsettings.json`): Uses SQLite — no setup required.

**Production** (Render Environment Variables):

```
ConnectionStrings__DefaultConnection = Host=<neon-host>;Database=neondb;Username=<user>;Password=<pass>;SSL Mode=VerifyFull;
```

The app automatically detects the environment — if the connection string contains `neon.tech` it uses PostgreSQL (Npgsql), otherwise falls back to SQLite for local development.

---

## Branch Strategy

- `main` — stable, production-ready code; triggers Vercel frontend deployment
- `dev` — active development; triggers Render backend deployment
- `feature/*` — feature branches, merged into `dev` via PR
- `fix/*` — bug fix branches
