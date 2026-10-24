using AutoMapper;
using BankingSystem.dto;
using BankingSystem.entity;

namespace BankingSystem.mapper;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<Customer, CustomerDto>();
        CreateMap<Account, AccountDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<AuditLog, AuditLogDto>();
    }
}
