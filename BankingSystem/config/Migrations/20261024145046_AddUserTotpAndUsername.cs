using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingSystem.config.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTotpAndUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('dbo.OtpCodes', 'U') IS NOT NULL DROP TABLE [dbo].[OtpCodes];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_PhoneNumber' AND object_id = OBJECT_ID('dbo.Users')) DROP INDEX [IX_Users_PhoneNumber] ON [dbo].[Users];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_UserId' AND object_id = OBJECT_ID('dbo.Customers')) DROP INDEX [IX_Customers_UserId] ON [dbo].[Customers];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'PhoneNumber' AND object_id = OBJECT_ID('dbo.Users')) ALTER TABLE [dbo].[Users] DROP COLUMN [PhoneNumber];");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'PhoneNumberConfirmed' AND object_id = OBJECT_ID('dbo.Users'))
                BEGIN
                    EXEC sp_rename 'dbo.Users.PhoneNumberConfirmed', 'TotpEnabled', 'COLUMN';
                END
                ELSE IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'TotpEnabled' AND object_id = OBJECT_ID('dbo.Users'))
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [TotpEnabled] bit NOT NULL DEFAULT 0;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'TotpSecretKey' AND object_id = OBJECT_ID('dbo.Users'))
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [TotpSecretKey] nvarchar(256) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'Username' AND object_id = OBJECT_ID('dbo.Users'))
                BEGIN
                    ALTER TABLE [dbo].[Users] ADD [Username] nvarchar(50) NOT NULL DEFAULT '';
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE name = 'UserId' AND object_id = OBJECT_ID('dbo.Customers'))
                BEGIN
                    ALTER TABLE [dbo].[Customers] ALTER COLUMN [UserId] uniqueidentifier NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name = 'PhoneNumber' AND object_id = OBJECT_ID('dbo.Customers'))
                BEGIN
                    ALTER TABLE [dbo].[Customers] ADD [PhoneNumber] nvarchar(20) NOT NULL DEFAULT '';
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_PhoneNumber' AND object_id = OBJECT_ID('dbo.Customers'))
                BEGIN
                    CREATE INDEX [IX_Customers_PhoneNumber] ON [dbo].[Customers] ([PhoneNumber]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_UserId' AND object_id = OBJECT_ID('dbo.Customers'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_Customers_UserId] ON [dbo].[Customers] ([UserId]) WHERE [UserId] IS NOT NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_UserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TotpSecretKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "TotpEnabled",
                table: "Users",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "InterestRate",
                table: "Accounts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.CreateTable(
                name: "OtpCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId",
                table: "Customers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_PhoneNumber_Purpose_ExpiresAt",
                table: "OtpCodes",
                columns: new[] { "PhoneNumber", "Purpose", "ExpiresAt" });
        }
    }
}
