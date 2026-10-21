using BankingSystem.entity;
using BankingSystem.security;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.config;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher, ILogger logger)
    {
        try
        {
            if (await context.Users.IgnoreQueryFilters().AnyAsync())
            {
                logger.LogInformation("Database already seeded. Skipping initial seeding.");
                return;
            }

            logger.LogInformation("Seeding initial banking system data...");

            // Admin
            var adminUser = User.Create(
                PhoneNumber.Create("+85512345678"),
                passwordHasher.HashPassword("Admin@123!"),
                "System Administrator",
                UserRole.Admin,
                "admin@bankingsystem.com");
            adminUser.ConfirmPhoneNumber();
            await context.Users.AddAsync(adminUser);

            // Teller
            var tellerUser = User.Create(
                PhoneNumber.Create("+85512345679"),
                passwordHasher.HashPassword("Teller@123!"),
                "Branch Head Teller",
                UserRole.Teller,
                "teller@bankingsystem.com");
            tellerUser.ConfirmPhoneNumber();
            await context.Users.AddAsync(tellerUser);

            // Customer 1
            var customerUser = User.Create(
                PhoneNumber.Create("+85512345680"),
                passwordHasher.HashPassword("Customer@123!"),
                "Sophea Chan",
                UserRole.Customer,
                "sophea.chan@example.com");
            customerUser.ConfirmPhoneNumber();
            await context.Users.AddAsync(customerUser);

            var customer = Customer.Create(
                customerUser.Id,
                "CUST-2026-0001",
                "Sophea",
                "Chan",
                new DateTime(1995, 5, 15),
                "KH-019283746",
                "Preah Norodom Blvd, Daun Penh, Phnom Penh, Cambodia");
            customer.UpdateKyc(KycStatus.Verified, 2);
            await context.Customers.AddAsync(customer);

            var savingsAccount = Account.Open(
                customer.Id,
                "1840100001",
                AccountType.Savings,
                Currency.USD,
                initialDeposit: 5000.00m,
                interestRate: 3.5m);
            await context.Accounts.AddAsync(savingsAccount);

            var checkingAccount = Account.Open(
                customer.Id,
                "2116200001",
                AccountType.Checking,
                Currency.KHR,
                initialDeposit: 20000000.00m,
                interestRate: 0.0m);
            await context.Accounts.AddAsync(checkingAccount);

            // Customer 2 (for transfers)
            var customer2User = User.Create(
                PhoneNumber.Create("+85512345681"),
                passwordHasher.HashPassword("Customer@Password123!"),
                "Vannak Sok",
                UserRole.Customer,
                "vannak.sok@example.com");
            customer2User.ConfirmPhoneNumber();
            await context.Users.AddAsync(customer2User);

            var customer2 = Customer.Create(
                customer2User.Id,
                "CUST-2026-0002",
                "Vannak",
                "Sok",
                new DateTime(1998, 9, 20),
                "KH-098765432",
                "Mao Tse Toung Blvd, Phnom Penh, Cambodia");
            customer2.UpdateKyc(KycStatus.Verified, 1);
            await context.Customers.AddAsync(customer2);

            var customer2Savings = Account.Open(
                customer2.Id,
                "1840100002",
                AccountType.Savings,
                Currency.USD,
                initialDeposit: 1500.00m,
                interestRate: 3.5m);
            await context.Accounts.AddAsync(customer2Savings);

            await context.SaveChangesAsync();
            logger.LogInformation("Database seeded successfully with initial users and bank accounts.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
