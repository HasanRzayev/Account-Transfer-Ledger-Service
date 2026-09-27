using System.Text.Json.Serialization;
using AccountTransferLedger.API.Middleware;
using AccountTransferLedger.Application.Interfaces;
using AccountTransferLedger.Infrastructure.Concurrency;
using AccountTransferLedger.Infrastructure.Persistence;
using AccountTransferLedger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

Dapper.SqlMapper.AddTypeHandler(new GuidTypeHandler());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hesab Köçürmələri və Baş Kitab Xidməti (Account Transfer & Ledger API)",
        Version = "v1",
        Description = "Yüksək paralellik və tranzaksiya bütövlüyü təmin edən daxili hesablararası pul köçürmə və Baş Kitab sistemi.",
        Contact = new OpenApiContact
        {
            Name = "Mühəndislik Komandası",
            Email = "support@ledger.bank"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetConnectionString("SqlServerConnection");

if (!string.IsNullOrEmpty(connectionString) && (connectionString.Contains("Server=") || connectionString.Contains("Data Source=") || connectionString.Contains("Host=")))
{
    builder.Services.AddDbContext<LedgerDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    var dbPath = Path.Combine(AppContext.BaseDirectory, "ledger_local.db");
    builder.Services.AddDbContext<LedgerDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}

builder.Services.AddSingleton<IKeyedAsyncLock, KeyedAsyncLock>();
builder.Services.AddScoped<ILedgerStatementRepository, DapperStatementRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

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

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Transfer & Ledger Service v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

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
