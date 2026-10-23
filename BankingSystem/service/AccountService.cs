using AutoMapper;
using BankingSystem.dto;
using BankingSystem.entity;
using BankingSystem.exception;
using BankingSystem.repository;
using BankingSystem.security;

namespace BankingSystem.service;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public AccountService(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<AccountDto> OpenAccountAsync(CreateAccountRequestDto dto, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId, ct);
        if (customer == null) throw new NotFoundException("Customer not found.");

        var accountNumber = $"{(int)dto.AccountType}{(dto.Currency == Currency.USD ? "840" : "116")}{Random.Shared.Next(1000000, 9999999)}";
        var interestRate = dto.AccountType switch
        {
            AccountType.Savings => 3.5m,
            AccountType.FixedDeposit => 6.0m,
            _ => 0.0m
        };

        var account = Account.Open(dto.CustomerId, accountNumber, dto.AccountType, dto.Currency, dto.InitialDeposit, interestRate);
        await _accountRepository.AddAsync(account, ct);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.AccountOpened, nameof(Account), $"Account {accountNumber} opened for customer {customer.CustomerNumber}",
            userId: _currentUser.UserId, entityId: account.Id.ToString(), ipAddress: _currentUser.IpAddress), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<AccountDto>(account);
    }

    public async Task<AccountDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);
        if (account == null) throw new NotFoundException("Account not found.");
        return _mapper.Map<AccountDto>(account);
    }

    public async Task<AccountDto> GetByAccountNumberAsync(string accountNumber, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByAccountNumberAsync(accountNumber, ct);
        if (account == null) throw new NotFoundException($"Account '{accountNumber}' not found.");
        return _mapper.Map<AccountDto>(account);
    }

    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken ct = default)
    {
        var accounts = await _accountRepository.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<AccountDto>>(accounts);
    }

    public async Task<IReadOnlyList<AccountDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var accounts = await _accountRepository.GetByCustomerIdAsync(customerId, ct);
        return _mapper.Map<IReadOnlyList<AccountDto>>(accounts);
    }

    public async Task<AccountBalanceDto> GetBalanceAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);
        if (account == null) throw new NotFoundException("Account not found.");
        return new AccountBalanceDto(account.Id, account.AccountNumber, account.Balance, account.Currency, account.Status);
    }

    public async Task FreezeAccountAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);
        if (account == null) throw new NotFoundException("Account not found.");
        account.Freeze(reason);
        _accountRepository.Update(account);
        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.AccountFrozen, nameof(Account), $"Account {account.AccountNumber} frozen. Reason: {reason}",
            userId: _currentUser.UserId, entityId: account.Id.ToString()), ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task UnfreezeAccountAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);
        if (account == null) throw new NotFoundException("Account not found.");
        account.Unfreeze();
        _accountRepository.Update(account);
        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.AccountUnfrozen, nameof(Account), $"Account {account.AccountNumber} unfrozen",
            userId: _currentUser.UserId, entityId: account.Id.ToString()), ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task CloseAccountAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);
        if (account == null) throw new NotFoundException("Account not found.");
        account.Close();
        _accountRepository.Update(account);
        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.AccountClosed, nameof(Account), $"Account {account.AccountNumber} closed",
            userId: _currentUser.UserId, entityId: account.Id.ToString()), ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
