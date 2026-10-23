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
    private readonly IPasswordHasher _passwordHasher;

    public CustomerService(
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper,
        IPasswordHasher passwordHasher)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequestDto dto, CancellationToken ct = default)
    {
        User? user = null;

        if (dto.UserId.HasValue && dto.UserId.Value != Guid.Empty)
        {
            user = await _userRepository.GetByIdAsync(dto.UserId.Value, ct);
        }
        else if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            var normalizedPhone = PhoneNumber.Normalize(dto.PhoneNumber);
            user = await _userRepository.GetByPhoneNumberAsync(normalizedPhone, ct);
            if (user == null)
            {
                var tempPassword = _passwordHasher.HashPassword("Customer@123!");
                user = User.Create(PhoneNumber.Create(normalizedPhone), tempPassword, $"{dto.FirstName} {dto.LastName}".Trim(), UserRole.Teller);
                user.ConfirmPhoneNumber();
                await _userRepository.AddAsync(user, ct);
            }
        }
        else
        {
            throw new DomainException("Please provide a valid Client Phone Number.");
        }

        if (user == null) throw new NotFoundException("User account could not be found or registered.");

        var existing = await _customerRepository.GetByUserIdAsync(user.Id, ct);
        if (existing != null) throw new ConflictException("A customer profile already exists for this phone number / user.");

        var customerNumber = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var customer = Customer.Create(user.Id, customerNumber, dto.FirstName, dto.LastName, dto.DateOfBirth, dto.NationalIdOrPassport, dto.Address);
        customer.UpdateKyc(KycStatus.Verified, 1);
        await _customerRepository.AddAsync(customer, ct);

        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.KycUpdated, nameof(Customer), $"Customer profile created: {customerNumber} ({dto.FirstName} {dto.LastName})",
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

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = await _customerRepository.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<CustomerDto>>(customers);
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
