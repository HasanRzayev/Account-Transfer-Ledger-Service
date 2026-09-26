# 🏦 Account Transfer & Ledger Service — Tam Texniki Sənədləşmə, Kod İzahı və Təqdimat Bələdçisi

Bu sənəd **Account Transfer & Ledger Service** layihəsinin tam arxitekturasını, qovluq və faylların daxili iş prinsiplərini, texniki tapşırıqdakı bütün tələblərin (**Funksional, Concurrency, Texnoloji, Test və DevOps**) kodun hansı hissələrində necə tətbiq olunduğunu və layihəni **vizual olaraq təqdim (sunum/demo) və test etməyin** addım-addım bələdçisini sadə, anlaşıqlı Azərbaycan dilində təqdim edir.

---

## 📑 Mündəricat
1. [Layihənin İcmalı və Əsas Məqsədi](#1-layihənin-icmalı-və-əsas-məqsədi)
2. [Verilmiş Tapşırıq Tələblərinin Kodla Dəqiq Xəritələnməsi](#2-verilmiş-tapşırıq-tələblərinin-kodla-dəqiq-xəritələnməsi)
3. [Layihənin Qovluq Arxitekturası (Clean Architecture)](#3-layihənin-qovluq-arxitekturası-clean-architecture)
4. [Hər Bir Faylın və Funksiyanın Sadə və Ətraflı İzahı](#4-hər-bir-faylın-və-funksiyanın-sadə-və-ətraflı-izahı)
   * [4.1. Domain Qatı (AccountTransferLedger.Domain)](#41-domain-qatı-accounttransferledgerdomain)
   * [4.2. Application Qatı (AccountTransferLedger.Application)](#42-application-qatı-accounttransferledgerapplication)
   * [4.3. Infrastructure Qatı (AccountTransferLedger.Infrastructure)](#43-infrastructure-qatı-accounttransferledgerinfrastructure)
   * [4.4. API Qatı (AccountTransferLedger.API)](#44-api-qatı-accounttransferledgerapi)
   * [4.5. Test Qatı (UnitTests & IntegrationTests)](#45-test-qatı-unittests--integrationtests)
5. [Canlı Təqdimat və Vizual Test Bələdçisi (Demo & Testing Guide)](#5-canlı-təqdimat-və-vizual-test-bələdçisi-demo--testing-guide)
   * [Addım 1: Əsas İdarəetmə Paneli (Dashboard)](#addım-1-əsas-idarəetmə-paneli-dashboard)
   * [Addım 2: Pul Köçürməsi və İdempotentlik Sınağı](#addım-2-pul-köçürməsi-və-idempotentlik-sınağı)
   * [Addım 3: Səhifələnmiş Çıxarış və Dapper Cari Qalıq](#addım-3-səhifələnmiş-çıxarış-və-dapper-cari-qalıq)
   * [Addım 4: Canlı Konkurentlik və Overdraft Qorunması Laboratoriyası](#addım-4-canlı-konkurentlik-və-overdraft-qorunması-laboratoriyası)
   * [Visual Studio Test Explorer və Swagger Nümayişi](#visual-studio-test-explorer-və-swagger-nümayişi)
6. [Əsas Arxitektur Qərarlar və Üstünlüklər (Design Decisions)](#6-əsas-arxitektur-qərarlar-və-üstünlüklər-design-decisions)

---

## 1. Layihənin İcmalı və Əsas Məqsədi

Bu layihə daxili bank hesabları arasında vəsait köçürmələrini idarə edən yüksək etibarlı backend və vizuallaşdırma sistemidir. 

**Əsas Məqsədlər:**
* **Dəyişməz İkiqat Mühasibatlıq (Double-Entry Bookkeeping):** Hesab cədvəlində heç bir dəyişən balans sütunu yoxdur. Bütün balanslar `LedgerEntry` çıxarışlarının cəmindən anlıq hesablanır.
* **ACID Tranzaksiya Atomikliyi:** Pul köçürməsi zamanı bir hesabdan vəsait çıxılması (Debet) və digər hesaba daxil olması (Kredit) tək bir atomik tranzaksiya daxilində icra edilir. Biri uğursuz olarsa, heç biri bazaya yazılmır (Rollback).
* **Konkurentlik və Overdraft Qorunması (Race Condition Prevention):** Eyni hesaba eyni anda paralel olaraq çoxlu köçürmə sorğusu gəldikdə belə, balansın heç vaxt mənfiyə düşməsinə icazə verilmir.
* **İdempotensiya (Idempotent Execution):** Şəbəkə xətası və ya təkrar kliklənmə nəticəsində eyni `Idempotency-Key` başlığı ilə göndərilən sorğular əməliyyatı ikinci dəfə təkrarlamır, keşlənmiş cavabı qaytarır.

---

## 2. Verilmiş Tapşırıq Tələblərinin Kodla Dəqiq Xəritələnməsi

Aşağıdakı cədvəldə tapşırıqdakı hər bir tələb və onun kodda harada tətbiq olunduğu aydın şəkildə göstərilmişdir:

| # | Tapşırıq Tələbi (Requirement) | Tələbin Mahiyyəti | Tətbiq Olunduğu Fayl | Kod Hissəsi / Funksiya |
|---|---|---|---|---|
| **1** | **Account creation with an opening balance** | İlkin balansla yeni bank hesabı yaratmaq | `src/AccountTransferLedger.Infrastructure/Services/AccountService.cs` | `CreateAccountAsync(...)` funksiyası `Account` entity-sini və ilk `Opening Balance` kredit çıxarışını vahid tranzaksiyada bazaya yazır. |
| **2** | **Fund transfer (debit + credit as a single atomic operation)** | İki hesab arası atomik pul köçürməsi | `src/AccountTransferLedger.Infrastructure/Services/TransferService.cs` | `ExecuteTransferAsync(...)` funksiyasında `_dbContext.Database.BeginTransactionAsync()` daxilində 1 Debet (`-məbləğ`) və 1 Kredit (`+məbləğ`) yaradılır və atomik commit edilir. |
| **3** | **Balance is derived from ledger entries, not stored as a mutable column** | Balans cədvəl sütunu deyil, çıxarışların cəmidir | `src/AccountTransferLedger.Domain/Entities/Account.cs` və `LedgerDbContext.cs` | `Account` entity-sində `Balance` sütunu yoxdur. Balans hər dəfə `LedgerEntries.Where(...).SumAsync(e => e.Amount)` və ya Dapper `SUM(Amount)` ilə hesablanır. |
| **4** | **Idempotent transfer execution via Idempotency-Key header** | Eyni açarla təkrar sorğu köçürməni 2 dəfə icra etməməlidir | `src/AccountTransferLedger.Infrastructure/Services/IdempotencyService.cs` və `TransferService.cs` | `GetOrCreateRecordAsync` və `SaveResponseAsync`: Açar yoxlanılır, əgər status `Completed` olarsa, köçürmə təkrarlanmır və saxlanılmış JSON cavabı qaytarılır. |
| **5** | **Overdraft prevention under concurrency** | Eyni anda gələn köçürmələrdə balans mənfiyə düşməməlidir | `src/AccountTransferLedger.Infrastructure/Services/TransferService.cs` və `Concurrency/KeyedAsyncLock.cs` | 1) `KeyedAsyncLock` ilə in-memory asinxron kilid.<br>2) MSSQL səviyyəsində `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` əmri ilə sıra kilidlənməsi. Balans < köçürmə məbləği olduqda `InsufficientFundsException` atılır. |
| **6** | **Paginated account statement with running balance** | Səhifələnmiş çıxarış və cari qalıq | `src/AccountTransferLedger.Infrastructure/Persistence/DapperStatementRepository.cs` | `GetPaginatedStatementAsync(...)`: Dapper və SQL Window funksiyası (`SUM(Amount) OVER (PARTITION BY AccountId ORDER BY CreatedAtUtc ASC)`) vasitəsilə hər sətirdə `RunningBalance` hesablanır və `OFFSET/FETCH NEXT` ilə səhifələnir. |
| **7** | **Proper error responses (400, 404, 409, 422)** | Standart və düzgün HTTP xəta cavabları | `src/AccountTransferLedger.Domain/Exceptions/DomainExceptions.cs` və `AccountTransferLedger.API/Middleware/ExceptionHandlingMiddleware.cs` | `InsufficientFundsException` -> **422 Unprocessable Entity**<br>`AccountNotFoundException` -> **404 Not Found**<br>`IdempotencyConflictException` -> **409 Conflict**<br>`ValidationException` -> **400 Bad Request**. |
| **8** | **Concurrency Integration Test** | Paralel sorğuların avtomatik testi | `tests/AccountTransferLedger.IntegrationTests/ConcurrentTransferTests.cs` | `ConcurrentTransfers_ShouldNeverAllowNegativeBalance_AndPreventOverdraft`: 100 AZN balansı olan hesaba eyni anda **10 paralel 20 AZN-lik köçürmə (cəmi 200 AZN)** göndərilir. Dəqiq 5-i uğurlu olur, 5-i 422 xətası alır və son balans dəqiq 0.00 AZN qalır. |
| **9** | **DevOps (Docker-Compose & CI)** | Konteynerləşdirmə və avtomatlaşdırma | `docker-compose.yml`, `Dockerfile`, `.github/workflows/ci.yml` | Multi-container MSSQL 2022 + Backend API + Next.js Frontend və GitHub Actions CI pipeline. |

---

## 3. Layihənin Qovluq Arxitekturası (Clean Architecture)

```
account-transfer-ledger-service/
├── src/
│   ├── AccountTransferLedger.Domain/          # Entity-lər, Enum-lar, Biznes Xətaları (Xalis C#)
│   ├── AccountTransferLedger.Application/     # DTO-lar, Servis İnterfeysləri
│   ├── AccountTransferLedger.Infrastructure/  # MSSQL (EF Core), Dapper, Concurrency Kilidi, Servis İcraatları
│   └── AccountTransferLedger.API/             # Controller-lər, Middleware-lər, Program.cs
│
├── tests/
│   ├── AccountTransferLedger.UnitTests/        # 8 ədəd sürətli Biznes Məntiqi və İdempotensiya Unit Testi
│   └── AccountTransferLedger.IntegrationTests/ # 1 ədəd Yüksək Concurrency Stress Testi (10 paralel sorğu)
│
├── frontend/                                   # Next.js 16 Müasir Light Theme Vizuallaşdırma UI
├── docker-compose.yml                          # MSSQL + API + Frontend orkestrasiyası
├── Dockerfile                                  # Multi-stage .NET 8 Release build faylı
├── .github/workflows/ci.yml                    # Avtomatlaşdırılmış CI Test və Build Workflow-u
└── README.md                                   # Əsas sənədləşmə və quraşdırma təlimatı
```

---

## 4. Hər Bir Faylın və Funksiyanın Sadə və Ətraflı İzahı

### 4.1. Domain Qatı (`AccountTransferLedger.Domain`)
Bu qat layihənin ürəyidir və heç bir xarici verilənlər bazasından asılı deyil.

* **`Entities/Account.cs`**:
  * **İşi:** Bank hesabının əsas məlumatlarını saxlayır (`Id`, `AccountNumber`, `OwnerName`, `Currency`, `CreatedAtUtc`).
  * **Əsas Məntiq:** Bu sinifdə `Balance` sütunu yoxdur! Balans yalnız çıxarışlardan çıxarılır.
* **`Entities/Transfer.cs`**:
  * **İşi:** İki hesab arasındakı köçürmə qeydini saxlayır (`SourceAccountId`, `TargetAccountId`, `Amount`, `Currency`, `IdempotencyKey`).
* **`Entities/LedgerEntry.cs`**:
  * **İşi:** İkiqat mühasibatlıq çıxarışıdır. Dəyişdirilə bilməz (`immutable`).
  * **Əsas Məntiq:** Debet üçün `Amount` mənfi (`-50.00`), Kredit üçün `Amount` müsbət (`+50.00`) saxlanılır.
* **`Entities/IdempotencyRecord.cs`**:
  * **İşi:** Eyni sorğunun təkrar icrasının qarşısını almaq üçün HTTP `Idempotency-Key` başlığını, sorğu heşini və JSON cavabını saxlayır.
* **`Enums/EntryType.cs` və `IdempotencyStatus.cs`**:
  * `EntryType`: `Debit = 1` (Hesabdan çıxılma), `Credit = 2` (Hesaba mədaxil).
  * `IdempotencyStatus`: `Pending = 1`, `Completed = 2`, `Failed = 3`.
* **`Exceptions/DomainExceptions.cs`**:
  * Biznes qaydaları pozulduqda atılan xətalar: `InsufficientFundsException` (balans çatışmazlığı), `AccountNotFoundException` (hesab tapılmadı), `IdempotencyConflictException` (eyni açarla fərqli parametr), `InvalidTransferAmountException` (0 və ya mənfi məbləğ).

---

### 4.2. Application Qatı (`AccountTransferLedger.Application`)
* **`DTOs/AccountDtos.cs`**: Hesab yaratmaq üçün sorğu (`CreateAccountRequest`) və cavab (`AccountResponse`) modelləri.
* **`DTOs/TransferDtos.cs`**: Köçürmə sorğusu (`CreateTransferRequest`) və nəticə (`TransferResponse`) modelləri.
* **`DTOs/StatementDtos.cs`**: Hesab çıxarışı sətirləri (`StatementEntryResponse`) və səhifələnmiş siyahı (`PaginatedStatementResponse`).
* **`DTOs/CommonDtos.cs`**: Standart xəta cavabı modeli (`ErrorResponse`).
* **`Interfaces/ITransferService.cs`**: `ExecuteTransferAsync(...)` metodunun interfeysi.
* **`Interfaces/IAccountService.cs`**: Hesab əməliyyatlarının interfeysi.
* **`Interfaces/IStatementService.cs`**: Çıxarış əməliyyatlarının interfeysi.
* **`Interfaces/IIdempotencyService.cs`**: İdempotentlik mexanizminin interfeysi.

---

### 4.3. Infrastructure Qatı (`AccountTransferLedger.Infrastructure`)

* **`Concurrency/KeyedAsyncLock.cs`**:
  * **İşi:** Tətbiq daxilində eyni hesab ID-si üzrə gələn paralel sorğuları növbəyə düzən və yaddaş sızması yaratmayan asinxron semafor kilididir.
  * **Funksiyası:** `LockAsync(Guid key)` — hesab üçün kilid açarı təqdim edir, iş bitdikdə `Dispose` ilə resursu azad edir.

* **`Persistence/LedgerDbContext.cs`**:
  * **İşi:** Entity Framework Core ilə verilənlər bazası cədvəllərini, açarları və indeksləri tənzimləyir.
  * **Əsas Məntiq:** `AccountNumber`, `Transfers.IdempotencyKey` və `IdempotencyRecords.Key` üçün unikal indekslər qurulub.

* **`Persistence/DapperStatementRepository.cs`**:
  * **İşi:** Çıxarışları yüksək sürətlə oxumaq üçün Dapper istifadə edir.
  * **Funksiyası:** `GetPaginatedStatementAsync(...)` — T-SQL Window funksiyası (`SUM(Amount) OVER (PARTITION BY AccountId ORDER BY CreatedAtUtc ASC)`) ilə hər sətirdəki cari qalığı (`RunningBalance`) hesablayır və `OFFSET / FETCH NEXT` ilə səhifələyir.

* **`Persistence/DbInitializer.cs`**:
  * **İşi:** Baza işə düşərkən cədvəlləri yaradır və 4 ədəd nümunəvi bank hesabını ilkin kredit çıxarışları ilə bazaya əlavə edir.

* **`Services/TransferService.cs` (Ən Əsas Biznes Mühərriki)**:
  * **Funksiyası:** `ExecuteTransferAsync(CreateTransferRequest request, string idempotencyKey)`:
    1. **Validasiya:** Məbləğin 0-dan böyük olduğunu və göndərən ilə alanın fərqli hesablar olduğunu yoxlayır.
    2. **İdempotentlik Yoxlanışı:** Açar bazada axtarılır. Əgər əməliyyat artıq icra edilibsə, dərhal əvvəlki cavab qaytarılır (təkrar pul çıxılmır).
    3. **Deterministik Kilidləmə (Deadlock Prevention):** İki hesab kilidlənərkən ID-lər sıralanır (`id1 < id2`) ki, qarşılıqlı deadlock yaranmasın.
    4. **MSSQL Sıra Kilidi:** `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` əmri ilə göndərən hesabın sətri bloklanır.
    5. **Balansın Hesablanması və Overdraft Qorunması:**
       ```csharp
       var sourceBalance = await _dbContext.LedgerEntries
           .Where(e => e.AccountId == request.SourceAccountId)
           .SumAsync(e => e.Amount);
       if (sourceBalance < request.Amount)
           throw new InsufficientFundsException(...);
       ```
    6. **İkiqat Qeydiyyat:** 1 Debet (`-məbləğ`), 1 Kredit (`+məbləğ`) və 1 `Transfer` qeydi yaradılır.
    7. **Atomik Commit:** `await transaction.CommitAsync()`.
    8. **Keşlənmə:** Nəticə idempotency cədvəlinə yazılır.

---

### 4.4. API Qatı (`AccountTransferLedger.API`)
* **`Controllers/TransfersController.cs`**: `POST /api/transfers` — `Idempotency-Key` başlığı ilə köçürmələri qəbul edir.
* **`Controllers/AccountsController.cs`**: `POST /api/accounts` (yeni hesab) və `GET /api/accounts` (bütün hesablar və onların hesablanmış balansları).
* **`Controllers/StatementsController.cs`**: `GET /api/statements/{accountId}?page=1&pageSize=20` (səhifələnmiş çıxarış).
* **`Controllers/StressTestController.cs`**: `POST /api/stresstest/concurrency` (canlı 10 paralel sorğu simulyasiyası).
* **`Middleware/ExceptionHandlingMiddleware.cs`**: Bütün biznes xətalarını tutur və standart HTTP kodlarına (400, 404, 409, 422, 500) çevirir.
* **`Program.cs`**: Asılılıqların (DI) qeydiyyatı, MSSQL konfiqurasiyası, root `/` müraciətinin avtomatik `/swagger`-ə yönləndirilməsi və CORS sazlamaları.

---

### 4.5. Test Qatı (`tests/` — 9/9 Keçir)
* **`tests/AccountTransferLedger.UnitTests/`**:
  * `TransferServiceUnitTests.cs`: İkiqat qeydiyyat və məbləğlərin düzgünlüyünü yoxlayır.
  * `InsufficientFundsTests.cs`: Balans çatışmadıqda xətanın atıldığını və bazadan heç bir vəsaitin silinmədiyini (Rollback) yoxlayır.
  * `IdempotencyTests.cs`: Təkrar açarla göndərilən sorğunun təkrar icra edilmədiyini və eyni açarla fərqli parametr göndərildikdə 409 münaqişə xətası verdiyini yoxlayır.
  * `StatementAndAccountTests.cs`: Dapper çıxarışlarının və `RunningBalance` hesablamasının dəqiqliyini yoxlayır.
* **`tests/AccountTransferLedger.IntegrationTests/`**:
  * `ConcurrentTransferTests.cs`: 100 AZN balansı olan hesaba eyni anda **10 ədəd 20 AZN-lik paralel sorğu (cəmi 200 AZN)** göndərir. Dəqiq 5-i uğurlu olur (100 AZN silinir), 5-i 422 alır və son balans dəqiq 0.00 AZN qalır.

---

## 5. Canlı Təqdimat və Vizual Test Bələdçisi (Demo & Testing Guide)

Layihəni komandaya və ya intervyuda təqdim edərkən nümayiş etdirəcəyiniz addımlar:

### 🚀 Başlanğıc: Sistemi Qaldırın
Terminalda kök qovluqda bu əmri icra edin:
```bash
docker-compose up -d
```
Brauzerdə açın:
* **Frontend Paneli:** [http://localhost:3000](http://localhost:3000)
* **Backend Swagger:** [http://localhost:8080](http://localhost:8080)

---

### 🎬 Təqdimat Addımları:

#### Addım 1: Əsas İdarəetmə Paneli (Dashboard)
1. Brauzerdə `http://localhost:3000` səhifəsinə daxil olun.
2. Ekranda 4 ilkin hesabı (`Azər Məmmədov`, `Leyla Əliyeva`, `Rəşad Quliyev`, `Nərgiz Hüseynova`) və ümumi likvidliyi (`5,000.00 AZN`) göstərin.
3. **İzah:** *"Bu balanslar bazada hazır sütun deyil, hər bir hesabın çıxarış qeydlərinin anlıq cəmlənməsindən əldə olunur."*

#### Addım 2: Pul Köçürməsi və İdempotentlik Sınağı
1. **"Pul Köçürməsi"** tabına keçin.
2. Göndərən hesabı və Alan hesabı seçin, məbləğə **50 AZN** yazıb **"Köçürməni İcra Et"** düyməsinə basın.
3. Ekranda tranzaksiyanın uğurla bitdiyini və balansların dərhal dəyişdiyini göstərin.
4. Dərhal sonra **"Təkrar Göndər (İdempotent Test)"** düyməsinə basın.
5. **İzah:** *"Gördüyünüz kimi, eyni Idempotency-Key ilə təkrar sorğu göndərildikdə sistem təkrar pul silmədi və keşlənmiş uğurlu cavabı dərhal qaytardı."*

#### Addım 3: Səhifələnmiş Çıxarış və Dapper Cari Qalıq
1. **"Hesab Çıxarışı"** tabına keçin.
2. Hesabı seçin (məs: `AZ88ACNT10001`).
3. Cədvəldə `Debit (-50 AZN)` və `Credit (+1,000 AZN)` qeydlərini göstərin.
4. **İzah:** *"Bu çıxarış Dapper və SQL Window funksiyası (`SUM(Amount) OVER (...)`) ilə hesablanır və hər əməliyyatdan sonrakı dəqiq cari qalığı (Running Balance) ən yüksək sürətlə təqdim edir."*

#### Addım 4: Canlı Konkurentlik və Overdraft Qorunması Laboratoriyası (Ən Maraqlı Hissə)
1. **"Konkurentlik Laboratoriyası"** tabına keçin.
2. Balansı **100 AZN** olan hesabı seçin.
3. **"Stres Testini Başlat (10x Paralel Sorğu)"** düyməsinə basın.
4. Ekranda eyni anda göndərilən **10 ədəd 20 AZN-lik (cəmi 200 AZN)** paralel sorğu icra olunacaq.
5. **İzah:** 
   * **Tam 5 sorğu uğurlu oldu** (100 AZN silindi).
   * **5 sorğu 422 Insufficient Funds xətası ilə imtina edildi**.
   * **Yekun balans mənfiyə düşmədi və dəqiq 0.00 AZN qaldı**.
   * *"Bu, MSSQL səviyyəsində `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` və tətbiqdaxili asinxron kilid vasitəsilə race condition-ın qarşısının tam alınmasını sübut edir."*

---

### 💻 Visual Studio Test Explorer və Swagger Nümayişi

* **Visual Studio-da Testlər:**
  1. `AccountTransferLedger.sln` faylını Visual Studio-da açın.
  2. **Test** ➡️ **Test Explorer** (`Ctrl + E, T`) menyusunu açıb **Run All Tests** düyməsinə basın.
  3. Bütün **9 testin** yaşıl olduğunu nümayiş etdirin.
* **Terminalda Testlər:**
  ```bash
  dotnet test AccountTransferLedger.sln
  ```
* **Swagger API Sənədləri:**
  `http://localhost:8080` ünvanında `POST /api/transfers`, `GET /api/statements/{id}` və digər bütün metodları birbaşa interaktiv şəkildə icra edib göstərin.

---

## 6. Əsas Arxitektur Qərarlar və Üstünlüklər (Design Decisions)

1. **Niyə mutable Balance sütunu yoxdur?**
   Maliyyə sistemlərində balans sütununu birbaşa yeniləmək audit izini (audit trail) itirir və yarış vəziyyətlərində xətalara yol açır. Hər şey çıxarış qeydlərindən (`LedgerEntry`) hesablandıqda heç bir qəpik itmir və tam şəffaflıq təmin olunur.

2. **İkiqat Concurrency Qorunması (Two-Tier Locking):**
   * **Tətbiq Səviyyəsi:** `KeyedAsyncLock` ilə eyni hesaba aid sorğular tətbiq daxilində asinxron növbəyə alınır.
   * **Verilənlər Bazası Səviyyəsi:** MSSQL `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` ilə sətirlər fiziki olaraq kilidlənir ki, çoxsaylı API instansiyalarında belə balans mənfiyə düşməsin.

3. **Dapper ilə SQL Window Funksiyaları:**
   Çıxarışları oxuyarkən EF Core yerinə birbaşa optimallaşdırılmış Dapper və SQL Window funksiyası istifadə olunur. Bu, milyonlarla qeyd olduqda belə saniyənin mində biri sürətində cari qalığı hesablamağa imkan verir.
