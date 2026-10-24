namespace BankingSystem.security;

public interface ITotpService
{
    string GenerateSecretKey();
    string GetQrCodeUri(string secretKey, string username, string issuer = "TrustBank");
    string GetQrCodeBase64Png(string qrCodeUri);
    bool ValidateCode(string secretKey, string code);
}