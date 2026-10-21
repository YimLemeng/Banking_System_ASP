namespace BankingSystem.exception;

public class AppException : Exception
{
    public AppException(string message) : base(message) { }
    public AppException(string message, Exception innerException) : base(message, innerException) { }
}

public class DomainException : AppException
{
    public DomainException(string message) : base(message) { }
}

public class InvalidPhoneNumberException : DomainException
{
    public InvalidPhoneNumberException(string message) : base(message) { }
}

public class InsufficientFundsException : DomainException
{
    public decimal CurrentBalance { get; }
    public decimal RequestedAmount { get; }

    public InsufficientFundsException(decimal currentBalance, decimal requestedAmount)
        : base($"Insufficient funds: available {currentBalance:N2}, requested {requestedAmount:N2}.")
    {
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }
}

public class AccountNotActiveException : DomainException
{
    public AccountNotActiveException(string status)
        : base($"Account operation is invalid because the account status is {status}.") { }
}

public class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(string expectedCurrency, string actualCurrency)
        : base($"Currency mismatch: expected {expectedCurrency}, received {actualCurrency}.") { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
}
