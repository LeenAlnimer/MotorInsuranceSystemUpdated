# Motor Insurance System API

A RESTful API for managing motor insurance operations built with ASP.NET Core 10 and Entity Framework Core. The system covers the full insurance lifecycle: user registration, car management, quote generation, policy issuance, and claim processing.

## Features

- JWT authentication with refresh tokens (2-hour access / 7-day refresh)
- Role-based authorization (Admin, Employee, Client)
- Insurance quote pricing engine with 8 configurable multipliers
- Full claim lifecycle with insured-value validation and concurrency protection
- Soft delete on all entities
- Pagination and filtering on all list endpoints
- Rate limiting on auth endpoints (login: 5/min, refresh: 10/min)
- Background service to auto-expire policies
- Email notifications on claim approve/reject (SMTP)
- Global exception handling and request logging middleware
- Health check endpoint

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 |
| Database | SQL Server |
| Auth | JWT Bearer + BCrypt |
| Testing | xUnit + Moq + EF In-Memory |
| Email | MailKit |

## Architecture

```
MotorInsuranceSystem/
├── MotorInsurance.API/
│   ├── Controllers/        # API endpoints (Auth, Users, Cars, Quotes, Policies, Claims)
│   ├── Services/           # Business logic layer
│   ├── Repositories/       # Data access layer
│   ├── Models/             # EF Core entities
│   ├── DTOs/               # Request/response objects
│   ├── Data/               # DbContext
│   ├── Migrations/         # EF Core migrations
│   ├── Middleware/         # Exception handling, request logging
│   └── Common/             # Enums, helpers, pagination, settings
└── MotorInsurance.Tests/   # Unit and integration tests
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or remote)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/YOUR_USERNAME/MotorInsuranceSystem.git
cd MotorInsuranceSystem
```

### 2. Set secrets (development)

The JWT key and database connection string are **not stored in appsettings.json**. Set them via dotnet user-secrets:

```bash
cd MotorInsurance.API

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=MotorInsuranceDb;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "your-super-secret-key-at-least-32-characters"
```

Optional — enable email notifications:

```bash
dotnet user-secrets set "Email:From" "your@gmail.com"
dotnet user-secrets set "Email:Password" "your-app-password"
```

### 3. Apply migrations

```bash
dotnet ef database update --project MotorInsurance.API
```

### 4. Run

```bash
dotnet run --project MotorInsurance.API
```

Swagger UI will be available at `https://localhost:{port}/swagger`.

## API Endpoints

### Auth — `/api/auth`

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/register` | Register as a client | No |
| POST | `/login` | Login (returns JWT + refresh token) | No |
| POST | `/refresh` | Refresh expired token | No |
| POST | `/logout` | Revoke refresh token | No |

### Users — `/api/users`

| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| POST | `/` | Create employee/admin account | Admin |
| GET | `/` | List all users (paginated) | Admin |
| PUT | `/{id}/role` | Update user role | Admin |
| GET | `/status` | System-wide stats | Admin |
| DELETE | `/{id}` | Soft delete user | Admin |
| GET | `/me` | Get own profile | All |
| PUT | `/me` | Update own profile | All |

### Cars — `/api/users/{userId}/cars`

| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | `/` | List user's cars | All |
| GET | `/{id}` | Get car by ID | All |
| POST | `/` | Add car | Admin, Employee |
| PUT | `/{id}` | Update car | Admin, Employee |
| DELETE | `/{id}` | Soft delete car | Admin, Employee |

### Quotes — `/api/quotes`

| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | `/` | List quotes | All |
| GET | `/{id}` | Get quote by ID | All |
| POST | `/generate` | Generate quote for a car | All |
| PUT | `/{id}/approve` | Approve quote → creates policy | Admin, Employee |
| PUT | `/{id}/reject` | Reject quote | Admin, Employee |
| DELETE | `/{id}` | Soft delete quote | Admin, Employee |

### Policies — `/api/policies`

| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | `/` | List policies | All |
| GET | `/{id}` | Get policy by ID | All |
| POST | `/{id}/renew` | Renew expired policy | Admin, Employee |
| POST | `/{id}/cancel` | Cancel active policy | Admin, Employee |

### Claims — `/api/claims`

| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | `/` | List claims | All |
| GET | `/{id}` | Get claim by ID | All |
| POST | `/` | Submit a claim | All |
| PUT | `/{id}/approve` | Approve claim | Admin, Employee |
| PUT | `/{id}/reject` | Reject claim | Admin, Employee |
| DELETE | `/{id}` | Soft delete claim | Admin, Employee |

> Clients only see their own resources. Admins and Employees see all.

## Insurance Pricing

Quote price is calculated as:

```
price = car.Price × BaseRatePercent (5%)
```

Then multiplied by applicable factors:

| Factor | Condition | Multiplier |
|--------|-----------|------------|
| New car | Age ≤ 3 years | ×1.2 |
| Old car | Age ≥ 8 years | ×0.9 |
| High value | Price > 30,000 | ×1.1 |
| Low value | Price < 10,000 | ×0.95 |
| Electric | Fuel = Electric | ×0.9 |
| Diesel | Fuel = Diesel | ×1.1 |
| Minimum | — | 300 JOD |

All values are configurable in `appsettings.json` under `InsurancePricing`.

## Running Tests

```bash
dotnet test
```

## Health Check

```
GET /health
```

Returns database connectivity status.
