using BankingSystem.dto;

namespace BankingSystem.service;

public interface ICustomerService
{
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequestDto dto, CancellationToken ct = default);
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerDto> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<CustomerDto> UpdateKycAsync(Guid id, UpdateKycRequestDto dto, CancellationToken ct = default);
    Task<CustomerDto> UpdateCustomerAsync(Guid id, UpdateCustomerRequestDto dto, CancellationToken ct = default);
}
