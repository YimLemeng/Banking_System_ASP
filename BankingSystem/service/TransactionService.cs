using AutoMapper;
using BankingSystem.dto;
using BankingSystem.entity;
using BankingSystem.exception;
using BankingSystem.repository;
using BankingSystem.security;

namespace BankingSystem.service;

public class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public TransactionService(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<TransactionDto> DepositAsync(DepositRequestDto dto, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existing = await _transactionRepository.GetByIdempotencyKeyAsync(dto.IdempotencyKey, ct);
            if (existing != null) return _mapper.Map<TransactionDto>(existing);
        }

        var account = await _accountRepository.GetByIdAsync(dto.AccountId, ct);
        if (account == null) throw new NotFoundException("Account not found.");
        if (account.Currency != dto.Currency)
            throw new CurrencyMismatchException(account.Currency.ToString(), dto.Currency.ToString());

        var txn = account.Deposit(dto.Amount, dto.Description, dto.IdempotencyKey);
        await _transactionRepository.AddAsync(txn, ct);
        _accountRepository.Update(account);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.Deposit, nameof(Transaction), $"Deposited {dto.Amount:N2} {dto.Currency} into {account.AccountNumber}",
            userId: _currentUser.UserId, entityId: txn.Id.ToString()), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<TransactionDto>(txn);
    }

    public async Task<TransactionDto> WithdrawAsync(WithdrawRequestDto dto, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existing = await _transactionRepository.GetByIdempotencyKeyAsync(dto.IdempotencyKey, ct);
            if (existing != null) return _mapper.Map<TransactionDto>(existing);
        }

        var account = await _accountRepository.GetByIdAsync(dto.AccountId, ct);
        if (account == null) throw new NotFoundException("Account not found.");
        if (account.Currency != dto.Currency)
            throw new CurrencyMismatchException(account.Currency.ToString(), dto.Currency.ToString());

        var txn = account.Withdraw(dto.Amount, dto.Description, dto.IdempotencyKey);
        await _transactionRepository.AddAsync(txn, ct);
        _accountRepository.Update(account);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.Withdrawal, nameof(Transaction), $"Withdrew {dto.Amount:N2} {dto.Currency} from {account.AccountNumber}",
            userId: _currentUser.UserId, entityId: txn.Id.ToString()), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<TransactionDto>(txn);
    }

    public async Task<TransactionDto> TransferAsync(TransferRequestDto dto, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existing = await _transactionRepository.GetByIdempotencyKeyAsync(dto.IdempotencyKey, ct);
            if (existing != null) return _mapper.Map<TransactionDto>(existing);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var source = await _accountRepository.GetByIdAsync(dto.SourceAccountId, ct);
            var target = await _accountRepository.GetByIdAsync(dto.TargetAccountId, ct);

            if (source == null || target == null) throw new NotFoundException("Account not found.");
            if (source.Currency != dto.Currency || target.Currency != dto.Currency)
                throw new CurrencyMismatchException(dto.Currency.ToString(), "Mismatch with account currency.");

            var debit = source.DebitForTransfer(dto.Amount, target.Id, dto.Description, dto.IdempotencyKey);
            var credit = target.CreditFromTransfer(dto.Amount, source.Id, dto.Description, $"{dto.IdempotencyKey}-cr");

            await _transactionRepository.AddAsync(debit, ct);
            await _transactionRepository.AddAsync(credit, ct);
            _accountRepository.Update(source);
            _accountRepository.Update(target);

            await _auditLogRepository.AddAsync(AuditLog.Create(
                AuditAction.Transfer, nameof(Transaction), $"Transferred {dto.Amount:N2} {dto.Currency} from {source.AccountNumber} to {target.AccountNumber}",
                userId: _currentUser.UserId, entityId: debit.Id.ToString()), ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return _mapper.Map<TransactionDto>(debit);
        }, ct);
    }

    public async Task<PagedResult<TransactionDto>> GetHistoryAsync(
        Guid accountId, int pageIndex, int pageSize, DateTimeOffset? from, DateTimeOffset? to, TransactionType? type, CancellationToken ct = default)
    {
        var items = await _transactionRepository.GetPagedTransactionsAsync(accountId, pageIndex, pageSize, from, to, type, ct);
        var count = await _transactionRepository.GetTransactionsCountAsync(accountId, from, to, type, ct);
        return new PagedResult<TransactionDto>(_mapper.Map<IReadOnlyList<TransactionDto>>(items), count, pageIndex, pageSize);
    }

    public async Task<AccountStatementDto> GetStatementAsync(Guid accountId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, ct);
        if (account == null) throw new NotFoundException("Account not found.");

        var txns = await _transactionRepository.GetPagedTransactionsAsync(accountId, 1, 1000, from, to, null, ct);
        var dtos = _mapper.Map<IReadOnlyList<TransactionDto>>(txns);

        var deposits = dtos.Where(t => t.Type is TransactionType.Deposit or TransactionType.TransferIn).Sum(t => t.Amount);
        var withdrawals = dtos.Where(t => t.Type is TransactionType.Withdrawal or TransactionType.TransferOut).Sum(t => t.Amount);
        var ending = account.Balance;
        var starting = ending - deposits + withdrawals;

        return new AccountStatementDto(_mapper.Map<AccountDto>(account), from, to, starting, ending, deposits, withdrawals, dtos);
    }
}
