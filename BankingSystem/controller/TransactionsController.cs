using BankingSystem.dto;
using BankingSystem.entity;
using BankingSystem.service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.controller;

[Authorize]
public class TransactionsController : ApiControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Deposit(
        [FromBody] DepositRequestDto dto,
        [FromHeader(Name = "X-Idempotency-Key")] string? headerKey,
        CancellationToken ct)
    {
        var key = !string.IsNullOrWhiteSpace(dto.IdempotencyKey) ? dto.IdempotencyKey : headerKey;
        var result = await _transactionService.DepositAsync(dto with { IdempotencyKey = key }, ct);
        return Ok(ApiResponse<TransactionDto>.Ok(result, "Deposit processed successfully."));
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Withdraw(
        [FromBody] WithdrawRequestDto dto,
        [FromHeader(Name = "X-Idempotency-Key")] string? headerKey,
        CancellationToken ct)
    {
        var key = !string.IsNullOrWhiteSpace(dto.IdempotencyKey) ? dto.IdempotencyKey : headerKey;
        var result = await _transactionService.WithdrawAsync(dto with { IdempotencyKey = key }, ct);
        return Ok(ApiResponse<TransactionDto>.Ok(result, "Withdrawal processed successfully."));
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Transfer(
        [FromBody] TransferRequestDto dto,
        [FromHeader(Name = "X-Idempotency-Key")] string? headerKey,
        CancellationToken ct)
    {
        var key = !string.IsNullOrWhiteSpace(dto.IdempotencyKey) ? dto.IdempotencyKey : headerKey;
        var result = await _transactionService.TransferAsync(dto with { IdempotencyKey = key }, ct);
        return Ok(ApiResponse<TransactionDto>.Ok(result, "Transfer completed successfully."));
    }

    [HttpGet("account/{accountId:guid}")]
    public async Task<ActionResult<ApiResponse<PagedResult<TransactionDto>>>> GetHistory(
        Guid accountId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] TransactionType? type = null,
        CancellationToken ct = default)
    {
        var result = await _transactionService.GetHistoryAsync(accountId, pageIndex, pageSize, fromDate, toDate, type, ct);
        return Ok(ApiResponse<PagedResult<TransactionDto>>.Ok(result));
    }

    [HttpGet("account/{accountId:guid}/statement")]
    public async Task<ActionResult<ApiResponse<AccountStatementDto>>> GetStatement(
        Guid accountId,
        [FromQuery] DateTimeOffset fromDate,
        [FromQuery] DateTimeOffset toDate,
        CancellationToken ct)
    {
        var result = await _transactionService.GetStatementAsync(accountId, fromDate, toDate, ct);
        return Ok(ApiResponse<AccountStatementDto>.Ok(result));
    }
}
