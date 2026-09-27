Account Transfer & Ledger Service — Tam Texniki Sənədləşmə, Kod İzahı və Təqdimat Bələdçisi

Bu sənəd Account Transfer & Ledger Service layihəsinin tam arxitekturasını, qovluq və faylların daxili iş prinsiplərini, texniki tapşırıqdakı bütün tələblərin (Funksional, Concurrency, Texnoloji, Test və DevOps) kodun hansı hissələrində necə tətbiq olunduğunu və layihəni vizual olaraq təqdim (sunum/demo) və test etməyin addım-addım bələdçisini sadə, anlaşıqlı Azərbaycan dilində təqdim edir.

====================================================================
MÜNDƏRİCAT
====================================================================
1. Layihənin İcmalı və Əsas Məqsədi
2. Verilmiş Tapşırıq Tələblərinin Kodla Dəqiq Xəritələnməsi
3. Layihənin Qovluq Arxitekturası (Clean Architecture)
4. Hər Bir Faylın və Funksiyanın Sadə və Ətraflı İzahı
   4.1. Domain Qatı (AccountTransferLedger.Domain)
   4.2. Application Qatı (AccountTransferLedger.Application)
   4.3. Infrastructure Qatı (AccountTransferLedger.Infrastructure)
   4.4. API Qatı (AccountTransferLedger.API)
   4.5. Test Qatı (UnitTests & IntegrationTests)
5. Canlı Təqdimat və Vizual Test Bələdçisi (Demo & Testing Guide)
   Addım 1: Əsas İdarəetmə Paneli (Dashboard)
   Addım 2: Pul Köçürməsi və İdempotentlik Sınağı
   Addım 3: Səhifələnmiş Çıxarış və Dapper Cari Qalıq
   Addım 4: Canlı Konkurentlik və Overdraft Qorunması Laboratoriyası
   Visual Studio Test Explorer və Swagger Nümayişi
6. Əsas Arxitektur Qərarlar və Üstünlüklər (Design Decisions)

====================================================================
1. LAYİHƏNİN İCMALI VƏ ƏSAS MƏQSƏDİ
====================================================================

Bu layihə daxili bank hesabları arasında vəsait köçürmələrini idarə edən yüksək etibarlı backend və vizuallaşdırma sistemidir.

Əsas Məqsədlər:
- Dəyişməz İkiqat Mühasibatlıq (Double-Entry Bookkeeping): Hesab cədvəlində heç bir dəyişən balans sütunu yoxdur. Bütün balanslar LedgerEntry çıxarışlarının cəmindən anlıq hesablanır.
- ACID Tranzaksiya Atomikliyi: Pul köçürməsi zamanı bir hesabdan vəsait çıxılması (Debet) və digər hesaba daxil olması (Kredit) tək bir atomik tranzaksiya daxilində icra edilir. Biri uğursuz olarsa, heç biri bazaya yazılmır (Rollback).
- Konkurentlik və Overdraft Qorunması (Race Condition Prevention): Eyni hesaba eyni anda paralel olaraq çoxlu köçürmə sorğusu gəldikdə belə, balansın heç vaxt mənfiyə düşməsinə icazə verilmir.
- İdempotensiya (Idempotent Execution): Şəbəkə xətası və ya təkrar kliklənmə nəticəsində eyni Idempotency-Key başlığı ilə göndərilən sorğular əməliyyatı ikinci dəfə təkrarlamır, keşlənmiş cavabı qaytarır.

====================================================================
2. VERİLMİŞ TAPŞIRIQ TƏLƏBLƏRİNİN KODLA DƏQİQ XƏRİTƏLƏNMƏSİ VƏ DETALLI İZAHI
====================================================================

Aşağıda texniki tapşırıqda göstərilən 7 əsas funksional tələbin biznes məqsədi, yarana biləcək təhlükələr və layihəmizdəki dəqiq texniki həlli detallı şəkildə verilmişdir:

--------------------------------------------------------------------
1. TƏLƏB: Account creation with an opening balance (İlkin balans ilə hesabın açılması)
--------------------------------------------------------------------
- Biznes Məqsədi: Bankda müştəriyə yeni hesab açılanda ona ilkin vəsait (depozit) daxil edilə bilər. Bu vəsait havadan yaranmır, rəsmi mühasibatlıq baxımından sistemə ilkin mədaxil kimi qeydə alınmalıdır.
- Yarana Biləcək Risk: Əgər hesab yaradılanda balans sadəcə cədvəldə rəqəm kimi yazılsaydı, həmin pulun haradan gəldiyini çıxarışda (statement) sübut etmək və audit etmək mümkün olmazdı.
- Layihədəki Həlli:
  * Fayl: src/AccountTransferLedger.Infrastructure/Services/AccountService.cs
  * Funksiya: CreateAccountAsync(...) metodu çağırılır.
  * Əvvəlcə Account obyekti (hesab nömrəsi, müştəri adı, valyuta) yaradılır.
  * Əgər InitialBalance > 0 göstərilibsə, dərhal LedgerEntries cədvəlinə EntryType.Credit (+məbləğ) qeydi əlavə olunur və təsvirinə "İlkin balans depoziti" yazılır.
  * Hər iki əməliyyat vahid tranzaksiyada bazaya yazılır (SaveChangesAsync). Beləliklə, ilk qəpikdən etibarən hər bir qalıq baş kitab qeydi ilə təsdiqlənir.

--------------------------------------------------------------------
2. TƏLƏB: Fund transfer between two accounts - debit + credit as a single atomic operation (İki hesab arası atomik pul köçürməsi)
--------------------------------------------------------------------
- Biznes Məqsədi: Bir müştəridən digərinə pul köçürüləndə göndərən hesabdan vəsait silinməli (Debit), alan hesaba isə vəsait daxil edilməlidir (Credit).
- Yarana Biləcək Risk: Əgər server göndərəndən pulu çıxandan dərhal sonra (elektrik kəsilməsi və ya xəta səbəbindən) sönərsə, göndərənin pulu silinmiş, amma alana çatmamış qalar. Bu, bank üçün ən böyük maliyyə itkisi və riskidir.
- Layihədəki Həlli:
  * Fayl: src/AccountTransferLedger.Infrastructure/Services/TransferService.cs
  * Funksiya: TransferFundsAsync(...) metodu icra olunur.
  * Əməliyyat _dbContext.Database.BeginTransactionAsync() daxilinə alınır.
  * Eyni tranzaksiya içində:
    1. 1 ədəd Transfer əsas audit qeydi yaradılır.
    2. 1 ədəd göndərən üçün LedgerEntry (Amount = -məbləğ, EntryType = Debit).
    3. 1 ədəd alan üçün LedgerEntry (Amount = +məbləğ, EntryType = Credit).
  * Ən sonda transaction.CommitAsync() çağırılır. Hər hansı xəta olarsa, Rollback baş verir və sistemdə heç bir tək qeyd qalmır (100% Atomiklik).

--------------------------------------------------------------------
3. TƏLƏB: Balance is derived from ledger entries, not stored as a mutable column (Balans dəyişən sütun deyil, çıxarışlardan dinamik hesablanır)
--------------------------------------------------------------------
- Biznes Məqsədi: Ənənəvi və qüsurlu sistemlərdə Accounts cədvəlində Balance = 500 sütunu olur və hər köçürmədə UPDATE Accounts SET Balance = Balance - 50 edilir. Bu yanaşmada kimsə bazaya birbaşa müdaxilə etsə və ya proqramda xəta olsa, real pul hərəkəti ilə balans arasında fərq yaranır.
- Layihədəki Həlli:
  * Fayl: src/AccountTransferLedger.Domain/Entities/Account.cs
  * Domen modelində heç bir Balance sütunu yoxdur!
  * Hesabın cari balansı lazım olduqda src/AccountTransferLedger.Infrastructure/Persistence/DapperStatementRepository.cs vasitəsilə birbaşa Baş Kitabdakı bütün sətirlər toplanır:
    SELECT COALESCE(SUM(Amount), 0) FROM LedgerEntries WHERE AccountId = @AccountId;
  * Bu, İkiqat Mühasibatlıq (Double-Entry Ledger) prinsipinə tam uyğundur: Balans faktiki olaraq keçmiş hadisələrin riyazi cəmidir və saxtalaşdırıla bilməz.

--------------------------------------------------------------------
4. TƏLƏB: Idempotent transfer execution via Idempotency-Key header (Təkrar sorğunun təkrar icrasının qarşısının alınması)
--------------------------------------------------------------------
- Biznes Məqsədi: İstifadəçi zəif internet səbəbilə köçürmə düyməsinə 2-3 dəfə dalbadal basa bilər və ya mobil tətbiq şəbəkə timeout-u alıb sorğunu avtomatik təkrar göndərə bilər.
- Yarana Biləcək Risk: Əgər sistem sorğunun unikal olduğunu yoxlamasa, eyni 50 AZN-lik köçürmə 3 dəfə icra olunar və müştərinin kartından 150 AZN silinər.
- Layihədəki Həlli:
  * Fayl: src/AccountTransferLedger.Infrastructure/Services/IdempotencyService.cs və TransferService.cs
  * Hər POST /api/transfers sorğusunun header-ində Idempotency-Key (məs: UUID) göndərilir.
  * IdempotencyService əvvəlcə bazada bu açarın olub-olmadığını yoxlayır.
  * Əgər açar artıq uğurla tamamlanıbsa (Status == Completed), köçürmə məntiqi təkrar icra edilmir. Bazada saxlanılmış ilkin cavab (ResponseBody) oxunur və dərhal WasCachedResponse: true ilə istifadəçiyə qaytarılır.
  * Nəticədə hesabdan yalnız 1 dəfə pul çıxılır, təkrar basılmalar isə təhlükəsiz qarşılanır.

--------------------------------------------------------------------
5. TƏLƏB: Overdraft prevention under concurrency (Eyni anda gələn köçürmələrdə balansın mənfiyə düşməsinin qarşısının alınması)
--------------------------------------------------------------------
- Biznes Məqsədi: Balansında cəmi 100 AZN olan hesaba eyni millisaniyədə iki fərqli yerdən 100 AZN-lik iki köçürmə sorğusu gəldikdə yarış şəraiti (Race Condition) yaranır.
- Yarana Biləcək Risk: Hər iki proses eyni anda balansı oxuyub 100 AZN görür və hər ikisi 100 AZN çıxarış edir. Nəticədə balans -100 AZN olur (icazəsiz overdraft).
- Layihədəki Həlli:
  * 1-ci Qat (Tətbiqdaxili Kilid): src/AccountTransferLedger.Infrastructure/Concurrency/KeyedAsyncLock.cs vasitəsilə eyni hesaba aid sorğular asinxron növbəyə durur.
  * 2-ci Qat (Baza Səviyyəsində Fiziki Kilid): src/AccountTransferLedger.Infrastructure/Services/TransferService.cs:
    SELECT Id FROM Accounts WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id IN (@Id1, @Id2) ORDER BY Id;
  * Sorğu 1 hesabı kilidləyir, 100 AZN çıxır və balansı 0 edir. Sorğu 2 kilidi alan kimi cari balansı oxuyur (0 AZN görür), dərhal InsufficientFundsException atır və HTTP 422 qaytarır. Balans heç bir halda mənfiyə düşə bilmir.

--------------------------------------------------------------------
6. TƏLƏB: Paginated account statement with running balance (Səhifələnmiş hesab çıxarışı və qaçış qalığı)
--------------------------------------------------------------------
- Biznes Məqsədi: Müştəri bank çıxarışına baxarkən hər bir əməliyyatın xronologiyasını və həmin əməliyyatdan sonra hesabında nə qədər qalıq qaldığını (Running Balance) görməlidir. Milyonlarla qeyd ola biləcəyi üçün bu əməliyyat səhifələnməli (PageNumber, PageSize) və çox sürətli işləməlidir.
- Layihədəki Həlli:
  * Fayl: src/AccountTransferLedger.Infrastructure/Persistence/DapperStatementRepository.cs
  * Yüksək sürətli Dapper Micro-ORM və SQL-in analitik Window funksiyası istifadə olunur:
    SELECT l.Id, l.Amount, l.EntryType, l.Description, l.CreatedAtUtc,
           SUM(l.Amount) OVER (
               PARTITION BY l.AccountId 
               ORDER BY l.CreatedAtUtc ASC, l.Id ASC
               ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
           ) as RunningBalance
    FROM LedgerEntries l
    WHERE l.AccountId = @AccountId
    ORDER BY l.CreatedAtUtc DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
  * Bu sorğu bütün keçmiş sətirləri yaddaşa çəkmədən, birbaşa SQL Server səviyyəsində qaçış balansını hesablayır və səhifələnmiş şəkildə millisaniyələr ərzində geri qaytarır.

--------------------------------------------------------------------
7. TƏLƏB: Proper error responses (Standartlaşdırılmış xəta idarəetməsi)
--------------------------------------------------------------------
- Biznes Məqsədi: API xəta baş verəndə sirli 500 server erroru əvəzinə frontend-ə və mobil tətbiqə standartlaşdırılmış, aydın və maşın tərəfindən oxuna bilən JSON formatında cavab verməlidir.
- Layihədəki Həlli:
  * Fayl: src/AccountTransferLedger.API/Middlewares/ExceptionMiddleware.cs və src/AccountTransferLedger.Domain/Exceptions/DomainExceptions.cs
  * Bütün qlobal xətaları tutur və domen istisnalarını müvafiq HTTP kodlarına çevirir:
    1. InsufficientFundsException -> HTTP 422 Unprocessable Entity (INSUFFICIENT_FUNDS, mövcud və tələb olunan balansla birlikdə).
    2. InvalidTransferAmountException / SelfTransferException -> HTTP 400 Bad Request (INVALID_AMOUNT, SELF_TRANSFER).
    3. AccountNotFoundException -> HTTP 404 Not Found (ACCOUNT_NOT_FOUND).
    4. IdempotencyConflictException -> HTTP 409 Conflict (IDEMPOTENCY_CONFLICT).
  * Cavab strukturu həmişə vahid formatdadır: {"success": false, "error": {"code": "...", "message": "..."}}

--------------------------------------------------------------------
8. TƏLƏB: Concurrency Integration Test (Paralel sorğuların avtomatik testi)
--------------------------------------------------------------------
- Fayl: tests/AccountTransferLedger.IntegrationTests/ConcurrentTransferTests.cs
- Həlli: ConcurrentTransfers_ShouldNeverAllowNegativeBalance_AndPreventOverdraft metodu: 100 AZN balansı olan hesaba eyni anda 10 paralel 20 AZN-lik köçürmə (cəmi 200 AZN) göndərilir. Dəqiq 5-i uğurlu olur, 5-i 422 xətası alır və son balans dəqiq 0.00 AZN qalır.

--------------------------------------------------------------------
9. TƏLƏB: DevOps (Docker-Compose & CI Pipeline)
--------------------------------------------------------------------
- Fayllar: docker-compose.yml, Dockerfile, .github/workflows/ci.yml
- Həlli: Multi-container MSSQL 2022 + Backend API + Next.js Frontend və GitHub Actions CI pipeline.

====================================================================
3. LAYİHƏNİN QOVLUQ ARXİTEKTURASI (CLEAN ARCHITECTURE)
====================================================================

account-transfer-ledger-service/
├── src/
│   ├── AccountTransferLedger.Domain/          (Entity-lər, Enum-lar, Biznes Xətaları - Xalis C#)
│   ├── AccountTransferLedger.Application/     (DTO-lar, Servis İnterfeysləri)
│   ├── AccountTransferLedger.Infrastructure/  (MSSQL, EF Core, Dapper, Concurrency Kilidi, Servislər)
│   └── AccountTransferLedger.API/             (Controller-lər, Middleware-lər, Program.cs)
│
├── tests/
│   ├── AccountTransferLedger.UnitTests/        (8 ədəd sürətli Biznes Məntiqi və İdempotensiya Unit Testi)
│   └── AccountTransferLedger.IntegrationTests/ (1 ədəd Yüksək Concurrency Stress Testi - 10 paralel sorğu)
│
├── frontend/                                   (Next.js 16 Müasir Light Theme Vizuallaşdırma UI)
├── docker-compose.yml                          (MSSQL + API + Frontend orkestrasiyası)
├── Dockerfile                                  (Multi-stage .NET 8 Release build faylı)
├── .github/workflows/ci.yml                    (Avtomatlaşdırılmış CI Test və Build Workflow-u)
└── README.md                                   (Əsas sənədləşmə və quraşdırma təlimatı)

====================================================================
4. HƏR BİR FAYLIN VƏ FUNKSİYANIN SADƏ VƏ ƏTRAFLI İZAHI
====================================================================

4.1. Domain Qatı (AccountTransferLedger.Domain)
Bu qat layihənin ürəyidir və heç bir xarici verilənlər bazasından asılı deyil.

- Entities/Account.cs:
  İşi: Bank hesabının əsas məlumatlarını saxlayır (Id, AccountNumber, OwnerName, Currency, CreatedAtUtc).
  Əsas Məntiq: Bu sinifdə Balance sütunu yoxdur! Balans yalnız çıxarışlardan çıxarılır.

- Entities/Transfer.cs:
  İşi: İki hesab arasındakı köçürmə qeydini saxlayır (SourceAccountId, TargetAccountId, Amount, Currency, IdempotencyKey).

- Entities/LedgerEntry.cs:
  İşi: İkiqat mühasibatlıq çıxarışıdır. Dəyişdirilə bilməz (immutable).
  Əsas Məntiq: Debet üçün Amount mənfi (-50.00), Kredit üçün Amount müsbət (+50.00) saxlanılır.

- Entities/IdempotencyRecord.cs:
  İşi: Eyni sorğunun təkrar icrasının qarşısını almaq üçün HTTP Idempotency-Key başlığını, sorğu heşini və JSON cavabını saxlayır.

- Enums/EntryType.cs və IdempotencyStatus.cs:
  EntryType: Debit = 1 (Hesabdan çıxılma), Credit = 2 (Hesaba mədaxil).
  IdempotencyStatus: Pending = 1, Completed = 2, Failed = 3.

- Exceptions/DomainExceptions.cs:
  Biznes qaydaları pozulduqda atılan xətalar: InsufficientFundsException (balans çatışmazlığı), AccountNotFoundException (hesab tapılmadı), IdempotencyConflictException (eyni açarla fərqli parametr), InvalidTransferAmountException (0 və ya mənfi məbləğ).

4.2. Application Qatı (AccountTransferLedger.Application)

- DTOs/AccountDtos.cs: Hesab yaratmaq üçün sorğu (CreateAccountRequest) və cavab (AccountResponse) modelləri.
- DTOs/TransferDtos.cs: Köçürmə sorğusu (CreateTransferRequest) və nəticə (TransferResponse) modelləri.
- DTOs/StatementDtos.cs: Hesab çıxarışı sətirləri (StatementEntryResponse) və səhifələnmiş siyahı (PaginatedStatementResponse).
- DTOs/CommonDtos.cs: Standart xəta cavabı modeli (ErrorResponse).
- Interfaces/ITransferService.cs: ExecuteTransferAsync(...) metodunun interfeysi.
- Interfaces/IAccountService.cs: Hesab əməliyyatlarının interfeysi.
- Interfaces/IStatementService.cs: Çıxarış əməliyyatlarının interfeysi.
- Interfaces/IIdempotencyService.cs: İdempotentlik mexanizminin interfeysi.

4.3. Infrastructure Qatı (AccountTransferLedger.Infrastructure)

- Concurrency/KeyedAsyncLock.cs:
  İşi: Tətbiq daxilində eyni hesab ID-si üzrə gələn paralel sorğuları növbəyə düzən və yaddaş sızması yaratmayan asinxron semafor kilididir.
  Funksiyası: LockAsync(Guid key) — hesab üçün kilid açarı təqdim edir, iş bitdikdə Dispose ilə resursu azad edir.

- Persistence/LedgerDbContext.cs:
  İşi: Entity Framework Core ilə verilənlər bazası cədvəllərini, açarları və indeksləri tənzimləyir.
  Əsas Məntiq: AccountNumber, Transfers.IdempotencyKey və IdempotencyRecords.Key üçün unikal indekslər qurulub.

- Persistence/DapperStatementRepository.cs:
  İşi: Çıxarışları yüksək sürətlə oxumaq üçün Dapper istifadə edir.
  Funksiyası: GetPaginatedStatementAsync(...) — T-SQL Window funksiyası (SUM(Amount) OVER (PARTITION BY AccountId ORDER BY CreatedAtUtc ASC)) ilə hər sətirdəki cari qalığı (RunningBalance) hesablayır və OFFSET / FETCH NEXT ilə səhifələyir.

- Persistence/DbInitializer.cs:
  İşi: Baza işə düşərkən cədvəlləri yaradır və 4 ədəd nümunəvi bank hesabını ilkin kredit çıxarışları ilə bazaya əlavə edir.

- Services/TransferService.cs (Ən Əsas Biznes Mühərriki):
  Funksiyası: ExecuteTransferAsync(CreateTransferRequest request, string idempotencyKey):
  1. Validasiya: Məbləğin 0-dan böyük olduğunu və göndərən ilə alanın fərqli hesablar olduğunu yoxlayır.
  2. İdempotentlik Yoxlanışı: Açar bazada axtarılır. Əgər əməliyyat artıq icra edilibsə, dərhal əvvəlki cavab qaytarılır (təkrar pul çıxılmır).
  3. Deterministik Kilidləmə (Deadlock Prevention): İki hesab kilidlənərkən ID-lər sıralanır (id1 < id2) ki, qarşılıqlı deadlock yaranmasın.
  4. MSSQL Sıra Kilidi: WITH (UPDLOCK, ROWLOCK, HOLDLOCK) əmri ilə göndərən hesabın sətri bloklanır.
  5. Balansın Hesablanması və Overdraft Qorunması: LedgerEntries cəmindən anlıq balans çıxarılır və balans çatışmadıqda InsufficientFundsException atılır.
  6. İkiqat Qeydiyyat: 1 Debet (-məbləğ), 1 Kredit (+məbləğ) və 1 Transfer qeydi yaradılır.
  7. Atomik Commit: transaction.CommitAsync() ilə bazaya vahid şəkildə yazılır.
  8. Keşlənmə: Nəticə idempotency cədvəlinə yazılır.

4.4. API Qatı (AccountTransferLedger.API)

- Controllers/TransfersController.cs: POST /api/transfers — Idempotency-Key başlığı ilə köçürmələri qəbul edir.
- Controllers/AccountsController.cs: POST /api/accounts (yeni hesab) və GET /api/accounts (bütün hesablar və onların hesablanmış balansları).
- Controllers/StatementsController.cs: GET /api/statements/{accountId}?page=1&pageSize=20 (səhifələnmiş çıxarış).
- Controllers/StressTestController.cs: POST /api/stresstest/concurrency (canlı 10 paralel sorğu simulyasiyası).
- Middleware/ExceptionHandlingMiddleware.cs: Bütün biznes xətalarını tutur və standart HTTP kodlarına (400, 404, 409, 422, 500) çevirir.
- Program.cs: Asılılıqların (DI) qeydiyyatı, MSSQL konfiqurasiyası, root / müraciətinin avtomatik /swagger-ə yönləndirilməsi və CORS sazlamaları.

4.5. Test Qatı (tests/ — 9/9 Keçir)

- tests/AccountTransferLedger.UnitTests/:
  - TransferServiceUnitTests.cs: İkiqat qeydiyyat və məbləğlərin düzgünlüyünü yoxlayır.
  - InsufficientFundsTests.cs: Balans çatışmadıqda xətanın atıldığını və bazadan heç bir vəsaitin silinmədiyini (Rollback) yoxlayır.
  - IdempotencyTests.cs: Təkrar açarla göndərilən sorğunun təkrar icra edilmədiyini və eyni açarla fərqli parametr göndərildikdə 409 münaqişə xətası verdiyini yoxlayır.
  - StatementAndAccountTests.cs: Dapper çıxarışlarının və RunningBalance hesablamasının dəqiqliyini yoxlayır.

- tests/AccountTransferLedger.IntegrationTests/:
  - ConcurrentTransferTests.cs: 100 AZN balansı olan hesaba eyni anda 10 ədəd 20 AZN-lik paralel sorğu (cəmi 200 AZN) göndərir. Dəqiq 5-i uğurlu olur (100 AZN silinir), 5-i 422 alır və son balans dəqiq 0.00 AZN qalır.

====================================================================
5. CANLI TƏQDİMAT VƏ VİZUAL TEST BƏLƏDÇİSİ (DEMO & TESTING GUIDE)
====================================================================

Layihəni komandaya və ya intervyuda təqdim edərkən nümayiş etdirəcəyiniz addımlar:

BAŞLANĞIC: Sistemi Qaldırın
Terminalda kök qovluqda bu əmri icra edin:
  docker-compose up -d

Brauzerdə açın:
- Frontend Paneli: http://localhost:3000
- Backend Swagger: http://localhost:8080

TƏQDİMAT ADDIMLARI:

Addım 1: Əsas İdarəetmə Paneli (Dashboard)
1. Brauzerdə http://localhost:3000 səhifəsinə daxil olun.
2. Ekranda 4 ilkin hesabı (Azər Məmmədov, Leyla Əliyeva, Rəşad Quliyev, Nərgiz Hüseynova) və ümumi likvidliyi (5,000.00 AZN) göstərin.
3. İzah: "Bu balanslar bazada hazır sütun deyil, hər bir hesabın çıxarış qeydlərinin anlıq cəmlənməsindən əldə olunur."

Addım 2: Pul Köçürməsi və İdempotentlik Sınağı
1. "Pul Köçürməsi" tabına keçin.
2. Göndərən hesabı və Alan hesabı seçin, məbləğə 50 AZN yazıb "Köçürməni İcra Et" düyməsinə basın.
3. Ekranda tranzaksiyanın uğurla bitdiyini və balansların dərhal dəyişdiyini göstərin.
4. Dərhal sonra "Təkrar Göndər (İdempotent Test)" düyməsinə basın.
5. İzah: "Gördüyünüz kimi, eyni Idempotency-Key ilə təkrar sorğu göndərildikdə sistem təkrar pul silmədi və keşlənmiş uğurlu cavabı dərhal qaytardı."

Addım 3: Səhifələnmiş Çıxarış və Dapper Cari Qalıq
1. "Hesab Çıxarışı" tabına keçin.
2. Hesabı seçin (məs: AZ88ACNT10001).
3. Cədvəldə Debit (-50 AZN) və Credit (+1,000 AZN) qeydlərini göstərin.
4. İzah: "Bu çıxarış Dapper və SQL Window funksiyası (SUM(Amount) OVER (...)) ilə hesablanır və hər əməliyyatdan sonrakı dəqiq cari qalığı (Running Balance) ən yüksək sürətlə təqdim edir."

Addım 4: Canlı Konkurentlik və Overdraft Qorunması Laboratoriyası (Ən Maraqlı Hissə)
1. "Konkurentlik Laboratoriyası" tabına keçin.
2. Balansı 100 AZN olan hesabı seçin.
3. "Stres Testini Başlat (10x Paralel Sorğu)" düyməsinə basın.
4. Ekranda eyni anda göndərilən 10 ədəd 20 AZN-lik (cəmi 200 AZN) paralel sorğu icra olunacaq.
5. İzah:
   - Tam 5 sorğu uğurlu oldu (100 AZN silindi).
   - 5 sorğu 422 Insufficient Funds xətası ilə imtina edildi.
   - Yekun balans mənfiyə düşmədi və dəqiq 0.00 AZN qaldı.
   - "Bu, MSSQL səviyyəsində WITH (UPDLOCK, ROWLOCK, HOLDLOCK) və tətbiqdaxili asinxron kilid vasitəsilə race condition-ın qarşısının tam alınmasını sübut edir."

VISUAL STUDIO TEST EXPLORER VƏ SWAGGER NÜMAYİŞİ:

- Visual Studio-da Testlər:
  1. AccountTransferLedger.sln faylını Visual Studio-da açın.
  2. Test menyusundan Test Explorer (Ctrl + E, T) pəncərəsini açıb Run All Tests düyməsinə basın.
  3. Bütün 9 testin yaşıl olduğunu nümayiş etdirin.

- Terminalda Testlər:
  dotnet test AccountTransferLedger.sln

- Swagger API Sənədləri:
  http://localhost:8080 ünvanında POST /api/transfers, GET /api/statements/{id} və digər bütün metodları birbaşa interaktiv şəkildə icra edib göstərin.

====================================================================
6. ƏSAS ARXİTEKTUR QƏRARLAR VƏ ÜSTÜNLÜKLƏR (DESIGN DECISIONS)
====================================================================

1. Niyə mutable Balance sütunu yoxdur?
   Maliyyə sistemlərində balans sütununu birbaşa yeniləmək audit izini (audit trail) itirir və yarış vəziyyətlərində xətalara yol açır. Hər şey çıxarış qeydlərindən (LedgerEntry) hesablandıqda heç bir qəpik itmir və tam şəffaflıq təmin olunur.

2. İkiqat Concurrency Qorunması (Two-Tier Locking):
   - Tətbiq Səviyyəsi: KeyedAsyncLock ilə eyni hesaba aid sorğular tətbiq daxilində asinxron növbəyə alınır.
   - Verilənlər Bazası Səviyyəsi: MSSQL WITH (UPDLOCK, ROWLOCK, HOLDLOCK) ilə sətirlər fiziki olaraq kilidlənir ki, çoxsaylı API instansiyalarında belə balans mənfiyə düşməsin.

3. Dapper ilə SQL Window Funksiyaları:
   Çıxarışları oxuyarkən EF Core yerinə birbaşa optimallaşdırılmış Dapper və SQL Window funksiyası istifadə olunur. Bu, milyonlarla qeyd olduqda belə saniyənin mində biri sürətində cari qalığı hesablamağa imkan verir.
