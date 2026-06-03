# ValueKu — Personal Financial Tracker

A comprehensive personal net-worth & wealth tracker built with **.NET 9 / ASP.NET Core MVC**, following a **Clean Architecture monolith** pattern. It tracks assets (with automatic depreciation/appreciation), accounts, and transactions; projects future net worth; and exports monthly PDF statements. Default currency is **MYR (Malaysian Ringgit)**.

## Tech stack

| Concern | Choice |
|---|---|
| Runtime | .NET 9 |
| Web | ASP.NET Core MVC, Razor views |
| Data | EF Core 9 (Code-First) + Microsoft SQL Server 2022 |
| Caching | .NET 9 `HybridCache` (in-memory, tag-based invalidation) |
| Auth | Custom cookie auth over a domain `User` entity (`PasswordHasher<User>`) |
| Background | `BackgroundService` + `PeriodicTimer` (daily revaluation) |
| Reporting | QuestPDF (Community license) |
| UI | Bootstrap 5.3, Bootstrap Icons, ApexCharts (CDN) |

## Features

1. **Dynamic asset & value management** — straight-line (linear) or compounding valuation; a daily background worker recomputes values and appends to `AssetValuationHistory`; net-worth/allocation metrics cached in `HybridCache`.
2. **Predictive forecasting** — aggregates current assets + account balances and projects net worth over 5 / 10 / 30 years using each asset's rate plus the user's average monthly cash flow.
3. **Automated reporting** — one-click PDF monthly statement: net-worth balance sheet, monthly cash-flow summary, and a 12-month net-worth trend table.
4. **Budgeting** — monthly spending limits per category with progress bars, over-budget alerts, and a dashboard budget strip.
5. **Savings goals** — targets with a date, progress tracking, suggested monthly contribution, and quick contributions.
6. **Zakat calculator** — Malaysian/Islamic: sums zakat-eligible wealth (cash, ASB, unit trusts, equity, gold) against a configurable nisab (~RM29,961) and computes 2.5% payable; records payment as an expense.
7. **Spending insights** — dashboard income-vs-expense trend, spending-by-category donut, and a savings-rate KPI.
8. **Transaction management** — filter by account, date range, type, category and search, with pagination.
9. **Settings** — change password; account overview.

Malaysian touches: MYR formatting, EPF/ASB/Unit Trust/Tabung Haji asset categories, an e-Wallet account type, and zakat.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (SQL Server runs under `linux/amd64` emulation on Apple Silicon)
- `dotnet-ef` tools: `dotnet tool install --global dotnet-ef`

## Getting started

Secrets are **not** committed — provide them locally via `.env` (for Docker) and .NET user-secrets (for the app):

```bash
# 1. Database password for the local SQL container
cp .env.example .env            # then edit MSSQL_SA_PASSWORD to a strong value
docker compose up -d            # starts SQL Server (waits ~30s to become healthy)

# 2. Tell the app how to reach the DB + which seed password to use (Development only)
cd ValueKu
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=ValueKu;User Id=sa;Password=<same-as-.env>;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
dotnet user-secrets set "SeedUser:Password" "Admin123!"
cd ..

# 3. Run (applies migrations + seeds demo data on first run)
dotnet run --project ValueKu
```

Then browse to the URL printed in the console (e.g. `https://localhost:xxxx`).

> In production the connection string and seed password come from **App Service application settings**, not from the repo.

### Default login

The first run seeds an `admin` user (username/email in `appsettings.json`; the **password** comes from `SeedUser:Password` in user-secrets / App Service settings) plus a self-registration page. The hosted demo uses:

| Username | Password |
|---|---|
| `admin` | `Admin123!` |

> This is a deliberately-public **demo** account on a throwaway environment — it guards nothing sensitive.

The seeder also creates a realistic demo portfolio (assets, accounts, ~12 months of transactions, and back-dated valuation history) so the dashboard, charts, and PDF are populated immediately.

### Sign in with Google (optional)

Users can also **Continue with Google** (the first Google sign-in auto-creates a passwordless local account; an existing account with the same email is linked). Password login and manual registration remain available. The Google button only appears once credentials are configured.

To enable it:

1. In the [Google Cloud Console](https://console.cloud.google.com/) → **APIs & Services → Credentials**, create an **OAuth client ID** of type **Web application**.
2. Add an **Authorized redirect URI**: `https://localhost:<port>/signin-google` (use the HTTPS port shown when you run the app; add the `http://` variant too if you browse over HTTP).
3. Store the credentials (the client secret is sensitive — prefer user-secrets):
   ```bash
   cd ValueKu
   dotnet user-secrets init
   dotnet user-secrets set "Authentication:Google:ClientId"     "<your-client-id>"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>"
   ```
   (Alternatively, fill the empty `Authentication:Google` keys in `appsettings.json` for local-only use.)

## Configuration

`ValueKu/appsettings.json`:

- `ConnectionStrings:DefaultConnection` — SQL Server connection (defaults to the Docker container on `localhost,1433`).
- `SeedUser` — credentials for the seeded user.
- `ValuationWorker` — `IntervalHours` (default 24) and `RunOnStartup` (default true).

> ⚠️ **Security note:** the SA password and seed credentials live in `docker-compose.yml` / `appsettings.json` for **local development only**. For anything beyond local, move them to user-secrets or environment variables.

## Development environment

Built and verified on macOS (Apple Silicon M4), JetBrains Rider, .NET 9 SDK, SQL Server 2022 in Docker.
