using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.controller;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
}
