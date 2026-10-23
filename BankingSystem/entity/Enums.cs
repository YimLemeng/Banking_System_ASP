namespace BankingSystem.entity;

public enum UserRole
{
    Teller = 1,
    Admin = 2
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    LockedOut = 4
}

public enum AccountType
{
    Savings = 1,
    Checking = 2,
    FixedDeposit = 3
}

public enum AccountStatus
{
    Active = 1,
    Frozen = 2,
    Closed = 3,
    Inactive = 4
}

public enum Currency
{
    USD = 1,
    KHR = 2
}

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    TransferIn = 3,
    TransferOut = 4
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Reversed = 4
}

public enum OtpPurpose
{
    Register = 1,
    Login = 2,
    ResetPassword = 3,
    TransactionConfirm = 4
}

public enum AuditAction
{
    Register = 1,
    LoginSuccess = 2,
    LoginFailed = 3,
    Logout = 4,
    OtpRequested = 5,
    OtpVerified = 6,
    OtpFailed = 7,
    PasswordChanged = 8,
    PasswordReset = 9,
    AccountOpened = 10,
    AccountFrozen = 11,
    AccountUnfrozen = 12,
    AccountClosed = 13,
    Deposit = 14,
    Withdrawal = 15,
    Transfer = 16,
    KycUpdated = 17
}

public enum KycStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3
}
