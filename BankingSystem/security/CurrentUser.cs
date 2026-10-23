using System.Security.Claims;

namespace BankingSystem.security;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var id = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? _accessor.HttpContext?.User?.FindFirstValue("sub");
            return Guid.TryParse(id, out var g) ? g : null;
        }
    }

    public string? PhoneNumber => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.MobilePhone)
                                 ?? _accessor.HttpContext?.User?.FindFirstValue("phone")
                                 ?? _accessor.HttpContext?.User?.FindFirstValue("mobilephone");

    public string? Role => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role)
                           ?? _accessor.HttpContext?.User?.FindFirstValue("role");

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    public string? IpAddress => _accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
    public string? UserAgent => _accessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
}
