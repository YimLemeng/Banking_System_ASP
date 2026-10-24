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
        string normalizedPhone = "";
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            if (PhoneNumber.TryCreate(dto.PhoneNumber, out var p) && p != null)
            {
                normalizedPhone = p.Value;
                var existingCust = await _customerRepository.GetByPhoneNumberAsync(normalizedPhone, ct);
                if (existingCust != null)
                    throw new ConflictException("លេខទូរស័ព្ទនេះត្រូវបានប្រើប្រាស់ដោយអតិថិជនផ្សេងរួចហើយ (A customer profile already exists for this phone number).");
            }
            else
            {
                throw new InvalidPhoneNumberException("ទម្រង់លេខទូរស័ព្ទមិនត្រឹមត្រូវ (Invalid phone number format).");
            }
        }

        Guid? userId = null;
        User? user = null;

        if (dto.UserId.HasValue && dto.UserId.Value != Guid.Empty)
        {
            user = await _userRepository.GetByIdAsync(dto.UserId.Value, ct);
            if (user == null)
                throw new NotFoundException("User account could not be found.");

            var existing = await _customerRepository.GetByUserIdAsync(user.Id, ct);
            if (existing != null)
                throw new ConflictException("A customer profile already exists for this user.");

            userId = user.Id;
        }

        var customerNumber = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var customer = Customer.Create(userId, customerNumber, dto.FirstName, dto.LastName, dto.DateOfBirth, dto.NationalIdOrPassport, dto.Address, normalizedPhone);
        if (user != null)
        {
            customer.User = user;
        }
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
        var customers = await _customerRepository.GetAllWithAccountsAsync(ct);
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
        var customer = await _customerRepository.GetByIdWithAccountsAsync(id, ct);
        if (customer == null) throw new NotFoundException("Customer not found.");
        customer.UpdateKyc(dto.Status, dto.Level);
        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(Guid id, UpdateCustomerRequestDto dto, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdWithAccountsAsync(id, ct);
        if (customer == null) throw new NotFoundException("Customer not found.");

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            if (PhoneNumber.TryCreate(dto.PhoneNumber, out var p) && p != null)
            {
                normalizedPhone = p.Value;
                var existingCust = await _customerRepository.GetByPhoneNumberAsync(normalizedPhone, ct);
                if (existingCust != null && existingCust.Id != customer.Id)
                {
                    throw new ConflictException($"លេខទូរស័ព្ទ {dto.PhoneNumber} ({normalizedPhone}) ត្រូវបានប្រើប្រាស់ដោយអតិថិជនផ្សេងរួចហើយ (This phone number is already registered to another customer).");
                }
            }
            else
            {
                throw new InvalidPhoneNumberException("ទម្រង់លេខទូរស័ព្ទមិនត្រឹមត្រូវ (Invalid phone number format).");
            }
        }

        customer.UpdateDetails(dto.FirstName, dto.LastName, dto.DateOfBirth, dto.NationalIdOrPassport, dto.Address, normalizedPhone);

        if (customer.User != null)
        {
            customer.User.FullName = $"{dto.FirstName.Trim()} {dto.LastName.Trim()}";
            _userRepository.Update(customer.User);
        }

        _customerRepository.Update(customer);
        await _auditLogRepository.AddAsync(AuditLog.Create(
            AuditAction.KycUpdated, nameof(Customer), $"Updated profile for customer {customer.CustomerNumber} ({customer.FirstName} {customer.LastName})",
            userId: _currentUser.UserId, entityId: customer.Id.ToString()), ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<CustomerDto>(customer);
    }
}
