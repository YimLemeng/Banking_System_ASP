namespace BankingSystem.entity;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TransactionReference { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public Guid? TargetAccountId { get; set; }

    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static Transaction Create(
        Guid accountId,
        TransactionType type,
        decimal amount,
        Currency currency,
        decimal balanceAfter,
        string description,
        string? idempotencyKey = null,
        Guid? targetAccountId = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionReference = GenerateReference(type),
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Currency = currency,
            BalanceAfter = balanceAfter,
            Description = description,
            Status = TransactionStatus.Completed,
            IdempotencyKey = idempotencyKey,
            TargetAccountId = targetAccountId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string GenerateReference(TransactionType type)
    {
        var prefix = type switch
        {
            TransactionType.Deposit => "DEP",
            TransactionType.Withdrawal => "WTH",
            TransactionType.TransferIn => "TRIN",
            TransactionType.TransferOut => "TROUT",
            _ => "TXN"
        };
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var random = Random.Shared.Next(1000, 9999);
        return $"{prefix}-{timestamp}-{random}";
    }
}
