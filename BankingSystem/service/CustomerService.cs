using AutoMapper;
using BankingSystem.dto;
using BankingSystem.entity;
using BankingSystem.exception;
using BankingSystem.repository;
using BankingSystem.security;

namespace BankingSystem.service;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public CustomerService(
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequestDto dto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(dto.UserId, ct);
        if (user == null) throw new NotFoundException("User not found.");

        var existing = await _customerRepository.GetByUserIdAsync(dto.UserId, ct);
        if (existing != null) throw new ConflictException("Customer profile already exists for this user.");

        var customerNumber = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var customer = Customer.Create(dto.UserId, customerNumber, dto.FirstName, dto.LastName, dto.DateOfBirth, dto.NationalIdOrPassport, dto.Address);
        await _customerRepository.AddAsync(customer, ct);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.KycUpdated, nameof(Customer), $"Customer profile created: {customerNumber}",
            userId: _currentUser.UserId, entityId: customer.Id.ToString()), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _customerRepository.GetByIdWithAccountsAsync(id, ct);
        if (c == null) throw new NotFoundException("Customer not found.");
        return _mapper.Map<CustomerDto>(c);
    }

    public async Task<CustomerDto> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var c = await _customerRepository.GetByUserIdAsync(userId, ct);
        if (c == null) throw new NotFoundException("Customer profile not found.");
        return _mapper.Map<CustomerDto>(c);
    }

    public async Task<CustomerDto> UpdateKycAsync(Guid id, UpdateKycRequestDto dto, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, ct);
        if (customer == null) throw new NotFoundException("Customer not found.");
        customer.UpdateKyc(dto.Status, dto.Level);
        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<CustomerDto>(customer);
    }
}
