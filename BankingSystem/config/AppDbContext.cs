using BankingSystem.entity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.config;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IdempotentRequest> IdempotentRequests => Set<IdempotentRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.PhoneNumber)
                .HasConversion(p => p.Value, v => PhoneNumber.Create(v, "+855"))
                .HasMaxLength(20).IsRequired();
            builder.HasIndex(u => u.PhoneNumber).IsUnique();
            builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            builder.HasMany(u => u.RefreshTokens).WithOne().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasQueryFilter(u => !u.IsDeleted);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedOnAdd();
            builder.Property(r => r.Token).HasMaxLength(500).IsRequired();
            builder.HasIndex(r => r.Token);
        });

        // Customer
        modelBuilder.Entity<Customer>(builder =>
        {
            builder.ToTable("Customers");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.CustomerNumber).HasMaxLength(50).IsRequired();
            builder.HasIndex(c => c.CustomerNumber).IsUnique();
            builder.HasOne(c => c.User).WithOne().HasForeignKey<Customer>(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(c => c.Accounts).WithOne(a => a.Customer).HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasQueryFilter(c => !c.IsDeleted);
        });

        // Account
        modelBuilder.Entity<Account>(builder =>
        {
            builder.ToTable("Accounts");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.AccountNumber).HasMaxLength(30).IsRequired();
            builder.HasIndex(a => a.AccountNumber).IsUnique();
            builder.Property(a => a.Balance).HasPrecision(18, 2).IsRequired();
            builder.Property(a => a.InterestRate).HasPrecision(5, 2);
            builder.Property(a => a.RowVersion).IsRowVersion();
            builder.HasMany(a => a.Transactions).WithOne(t => t.Account).HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Cascade);
            builder.HasQueryFilter(a => !a.IsDeleted);
        });

        // Transaction
        modelBuilder.Entity<Transaction>(builder =>
        {
            builder.ToTable("Transactions");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.TransactionReference).HasMaxLength(50).IsRequired();
            builder.HasIndex(t => t.TransactionReference).IsUnique();
            builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
            builder.Property(t => t.BalanceAfter).HasPrecision(18, 2).IsRequired();
            builder.HasIndex(t => t.IdempotencyKey);
            builder.HasIndex(t => new { t.AccountId, t.CreatedAt });
            builder.HasQueryFilter(t => !t.Account.IsDeleted);
        });

        // OtpCode
        modelBuilder.Entity<OtpCode>(builder =>
        {
            builder.ToTable("OtpCodes");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.PhoneNumber).HasMaxLength(20).IsRequired();
            builder.Property(o => o.CodeHash).HasMaxLength(256).IsRequired();
            builder.HasIndex(o => new { o.PhoneNumber, o.Purpose, o.ExpiresAt });
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(a => a.Id);
            builder.HasIndex(a => new { a.UserId, a.CreatedAt });
        });

        // IdempotentRequest
        modelBuilder.Entity<IdempotentRequest>(builder =>
        {
            builder.ToTable("IdempotentRequests");
            builder.HasKey(i => i.Id);
            builder.HasIndex(i => i.IdempotencyKey).IsUnique();
        });
    }
}
