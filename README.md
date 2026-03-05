# TaskFlow MVC

TaskFlow MVC is an ASP.NET Core MVC application for team, project, and task collaboration.

## Implemented highlights

- Authentication with ASP.NET Core Identity
- Optional Google OAuth login
- Email OTP login flow and 2FA support hooks
- Session timeout and device session tracking
- Login activity history
- Admin user management with roles
- Team, project, milestone, and task management
- Kanban drag-and-drop status updates
- Notifications (in-app + email hooks)
- Real-time notifications via SignalR
- Task comments with edit/delete
- User mentions in comments (`@email`, `@username`, `@userId`)
- Emoji reactions on comments
- File attachments in comments (docs/images/videos/pdfs)
- File preview + download endpoints
- Attachment versioning per task/comment/file name
- Time tracking (start/stop timer + manual log)
- Reports dashboard with CSV export

## Prerequisites

1. Windows with SQL Server LocalDB installed.
2. .NET 10 SDK installed.
3. `dotnet-ef` tool installed.

Check SDK:

```powershell
dotnet --version
```

Install EF tool (if missing):

```powershell
dotnet tool install --global dotnet-ef
```

## Run from scratch (step by step)

1. Open terminal and move to the project:

```powershell
cd C:\Users\PuneetGupta\Desktop\TaskFlowMvc
```

2. Restore packages:

```powershell
dotnet restore
```

3. Configure connection string.

Use `appsettings.Development.json` (or user-secrets) and ensure `ConnectionStrings:DefaultConnection` points to LocalDB, for example:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TaskFlowMvcDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

4. (Optional) Configure secrets for Google login and SMTP:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"
dotnet user-secrets set "Email:Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:Username" "yourgmail@gmail.com"
dotnet user-secrets set "Email:Smtp:Password" "your-app-password"
dotnet user-secrets set "Email:Smtp:FromEmail" "yourgmail@gmail.com"
dotnet user-secrets set "Email:Smtp:FromName" "TaskFlow MVC"
dotnet user-secrets set "Email:Smtp:EnableSsl" "true"
```

5. Apply database migrations:

```powershell
dotnet ef database update --project C:\Users\PuneetGupta\Desktop\TaskFlowMvc\TaskFlowMvc.csproj
```

6. Build the project:

```powershell
dotnet build C:\Users\PuneetGupta\Desktop\TaskFlowMvc\TaskFlowMvc.csproj
```

7. Run the app:

```powershell
dotnet run
```

8. Open in browser:

`https://localhost:5000`

## First-run notes

- The first registered user is auto-promoted to `Admin` if no admin exists.
- Uploaded comment files are saved under `wwwroot/uploads/comments`.
- Attachments are served through authorized endpoints:
  - `/Attachments/Preview/{id}`
  - `/Attachments/Download/{id}`
- Notification hub endpoint:
  - `/hubs/notifications`

## Useful commands

Build only:

```powershell
dotnet build C:\Users\PuneetGupta\Desktop\TaskFlowMvc\TaskFlowMvc.csproj
```

Run tests:

```powershell
dotnet test C:\Users\PuneetGupta\Desktop\TaskFlowMvc\TaskFlowMvc.csproj
```

List migrations:

```powershell
dotnet ef migrations list --project C:\Users\PuneetGupta\Desktop\TaskFlowMvc\TaskFlowMvc.csproj
```

## Troubleshooting

1. `dotnet ef` not found:
- Install: `dotnet tool install --global dotnet-ef`
- Restart terminal.

2. Database update errors:
- Confirm LocalDB is installed.
- Verify connection string.
- Re-run `dotnet ef database update`.

3. Google login button not visible:
- Set both `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret`.

4. Email/OTP not sending:
- Use Gmail App Password for SMTP.
- Verify all `Email:Smtp:*` values.

5. Attachment preview/download fails:
- Check file exists under `wwwroot/uploads/comments`.
- Confirm authenticated user has access to the project/task.
