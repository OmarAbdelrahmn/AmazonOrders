# Employee Order API

A clean, production-ready ASP.NET Core 8 Web API for tracking employee orders/assignments,
with JWT authentication, two roles (Admin / Supervisor), image upload, and rich admin reports.

---

## 🚀 Quick Start

### 1. Prerequisites
- .NET 8 SDK
- SQL Server (or change the connection string to SQLite/PostgreSQL)

### 2. Configure
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=EmployeeOrderDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyMustBe32CharsLong!",   // ← change this
    "Issuer": "EmployeeOrderApi",
    "Audience": "EmployeeOrderClient",
    "ExpiryHours": "8"
  }
}
```

### 3. Run migrations & start
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Swagger UI opens at: **http://localhost:5000**

### 4. Default seed accounts
| Role       | Username   | Password         |
|------------|------------|------------------|
| Admin      | admin      | Admin@123456     |
| Supervisor | supervisor | Supervisor@123456|

---

## 📦 Project Structure

```
EmployeeOrderApi/
├── Controllers/
│   ├── AuthController.cs        # Login, Register, Users
│   ├── EmployeeController.cs    # CRUD + image upload
│   ├── OrderController.cs       # Order lifecycle
│   └── ReportController.cs      # Admin analytics
├── Data/
│   ├── AppDbContext.cs          # EF Core + Identity context
│   └── DbSeeder.cs             # Roles + default users
├── Domain/
│   ├── ApplicationUser.cs       # Identity user + Roles constants
│   ├── Employee.cs              # Employee entity
│   └── Order.cs                # Order entity
├── Services/
│   ├── Auth/AuthService.cs      # JWT generation, user management
│   ├── Employee/EmployeeService.cs
│   ├── Order/OrderService.cs    # Core order business logic
│   ├── Report/ReportService.cs  # All analytics
│   ├── Image/ImageService.cs    # wwwroot/images management
│   └── Common/Result.cs        # Result<T> / Error pattern
├── Contracts/                   # Request / Response DTOs
├── Extensions/ResultExtensions.cs  # Result → IActionResult
├── Middleware/ExceptionMiddleware.cs
└── wwwroot/images/             # Uploaded employee images served statically
```

---

## 🔑 Authentication

All endpoints except `POST /api/auth/login` require a JWT Bearer token.

```
Authorization: Bearer <token>
```

---

## 📋 API Reference

### Auth  `/api/auth`
| Method | Endpoint                         | Role       | Description           |
|--------|----------------------------------|------------|-----------------------|
| POST   | `/login`                         | Public     | Get JWT token         |
| POST   | `/register`                      | Admin      | Create user           |
| POST   | `/change-password`               | Any        | Change own password   |
| GET    | `/users`                         | Admin      | List all users        |
| PATCH  | `/users/{username}/toggle`       | Admin      | Enable/disable user   |

### Employees  `/api/employee`
| Method | Endpoint                         | Role       | Description                          |
|--------|----------------------------------|------------|--------------------------------------|
| POST   | `/`                              | Admin      | Create employee + optional image      |
| PUT    | `/{iqamaNo}`                     | Admin      | Update employee + optional image      |
| DELETE | `/{iqamaNo}`                     | Admin      | Soft delete                           |
| PATCH  | `/{iqamaNo}/restore`             | Admin      | Restore soft-deleted employee         |
| GET    | `/{iqamaNo}`                     | Any        | Full detail with active order         |
| GET    | `/`                              | Any        | Paginated list with filters           |
| GET    | `/search?keyword=`               | Any        | Smart search (name AR/EN, IqamaNo…)  |

**Image upload** uses `multipart/form-data`. Images are saved to `wwwroot/images/` and served
at `GET /images/{filename}`.

### Orders  `/api/order`
| Method | Endpoint                         | Role               | Description                          |
|--------|----------------------------------|--------------------|--------------------------------------|
| POST   | `/`                              | Admin, Supervisor  | Assign new order (auto-closes prev)   |
| POST   | `/{iqamaNo}/close`               | Admin, Supervisor  | Manually close active order           |
| GET    | `/{id}`                          | Any                | Get order by ID                       |
| GET    | `/active/{iqamaNo}`              | Any                | Get active order for employee         |
| GET    | `/employee/{iqamaNo}`            | Any                | Full order history for employee       |
| GET    | `/`                              | Any                | Filtered, paginated order list        |

### Reports  `/api/report`  *(Admin only)*
| Method | Endpoint                         | Description                                  |
|--------|----------------------------------|----------------------------------------------|
| GET    | `/dashboard`                     | KPIs: counts, avg duration                   |
| GET    | `/employees/orders`              | Per-employee order counts + minutes worked   |
| GET    | `/housing`                       | Per-housing breakdown with employees nested  |
| GET    | `/services`                      | By service type: totals, avg duration        |
| GET    | `/daily?from=&to=`               | Day-by-day summary                           |
| GET    | `/supervisors`                   | Per-supervisor activity                      |
| GET    | `/orders/active`                 | All currently active orders                  |
| GET    | `/orders/null-end?date=`         | Closed orders where EndedAt is null          |

---

## 🔄 Order Lifecycle

```
Supervisor calls POST /api/order
           │
           ▼
   Employee has active order?
   ┌── YES ───────────────────────────────────────────────┐
   │  Same calendar day?                                  │
   │  ├── YES → previousOrder.EndedAt = now (StartedAt)  │
   │  └── NO  → previousOrder.EndedAt = null (day ended) │
   │  previousOrder.IsOrder = false                       │
   └──────────────────────────────────────────────────────┘
           │
           ▼
   Create new order:
     StartedAt = now
     EndedAt   = null   (will be set by next order or stays null)
     IsOrder   = true
```

**EndedAt is `null` when:**
- The order is still active (current assignment)
- The day ended with no follow-up order for that employee

---

## 🛡️ Role Summary

| Feature                        | Admin | Supervisor |
|--------------------------------|-------|-----------|
| Login                          | ✅    | ✅        |
| Register users                 | ✅    | ❌        |
| Manage employees (CRUD)        | ✅    | ❌        |
| Assign / close orders          | ✅    | ✅        |
| View employees & orders        | ✅    | ✅        |
| View all reports               | ✅    | partial   |
| View active orders (report)    | ✅    | ✅        |
