using AutoMapper;
using BankingSystem.dto;
using BankingSystem.repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.controller;

[Authorize(Roles = "Admin")]
public class UsersController : ApiControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserDto>>>> GetAll(CancellationToken ct)
    {
        var users = await _userRepository.GetAllAsync(ct);
        var dtos = _mapper.Map<IReadOnlyList<UserDto>>(users);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user == null)
            return NotFound(ApiResponse<UserDto>.Fail("User not found."));

        return Ok(ApiResponse<UserDto>.Ok(_mapper.Map<UserDto>(user)));
    }
}
