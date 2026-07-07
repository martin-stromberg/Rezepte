using System.Text.RegularExpressions;

namespace Rezepte.Web.Services.Validation;

public sealed partial class UsernameValidator : IUsernameValidator
{
    public const string LengthMessage = "The username must be between 3 and 20 characters long.";
    public const string CharactersMessage = "The username may only contain letters, numbers, underscores, and hyphens.";
    public const string ReservedMessage = "The username is reserved.";
    public const string GenericBlockedMessage = "This username cannot be used. Please choose another name.";
    public const string IpOrDomainMessage = "The username must not be an IP address or domain.";

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "administrator",
        "root",
        "system",
        "support",
        "guest",
        "test",
        "null",
        "moderator",
        "superuser",
        "owner",
        "help",
        "contact",
        "info",
        "about",
        "login",
        "signup",
        "me",
        "you",
        "self",
        "someone",
        "anyone",
        "webmaster",
        "security",
        "rezepte",
        "rezepteapp",
        "rezepte-admin",
        "rezepte_support"
    };

    private static readonly HashSet<string> AbuseTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "abuse",
        "spam",
        "scam",
        "phishing"
    };

    private static readonly HashSet<string> HighRiskNormalizedNames = new(StringComparer.Ordinal)
    {
        "admin",
        "root",
        "support"
    };

    public UsernameValidationResult Validate(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return UsernameValidationResult.Invalid(LengthMessage);

        var trimmed = username.Trim();

        if (IpAddressRegex().IsMatch(trimmed) || DomainRegex().IsMatch(trimmed))
            return UsernameValidationResult.Invalid(IpOrDomainMessage);

        if (trimmed.Length is < 3 or > 20)
            return UsernameValidationResult.Invalid(LengthMessage);

        if (!AllowedCharactersRegex().IsMatch(trimmed))
            return UsernameValidationResult.Invalid(CharactersMessage);

        if (ReservedNames.Contains(trimmed))
            return UsernameValidationResult.Invalid(ReservedMessage);

        if (LooksOfficial(trimmed) || ContainsAbuseTerm(trimmed) || LooksLikeHighRiskName(trimmed))
            return UsernameValidationResult.Invalid(GenericBlockedMessage);

        return UsernameValidationResult.Valid;
    }

    private static bool LooksOfficial(string username)
    {
        var normalized = username.ToLowerInvariant();
        var collapsed = SeparatorRegex().Replace(normalized, string.Empty);
        var tokens = SeparatorRegex().Split(normalized).Where(token => token.Length > 0).ToArray();

        if (collapsed is "microsoftsupport")
            return true;

        if (IsOfficialCombination(collapsed))
            return true;

        if (tokens.Length >= 2)
        {
            var hasOfficialToken = tokens.Any(IsOfficialToken);
            var hasQualifier = tokens.Any(IsOfficialQualifier);
            if (hasOfficialToken && hasQualifier)
                return true;
        }

        return normalized.EndsWith("support", StringComparison.Ordinal)
            && normalized.Length > "support".Length;
    }

    private static bool ContainsAbuseTerm(string username)
    {
        return AbuseTerms.Any(term => username.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeHighRiskName(string username)
    {
        var normalized = NormalizeLeetspeak(username);
        return HighRiskNormalizedNames.Contains(normalized) && !username.Equals(normalized, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLeetspeak(string username)
    {
        return username.ToLowerInvariant()
            .Replace('0', 'o')
            .Replace('1', 'i')
            .Replace('3', 'e')
            .Replace('4', 'a')
            .Replace('5', 's')
            .Replace('7', 't');
    }

    private static bool IsOfficialToken(string token)
    {
        return token is "admin" or "support" or "security" or "moderator";
    }

    private static bool IsOfficialQualifier(string token)
    {
        return token is "team" or "admin" or "support" or "security" or "helpdesk" or "moderator";
    }

    private static bool IsOfficialCombination(string value)
    {
        for (var splitIndex = 1; splitIndex < value.Length; splitIndex++)
        {
            var left = value[..splitIndex];
            var right = value[splitIndex..];

            if ((IsOfficialToken(left) && IsOfficialQualifier(right))
                || (IsOfficialQualifier(left) && IsOfficialToken(right)))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"^\d{1,3}(\.\d{1,3}){3}$", RegexOptions.CultureInvariant)]
    private static partial Regex IpAddressRegex();

    [GeneratedRegex(@"^[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedCharactersRegex();

    [GeneratedRegex(@"[_-]+", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorRegex();
}
