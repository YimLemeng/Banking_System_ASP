using BankingSystem.entity;
using BankingSystem.exception;
using FluentAssertions;
using Xunit;

namespace BankingSystem.Tests;

public class AccountTests
{
    [Fact]
    public void Open_WithInitialDeposit_ShouldSetCorrectBalanceAndStatus()
    {
        var customerId = Guid.NewGuid();
        var account = Account.Open(customerId, "1840100001", AccountType.Savings, Currency.USD, 500m, 3.5m);

        account.CustomerId.Should().Be(customerId);
        account.AccountNumber.Should().Be("1840100001");
        account.Balance.Should().Be(500m);
        account.Currency.Should().Be(Currency.USD);
        account.Status.Should().Be(AccountStatus.Active);
        account.Transactions.Should().HaveCount(1);
        account.Transactions.First().Type.Should().Be(TransactionType.Deposit);
    }

    [Fact]
    public void Deposit_ValidAmount_ShouldIncreaseBalanceAndRecordTransaction()
    {
        var account = Account.Open(Guid.NewGuid(), "1840100001", AccountType.Savings, Currency.USD, 100m);
        var txn = account.Deposit(250.50m, "ATM deposit");

        account.Balance.Should().Be(350.50m);
        txn.Amount.Should().Be(250.50m);
        txn.BalanceAfter.Should().Be(350.50m);
        txn.Type.Should().Be(TransactionType.Deposit);
    }

    [Fact]
    public void Withdraw_SufficientBalance_ShouldDecreaseBalance()
    {
        var account = Account.Open(Guid.NewGuid(), "1840100001", AccountType.Savings, Currency.USD, 500m);
        var txn = account.Withdraw(200m, "Cash withdrawal");

        account.Balance.Should().Be(300m);
        txn.Amount.Should().Be(200m);
        txn.BalanceAfter.Should().Be(300m);
    }

    [Fact]
    public void Withdraw_InsufficientBalance_ShouldThrowInsufficientFundsException()
    {
        var account = Account.Open(Guid.NewGuid(), "1840100001", AccountType.Savings, Currency.USD, 100m);
        var act = () => account.Withdraw(150m, "Overdraft attempt");

        act.Should().Throw<InsufficientFundsException>();
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Freeze_ActiveAccount_ShouldChangeStatusToFrozen()
    {
        var account = Account.Open(Guid.NewGuid(), "1840100001", AccountType.Savings, Currency.USD, 100m);
        account.Freeze("Suspicious activity");
        account.Status.Should().Be(AccountStatus.Frozen);
    }

    [Fact]
    public void DepositOrWithdraw_FrozenAccount_ShouldThrowAccountNotActiveException()
    {
        var account = Account.Open(Guid.NewGuid(), "1840100001", AccountType.Savings, Currency.USD, 100m);
        account.Freeze("Investigation");

        var depositAct = () => account.Deposit(50m, "Deposit to frozen");
        var withdrawAct = () => account.Withdraw(50m, "Withdraw from frozen");

        depositAct.Should().Throw<AccountNotActiveException>();
        withdrawAct.Should().Throw<AccountNotActiveException>();
    }

    [Fact]
    public void Close_AccountWithPositiveBalance_ShouldThrowDomainException()
    {
        var account = Account.Open(Guid.NewGuid(), "1840100001", AccountType.Savings, Currency.USD, 100m);
        var act = () => account.Close();
        act.Should().Throw<DomainException>().WithMessage("*positive balance*");
    }

    [Fact]
    public void Close_AccountWithZeroBalance_ShouldSetStatusToClosed()
    {
        var account = Account.Open(Guid.NewGuid(), "1840100001", AccountType.Savings, Currency.USD, 0m);
        account.Close();
        account.Status.Should().Be(AccountStatus.Closed);
        account.ClosedAt.Should().NotBeNull();
    }
}
