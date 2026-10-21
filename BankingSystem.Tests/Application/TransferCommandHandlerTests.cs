using AutoMapper;
using BankingSystem.config;
using BankingSystem.dto;
using BankingSystem.entity;
using BankingSystem.exception;
using BankingSystem.mapper;
using BankingSystem.repository;
using BankingSystem.security;
using BankingSystem.service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BankingSystem.Tests;

public class TransferServiceTests
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly Mock<IOtpService> _otpServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly ITransactionService _transactionService;

    public TransferServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(dbOptions);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
        _mapper = config.CreateMapper();

        _otpServiceMock = new Mock<IOtpService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(c => c.PhoneNumber).Returns("+85512345680");

        var unitOfWork = new UnitOfWork(_context);
        var accountRepo = new AccountRepository(_context);
        var txnRepo = new TransactionRepository(_context);
        var auditRepo = new AuditLogRepository(_context);

        _transactionService = new TransactionService(
            accountRepo,
            txnRepo,
            auditRepo,
            _otpServiceMock.Object,
            unitOfWork,
            _currentUserMock.Object,
            _mapper);
    }

    [Fact]
    public async Task Transfer_Successful_ShouldDebitSourceAndCreditTarget()
    {
        var customerId = Guid.NewGuid();
        var source = Account.Open(customerId, "1840100001", AccountType.Savings, Currency.USD, 1000m);
        var target = Account.Open(customerId, "1840100002", AccountType.Savings, Currency.USD, 200m);

        await _context.Accounts.AddRangeAsync(source, target);
        await _context.SaveChangesAsync();

        var dto = new TransferRequestDto(source.Id, target.Id, 300m, Currency.USD, "Rent payment");
        var result = await _transactionService.TransferAsync(dto);

        result.Should().NotBeNull();
        result.Amount.Should().Be(300m);

        var updatedSource = await _context.Accounts.FindAsync(source.Id);
        var updatedTarget = await _context.Accounts.FindAsync(target.Id);

        updatedSource!.Balance.Should().Be(700m);
        updatedTarget!.Balance.Should().Be(500m);
    }

    [Fact]
    public async Task Transfer_InsufficientFunds_ShouldThrowInsufficientFundsException()
    {
        var customerId = Guid.NewGuid();
        var source = Account.Open(customerId, "1840100001", AccountType.Savings, Currency.USD, 100m);
        var target = Account.Open(customerId, "1840100002", AccountType.Savings, Currency.USD, 200m);

        await _context.Accounts.AddRangeAsync(source, target);
        await _context.SaveChangesAsync();

        var dto = new TransferRequestDto(source.Id, target.Id, 500m, Currency.USD, "Overdraft transfer");
        var act = async () => await _transactionService.TransferAsync(dto);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task Transfer_CurrencyMismatch_ShouldThrowCurrencyMismatchException()
    {
        var customerId = Guid.NewGuid();
        var usdAccount = Account.Open(customerId, "1840100001", AccountType.Savings, Currency.USD, 1000m);
        var khrAccount = Account.Open(customerId, "2116200001", AccountType.Checking, Currency.KHR, 2000000m);

        await _context.Accounts.AddRangeAsync(usdAccount, khrAccount);
        await _context.SaveChangesAsync();

        var dto = new TransferRequestDto(usdAccount.Id, khrAccount.Id, 100m, Currency.USD, "Cross currency");
        var act = async () => await _transactionService.TransferAsync(dto);

        await act.Should().ThrowAsync<CurrencyMismatchException>();
    }

    [Fact]
    public async Task Transfer_HighValueWithoutOtp_ShouldThrowDomainException()
    {
        var customerId = Guid.NewGuid();
        var source = Account.Open(customerId, "1840100001", AccountType.Savings, Currency.USD, 5000m);
        var target = Account.Open(customerId, "1840100002", AccountType.Savings, Currency.USD, 200m);

        await _context.Accounts.AddRangeAsync(source, target);
        await _context.SaveChangesAsync();

        var dto = new TransferRequestDto(source.Id, target.Id, 1500m, Currency.USD, "High value transfer", OtpCode: null);
        var act = async () => await _transactionService.TransferAsync(dto);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*2FA OTP code is required*");
    }
}
