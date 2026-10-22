# TaskFlow - Team Task Management System

A role-based full-stack task management application. Admins manage teams, managers create and assign tasks to their team members, and users track and update their assigned tasks. Built with ASP.NET Core (.NET 8), SQL Server, Entity Framework Core and React.

## Features

- JWT authentication with salted PBKDF2 password hashing and token expiry handling (the UI logs out automatically when the token expires)
- Role-based access control for **Admin**, **Manager** and **User**
- Team management: admins create, edit and delete teams and assign managers; managers add and remove members of their own team
- Task creation, assignment and status tracking (To Do, In Progress, Done) with priority and deadline
- Comments on tasks for collaboration
- Notifications on **task assignment** and **task status update**: in-app notifications (bell icon) plus a mock email written to the application log
- Dashboard with status counts, overdue count, a per-user status overview (Admin and Manager) and filtering by status, priority, deadline range and assignee
- Responsive, accessible UI (labelled form fields, keyboard-friendly dialogs, skip link, screen reader labels)
- Swagger / OpenAPI documentation with JWT support

## Tech stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 8), REST |
| ORM | Entity Framework Core 8 (code-first migrations) |
| Database | SQL Server |
| Auth | JWT bearer tokens, PBKDF2-SHA256 password hashing |
| Frontend | React 19, Vite, React Router, Axios |
| API docs | Swagger (Swashbuckle) |

## Project structure

```
TaskManagement/
├─ backend/
│  ├─ TaskManagement.sln
│  └─ TaskManagement.Api/
│     ├─ Controllers/     REST controllers (thin, role-protected)
│     ├─ DTOs/            Request / response models with validation
│     ├─ Services/        Access rules, notifications, password hashing
│     ├─ Models/          EF Core entities
│     ├─ Data/            DbContext and database seeder
│     ├─ Middleware/      Global exception handling
│     └─ Migrations/      EF Core migrations
├─ frontend/
│  └─ src/
│     ├─ api/             Axios client and error handling
│     ├─ context/         Authentication provider
│     ├─ components/      Layout, modals, notification bell
│     └─ pages/           Login, Dashboard, Teams
├─ docs/                  API documentation (Postman collection, OpenAPI)
└─ README.md
```

## Roles and permissions

| Capability | Admin | Manager | User |
|---|:-:|:-:|:-:|
| Create / edit / delete teams, assign a manager | Yes | No | No |
| Add / remove team members | Any team | Own team | No |
| Change user roles | Yes | No | No |
| Create tasks | Yes | Yes | No |
| Assign tasks to | Managers and Users | Members of own team | - |
| View tasks | All | Own team's tasks and tasks they created | Assigned to them |
| Update task status | Yes | Visible tasks | Assigned to them |
| Comment on tasks | Visible tasks | Visible tasks | Assigned to them |
| Per-user dashboard overview | Yes | Own team | No |

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js 20 or newer
- SQL Server (LocalDB, SQL Server Express or any SQL Server instance)

### 1. Run the API

The default connection string in `backend/TaskManagement.Api/appsettings.json` uses SQL Server LocalDB. To use another SQL Server, either edit it or create `appsettings.Development.json` next to it (git-ignored) with your own `ConnectionStrings:DefaultConnection`.

```bash
cd backend/TaskManagement.Api
dotnet run --launch-profile https
```

On startup the API applies the EF Core migrations and seeds demo data. Swagger UI is available at `https://localhost:7019/swagger`.

If the browser does not trust the development certificate, run `dotnet dev-certs https --trust` once.

### 2. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The API address is read from `VITE_API_URL` (see `frontend/.env.example`); it defaults to `https://localhost:7019/api`.

## Sample credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@taskflow.com | Admin@123 |
| Manager | manager@taskflow.com | Manager@123 |
| User | user1@taskflow.com | User@1234 |
| User | user2@taskflow.com | User@1234 |

The seed data also creates an "Engineering" team (managed by the manager) and a few sample tasks. Public registration always creates a **User**; an admin can change roles on the Teams page.

## Configuration

Any setting can be overridden with environment variables (double underscore for nesting):

| Setting | Environment variable |
|---|---|
| Database connection | `ConnectionStrings__DefaultConnection` |
| JWT signing key (at least 32 characters) | `Jwt__Key` |
| JWT lifetime in minutes | `Jwt__ExpiryMinutes` |
| Allowed frontend origin | `Cors__Origins__0` |

The JWT key in `appsettings.json` is for local development only. Use a different secret in any shared or deployed environment.

## API overview

Full interactive documentation is in Swagger UI (`/swagger`). Use the **Authorize** button and paste the token returned by `POST /api/Auth/login`.

| Area | Endpoints |
|---|---|
| Auth | `POST /api/Auth/register`, `POST /api/Auth/login` |
| Users | `GET /api/Users`, `GET /api/Users/assignable`, `PATCH /api/Users/{id}/role` |
| Teams | `GET /api/Teams`, `GET /api/Teams/{id}`, `POST /api/Teams`, `PUT /api/Teams/{id}`, `DELETE /api/Teams/{id}`, `POST /api/Teams/{id}/members`, `DELETE /api/Teams/{id}/members/{userId}` |
| Tasks | `GET /api/Task` (filters: `status`, `priority`, `deadlineFrom`, `deadlineTo`, `assignedToId`), `GET /api/Task/{id}`, `POST /api/Task`, `PATCH /api/Task/{id}/status` |
| Comments | `GET /api/Comment/{taskId}`, `POST /api/Comment/{taskId}` |
| Notifications | `GET /api/Notification`, `POST /api/Notification/{id}/read`, `POST /api/Notification/read-all` |
| Dashboard | `GET /api/Dashboard/summary`, `GET /api/Dashboard/per-user` |

Errors use a consistent JSON shape: `{ "message": "...", "errors": { ... } }` for validation failures (HTTP 400) and `{ "message": "..." }` for other errors (401, 403, 404, 409, 500).

A Postman collection is available in `docs/TaskFlow.postman_collection.json`.

## Design notes

- **Access rules in one place:** `TaskAccessService` decides which tasks a user can see and who they can assign to, and is shared by the task, comment and dashboard endpoints.
- **Database design:** foreign keys between users, teams, tasks, comments and notifications; a unique index on user email and team name; indexes on the task columns used for filtering. Restrictive delete behaviour on task ownership prevents orphaned data.
- **Notifications:** a task assignment notifies the assignee. A status change notifies the assignee and the task creator (except the person who made the change). Each notification is stored for the in-app bell and logged as a `[MOCK EMAIL]` entry.
- **Validation and errors:** DTO validation attributes, a custom model-state response and a global exception middleware that never leaks stack traces.

## Known limitations

- A role change takes effect after the user logs in again, because the role is stored in the JWT.
- No refresh tokens; the session ends when the token expires (60 minutes by default).
- Tasks cannot be edited, reassigned or deleted after creation (not part of the brief).
- Email delivery is mocked; no SMTP integration.
## Testing

The backend has xUnit unit tests (access rules, password hashing, notifications) and integration tests that run the real API in memory with an in-memory database.

```bash
dotnet test backend/TaskManagement.sln
```

## Continuous integration

A GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push and pull request to `main`. It builds and tests the backend, and lints and builds the frontend.