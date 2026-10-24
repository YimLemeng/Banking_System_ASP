# Enterprise Banking System Web API (.NET 8)

គម្រោង Banking System Web API ត្រូវបានរៀបចំឡើងតាមរចនាសម្ព័ន្ធ Layered Architecture ក្នុង Project តែមួយ (Single-Project Layered Structure) ដូចទៅនឹងគម្រោងស្ដង់ដារដែលបានកំណត់៖

```
BankingSystem/
├── config/             # DbContext, Entity Configurations, EF Migrations, Options
├── controller/         # AuthController, AccountsController, TransactionsController, CustomersController, AuditLogsController
├── dto/                # Request & Response DTOs, Unified ApiResponse<T>, PagedResult<T>
├── entity/             # User, RefreshToken, Customer, Account, Transaction, OtpCode, AuditLog, PhoneNumber, Enums
├── exception/          # Custom Exceptions & RFC 7807 GlobalExceptionHandlerMiddleware
├── mapper/             # AutoMapper Profile
├── repository/         # GenericRepository, UserRepository, AccountRepository, TransactionRepository, UnitOfWork
├── security/           # BCrypt PasswordHasher, JwtTokenService, OtpService, CurrentUser accessor
├── service/            # AuthService, AccountService, TransactionService, CustomerService, SmsSender, OtpCleanupService
├── Program.cs          # Pipeline, Serilog, Swagger UI, JWT Bearer, CORS, Database Migration & Seeding
├── appsettings.json    # SQL Server Connection, JWT Settings, OTP Rate Limits
└── BankingSystem.csproj
```

---

## 🚀 មុខងារចម្បងៗ (Core Features)

1. **Authentication & Authorization (Phone Number Required)**:
   - ចុះឈ្មោះដោយប្រើលេខទូរស័ព្ទ (E.164 Cambodia format `+855...`) និង Password
   - Login តាមរយៈ លេខទូរស័ព្ទ + Password
   - Login តាមរយៈ លេខទូរស័ព្ទ + OTP (Passwordless via SMS)
   - Fallback Login តាមរយៈ Email + Password
   - Rotating Refresh Token និង Logout (Revoke Token)
   - Lockout រយៈពេល 15 នាទី ប្រសិនបើវាយ Password ខុសលើសពី 5 ដង
2. **Cryptographic OTP Security Rules**:
   - បង្កើតលេខកូដសម្ងាត់ 6 ខ្ទង់ដោយប្រើ `RandomNumberGenerator`
   - រក្សាទុកតែតម្លៃ SHA-256 Hash នៅក្នុង Database (មិនរក្សាទុក Plaintext ដាច់ខាត)
   - សុពលភាព 5 នាទី, ប្រើបានតែ 1 ដង (Single use)
   - Cooldown 60 វិនាទី រវាងការស្នើសុំកូដម្តងៗ, អនុញ្ញាតអតិបរមា 5 ដងក្នុង 1 ម៉ោង
   - វាយកូដខុសលើសពី 5 ដង នឹង Lockout រយៈពេល 15 នាទី
   - Background service សម្អាត OTP ដែលផុតកំណត់រៀងរាល់ 1 ម៉ោង
3. **Account & Core Banking**:
   - ប្រភេទគណនី៖ Savings, Checking, Fixed Deposit
   - រូបិយប័ណ្ណ៖ **USD** និង **KHR** (ទសភាគ `decimal(18,2)`)
   - ប្រតិបត្តិការ Deposit និង Withdraw ដោយមានការពារ Idempotency Key
   - ការផ្ទេរប្រាក់ (Transfer) ស្ថិតក្នុង Database Transaction ធានា ACID
   - Optimistic Concurrency តាមរយៈ SQL Server `RowVersion`
   - ផ្ទេរប្រាក់ចាប់ពី $1,000 USD ឬ 4,000,000 KHR ឡើងទៅ តម្រូវឱ្យផ្ទៀងផ្ទាត់ 2FA OTP
   - បង្កក (Freeze), ដោះបង្កក (Unfreeze), និងបិទគណនី (Close Account - សមតុល្យត្រូវតែស្មើ 0)
   - ទាញយករបាយការណ៍គណនី (Account Statement) និងប្រវត្តិប្រតិបត្តិការ
4. **Audit Trail**:
   - កត់ត្រារាល់សកម្មភាពសំខាន់ៗទាំងអស់ (Login, OTP, Transfer, Deposit, Withdraw, Freeze) ព្រមទាំង Mask លេខទូរស័ព្ទ (`+855****5680`)

---

## 🛠️ របៀបតម្លើង និងដំណើរការ (Run & Setup)

### 1. Build គម្រោង
```bash
dotnet restore
dotnet build
```

### 2. Run Database Migration ទៅកាន់ SQL Server
> [!NOTE]
> ប្រសិនបើ Terminal របស់អ្នកកំពុងស្ថិតនៅក្នុង folder **`BankingSystem`** ស្រាប់ (ដូចក្នុង JetBrains Rider/VS Code):
> ```bash
> dotnet ef database update
> ```
> ប៉ុន្តែប្រសិនបើស្ថិតនៅ root folder ខាងក្រៅ (`Banking-system`):
> ```bash
> dotnet ef database update --project BankingSystem
> ```

### 3. Run Web API
> ប្រសិនបើនៅក្នុង folder **`BankingSystem`**:
> ```bash
> dotnet run
> ```
> ប្រសិនបើស្ថិតនៅ root folder ខាងក្រៅ:
> ```bash
> dotnet run --project BankingSystem
> ```
- Swagger UI នឹងបើកនៅ: `https://localhost:7189` (ឬ `http://localhost:5000`)

### 4. ដំណើរការ Unit Tests
```bash
dotnet test
```
*(Tests ទាំងអស់ចំនួន 32 tests ដំណើរការជោគជ័យ 100%)*

---

## 👤 គណនីគំរូដែលបាន Seed ក្នុង Database

| តួនាទី (Role) | លេខទូរស័ព្ទ | Password | សម្គាល់ |
|---|---|---|---|
| **Admin** | `012345678` (ឬ `+85512345678`) | `Admin@123!` | System Administrator |
| **Teller** | `012345679` (ឬ `+85512345679`) | `Teller@123!` | Branch Head Teller |

