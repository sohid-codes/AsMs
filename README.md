# Assignment & Submission Management System (ASMS)

ASMS is a role-based school or college application for managing assignments, student submissions, marks, and feedback. It provides separate Admin, Teacher, and Student workflows through a Next.js frontend and an ASP.NET Core REST API.

## Features

- JWT login and API-enforced role authorization for Admin, Teacher, and Student.
- Admin management of users, classes, subjects, teacher allocations, and student enrollments.
- Teacher assignment creation, editing, publishing, deletion, submission review, grading, and feedback.
- Student assignment discovery, submission, pre-deadline updates, and result viewing.
- Swagger/OpenAPI for API discovery and testing.
- Serilog API file logs and Next.js client error file logs.
- EF Core migrations and development seed accounts.

## Technology

| Area | Technology |
| --- | --- |
| Frontend | Next.js 16, React 19, TypeScript, Tailwind CSS |
| Backend | ASP.NET Core Web API on .NET 10, C# |
| Persistence | EF Core 10 and Microsoft SQL Server / LocalDB |
| Identity | ASP.NET Core Identity, JWT bearer tokens, role authorization |
| Dependency injection | Autofac |
| Logging | Serilog (API), file-backed Next.js error logging |

The recruitment brief permits PostgreSQL or MongoDB. SQL Server was deliberately selected for this implementation, as agreed for the project; the supplied EF Core migration targets SQL Server.

## Solution structure

```text
src/
  ASMS.Api/              ASP.NET Core API, Swagger, JWT configuration
  AsMs.Application/      Services, repositories, unit of work, Autofac module
  AsMs.Data/             EF Core context, Identity model, migrations, shared data infrastructure
  AsMs.Domain/           Entities and enums
  AsMs.Web/              Next.js frontend
  Tests/                 Test output directory (source test project is not currently included)
```

The application flow is `Controller -> Service -> Unit of Work / Repository -> DbContext`. Controllers handle HTTP; services contain workflow rules; repositories contain data access.

## Prerequisites

- Windows 10/11 or Windows Server (the default configuration uses LocalDB).
- [.NET 10 SDK](https://dotnet.microsoft.com/download).
- SQL Server 2022+ or SQL Server LocalDB.
- Node.js 20 LTS or newer and npm.
- Git (optional, for cloning).
- A trusted ASP.NET Core development certificate for local HTTPS.

For a shared server or production deployment, use SQL Server Express/Standard/Enterprise rather than LocalDB, and install the .NET 10 ASP.NET Core Hosting Bundle when deploying the API to IIS.

## Configuration

### API

Development configuration is in `src/ASMS.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ASMSDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Serilog": {
    "LogDirectory": "C:\\Logs\\ASMS\\ASMS.Api.Log"
  },
  "Jwt": {
    "Issuer": "ASMS.Api",
    "Audience": "ASMS.Web",
    "SigningKey": "replace-this-with-a-long-random-secret",
    "ExpiryMinutes": 15
  }
}
```

Never commit a production connection string or JWT signing key. Configure them through environment variables, a secret store, or server-managed `appsettings.Production.json`. The equivalent environment-variable keys use double underscores, for example:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=SERVER;Database=ASMSDb;Trusted_Connection=True;TrustServerCertificate=True"
$env:Jwt__SigningKey = "a-long-random-production-secret"
$env:Jwt__Issuer = "ASMS.Api"
$env:Jwt__Audience = "ASMS.Web"
$env:Serilog__LogDirectory = "C:\Logs\ASMS\ASMS.Api.Log"
```

### Web

Copy `src/AsMs.Web/.env.example` to `src/AsMs.Web/.env.local` and set:

```dotenv
NEXT_PUBLIC_API_BASE_URL=https://localhost:7295
WEB_LOG_DIRECTORY=C:\Logs\ASMS\AsMs.Web.Log
```

For a deployed frontend, `NEXT_PUBLIC_API_BASE_URL` must be the public HTTPS address of the API. Because this variable is embedded during `next build`, set it before building the web application.

## Database setup

From the `src` directory:

```powershell
dotnet restore AsMs.slnx
dotnet ef database update --project AsMs.Data --startup-project ASMS.Api
```

The initial migration is stored in `src/AsMs.Data/Migrations`. It creates the ASMS tables and ASP.NET Core Identity tables.

To create a migration after a future model change:

```powershell
dotnet ef migrations add MeaningfulMigrationName --project AsMs.Data --startup-project ASMS.Api
dotnet ef database update --project AsMs.Data --startup-project ASMS.Api
```

## Run locally

1. Trust the development certificate once:

   ```powershell
   dotnet dev-certs https --trust
   ```

2. Apply the database migration as described above.

3. Start the API:

   ```powershell
   cd src/ASMS.Api
   dotnet run --launch-profile https
   ```

   Swagger is available at `https://localhost:7295/swagger`.

4. In another terminal, start the web application:

   ```powershell
   cd src/AsMs.Web
   npm install
   npm run dev
   ```

5. Browse to `http://localhost:3000`.

## Demo accounts

Development startup seeds the following roles and users when they do not already exist:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@asms.local` | `Admin@123` |
| Teacher | `teacher@asms.local` | `Teacher@123` |
| Student | `student@asms.local` | `Student@123` |

These are development-only credentials. Change or remove them before any public deployment.

## Deployment

### API deployment to IIS

1. Install the .NET 10 ASP.NET Core Hosting Bundle on the server and restart IIS.
2. Provision a SQL Server database and give the IIS application identity the required database access. For SQL authentication, use a protected connection string rather than committing credentials.
3. Set production configuration, especially `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`, and `Serilog__LogDirectory`.
4. Create the database schema during the release process:

   ```powershell
   dotnet ef database update --project src/AsMs.Data --startup-project src/ASMS.Api --configuration Release
   ```

5. Publish the API:

   ```powershell
   dotnet publish src/ASMS.Api/ASMS.Api.csproj -c Release -o .\publish\api
   ```

6. Create an IIS site/app pool pointing to `publish\api`, configure HTTPS, and grant the app pool Modify access to `C:\Logs\ASMS\ASMS.Api.Log`.
7. Confirm `/swagger` is available only if you intentionally enable Swagger outside Development. The current code exposes Swagger in Development only.

### Web deployment

1. Set the production API URL and logging folder:

   ```powershell
   cd src/AsMs.Web
   $env:NEXT_PUBLIC_API_BASE_URL = "https://api.example.com"
   $env:WEB_LOG_DIRECTORY = "C:\Logs\ASMS\AsMs.Web.Log"
   npm ci
   npm run build
   ```

2. Run the production server with a process manager or IIS reverse proxy:

   ```powershell
   npm run start
   ```

3. Grant the web process identity Modify access to `C:\Logs\ASMS\AsMs.Web.Log`.
4. Configure TLS and a reverse proxy so the browser reaches both applications over HTTPS.

The API currently allows local Next.js development origins through its CORS policy. Before deploying the frontend on a different origin, update the `WebClient` CORS origins in `src/ASMS.Api/Program.cs` to the exact production frontend URL, then redeploy the API.

## Logging

| Application | Folder |
| --- | --- |
| API | `C:\Logs\ASMS\ASMS.Api.Log` |
| Web client errors | `C:\Logs\ASMS\AsMs.Web.Log` |

Ensure the relevant service account has permission to create and append files in its log folder. Set log retention, disk monitoring, and central log collection according to the hosting environment.

## Testing

Build the backend:

```powershell
cd src
dotnet build AsMs.slnx
```

Build the frontend:

```powershell
cd src/AsMs.Web
npm run build
```

Run the application-service unit tests with FakeItEasy:

```powershell
dotnet test src/Tests/AsMs.Application.Tests/AsMs.Application.Tests.csproj
``` 

The current test suite covers expired and duplicate submissions, teacher ownership during grading, and maximum-mark validation. Add API authorization tests and broader workflow coverage before final submission.

## Assumptions and known limitations

- A student can have one submission per assignment and can update it only before the deadline.
- A teacher can grade only submissions associated with that teacher's allocation.
- Assignments with submissions cannot be deleted.
- Roles are enforced by the API; the frontend is a convenience layer, not a security boundary.
- No document/file upload, notifications, pagination, advanced filtering, or email workflow is implemented.
- SQL Server is used instead of the brief's PostgreSQL/MongoDB options by project decision.
- Production CORS origins and secrets must be configured before deployment.

## Submission checklist

- Commit the source, migrations, this README, and `.env.example` files.
- Do not commit `.env.local`, production `appsettings.*.json`, database backups containing sensitive data, or real secrets.
- Verify database migration, API Swagger endpoint, frontend login, and all three demo roles from a clean machine.
- Add and execute automated tests before submitting the repository link.
