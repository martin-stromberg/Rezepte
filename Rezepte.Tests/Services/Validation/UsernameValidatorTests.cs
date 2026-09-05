using FluentAssertions;
using Rezepte.Web.Services.Validation;
using Xunit;

namespace Rezepte.Tests.Services.Validation;

/// <summary>
/// Class representing the username validator tests.
/// </summary>
public class UsernameValidatorTests
{
    private readonly UsernameValidator _sut = new();

    /// <summary>
    /// Validate should accept valid usernames.
    /// </summary>
    /// <param name="username">The username parameter.</param>
    [Theory]
    [InlineData("max_mustermann")]
    [InlineData("anna-2026")]
    [InlineData("kochbuchFan1")]
    public void Validate_ShouldAcceptValidUsernames(string username)
    {
        var result = _sut.Validate(username);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Validate should reject missing or invalid length usernames.
    /// </summary>
    /// <param name="username">The username parameter.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("me")]
    [InlineData("abcdefghijklmnopqrstu")]
    public void Validate_ShouldRejectMissingOrInvalidLengthUsernames(string? username)
    {
        var result = _sut.Validate(username);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(UsernameValidator.LengthMessage);
    }

    /// <summary>
    /// Validate should reject invalid characters.
    /// </summary>
    /// <param name="username">The username parameter.</param>
    [Theory]
    [InlineData("max mustermann")]
    [InlineData("max/mustermann")]
    [InlineData("max@mustermann")]
    [InlineData("max#mustermann")]
    [InlineData("max%mustermann")]
    [InlineData("max😀")]
    public void Validate_ShouldRejectInvalidCharacters(string username)
    {
        var result = _sut.Validate(username);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(UsernameValidator.CharactersMessage);
    }

    /// <summary>
    /// Validate should reject reserved usernames.
    /// </summary>
    /// <param name="username">The username parameter.</param>
    [Theory]
    [InlineData("admin")]
    [InlineData("Admin")]
    [InlineData("ADMIN")]
    [InlineData("root")]
    [InlineData("support")]
    [InlineData("guest")]
    [InlineData("test")]
    [InlineData("null")]
    [InlineData("administrator")]
    [InlineData("moderator")]
    [InlineData("superuser")]
    [InlineData("owner")]
    [InlineData("help")]
    [InlineData("contact")]
    [InlineData("info")]
    [InlineData("about")]
    [InlineData("login")]
    [InlineData("signup")]
    [InlineData("you")]
    [InlineData("self")]
    [InlineData("someone")]
    [InlineData("anyone")]
    [InlineData("rezepte")]
    [InlineData("rezepteapp")]
    [InlineData("rezepte-admin")]
    [InlineData("rezepte_support")]
    [InlineData("webmaster")]
    public void Validate_ShouldRejectReservedUsernames(string username)
    {
        var result = _sut.Validate(username);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(UsernameValidator.ReservedMessage);
    }

    /// <summary>
    /// Validate should reject ip addresses and domains.
    /// </summary>
    /// <param name="username">The username parameter.</param>
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("example.com")]
    [InlineData("rezepte.local")]
    public void Validate_ShouldRejectIpAddressesAndDomains(string username)
    {
        var result = _sut.Validate(username);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(UsernameValidator.IpOrDomainMessage);
    }

    /// <summary>
    /// Validate should reject official looking usernames.
    /// </summary>
    /// <param name="username">The username parameter.</param>
    [Theory]
    [InlineData("support_team")]
    [InlineData("security_admin")]
    [InlineData("admin-support")]
    [InlineData("securityadmin")]
    [InlineData("supportadmin")]
    [InlineData("moderatoradmin")]
    [InlineData("helpdeskadmin")]
    [InlineData("microsoftsupport")]
    public void Validate_ShouldRejectOfficialLookingUsernames(string username)
    {
        var result = _sut.Validate(username);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(UsernameValidator.GenericBlockedMessage);
    }

    /// <summary>
    /// Validate should reject leetspeak variants of high risk names.
    /// </summary>
    /// <param name="username">The username parameter.</param>
    [Theory]
    [InlineData("adm1n")]
    [InlineData("r00t")]
    [InlineData("supp0rt")]
    public void Validate_ShouldRejectLeetspeakVariantsOfHighRiskNames(string username)
    {
        var result = _sut.Validate(username);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(UsernameValidator.GenericBlockedMessage);
    }
}
