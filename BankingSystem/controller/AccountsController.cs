using BankingSystem.dto;
using BankingSystem.service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.controller;

[Authorize]
public class AccountsController : ApiControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teller")]
    public async Task<ActionResult<ApiResponse<AccountDto>>> OpenAccount([FromBody] CreateAccountRequestDto dto, CancellationToken ct)
    {
        var result = await _accountService.OpenAccountAsync(dto, ct);
        return Ok(ApiResponse<AccountDto>.Ok(result, "Account opened successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _accountService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<AccountDto>.Ok(result));
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountDto>>>> GetByCustomer(Guid customerId, CancellationToken ct)
    {
        var result = await _accountService.GetByCustomerIdAsync(customerId, ct);
        return Ok(ApiResponse<IReadOnlyList<AccountDto>>.Ok(result));
    }

    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<ApiResponse<AccountBalanceDto>>> GetBalance(Guid id, CancellationToken ct)
    {
        var result = await _accountService.GetBalanceAsync(id, ct);
        return Ok(ApiResponse<AccountBalanceDto>.Ok(result));
    }

    [HttpPost("{id:guid}/freeze")]
    [Authorize(Roles = "Admin,Teller")]
    public async Task<ActionResult<ApiResponse<bool>>> FreezeAccount(Guid id, [FromBody] FreezeAccountRequestDto dto, CancellationToken ct)
    {
        await _accountService.FreezeAccountAsync(id, dto.Reason, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Account frozen successfully."));
    }

    [HttpPost("{id:guid}/unfreeze")]
    [Authorize(Roles = "Admin,Teller")]
    public async Task<ActionResult<ApiResponse<bool>>> UnfreezeAccount(Guid id, CancellationToken ct)
    {
        await _accountService.UnfreezeAccountAsync(id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Account unfrozen successfully."));
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "Admin,Teller")]
    public async Task<ActionResult<ApiResponse<bool>>> CloseAccount(Guid id, CancellationToken ct)
    {
        await _accountService.CloseAccountAsync(id, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Account closed successfully."));
    }
}
