using BankingSystem.dto;
using BankingSystem.service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.controller;

[Authorize]
public class CustomersController : ApiControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teller")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> CreateCustomer([FromBody] CreateCustomerRequestDto dto, CancellationToken ct)
    {
        var result = await _customerService.CreateCustomerAsync(dto, ct);
        return Ok(ApiResponse<CustomerDto>.Ok(result, "Customer profile created successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _customerService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<CustomerDto>.Ok(result));
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetByUserId(Guid userId, CancellationToken ct)
    {
        var result = await _customerService.GetByUserIdAsync(userId, ct);
        return Ok(ApiResponse<CustomerDto>.Ok(result));
    }

    [HttpPut("{id:guid}/kyc")]
    [Authorize(Roles = "Admin,Teller")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> UpdateKyc(Guid id, [FromBody] UpdateKycRequestDto dto, CancellationToken ct)
    {
        var result = await _customerService.UpdateKycAsync(id, dto, ct);
        return Ok(ApiResponse<CustomerDto>.Ok(result, "Customer KYC updated successfully."));
    }
}
