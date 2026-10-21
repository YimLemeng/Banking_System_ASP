using AutoMapper;
using BankingSystem.dto;
using BankingSystem.entity;
using BankingSystem.repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.controller;

[Authorize(Roles = "Admin")]
public class AuditLogsController : ApiControllerBase
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMapper _mapper;

    public AuditLogsController(IAuditLogRepository auditLogRepository, IMapper mapper)
    {
        _auditLogRepository = auditLogRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetLogs(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken ct = default)
    {
        var items = await _auditLogRepository.GetPagedLogsAsync(pageIndex, pageSize, userId, action, fromDate, toDate, ct);
        var total = await _auditLogRepository.GetLogsCountAsync(userId, action, fromDate, toDate, ct);
        var dtos = _mapper.Map<IReadOnlyList<AuditLogDto>>(items);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>(dtos, total, pageIndex, pageSize)));
    }
}
