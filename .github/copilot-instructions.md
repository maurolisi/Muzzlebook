# Copilot instructions for Muzzlebook

## Project snapshot

- Muzzlebook is an ASP.NET Core Razor Pages app targeting .NET 10 (`net10.0`).
- The app is a dog-management system: it stores dog profiles, medical visits, vaccinations, parasite treatments, and clinical documents in SQLite.
- The primary persistence layer is SQLite (`Data/muzzlebook.db`) with Dapper queries in page models; there is no EF Core DbContext or repository layer in this app.
- Authentication is cookie-based ASP.NET Core auth, and file/media uploads are sent to Cloudflare R2 using the AWS S3 SDK.
- The main app logic lives under `Pages/`, with supporting models in `Models/`, schema setup in `Data/`, and deployment notes in `Comandi.txt`.
- There is no dedicated root README or CONTRIBUTING file in this repo; operational guidance is in `Comandi.txt`.

## Build, test, run, and deploy commands

Use these commands from the repo root:

- Sync before starting a new task: `git pull origin main`
- Check repo status: `git fetch && git status`
- Restore dependencies: `dotnet restore`
- Build the app/solution: `dotnet build Muzzlebook.slnx --nologo`
- Run locally: `dotnet run --project Muzzlebook.csproj`
- Publish for deployment: `dotnet publish Muzzlebook.csproj -c Release -o /var/www/muzzlebook`
- Repository-level tests: `dotnet test Muzzlebook.slnx --nologo`

This repository currently has no dedicated test project or lint toolchain. Treat `dotnet build` as the validation baseline unless a test project is added later. When tests exist, prefer running a single test or class by name with a filter, for example:

- `dotnet test path/to/Project.Tests.csproj --filter "Name~MySpecificTest"`
- `dotnet test path/to/Project.Tests.csproj --filter "FullyQualifiedName~Namespace.MySpecificTest"`

## High-level architecture

### App startup and dependency wiring

`Program.cs` is the central entry point and wires the app together:

- Creates and persists ASP.NET Core Data Protection keys under `Data/Keys/`
- Adds Razor Pages and cookie-based authentication
- Reads `CloudflareR2` settings from configuration and registers an `IAmazonS3` client
- Registers the SQLite `IDbConnection` used by page models throughout the app
- Maps Razor Pages and starts the application

Keep configuration changes aligned with `appsettings.json`, `appsettings.Development.json`, and environment/user-secret values used in deployment.

### Data layer and schema bootstrapping

The schema is intentionally lightweight and bootstrap-based:

- `Data/DatabaseInitializer.cs` creates the SQLite tables and indexes for breeds, dogs, and medical records
- The initializer seeds breed data from `Data/breeds.json`
- `appsettings.json` defines the default connection string: `Data Source=Data/muzzlebook.db;`
- `Models/` contains the POCOs used by Dapper queries (`User`, `Dog`, `Breed`, etc.)

When adding a new data feature, keep the schema update in `Data/DatabaseInitializer.cs` and the corresponding SQL in the relevant Razor Page model together.

### Razor Pages structure

The app is page-oriented rather than controller-based:

- `Pages/*.cshtml` and matching `*.cshtml.cs` files hold the view and page logic
- Page models rely on `OnGetAsync` / `OnPostAsync` patterns and form binding with `[BindProperty]`
- Input validation is usually explicit via `ModelState` and value checks before executing SQL

The repository convention is direct SQL in each page model, with minimal abstraction and no service/repository layer unless a feature already has one.

### Storage and media

This project mixes local SQLite persistence with Cloudflare R2-backed storage:

- Local app data and database files live under `Data/`
- Uploaded media or remote document links are handled via `IAmazonS3` and Cloudflare R2 configuration
- Keep Cloudflare R2 secrets aligned with environment variables/user secrets; required keys include `CloudflareR2:AccountId`, `AccessKeyId`, and `SecretAccessKey`

## Key conventions specific to this codebase

- Prefer Dapper + raw SQL in page models rather than introducing a new ORM, repository abstraction, or controller layer.
- Preserve the existing project structure: `Pages/` for Razor Pages, `Models/` for DTOs, `Data/` for initialization and seed data, and `Data/Keys/` for ASP.NET Data Protection keys.
- Follow the existing cookie-auth pattern used in the login/register pages: hash with `PasswordHasher<User>`, create claims with `ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, and `ClaimTypes.Email`, then sign in with `HttpContext.SignInAsync(...)` using `CookieAuthenticationDefaults.AuthenticationScheme`.
- Keep user-facing strings and labels consistent with the current Italian naming used in the app (`Benessere`, `Meticci`, `Musi`, etc.).
- Do not rename the SQLite path or move the app data folder without checking the config and startup code.
- If you need to update the schema, verify the database initializer and any relevant page queries together; this project depends on schema bootstrap rather than migrations.
- The Git sync and deployment workflow documented in `Comandi.txt` is the operational source of truth for this repository, especially for VPS publishing.

## Repository workflow notes

- Before working, pull the latest changes from the default branch: `git pull origin main`
- For deployment and production sync, follow the commands in `Comandi.txt`; they cover both GitHub sync and the IONOS VPS publish flow
- Keep changes small and aligned with the page-based design of this app; avoid broad framework changes unless explicitly requested
