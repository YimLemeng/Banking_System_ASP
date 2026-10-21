using AutoMapper;
using BankingSystem.dto;
using BankingSystem.entity;

namespace BankingSystem.mapper;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.PhoneNumber.Value));

        CreateMap<Customer, CustomerDto>();
        CreateMap<Account, AccountDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<AuditLog, AuditLogDto>();
    }
}
