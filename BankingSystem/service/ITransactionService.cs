using BankingSystem.dto;
using BankingSystem.entity;

namespace BankingSystem.service;

public interface ITransactionService
{
    Task<TransactionDto> DepositAsync(DepositRequestDto dto, CancellationToken ct = default);
    Task<TransactionDto> WithdrawAsync(WithdrawRequestDto dto, CancellationToken ct = default);
    Task<TransactionDto> TransferAsync(TransferRequestDto dto, CancellationToken ct = default);
    Task<PagedResult<TransactionDto>> GetHistoryAsync(Guid accountId, int pageIndex, int pageSize, DateTimeOffset? from, DateTimeOffset? to, TransactionType? type, CancellationToken ct = default);
    Task<AccountStatementDto> GetStatementAsync(Guid accountId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
