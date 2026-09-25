# Hesab Köçürmələri və Baş Kitab Xidməti (Account Transfer & Ledger Service)

Yüksək paralellik (**high concurrency**), tranzaksiya bütövlüyü (**ACID transactions**), dəyişməz baş kitab (**immutable double-entry ledger**) və təkrar sorğuların idarə olunması (**idempotency**) prinsipləri əsasında qurulmuş daxili hesablararası pul köçürmə sistemi.

---

## 📑 Mündəricat
1. [Layihə Haqqında Xülasə](#-layihə-haqqında-xülasə)
2. [Əsas Funksional və Texniki Tələblər](#-əsas-funksional-və-texniki-tələblər)
3. [Sistem Memarlığı (Layered Architecture)](#-sistem-memarlığı-layered-architecture)
4. [Konkurentlik Nəzarəti və Overdraft Qorunması](#-konkurentlik-nəzarəti-və-overdraft-qorunması)
5. [İdempotentlik Mexanizmi (Idempotency-Key)](#-idempotentlik-mexanizmi-idempotency-key)
6. [İkiqat Qeydiyyat (Double-Entry) və Törəmə Balans](#-ikiqat-qeydiyyat-double-entry-və-törəmə-balans)
7. [Dapper ilə Optimizasiya Edilmiş Hesab Çıxarışı](#-dapper-ilə-optimizasiya-edilmiş-hesab-çıxarışı)
8. [Quraşdırma və İşə Salma (Setup Instructions)](#-quraşdırma-və-işə-salma-setup-instructions)
9. [Avtomatlaşdırılmış Testlər (Unit & Concurrency Tests)](#-avtomatlaşdırılmış-testlər-unit--concurrency-tests)
10. [API Endpoints və Postman Kolleksiyası](#-api-endpoints-və-postman-kolleksiyası)

---

## 🌟 Layihə Haqqında Xülasə

Bu xidmət bank daxilində müştəri hesabları arasında vəsait köçürmələrinin etibarlı, dəqiq və kəsintisiz icrasını təmin edir. Əsas diqqət geniş funksionallıqdan daha çox **paralel yarış şərtlərində (race conditions) düzgünlük**, **overdraft-ın (balansın mənfiyə düşməsinin) qəti şəkildə qarşısının alınması** və **təkrar sorğuların ikili icrasının bloklanması** üzərində cəmlənmişdir.

---

## 🎯 Əsas Funksional və Texniki Tələblər

| # | Tələb | Təsvir | Status |
|---|---|---|:---:|
| 1 | **İlkin Balansla Hesab Açılışı** | Yeni bank hesabı açılır və ilkin depozit atomik olaraq Baş Kitaba (Kredit) yazılır. | ✅ |
| 2 | **Atomik Pul Köçürməsi** | Mənbə və hədəf hesab üçün eyni tranzaksiyada 1 Debet (-) və 1 Kredit (+) qeydi formalaşdırılır. | ✅ |
| 3 | **Törəmə Balans (Derived Balance)** | Balans cədvəldə saxlanılmır (mutable column yoxdur); bütün balanslar `SUM(Amount)` ilə dinamik hesablanır. | ✅ |
| 4 | **İdempotent İcra (Idempotency-Key)** | Eyni `Idempotency-Key` ilə təkrar göndərilən sorğu ikili çıxarış etmir, keşlənmiş cavabı qaytarır. | ✅ |
| 5 | **Overdraft Qorunması** | Eyni hesaba eyni anda göndərilən 20 paralel köçürmə zamanı balans heç vaxt mənfiyə düşmür. | ✅ |
| 6 | **Səhifələnmiş Hesab Çıxarışı** | Dapper və SQL Window funksiyaları ilə hər sətirdə cari qalıq (running balance) hesablanan sürətli çıxarış. | ✅ |
| 7 | **Standartlaşdırılmış Xəta İdarəetməsi** | Insufficient Funds (422), Unknown Account (404), Idempotency Conflict (409) və Validation (400) cavabları. | ✅ |

---

## 🏛 Sistem Memarlığı (Layered Architecture)

Layihə təmiz qatlı memarlıq (Layered Clean Architecture) prinsipləri ilə dizayn edilmişdir:

```
├── src/
│   ├── AccountTransferLedger.Domain/             # Domen Varlıqları, Enumlar, Xüsusi İstisnalar
│   │   ├── Entities/                             # Account, Transfer, LedgerEntry, IdempotencyRecord
│   │   ├── Enums/                                # EntryType (Debit/Credit), IdempotencyStatus
│   │   └── Exceptions/                           # InsufficientFundsException, AccountNotFoundException və s.
│   │
│   ├── AccountTransferLedger.Application/        # Biznes Məntiqi, DTO-lar, İnterfeyslər
│   │   ├── DTOs/                                 # AccountDto, TransferRequest, StatementDto və s.
│   │   └── Interfaces/                           # IAccountService, ITransferService, IStatementService, IIdempotencyService
│   │
│   ├── AccountTransferLedger.Infrastructure/     # EF Core, MSSQL, Dapper, Kilidləmə mexanizmləri
│   │   ├── Persistence/                          # LedgerDbContext, DapperStatementRepository, DbInitializer
│   │   ├── Concurrency/                          # KeyedAsyncLock (Deadlock-Free Semaphore)
│   │   └── Services/                             # TransferService, AccountService, StatementService, IdempotencyService
│   │
│   └── AccountTransferLedger.API/                # REST API Endpoints, Middleware, Swagger
│       ├── Controllers/                          # AccountsController, TransfersController, StatementsController, StressTestController
│       ├── Middleware/                           # ExceptionHandlingMiddleware (RFC 7807)
│       └── Program.cs                            # DI, CORS, Swagger konfiqurasiyası
│
├── tests/
│   ├── AccountTransferLedger.UnitTests/          # Unit testlər (Transfer, Idempotency, Overdraft, Ledger)
│   └── AccountTransferLedger.IntegrationTests/   # Paralel konkurentlik və yüksək yük testləri
│
├── frontend/                                     # Next.js + Tailwind CSS Light Theme UI
│   └── account-transfer-ledger-service/
│
├── docker-compose.yml                            # MSSQL + Backend API + Frontend servisləri
├── Dockerfile                                    # Backend multi-stage build faylı
├── .gitignore                                    # Git konfiqurasiyası
└── README.md                                     # Tam sənədləşdirmə
```

---

## 🔒 Konkurentlik Nəzarəti və Overdraft Qorunması

### Seçilmiş Strategiya: İki-Səviyyəli Müdafiə (Dual-Layer Defense)
1. **Tətbiq Səviyyəsində Deterministik Asinxron Kilid (`KeyedAsyncLock`)**:
   - Mənbə və hədəf hesabların GUID-ləri müqayisə edilərək (`firstLockId < secondLockId`) həmişə eyni ardıcıllıqla kilidlənir. Bu, **A ➔ B** və **B ➔ A** eyni anda baş verdikdə yaranan **Deadlock** vəziyyətini 100% aradan qaldırır.
2. **Verilənlər Bazası Səviyyəsində Sətir Kilidləməsi (`WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` & ACID Isolation)**:
   - SQL Server (MSSQL) tranzaksiyasında `UPDLOCK, ROWLOCK, HOLDLOCK` göstərişləri ilə hesab sətirləri eksklüziv kilidlənir.
   - Tranzaksiya daxilində hesabın mövcud Baş Kitab balansı hesablanır.
   - Əgər `currentBalance < request.Amount` olarsa, tranzaksiya ləğv edilir (Rollback) və `InsufficientFundsException` (HTTP 422) atılır.
   - Yalnız balans kifayət etdikdə atomik olaraq 2 Baş Kitab sətri əlavə edilir və Commit edilir.

### Strategiyaların Müqayisəsi və Trade-off Analizi:
* **Pessimistic Row Locking (Seçilən - MSSQL UPDLOCK/HOLDLOCK)**:
  - *Üstünlüyü*: Sıfır maliyyə xətası, overdraft riski yoxdur, balans heç vaxt mənfiyə düşə bilməz.
  - *Trade-off*: Çox yüksək paralellikdə eyni hesab üzrə sətir kilidi bir neçə millisaniyə növbə yaradır.
* **Optimistic Concurrency (RowVersion / Versiya tokeni)**:
  - *Üstünlüyü*: Oxuma zamanı heç bir kilid tələb etmir.
  - *Trade-off*: Eyni hesaba 20 paralel sorğu gəldikdə 19 sorğu toqquşma xətası alaraq ləğv edilir və yenidən cəhd (retry loop) tələb edir, bu da şəbəkə yükünü artırır.

---

## 🔄 İdempotentlik Mexanizmi (Idempotency-Key)

Hər bir köçürmə sorğusu `Idempotency-Key` HTTP başlığı qəbul edir:
1. Sorğu gəldikdə unikal açar `IdempotencyRecords` cədvəlində yoxlanılır.
2. Əgər status **`Completed`** olarsa: Əməliyyat təkrar icra olunmur; əvvəllər generasiya edilmiş nəticə (`wasCachedResponse: true`) dərhal qaytarılır.
3. Əgər status **`Pending`** olarsa: Eyni açarla paralel sorğu hazırda icra olunduğu üçün `HTTP 409 Conflict` qaytarılır.
4. Əgər qeyd yoxdursa: Status `Pending` olaraq qeyd edilir, tranzaksiya icra olunur və uğurla bitdikdən sonra nəticə JSON formatında keşlənərək status `Completed` edilir.

---

## ⚖️ İkiqat Qeydiyyat (Double-Entry) və Törəmə Balans

Layihədə balans dəyişən (mutable) sütun kimi saxlanılmır. Bütün balanslar **Baş Kitab (LedgerEntry)** cədvəlindəki dəyişməz qeydlərdən hesablanır:

$$\text{Hesab Balansı} = \sum \text{LedgerEntry.Amount}$$

* **Debet Qeydi (-)**: Mənbə hesabdan vəsait çıxışı.
* **Kredit Qeydi (+)**: Hədəf hesaba vəsait daxilolması.
* **İlkin Açılış Depoziti**: Sistem tərəfindən hesaba birbaşa Kredit (+) qeydi.

---

## ⚡ Dapper ilə Optimizasiya Edilmiş Hesab Çıxarışı

Hesab çıxarışı üçün **Dapper** və MSSQL T-SQL Window funksiyalarından istifadə olunur:

```sql
WITH OrderedEntries AS (
    SELECT 
        l.Id, l.AccountId, l.Amount, l.EntryType, l.Description, l.CreatedAtUtc,
        SUM(l.Amount) OVER (
            PARTITION BY l.AccountId 
            ORDER BY l.CreatedAtUtc ASC, l.Id ASC
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) as RunningBalance,
        CASE WHEN l.EntryType = 1 THEN toAcc.AccountHolderName ELSE fromAcc.AccountHolderName END as CounterpartyHolderName
    FROM LedgerEntries l
    LEFT JOIN Transfers t ON l.TransferId = t.Id
    LEFT JOIN Accounts fromAcc ON t.FromAccountId = fromAcc.Id
    LEFT JOIN Accounts toAcc ON t.ToAccountId = toAcc.Id
    WHERE l.AccountId = @AccountId
)
SELECT * FROM OrderedEntries
ORDER BY CreatedAtUtc DESC, Id DESC
OFFSET @Offset ROWS
FETCH NEXT @Limit ROWS ONLY;
```
Bu yanaşma yaddaşda heç bir əlavə hesablama aparmadan hər sətirdə anlıq qalıq (running balance) dəyərini sub-millisaniyə sürətlə qaytarır.

---

## 🚀 Quraşdırma və İşə Salma (Setup Instructions)

### 1. Docker Compose ilə 1 Addımda İşə Salma (Tövsiyə olunan)

Bütün sistemi (Microsoft SQL Server 2022 bazası, .NET 8 Backend API və Next.js Frontend) işə salmaq üçün layihənin kök qovluğunda bu əmri icra edin:

```bash
docker-compose up --build
```

Servislər hazır olduqdan sonra:
* 🌐 **İstifadəçi İnterfeysi (Frontend UI)**: [http://localhost:3000](http://localhost:3000)
* 📖 **Swagger API Sənədləri**: [http://localhost:8080/swagger](http://localhost:8080/swagger)
* 🗄 **Microsoft SQL Server (MSSQL)**: `localhost:1433` (db: `LedgerDb`, user: `sa`, pass: `Your_Strong_Password123!`)

---

### 2. Lokal Mühitdə Manual İşə Salma

#### Backend API:
```bash
cd backend/account-transfer-ledger-service
dotnet restore
dotnet run
```
Backend avtomatik olaraq `http://localhost:8080` ünvanında işə düşəcək və nümunə hesabları avtomatik bazaya dolduracaq (seed data).

#### Frontend Tətbiqi:
```bash
cd frontend/account-transfer-ledger-service
npm install
npm run dev
```
Frontend `http://localhost:3000` ünvanında açılacaq.

---

## 🧪 Avtomatlaşdırılmış Testlər (Unit & Concurrency Tests)

Layihədə 100% keçid dərəcəsinə malik xUnit test dəsti mövcuddur:
* **`TransferServiceUnitTests`**: Atomik ikiqat qeydiyyat, düzgün məbləğ və domen yoxlanışları.
* **`InsufficientFundsTests`**: Balansdan artıq köçürmələrin təhlükəsiz dayandırılması və Rollback yoxlanışı.
* **`IdempotencyTests`**: Eyni `Idempotency-Key` ilə təkrar sorğuların təkrar icra edilməməsi və keşlənmiş nəticənin qaytarılması.
* **`ConcurrentTransferTests`**: Eyni hesaba eyni anda göndərilən 10 paralel köçürmə zamanı balansın mənfiyə düşməməsi və overdraft qarşısının 100% alınmasının sübutu.
* **`StatementAndAccountTests`**: Dapper ilə çıxarış və running balance hesablanması.

Bütün testləri icra etmək üçün:
```bash
dotnet test backend/account-transfer-ledger-service.sln
```

---

## 📡 API Endpoints və Postman Kolleksiyası

Layihənin kök qovluğunda hazır `Account_Transfer_Ledger_API.postman_collection.json` faylı yerləşir.

### Əsas Endpoint-lər:
* `POST /api/accounts` — Yeni hesab açılışı (ilkin balansla)
* `GET /api/accounts` — Bütün hesabların və Baş Kitabdan hesablanmış balansların siyahısı
* `GET /api/accounts/{id}` — Hesab detalları
* `GET /api/accounts/{id}/balance` — Cari Baş Kitab balansı
* `POST /api/transfers` — Atomik pul köçürməsi (`Idempotency-Key` başlığı ilə)
* `GET /api/transfers` — Son köçürmələrin siyahısı
* `GET /api/statements/{accountId}?pageNumber=1&pageSize=10` — Dapper ilə səhifələnmiş hesab çıxarışı
* `POST /api/stresstest/concurrency` — Canlı paralel köçürmə və overdraft sınaq testi
