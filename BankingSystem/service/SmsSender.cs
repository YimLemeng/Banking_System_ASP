using Microsoft.Extensions.Logging;

namespace BankingSystem.service;

public class SmsSender : ISmsSender
{
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(ILogger<SmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var masked = phoneNumber.Length > 7 ? phoneNumber[..4] + "****" + phoneNumber[^3..] : phoneNumber;
        _logger.LogInformation("================================================");
        _logger.LogInformation("[SMS DISPATCH] To: {MaskedPhone} | Content: {Message}", masked, message);
        _logger.LogInformation("================================================");
        return Task.CompletedTask;
    }
}
