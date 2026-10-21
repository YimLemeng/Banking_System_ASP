using BankingSystem.dto;

namespace BankingSystem.service;

public interface IAccountService
{
    Task<AccountDto> OpenAccountAsync(CreateAccountRequestDto dto, CancellationToken ct = default);
    Task<AccountDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AccountDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<AccountBalanceDto> GetBalanceAsync(Guid id, CancellationToken ct = default);
    Task FreezeAccountAsync(Guid id, string reason, CancellationToken ct = default);
    Task UnfreezeAccountAsync(Guid id, CancellationToken ct = default);
    Task CloseAccountAsync(Guid id, CancellationToken ct = default);
}
