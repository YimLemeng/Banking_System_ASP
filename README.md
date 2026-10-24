# Enterprise Banking System Web API (.NET 8)

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4)](https://dotnet.microsoft.com/apps/aspnet)
[![SQL Server](https://img.shields.io/badge/Database-SQL_Server-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![JWT Authentication](https://img.shields.io/badge/Auth-JWT_%2B_TOTP_2FA-000000?logo=jsonwebtokens)](https://jwt.io/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

ប្រព័ន្ធធនាគារកម្រិតសហគ្រាស (Enterprise Banking System Web API) បង្កើតឡើងដោយប្រើប្រាស់ **ASP.NET Core .NET 8** ជាមួយ **Clean Architecture & Repository Pattern** ផ្តោតសំខាន់លើសុវត្ថិភាពខ្ពស់ គ្មាន Hardcoded Secrets និងគាំទ្រស្តង់ដារធនាគារពេញលេញ។

---

## 🏛️ រចនាសម្ព័ន្ធគម្រោង (Architecture & Directory Structure)

គម្រោងត្រូវបានរៀបចំឡើងយ៉ាងមានរបៀបរៀបរយក្នុង Layered Structure ៖

```
BankingSystem/
├── config/             # DbContext, Entity Configurations, EF Core Migrations, Options
├── controller/         # RESTful API Controllers (Auth, Accounts, Transactions, Customers, Users, AuditLogs)
├── dto/                # Request & Response DTOs, Unified ApiResponse<T>, PagedResult<T>
├── entity/             # User, RefreshToken, Customer, Account, Transaction, OtpCode, AuditLog, Enums
├── exception/          # Custom Domain Exceptions & RFC 7807 GlobalExceptionHandlerMiddleware
├── mapper/             # AutoMapper Profiles
├── repository/         # GenericRepository, UserRepository, AccountRepository, TransactionRepository, UnitOfWork
├── security/           # BCrypt PasswordHasher, JwtTokenService, TotpService, CurrentUser context
├── service/            # AuthService, AccountService, TransactionService, CustomerService, OtpCleanupService
├── Program.cs          # Pipeline, Serilog, Swagger UI, JWT Bearer, CORS, Environment Loader
├── appsettings.json    # Base application configurations (Secrets are loaded from .env)
└── BankingSystem.csproj
```

---

## 🔒 សុវត្ថិភាព និងការគ្រប់គ្រង Secrets (Security & Environment)

> [!IMPORTANT]
> **Zero Hardcoded Secrets Policy**:
> គ្រប់ Secret Keys, JWT Signing Keys និង Database Connection Strings ត្រូវបានផ្ទុកនៅក្នុង file `.env` ក្នុងម៉ាស៊ីនផ្ទាល់តែប៉ុណ្ណោះ។ File `.env` ត្រូវបាន Ignore ដោយ `.gitignore` មិនត្រូវបាន Push ចូល GitHub ដាច់ខាត។

### បង្កើតឯកសារ `.env` (Environment Setup)
ចម្លង file គំរូ `.env.example` ទៅជា `.env` ៖
```bash
cp .env.example .env
```

កែប្រែតម្លៃនៅក្នុង file `.env` ៖
```dotenv
# Database Connection (SQL Server)
DB_CONNECTION="Server=localhost,1433;Database=banking_system_db;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=banking_system_db;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true"

# JWT Secret Key (Must be at least 32 characters for HMAC-SHA256)
JWT_SECRET="YourUltraSecureSecretKeyForJwt2026Minimum32Characters!"
Jwt__SecretKey="YourUltraSecureSecretKeyForJwt2026Minimum32Characters!"
```

---

## 🚀 មុខងារចម្បងៗ (Core Features)

1. **Authentication & Multi-Factor Security (TOTP 2FA)**:
   - Login តាមរយៈ **Username + Password**
   - គាំទ្រ **TOTP 2FA Authenticator** (Google Authenticator, Microsoft Authenticator) តាមរយៈ Base64 QR Code & Secret Key
   - JWT Access Token (HMAC-SHA256) រួមជាមួយ Rotating Refresh Token
   - Lockout រយៈពេល 15 នាទី ប្រសិនបើវាយ Password ខុសលើសពី 5 ដង
2. **Account Management (Core Banking)**:
   - ប្រភេទគណនី៖ **Savings**, **Checking**, **Fixed Deposit**
   - រូបិយប័ណ្ណ៖ **USD** ($) និង **KHR** (៛) ដោយប្រើប្រាស់ `decimal(18,2)` ធានាតម្លៃជាក់លាក់
   - បង្កក (Freeze), ដោះបង្កក (Unfreeze), និងបិទគណនី (Close Account)
3. **Financial Transactions & ACID Compliance**:
   - ប្រតិបត្តិការ **Deposit (ដាក់ប្រាក់)** និង **Withdraw (ដកប្រាក់)**
   - ប្រតិបត្តិការ **Transfer (ផ្ទេរប្រាក់)** ដំណើរការក្នុង EF Core Database Transaction ដោយធានា ACID 100%
   - ការពារការកាត់ប្រាក់ស្ទួនដោយស្វ័យប្រវត្តិតាមរយៈ **Idempotency Key**
   - ទាញយករបាយការណ៍គណនី (Account Statement) និងប្រវត្តិប្រតិបត្តិការ
4. **Customer KYC & Staff Directory**:
   - Customer Onboarding ជាមួយ KYC Status (Pending, Verified, Rejected)
   - Endpoint សម្រាប់ Admin ពិនិត្យបញ្ជីឈ្មោះបុគ្គលិក និងអ្នកគ្រប់គ្រង (`/api/v1/users`)
5. **Security Audit Trail**:
   - កត់ត្រារាល់សកម្មភាពសំខាន់ៗទាំងអស់ (Login, Transfer, Freeze, Password Change)
   - ការពារឯកជនភាពដោយ Mask ព័ត៌មានរសើប និង IP Address

---

## 🛠️ របៀបតម្លើង និងដំណើរការ (Run & Setup)

### 1. Requirements
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server) (Local ឬ Docker)

### 2. Build គម្រោង
```bash
dotnet restore
dotnet build
```

### 3. Run Database Migrations
ប្រសិនបើ Terminal ស្ថិតនៅក្នុង folder `BankingSystem`:
```bash
dotnet ef database update
```
ឬប្រសិនបើ Terminal ស្ថិតនៅ root folder (`Banking-system`):
```bash
dotnet ef database update --project BankingSystem
```

### 4. ដំណើរការ Web API
```bash
cd BankingSystem
dotnet run --urls=http://localhost:5107
```
- API Base URL: `http://localhost:5107`
- Swagger UI Documentation: `http://localhost:5107/swagger`

---

## 👤 គណនីគំរូសម្រាប់តេស្ត (Default Demo Accounts)

| Username | Role | Password | មុខងារ (Capabilities) |
|---|---|---|---|
| **admin** | `Admin` | `Admin@123!` | គ្រប់គ្រងបុគ្គលិក, មើល Audit Logs, Freeze Account, Full Access |
| **teller** | `Teller` | `Teller@123!` | Onboard Customer, បើកគណនី, ដាក់/ដក/ផ្ទេរប្រាក់, KYC Check |

---

## 📡 API Endpoints Overview

| Method | Endpoint | Role Required | បរិយាយ (Description) |
|---|---|---|---|
| `POST` | `/api/v1/auth/login/pre-check` | Anonymous | ផ្ទៀងផ្ទាត់ Username & Password និងបង្កើត TOTP Setup |
| `POST` | `/api/v1/auth/login/totp` | Anonymous | បញ្ចូលលេខកូដ 6 ខ្ទង់ Authenticator ដើម្បីទទួល JWT Token |
| `POST` | `/api/v1/auth/refresh-token` | Anonymous | ផ្លាស់ប្តូរ Token ថ្មីដោយប្រើ Refresh Token |
| `POST` | `/api/v1/auth/logout` | Authenticated | ចាកចេញ និងលុប Refresh Token |
| `GET` | `/api/v1/users` | `Admin` | បង្ហាញបញ្ជីឈ្មោះបុគ្គលិក និង Admin |
| `GET` | `/api/v1/customers` | `Admin, Teller` | បង្ហាញបញ្ជីអតិថិជន និងព័ត៌មាន KYC |
| `POST` | `/api/v1/customers` | `Admin, Teller` | ចុះឈ្មោះអតិថិជនថ្មី |
| `GET` | `/api/v1/accounts` | `Admin, Teller` | បង្ហាញបញ្ជីគណនីធនាគារទាំងអស់ |
| `POST` | `/api/v1/accounts` | `Admin, Teller` | បង្កើតគណនីធនាគារថ្មី (Savings/Checking/Fixed) |
| `POST` | `/api/v1/transactions/deposit` | `Admin, Teller` | ដាក់ប្រាក់ចូលគណនី |
| `POST` | `/api/v1/transactions/withdraw` | `Admin, Teller` | ដកប្រាក់ពីគណនី |
| `POST` | `/api/v1/transactions/transfer` | `Admin, Teller` | ផ្ទេរប្រាក់រវាងគណនី (ACID Transaction) |
| `GET` | `/api/v1/audit-logs` | `Admin` | ពិនិត្យមើល System Audit Trails |
