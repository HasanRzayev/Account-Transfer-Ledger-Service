# 🏦 Account Transfer & Ledger Service — Tam Texniki Sənədləşmə və Kod Təhlili

Bu sənəddə layihənin bütün qovluq və faylları, onların daxilindəki hər bir sinif (class), metod/funksiya, onların gördüyü işlər və verilmiş texniki tapşırıq tələblərinin (**Functional, Concurrency & Integrity, Technology & DevOps Requirements**) kodun hansı hissələrində necə tətbiq olunduğu tam və ətraflı şəkildə izah edilmişdir.

---

## 📑 Mündəricat
1. [Layihə Tələblərinin Kodla Xəritələnməsi (Requirements Mapping)](#1-layihə-tələblərinin-kodla-xəritələnməsi)
2. [Layihə Strukturu və Qovluq Arxitekturası](#2-layihə-strukturu-və-qovluq-arxitekturası)
3. [Domain Qatı (AccountTransferLedger.Domain)](#3-domain-qatı-accounttransferledgerdomain)
4. [Application Qatı (AccountTransferLedger.Application)](#4-application-qatı-accounttransferledgerapplication)
5. [Infrastructure Qatı (AccountTransferLedger.Infrastructure)](#5-infrastructure-qatı-accounttransferledgerinfrastructure)
6. [API Qatı (AccountTransferLedger.API)](#6-api-qatı-accounttransferledgerapi)
7. [Test Qatı (UnitTests & IntegrationTests)](#7-test-qatı-unittests--integrationtests)
8. [DevOps və Konfiqurasiya Faylları](#8-devops-və-konfiqurasiya-faylları)
9. [Frontend Tətbiqi (Next.js 16 Light Theme UI)](#9-frontend-tətbiqi-nextjs-16-light-theme-ui)

---

## 1. Layihə Tələblərinin Kodla Xəritələnməsi

Aşağıdakı cədvəldə tapşırıqdakı hər bir tələbin layihənin hansı faylında və hansı metodunda reallaşdırıldığı göstərilmişdir:

| # | Tələb (Requirement) | Tətbiq Olunduğu Fayl | Metod / Sinif / Kod Hissəsi |
|---|---|---|---|
| **1** | **Account creation with opening balance** (İlkin balansla hesabın yaradılması) | `src/AccountTransferLedger.Infrastructure/Services/AccountService.cs` | `CreateAccountAsync(CreateAccountRequest request)`: Yeni `Account` və `Opening Balance` üçün ilk `LedgerEntry` (Credit) atomik tranzaksiyada yaradılır. |
| **2** | **Fund transfer (Debit + Credit atomic)** (İki hesab arası atomik köçürmə) | `src/AccountTransferLedger.Infrastructure/Services/TransferService.cs` | `ExecuteTransferAsync(...)`: `BeginTransactionAsync()` daxilində 1 Debet (`-məbləğ`) və 1 Kredit (`+məbləğ`) qeydi tək bir tranzaksiyada bazaya yazılır və commit edilir. |
| **3** | **Balance is derived from ledger entries** (Balans dəyişən sütun deyil, çıxarışların cəmidir) | `src/AccountTransferLedger.Infrastructure/Persistence/LedgerDbContext.cs` və `Services/TransferService.cs` | `Account` entity-sində heç bir `Balance` sütunu yoxdur. Balans hər zaman `_dbContext.LedgerEntries.Where(e => e.AccountId == id).SumAsync(e => e.Amount)` və ya Dapper `SUM(Amount)` ilə hesablanır. |
| **4** | **Idempotent transfer via Idempotency-Key header** (Təkrar sorğu köçürməni 2 dəfə icra etməməlidir) | `src/AccountTransferLedger.Infrastructure/Services/IdempotencyService.cs` və `TransferService.cs` | `GetOrCreateRecordAsync` və `SaveResponseAsync`: `IdempotencyRecord` cədvəlində status yoxlanılır. Əgər eyni açar artıq `Completed` olubsa, birbaşa saxlanılmış JSON cavabı qaytarılır, yenidən köçürmə edilmir. |
| **5** | **Overdraft prevention under concurrency** (Eyni anda gələn köçürmələrdə balans heç vaxt mənfiyə düşməməlidir) | `src/AccountTransferLedger.Infrastructure/Services/TransferService.cs` və `Concurrency/KeyedAsyncLock.cs` | 1) İkiqat kilid: `KeyedAsyncLock` ilə tətbiq daxili asinxron növbə.<br>2) MSSQL səviyyəsində `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` kilidlənməsi ilə `Account` sıraları bloklanır və hesablanan balans < köçürmə məbləği olduqda `InsufficientFundsException` atılır. |
| **6** | **Paginated account statement with running balance** (Səhifələnmiş hesab çıxarışı) | `src/AccountTransferLedger.Infrastructure/Persistence/DapperStatementRepository.cs` | `GetPaginatedStatementAsync(...)`: Yüksək performanslı Dapper və T-SQL Window Funksiyası (`SUM(le.Amount) OVER (PARTITION BY ... ORDER BY CreatedAt ASC)`) vasitəsilə hər sətir üzrə qalıq və `OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY` ilə səhifələmə icra olunur. |
| **7** | **Proper error responses** (400, 404, 409, 422 xəta idarəetməsi) | `src/AccountTransferLedger.Domain/Exceptions/DomainExceptions.cs` və `src/AccountTransferLedger.API/Middleware/ExceptionHandlingMiddleware.cs` | `ValidationException` -> 400 Bad Request<br>`AccountNotFoundException` -> 404 Not Found<br>`IdempotencyConflictException` -> 409 Conflict<br>`InsufficientFundsException` & `InvalidTransferAmountException` -> 422 Unprocessable Entity. |
| **8** | **Simultaneous concurrency integration test** (Paralel köçürmə stress testi) | `tests/AccountTransferLedger.IntegrationTests/ConcurrentTransferTests.cs` | `ConcurrentTransfers_ShouldNeverAllowNegativeBalance_AndPreventOverdraft`: 100 AZN balansı olan hesaba eyni anda **10 ədəd 20 AZN-lik paralel sorğu** göndərilir. Dəqiq 5-i uğurlu olur, 5-i 422 alır və son balans 0.00 AZN qalır. |
| **9** | **DevOps (Docker-Compose & CI)** | `docker-compose.yml`, `Dockerfile`, `.github/workflows/ci.yml` | Multi-container MSSQL 2022 + Backend API + Frontend servisləri, həmçinin avtomatlaşdırılmış GitHub Actions pipeline. |

---

## 2. Layihə Strukturu və Qovluq Arxitekturası

```
account-transfer-ledger-service/
├── src/
│   ├── AccountTransferLedger.Domain/          # Entity-lər, Enum-lar, Biznes Xətaları
│   ├── AccountTransferLedger.Application/     # DTO-lar, İnterfeyslər
│   ├── AccountTransferLedger.Infrastructure/  # EF Core, MSSQL, Dapper, Servislər, Lock
│   └── AccountTransferLedger.API/             # Controllers, Middleware, Konfiqurasiya
│
├── tests/
│   ├── AccountTransferLedger.UnitTests/        # 8 ədəd Unit Test
│   └── AccountTransferLedger.IntegrationTests/ # 1 ədəd Yüksək Concurrency Stress Test
│
├── frontend/                                   # Next.js 16 Müasir Light Theme Vizuallaşdırma Paneli
├── docker-compose.yml                          # MSSQL + Backend + Frontend orkestrasiyası
├── Dockerfile                                  # Multi-stage .NET 8 Release build faylı
├── .github/workflows/ci.yml                    # Avtomatik Test və Build CI Workflow-u
└── README.md                                   # Layihənin təqdimat və quraşdırma sənədi
```

---

## 3. Domain Qatı (`src/AccountTransferLedger.Domain`)

Domain qatı xalis C# kodudur, xarici kütübxana və bazalardan asılı deyil.

### 3.1. `Entities/Account.cs`
* **Gördüyü İş:** Hesab entity-sini təmsil edir.
* **Sahələr:**
  * `Id (Guid)`: Hesabın unikal identifikatoru.
  * `AccountNumber (string)`: Unikal hesab nömrəsi (məs: `AZ88ACNT10001`).
  * `OwnerName (string)`: Hesab sahibinin adı və soyadı.
  * `Currency (string)`: Valyuta növü (məs: `AZN`, `USD`).
  * `CreatedAtUtc (DateTime)`: Hesabın yaradılma tarixi.
  * `RowVersion (byte[])`: Concurrency üçün optimist versiya baytları.
* **Xüsusi Qeyd:** Bu entity-də heç bir `Balance` sütunu yoxdur! İkiqat mühasibatlıq tələbinə əsasən balans yalnız `LedgerEntry`-lər üzərindən hesablanır.

### 3.2. `Entities/Transfer.cs`
* **Gördüyü İş:** İki hesab arasındakı köçürmə əməliyyatının qeydidir.
* **Sahələr:**
  * `Id (Guid)`: Köçürmənin unikal identifikatoru.
  * `SourceAccountId (Guid)`: Vəsait çıxılan (göndərən) hesabın ID-si.
  * `TargetAccountId (Guid)`: Vəsait daxil olan (alan) hesabın ID-si.
  * `Amount (decimal)`: Köçürülən məbləğ (mütləq müsbət olmalıdır).
  * `Currency (string)`: Valyuta.
  * `Description (string)`: Əməliyyat qeydi.
  * `IdempotencyKey (string)`: Təkrar icranın qarşısını alan unikal sorğu açarı.
  * `CreatedAtUtc (DateTime)`: İcra olunma vaxtı.

### 3.3. `Entities/LedgerEntry.cs`
* **Gördüyü İş:** Dəyişdirilə bilməyən (immutable) mühasibat çıxarış qeydi.
* **Sahələr:**
  * `Id (Guid)`: Çıxarış qeydinin unikal ID-si.
  * `AccountId (Guid)`: Əlaqədar hesab.
  * `TransferId (Guid?)`: Əgər köçürmədirsə, `Transfer` ID-si; ilkin balansdırsa `null`.
  * `Amount (decimal)`: Məbləğ. Debet üçün **mənfi** (`-50.00`), Kredit üçün **müsbət** (`+50.00`).
  * `Type (EntryType)`: `Debit` və ya `Credit`.
  * `Description (string)`: Çıxarışın təsviri.
  * `CreatedAtUtc (DateTime)`: Yaradılma vaxtı.

### 3.4. `Entities/IdempotencyRecord.cs`
* **Gördüyü İş:** API sorğusunun təkrarlanmasının qarşısını almaq üçün nəticənin keşlənməsi.
* **Sahələr:**
  * `Key (string)`: HTTP `Idempotency-Key` başlığı.
  * `RequestHash (string)`: Göndərilən sorğu gövdəsinin SHA256 heşi (eyni açarla fərqli parametr göndərilərsə xəta atmaq üçün).
  * `Status (IdempotencyStatus)`: `Pending`, `Completed` və ya `Failed`.
  * `ResponseBody (string)`: Uğurlu tranzaksiyanın JSON cavabı.
  * `StatusCode (int)`: Qaytarılan HTTP status kodu (məs: `200`).
  * `CreatedAtUtc / ExpiresAtUtc (DateTime)`: Keşin müddəti.

### 3.5. `Enums/EntryType.cs` və `Enums/IdempotencyStatus.cs`
* `EntryType`: `Debit = 1` (Hesabdan çıxılma), `Credit = 2` (Hesaba mədaxil).
* `IdempotencyStatus`: `Pending = 1`, `Completed = 2`, `Failed = 3`.

### 3.6. `Exceptions/DomainExceptions.cs`
* **Gördüyü İş:** Biznes qaydaları pozulduqda atılan xüsusi istisnalar:
  * `InsufficientFundsException`: Balans çatışmadıqda atılır (HTTP 422).
  * `AccountNotFoundException`: Hesab tapılmadıqda atılır (HTTP 404).
  * `InvalidTransferAmountException`: Məbləğ 0 və ya mənfi olduqda atılır (HTTP 422).
  * `SameAccountTransferException`: Göndərən və alan eyni hesab olduqda atılır (HTTP 422).
  * `CurrencyMismatchException`: Fərqli valyutalı hesablar arasında köçürmə cəhdində atılır (HTTP 422).
  * `IdempotencyConflictException`: Eyni `Idempotency-Key` ilə fərqli parametr göndərildikdə atılır (HTTP 409).
  * `ValidationException`: Giriş parametrləri yanlış olduqda atılır (HTTP 400).

---

## 4. Application Qatı (`src/AccountTransferLedger.Application`)

Application qatı DTO-ları və xidmət interfeyslərini saxlayır.

### 4.1. `DTOs/` (Data Transfer Objects)
* **`AccountDtos.cs`**:
  * `CreateAccountRequest`: Yeni hesab üçün `AccountNumber`, `OwnerName`, `Currency`, `OpeningBalance`.
  * `AccountResponse`: Hesab məlumatları və hesablanmış cari `Balance`.
* **`TransferDtos.cs`**:
  * `CreateTransferRequest`: `SourceAccountId`, `TargetAccountId`, `Amount`, `Description`.
  * `TransferResponse`: İcra olunmuş köçürmənin detalları və hər iki hesabın yekun balansı.
* **`StatementDtos.cs`**:
  * `StatementEntryResponse`: Hər bir çıxarış sətri, `Amount`, `Type`, `RunningBalance` (cari qalıq).
  * `PaginatedStatementResponse`: Səhifələnmiş siyahı (`Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`).
* **`CommonDtos.cs`**:
  * `ErrorResponse`: Standartlaşdırılmış xəta modeli (`ErrorCode`, `Message`, `TimestampUtc`, `TraceId`).

### 4.2. `Interfaces/`
* **`IAccountService.cs`**: `CreateAccountAsync`, `GetAccountByIdAsync`, `GetAllAccountsAsync`, `GetAccountBalanceAsync`.
* **`ITransferService.cs`**: `ExecuteTransferAsync(CreateTransferRequest request, string idempotencyKey)`.
* **`IStatementService.cs`**: `GetStatementAsync(Guid accountId, int page, int pageSize)`.
* **`IIdempotencyService.cs`**: `GetOrCreateRecordAsync`, `SaveResponseAsync`, `MarkFailedAsync`.

---

## 5. Infrastructure Qatı (`src/AccountTransferLedger.Infrastructure`)

Infrastructure qatı baza əməliyyatlarını, kilidləmə mexanizmini və xidmətlərin real icrasını həyata keçirir.

### 5.1. `Concurrency/KeyedAsyncLock.cs`
* **Gördüyü İş:** Tətbiq səviyyəsində (in-memory) eyni hesab üzrə eyni anda gələn asinxron sorğuları növbəyə düzən və resurs sızması yaratmayan `SemaphoreSlim` əsaslı asinxron kilid mexanizmidir.
* **Funksiyalar:**
  * `LockAsync(Guid key)`: Verilmiş açar (məs: `SourceAccountId`) üçün `IDisposable` kilid qaytarır.
  * `Releaser.Dispose()`: Kilid bitdikdə semaforu azad edir və sayğac 0 olduqda lüğətdən təmizləyir.

### 5.2. `Persistence/LedgerDbContext.cs`
* **Gördüyü İş:** Entity Framework Core kontekstidir.
* **Model Konfiqurasiyası:**
  * `Accounts`: `AccountNumber` üçün unikal indeks (`IsUnique()`).
  * `LedgerEntries`: `AccountId` və `CreatedAtUtc` üzrə indeks (balans və hesabat sorğularını sürətləndirmək üçün).
  * `Transfers`: `IdempotencyKey` üçün unikal indeks.
  * `IdempotencyRecords`: `Key` üçün unikal indeks.

### 5.3. `Persistence/DapperStatementRepository.cs`
* **Gördüyü İş:** Hesab çıxarışını yüksək sürətli Dapper və SQL Window funksiyası ilə çıxaran repository.
* **Funksiyalar:**
  * `GetPaginatedStatementAsync(...)`:
    1. Ümumi qeyd sayını (`COUNT(1)`) hesablayır.
    2. SQL sorğusunda `SUM(Amount) OVER (PARTITION BY AccountId ORDER BY CreatedAtUtc ASC, Id ASC)` pəncərə funksiyası vasitəsilə hər sətirdəki **Running Balance (Cari Qalıq)** hesablayır.
    3. MSSQL üçün `OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY`, SQLite üçün isə `LIMIT @Limit OFFSET @Offset` istifadə edərək səhifələmə aparır.

### 5.4. `Persistence/DbInitializer.cs`
* **Gördüyü İş:** Baza ilk dəfə işə düşərkən cədvəlləri yaradır (`EnsureCreatedAsync`) və 4 ədəd nümunəvi hesabı ilkin balans qeydləri ilə birlikdə bazaya yazır (Seed Data).

### 5.5. `Services/TransferService.cs` (Ən Kritik Biznes Məntiqi)
* **Gördüyü İş:** Köçürmələrin atomik, idempotensiyalı və overdraft qorunması ilə icrasını təmin edir.
* **Funksiyalar və İcra Addımları:**
  1. **Validasiya:** Məbləğin > 0 olması, göndərən və alanın fərqli olması yoxlanılır.
  2. **İdempotensiya Yoxlanışı:** `IdempotencyService` vasitəsilə açar yoxlanılır. Əgər əməliyyat artıq bitibsə, dərhal saxlanılmış cavab qaytarılır.
  3. **Deadlock-un Qarşısının Alınması (Deterministic Lock Ordering):** Hər iki hesabın ID-si müqayisə edilir (`id1 < id2`) və kilidlər həmişə eyni ardıcıllıqla alınır.
  4. **MSSQL Sıra Kilidlənməsi:** `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` əmri ilə göndərən hesabın sətri digər tranzaksiyalar üçün bloklanır.
  5. **Balansın Hesablanması və Overdraft Qorunması:**
     ```csharp
     var sourceBalance = await _dbContext.LedgerEntries
         .Where(e => e.AccountId == request.SourceAccountId)
         .SumAsync(e => e.Amount);
     if (sourceBalance < request.Amount)
         throw new InsufficientFundsException(...);
     ```
  6. **İkiqat Qeydiyyat (Double-Entry Bookkeeping):**
     * 1 ədəd `Debit` qeydi: `Amount = -request.Amount`
     * 1 ədəd `Credit` qeydi: `Amount = +request.Amount`
     * 1 ədəd `Transfer` qeydi
  7. **Atomik Commit:** `await transaction.CommitAsync()`.
  8. **Cavabın Keşlənməsi:** `SaveResponseAsync` ilə nəticə idempotency cədvəlinə yazılır.

### 5.6. `Services/AccountService.cs`, `StatementService.cs`, `IdempotencyService.cs`
* `AccountService`: Hesabların yaradılması və balansların hesablanması.
* `StatementService`: Dapper repository-dən məlumatları alıb DTO formatına salır.
* `IdempotencyService`: `IdempotencyRecord` qeydlərinin yaradılması, SHA256 heş yoxlanması və nəticələrin saxlanması.

---

## 6. API Qatı (`src/AccountTransferLedger.API`)

### 6.1. `Controllers/TransfersController.cs`
* **`POST /api/transfers`**:
  * `[FromHeader(Name = "Idempotency-Key")]` başlığını qəbul edir (əgər göndərilməzsə xəta qaytarır).
  * `_transferService.ExecuteTransferAsync` metodunu çağırır.
* **`GET /api/transfers/{id}`**: Köçürmənin detallarını qaytarır.

### 6.2. `Controllers/AccountsController.cs`
* **`POST /api/accounts`**: Yeni hesab yaradır.
* **`GET /api/accounts`**: Bütün hesabları və onların anlıq hesablanmış balanslarını qaytarır.
* **`GET /api/accounts/{id}`**: Tək bir hesabı qaytarır.

### 6.3. `Controllers/StatementsController.cs`
* **`GET /api/statements/{accountId}?page=1&pageSize=20`**: Hesabın səhifələnmiş çıxarışını və hər əməliyyatdan sonrakı qalığı qaytarır.

### 6.4. `Controllers/StressTestController.cs`
* **`POST /api/stresstest/concurrency`**: Frontend və ya testlər üçün canlı rejimdə 10 paralel köçürmə göndərərək sistemin rəqabətədavamlılığını sübut edir.

### 6.5. `Middleware/ExceptionHandlingMiddleware.cs`
* **Gördüyü İş:** Sistemdə baş verən bütün istisnaları (Exceptions) tutur və HTTP status kodlarına çevirir:
  * `InsufficientFundsException` -> `422 Unprocessable Entity`
  * `AccountNotFoundException` -> `404 Not Found`
  * `IdempotencyConflictException` -> `409 Conflict`
  * `ValidationException` -> `400 Bad Request`
  * Digər xətalar -> `500 Internal Server Error`

### 6.6. `Program.cs`
* Asılılıqların qeydiyyatı (Dependency Injection).
* MSSQL bağlantısının qurulması (`EnableRetryOnFailure`).
* Root (`/`) müraciətinin avtomatik olaraq `/swagger` səhifəsinə yönləndirilməsi (`Results.Redirect("/swagger")`).
* CORS siyasətinin konfiqurasiyası.

---

## 7. Test Qatı (`tests/`)

Bütün testlər 100% uğurla icra olunur (`9/9 Passed`).

### 7.1. Unit Testlər (`tests/AccountTransferLedger.UnitTests`)
1. **`TransferServiceUnitTests.cs`**:
   * Uğurlu köçürmə zamanı düzgün 1 Debet və 1 Kredit yazıldığını yoxlayır.
   * Mənfi məbləğ və ya eyni hesaba köçürmə zamanı validasiya xətalarını yoxlayır.
2. **`InsufficientFundsTests.cs`**:
   * Balans kifayət etmədikdə `InsufficientFundsException` atıldığını və bazada heç bir dəyişiklik edilmədiyini (Rollback) yoxlayır.
3. **`IdempotencyTests.cs`**:
   * Eyni `Idempotency-Key` ilə ikinci dəfə müraciət edildikdə köçürmənin təkrarlanmadığını və eyni nəticənin qayıtdığını yoxlayır.
   * Eyni açarla fərqli məbləğ göndərildikdə `409 Conflict` atıldığını yoxlayır.
4. **`StatementAndAccountTests.cs`**:
   * Dapper əsaslı hesab çıxarışının düzgün səhifələndiyini və `RunningBalance` qalıqlarının dəqiqliyini yoxlayır.

### 7.2. İnteqrasiya və Concurrency Testi (`tests/AccountTransferLedger.IntegrationTests`)
* **`ConcurrentTransferTests.cs`**:
  * **Test Ssenarisi:** 100 AZN balansı olan `Source` hesabına eyni anda `Task.WhenAll` ilə **10 ədəd paralel 20 AZN-lik köçürmə sorğusu (cəmi 200 AZN)** göndərilir.
  * **Təsdiq (Assertion):** Dəqiq olaraq **5 sorğu uğurlu** olur (100 AZN silinir), qalan **5 sorğu 422 Insufficient Funds** xətası alır. Son balans **dəqiq 0.00 AZN** təşkil edir və heç bir halda mənfiyə düşmür.

---

## 8. DevOps və Konfiqurasiya Faylları

### 8.1. `docker-compose.yml`
* **`mssql-db`**: `mcr.microsoft.com/mssql/server:2022-latest` (Port 1433, Healthcheck ilə).
* **`backend-api`**: .NET 8 Web API (Port 8080).
* **`frontend-app`**: Next.js 16 UI (Port 3000).

### 8.2. `Dockerfile`
* Multi-stage build (.NET SDK 8.0 mühitində kompilyasiya və yüngül ASP.NET 8.0 runtime imicində icra).

### 8.3. `.github/workflows/ci.yml`
* Kod `main`/`master` budağına push edildikdə avtomatik olaraq layihəni build edir, bütün testləri icra edir və frontend-i yoxlayır.

### 8.4. `Account_Transfer_Ledger_API.postman_collection.json`
* Bütün API endpoint-lərini, `Idempotency-Key` başlıqlarını və test sorğularını saxlayan hazır Postman kolleksiyası.

---

## 9. Frontend Tətbiqi (Next.js 16 Light Theme UI)

Frontend tamamilə ağ/açıq mavi bankçılıq üslubunda hazırlanmışdır (qara/tünd rənglərdən istifadə edilməmişdir):
* **`DashboardTab.tsx`**: Ümumi likvidlik və hesab kartları.
* **`TransferTab.tsx`**: Köçürmə forması və İdempotentlik təkrar test düyməsi.
* **`StatementsTab.tsx`**: Hesab çıxarışları və dinamik qalıq cədvəli.
* **`ConcurrencyLabTab.tsx`**: 10 paralel sorğulu canlı Overdraft Qorunması vizuallaşdırıcısı.

---

Bu sənədləşmə layihənin arxitekturasını, tələblərin hər bir kod sətrində necə əks olunduğunu və layihənin bütövlüyünü tam şəkildə nümayiş etdirir.
