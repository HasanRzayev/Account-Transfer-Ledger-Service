using AccountTransferLedgerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountTransferLedgerService.Infrastructure.Persistence;

public class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account Entity Configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AccountNumber).IsRequired().HasMaxLength(32);
            entity.HasIndex(a => a.AccountNumber).IsUnique();
            entity.Property(a => a.AccountHolderName).IsRequired().HasMaxLength(150);
            entity.Property(a => a.Currency).IsRequired().HasMaxLength(3);
            entity.Property(a => a.RowVersion).IsConcurrencyToken();
            entity.HasMany(a => a.LedgerEntries)
                  .WithOne(l => l.Account)
                  .HasForeignKey(l => l.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Transfer Entity Configuration
        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasPrecision(18, 4).IsRequired();
            entity.Property(t => t.Currency).IsRequired().HasMaxLength(3);
            entity.Property(t => t.Description).HasMaxLength(250);
            entity.Property(t => t.IdempotencyKey).HasMaxLength(128);
            entity.HasIndex(t => t.IdempotencyKey);

            entity.HasOne(t => t.FromAccount)
                  .WithMany()
                  .HasForeignKey(t => t.FromAccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToAccount)
                  .WithMany()
                  .HasForeignKey(t => t.ToAccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(t => t.LedgerEntries)
                  .WithOne(l => l.Transfer)
                  .HasForeignKey(l => l.TransferId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // LedgerEntry Entity Configuration
        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Amount).HasPrecision(18, 4).IsRequired();
            entity.Property(l => l.Description).HasMaxLength(250);
            entity.HasIndex(l => l.AccountId);
            entity.HasIndex(l => new { l.AccountId, l.CreatedAtUtc });
            entity.HasIndex(l => l.TransferId);
        });

        // IdempotencyRecord Entity Configuration
        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Key).IsRequired().HasMaxLength(128);
            entity.HasIndex(i => i.Key).IsUnique();
            entity.Property(i => i.RequestPath).HasMaxLength(256);
            entity.Property(i => i.RequestMethod).HasMaxLength(16);
            entity.Property(i => i.RequestBodyHash).HasMaxLength(64);
        });
    }
}
