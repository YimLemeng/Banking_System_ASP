using System.Text.RegularExpressions;
using BankingSystem.exception;

namespace BankingSystem.entity;

public sealed class PhoneNumber : IEquatable<PhoneNumber>
{
    private static readonly Regex E164Regex = new(@"^\+[1-9]\d{6,14}$", RegexOptions.Compiled);
    private const string DefaultCountryCode = "+855"; // Cambodia

    public string Value { get; }

    public PhoneNumber()
    {
        Value = string.Empty;
    }

    public PhoneNumber(string rawNumber, string defaultCountryCode = DefaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
            throw new InvalidPhoneNumberException("Phone number cannot be empty.");

        Value = Normalize(rawNumber, defaultCountryCode);

        if (!E164Regex.IsMatch(Value))
            throw new InvalidPhoneNumberException($"Phone number '{rawNumber}' is not a valid E.164 format.");
    }

    public static PhoneNumber Create(string rawNumber, string defaultCountryCode = DefaultCountryCode)
        => new(rawNumber, defaultCountryCode);

    public static bool TryCreate(string rawNumber, out PhoneNumber? phoneNumber, string defaultCountryCode = DefaultCountryCode)
    {
        try
        {
            phoneNumber = new PhoneNumber(rawNumber, defaultCountryCode);
            return true;
        }
        catch
        {
            phoneNumber = null;
            return false;
        }
    }

    public static string Normalize(string rawNumber, string defaultCountryCode = DefaultCountryCode)
    {
        var cleaned = Regex.Replace(rawNumber.Trim(), @"[\s\-\(\)\.]", "");

        if (cleaned.StartsWith("00"))
        {
            cleaned = "+" + cleaned[2..];
        }
        else if (cleaned.StartsWith("0"))
        {
            var code = defaultCountryCode.StartsWith("+") ? defaultCountryCode : "+" + defaultCountryCode;
            cleaned = code + cleaned[1..];
        }
        else if (!cleaned.StartsWith("+"))
        {
            var code = defaultCountryCode.StartsWith("+") ? defaultCountryCode : "+" + defaultCountryCode;
            cleaned = code + cleaned;
        }

        return cleaned;
    }

    public string Mask()
    {
        if (Value.Length <= 7) return "***";

        var prefixLength = Value.StartsWith("+855") ? 4 : 3;
        var prefix = Value[..prefixLength];
        var suffix = Value[^4..];
        var maskedStars = new string('*', Math.Max(0, Value.Length - prefixLength - 4));

        return $"{prefix}{maskedStars}{suffix}";
    }

    public override string ToString() => Value;

    public bool Equals(PhoneNumber? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as PhoneNumber);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(PhoneNumber? left, PhoneNumber? right) => Equals(left, right);

    public static bool operator !=(PhoneNumber? left, PhoneNumber? right) => !Equals(left, right);
}
