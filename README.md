# BlindMatch PAS
### Blind-Match Project Approval System
**PUSL2020 – Software Development Tools and Practices | NSBM Green University (University of Plymouth)**

A secure web-based platform that matches student research project proposals with academic supervisors using a **blind review process** — supervisors browse proposals anonymously, and identities are only revealed after a match is confirmed.

---

## Team Members

| Member | Role | Branch |
|---|---|---|
| Nethupa Rakjitha | Project Lead & Setup | feature/project-setup |
| Anupa | Database Admin & Models | feature/database-setup |
| Udan | Student Module Developer | feature/student-module |
| Vigee | Supervisor Module Developer | feature/supervisor-module |
| Induwara | Admin Module Developer | feature/admin-module |
| Dissanayaka Dissanayaka | Core Services Developer | feature/core-services |
| PDCS-Codes | Unit Testing Engineer | feature/unit-tests |
| VihangaDissanayake | Integration Testing Engineer | feature/integration-tests |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8 MVC |
| ORM | Entity Framework Core 8 |
| Database | SQL Server 2022 |
| Auth | ASP.NET Core Identity + RBAC |
| Frontend | Bootstrap 5, Razor Views |
| Testing | xUnit, Moq, EF Core InMemory |

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server 2022

### 1. Clone the repository
```bash
git clone https://github.com/oshada28nilendra-debug/BlindMatchPAS-Team.git
cd BlindMatchPAS-Team
```

### 2. Update connection string
Open `src/BlindMatchPAS.Web/appsettings.json` and update:
```json
"DefaultConnection": "Server=localhost;Database=BlindMatchPAS;Trusted_Connection=True;TrustServerCertificate=True;"
```

### 3. Run the application
```bash
cd src/BlindMatchPAS.Web
dotnet restore
dotnet run
```

Open browser at `http://localhost:5194`

---

## Default Test Accounts

| Role | Email | Password |
|---|---|---|
| System Admin | admin@pas.edu | Admin@123456 |
| Module Leader | leader@pas.edu | Admin@123456 |
| Supervisor | supervisor@pas.edu | Admin@123456 |
| Student | student@pas.edu | Admin@123456 |

---

## Running Tests

```bash
cd tests/BlindMatchPAS.Tests
dotnet test
```

---

## Key Features

- **Blind Matching** — Supervisors browse proposals without seeing student identities
- **Identity Reveal** — Triggered automatically when supervisor confirms a match
- **Role-Based Access Control** — 4 roles with strictly separated access
- **EF Core Migrations** — Auto-applied on startup
- **37 Tests** — Unit, integration and journey tests
