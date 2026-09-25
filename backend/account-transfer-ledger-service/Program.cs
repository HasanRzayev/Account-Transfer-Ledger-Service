using System.Text.Json.Serialization;
using AccountTransferLedgerService.Api.Middleware;
using AccountTransferLedgerService.Application.Interfaces;
using AccountTransferLedgerService.Application.Services;
using AccountTransferLedgerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller & JSON Configuration
Dapper.SqlMapper.AddTypeHandler(new AccountTransferLedgerService.Infrastructure.Persistence.GuidTypeHandler());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

// 2. Swagger / OpenAPI Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hesab Köçürmələri və Baş Kitab Xidməti (Account Transfer & Ledger API)",
        Version = "v1",
        Description = "Yüksək paralellik (concurrency) və tranzaksiya bütövlüyü təmin edən daxili hesablararası pul köçürmə və Baş Kitab (Ledger) sistemi.",
        Contact = new OpenApiContact
        {
            Name = "Mühəndislik Komandası",
            Email = "support@ledger.bank"
        }
    });

    // XML Documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 3. Database Configuration (PostgreSQL / SQLite fallback)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("PostgreSqlConnection");

if (!string.IsNullOrEmpty(connectionString) && (connectionString.Contains("Host=") || connectionString.Contains("Server=") || connectionString.Contains("Port=")))
{
    builder.Services.AddDbContext<LedgerDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
        }));
}
else
{
    // SQLite local / fallback for seamless local execution
    var dbPath = Path.Combine(AppContext.BaseDirectory, "ledger_local.db");
    builder.Services.AddDbContext<LedgerDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}

// 4. Dependency Injection
builder.Services.AddSingleton<AccountTransferLedgerService.Infrastructure.Concurrency.IKeyedAsyncLock, AccountTransferLedgerService.Infrastructure.Concurrency.KeyedAsyncLock>();
builder.Services.AddScoped<ILedgerStatementRepository, DapperStatementRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

// 5. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 6. Global Exception Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 7. Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Transfer & Ledger Service v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Ana səhifəni avtomatik /swagger ünvanına yönləndir
app.MapGet("/", () => Results.Redirect("/swagger"));

// 8. Auto-migrate / seed demo data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<LedgerDbContext>();
        await DbInitializer.InitializeDatabaseAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Verilənlər bazası inisializasiya olunarkən xəta baş verdi.");
    }
}

app.Run();

public partial class Program { }
