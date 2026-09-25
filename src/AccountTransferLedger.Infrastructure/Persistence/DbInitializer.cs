using AccountTransferLedger.Domain.Entities;
using AccountTransferLedger.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountTransferLedger.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(LedgerDbContext context, ILogger logger)
    {
        try
        {
            if (context.Database.IsRelational())
            {
                await context.Database.EnsureCreatedAsync();
            }

            if (!await context.Accounts.AnyAsync())
            {
                logger.LogInformation("Verilənlər bazası boşdur. Nümunə bank hesabları yaradılır...");

                var azer = new Account
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    AccountNumber = "AZ88ACNT10001",
                    AccountHolderName = "Azər Məmmədov",
                    Currency = "AZN",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                    IsActive = true
                };

                var nigar = new Account
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    AccountNumber = "AZ88ACNT10002",
                    AccountHolderName = "Nigar Əliyeva",
                    Currency = "AZN",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-8),
                    IsActive = true
                };

                var reshad = new Account
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    AccountNumber = "AZ88ACNT10003",
                    AccountHolderName = "Rəşad Hüseynov",
                    Currency = "AZN",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                    IsActive = true
                };

                var leyla = new Account
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    AccountNumber = "AZ88ACNT10004",
                    AccountHolderName = "Leyla Qasımova",
                    Currency = "AZN",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                    IsActive = true
                };

                await context.Accounts.AddRangeAsync(azer, nigar, reshad, leyla);
                await context.SaveChangesAsync();

                var initialEntries = new List<LedgerEntry>
                {
                    new()
                    {
                        AccountId = azer.Id,
                        Amount = 1500.00m,
                        EntryType = EntryType.Credit,
                        Description = "İlkin açılış balansı depoziti",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
                    },
                    new()
                    {
                        AccountId = nigar.Id,
                        Amount = 850.00m,
                        EntryType = EntryType.Credit,
                        Description = "İlkin açılış balansı depoziti",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-8)
                    },
                    new()
                    {
                        AccountId = reshad.Id,
                        Amount = 2300.00m,
                        EntryType = EntryType.Credit,
                        Description = "İlkin açılış balansı depoziti",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
                    },
                    new()
                    {
                        AccountId = leyla.Id,
                        Amount = 350.00m,
                        EntryType = EntryType.Credit,
                        Description = "İlkin açılış balansı depoziti",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
                    }
                };

                await context.LedgerEntries.AddRangeAsync(initialEntries);
                await context.SaveChangesAsync();

                logger.LogInformation("Nümunə hesablar və ilkin baş kitab qeydləri uğurla əlavə edildi.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Verilənlər bazası inisializasiya edilərkən xəta baş verdi.");
            throw;
        }
    }
}
