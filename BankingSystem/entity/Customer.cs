namespace BankingSystem.entity;

public class Customer
{
    private readonly List<Account> _accounts = new();

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string CustomerNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string NationalIdOrPassport { get; set; } = string.Empty;
    public KycStatus KycStatus { get; set; } = KycStatus.Pending;
    public int KycLevel { get; set; } = 1;
    public string Address { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModifiedAt { get; set; }
    public bool IsDeleted { get; set; }

    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

    public static Customer Create(
        Guid userId,
        string customerNumber,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        string nationalIdOrPassport,
        string address)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CustomerNumber = customerNumber,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            NationalIdOrPassport = nationalIdOrPassport.Trim(),
            Address = address.Trim(),
            KycStatus = KycStatus.Pending,
            KycLevel = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateKyc(KycStatus status, int level)
    {
        KycStatus = status;
        KycLevel = level;
    }
}
