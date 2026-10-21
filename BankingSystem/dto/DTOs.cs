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
    Guid Id,
    string PhoneNumber,
    string? Email,
    string FullName,
    UserRole Role,
    UserStatus Status,
    bool PhoneNumberConfirmed,
    DateTimeOffset CreatedAt);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType,
    UserDto User);

public record RegisterRequestDto(
    string PhoneNumber,
    string Password,
    string FullName,
    string? Email);

public record LoginPhoneRequestDto(
    string PhoneNumber,
    string Password);

public record LoginPhoneOtpRequestDto(
    string PhoneNumber,
    string Code);

public record LoginEmailRequestDto(
    string Email,
    string Password);

public record RequestOtpDto(
    string PhoneNumber,
    OtpPurpose Purpose);

public record VerifyOtpDto(
    string PhoneNumber,
    string Code,
    OtpPurpose Purpose);

public record RefreshTokenRequestDto(
    string RefreshToken);

public record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword);

public record ResetPasswordRequestDto(
    string PhoneNumber,
    string OtpCode,
    string NewPassword);

public record CustomerDto(
    Guid Id,
    Guid UserId,
    string CustomerNumber,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string NationalIdOrPassport,
    KycStatus KycStatus,
    int KycLevel,
    string Address,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AccountDto>? Accounts = null);

public record CreateCustomerRequestDto(
    Guid UserId,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string NationalIdOrPassport,
    string Address);

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
