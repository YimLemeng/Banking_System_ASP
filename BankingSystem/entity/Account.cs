using BankingSystem.exception;

namespace BankingSystem.entity;

public class Account
{
    private readonly List<Transaction> _transactions = new();

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string AccountNumber { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public Currency Currency { get; set; }
    public decimal Balance { get; set; }
    public AccountStatus Status { get; set; }
    public decimal InterestRate { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModifiedAt { get; set; }
    public bool IsDeleted { get; set; }

    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    public static Account Open(
        Guid customerId,
        string accountNumber,
        AccountType accountType,
        Currency currency,
        decimal initialDeposit = 0m,
        decimal interestRate = 0m)
    {
        if (initialDeposit < 0)
            throw new DomainException("Initial deposit cannot be negative.");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AccountNumber = accountNumber,
            AccountType = accountType,
            Currency = currency,
            Balance = 0m,
            Status = AccountStatus.Active,
            InterestRate = interestRate,
            OpenedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (initialDeposit > 0)
        {
            account.Deposit(initialDeposit, "Initial deposit on account opening");
        }

        return account;
    }

    public Transaction Deposit(decimal amount, string description, string? idempotencyKey = null)
    {
        EnsureAccountIsActive();
        if (amount <= 0) throw new DomainException("Deposit amount must be greater than zero.");

        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Balance += roundedAmount;

        var txn = Transaction.Create(Id, TransactionType.Deposit, roundedAmount, Currency, Balance, description, idempotencyKey);
        _transactions.Add(txn);
        return txn;
    }

    public Transaction Withdraw(decimal amount, string description, string? idempotencyKey = null)
    {
        EnsureAccountIsActive();
        if (amount <= 0) throw new DomainException("Withdrawal amount must be greater than zero.");

        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (Balance < roundedAmount)
            throw new InsufficientFundsException(Balance, roundedAmount);

        Balance -= roundedAmount;

        var txn = Transaction.Create(Id, TransactionType.Withdrawal, roundedAmount, Currency, Balance, description, idempotencyKey);
        _transactions.Add(txn);
        return txn;
    }

    public Transaction DebitForTransfer(decimal amount, Guid targetAccountId, string description, string? idempotencyKey = null)
    {
        EnsureAccountIsActive();
        if (amount <= 0) throw new DomainException("Transfer amount must be greater than zero.");

        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (Balance < roundedAmount)
            throw new InsufficientFundsException(Balance, roundedAmount);

        Balance -= roundedAmount;

        var txn = Transaction.Create(Id, TransactionType.TransferOut, roundedAmount, Currency, Balance, description, idempotencyKey, targetAccountId);
        _transactions.Add(txn);
        return txn;
    }

    public Transaction CreditFromTransfer(decimal amount, Guid sourceAccountId, string description, string? idempotencyKey = null)
    {
        EnsureAccountIsActive();
        if (amount <= 0) throw new DomainException("Transfer amount must be greater than zero.");

        var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Balance += roundedAmount;

        var txn = Transaction.Create(Id, TransactionType.TransferIn, roundedAmount, Currency, Balance, description, idempotencyKey, sourceAccountId);
        _transactions.Add(txn);
        return txn;
    }

    public void Freeze(string reason)
    {
        if (Status == AccountStatus.Closed)
            throw new DomainException("Cannot freeze a closed account.");
        Status = AccountStatus.Frozen;
    }

    public void Unfreeze()
    {
        if (Status != AccountStatus.Frozen)
            throw new DomainException("Account is not frozen.");
        Status = AccountStatus.Active;
    }

    public void Close()
    {
        if (Status == AccountStatus.Closed)
            throw new DomainException("Account is already closed.");
        if (Balance > 0)
            throw new DomainException($"Cannot close account with positive balance: {Balance:N2} {Currency}. Please withdraw or transfer all funds first.");
        Status = AccountStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureAccountIsActive()
    {
        if (Status != AccountStatus.Active)
            throw new AccountNotActiveException(Status.ToString());
    }
}
