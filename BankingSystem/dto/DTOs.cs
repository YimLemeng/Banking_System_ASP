using BankingSystem.entity;

namespace BankingSystem.dto;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new()
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors?.ToList().AsReadOnly() ?? new List<string> { message }.AsReadOnly()
        };
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int count, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = count;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}

public record UserDto(
    Guid Id = default,
    string? Username = null,
    string? Email = null,
    string FullName = "",
    UserRole Role = UserRole.Teller,
    UserStatus Status = UserStatus.Active,
    bool TotpEnabled = false,
    DateTimeOffset CreatedAt = default,
    string? PhoneNumber = null,
    bool PhoneNumberConfirmed = false)
{
    public UserDto() : this(Guid.Empty, null, null, string.Empty, UserRole.Teller, UserStatus.Active, false, DateTimeOffset.UtcNow, null, false) { }
}

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    UserDto User);

public record LoginRequestDto(
    string Username,
    string Password);

public record TotpSetupResponseDto(
    string SecretKey,
    string QrCodeUri,
    string QrCodeBase64Png);

public record LoginWithTotpRequestDto(
    string Username,
    string Password,
    string TotpCode);

public record LoginPreCheckResponseDto(
    bool RequiresTotp,
    bool TotpEnabled,
    string? TotpSetupQrBase64,   
    string? TotpSecretKey,       
    string? TempToken);          

public record RegisterRequestDto(
    string Username,
    string Password,
    string FullName,
    string? PhoneNumber = null,
    string? Email = null);

public record LoginEmailRequestDto(
    string Email,
    string Password);

public record RefreshTokenRequestDto(
    string RefreshToken);

public record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword);

public record CustomerDto(
    Guid Id,
    Guid? UserId,
    string CustomerNumber,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string NationalIdOrPassport,
    KycStatus KycStatus,
    int KycLevel,
    string Address,
    DateTimeOffset CreatedAt,
    string? PhoneNumber = null,
    IReadOnlyList<AccountDto>? Accounts = null)
{
    public CustomerDto() : this(Guid.Empty, null, string.Empty, string.Empty, string.Empty, default, string.Empty, KycStatus.Pending, 1, string.Empty, DateTimeOffset.UtcNow, null, null) { }
}

public record CreateCustomerRequestDto(
    Guid? UserId,
    string? PhoneNumber,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string NationalIdOrPassport,
    string Address);

public record UpdateCustomerRequestDto(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string NationalIdOrPassport,
    string Address,
    string? PhoneNumber = null);

public record UpdateKycRequestDto(
    KycStatus Status,
    int Level);

public record AccountDto(
    Guid Id,
    Guid CustomerId,
    string AccountNumber,
    AccountType AccountType,
    Currency Currency,
    decimal Balance,
    AccountStatus Status,
    decimal InterestRate,
    DateTimeOffset OpenedAt);

public record CreateAccountRequestDto(
    Guid CustomerId,
    AccountType AccountType,
    Currency Currency,
    decimal InitialDeposit = 0m);

public record AccountBalanceDto(
    Guid AccountId,
    string AccountNumber,
    decimal Balance,
    Currency Currency,
    AccountStatus Status);

public record FreezeAccountRequestDto(
    string Reason);

public record TransactionDto(
    Guid Id,
    string TransactionReference,
    Guid AccountId,
    Guid? TargetAccountId,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    decimal BalanceAfter,
    string Description,
    TransactionStatus Status,
    string? IdempotencyKey,
    DateTimeOffset CreatedAt);

public record DepositRequestDto(
    Guid AccountId,
    decimal Amount,
    Currency Currency,
    string Description,
    string? IdempotencyKey = null);

public record WithdrawRequestDto(
    Guid AccountId,
    decimal Amount,
    Currency Currency,
    string Description,
    string? IdempotencyKey = null);

public record TransferRequestDto(
    Guid SourceAccountId,
    Guid TargetAccountId,
    decimal Amount,
    Currency Currency,
    string Description,
    string? IdempotencyKey = null,
    string? OtpCode = null);

public record AccountStatementDto(
    AccountDto Account,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    decimal StartingBalance,
    decimal EndingBalance,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    IReadOnlyList<TransactionDto> Transactions);

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? MaskedPhoneNumber,
    AuditAction Action,
    string EntityName,
    string? EntityId,
    string Details,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAt);
