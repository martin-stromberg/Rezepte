using System.Security.Cryptography;
using FluentAssertions;
using Rezepte.Web.Security;
using Xunit;

namespace Rezepte.Tests.Security;

public class PasswordHasherTests
{
    private static string CraftHash(string password, int iterations, int saltLength = PasswordHasher.SaltLengthBytes, int hashLength = PasswordHasher.HashLengthBytes)
    {
        var salt = new byte[saltLength];
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashLength);
        return $"{iterations}.{Convert.ToHexString(salt)}.{Convert.ToHexString(hash)}";
    }

    [Fact]
    public void Hash_ShouldUseCurrentIterations_ByDefault()
    {
        var hashString = PasswordHasher.Hash("secret!");
        var parts = hashString.Split('.');

        parts.Should().HaveCount(3);
        parts[0].Should().Be(PasswordHasher.CurrentIterations.ToString());
        Convert.FromHexString(parts[1]).Should().HaveCount(PasswordHasher.SaltLengthBytes);
        Convert.FromHexString(parts[2]).Should().HaveCount(PasswordHasher.HashLengthBytes);
    }

    [Theory]
    [InlineData(PasswordHasher.MinIterations - 1)]
    [InlineData(PasswordHasher.MaxIterations + 1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Hash_ShouldThrow_WhenIterationsOutsidePolicy(int iterations)
    {
        var act = () => PasswordHasher.Hash("secret!", iterations);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Verify_ShouldReturnSuccess_WhenHashCurrent()
    {
        var hashString = PasswordHasher.Hash("secret!");

        PasswordHasher.Verify("secret!", hashString).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void Verify_ShouldReturnFailed_WhenPasswordWrong()
    {
        var hashString = PasswordHasher.Hash("secret!");

        PasswordHasher.Verify("wrong", hashString).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Verify_ShouldReturnSuccessRehashNeeded_WhenIterationsBelowCurrent()
    {
        var hashString = PasswordHasher.Hash("secret!", PasswordHasher.MinIterations);

        PasswordHasher.Verify("secret!", hashString).Should().Be(PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Fact]
    public void Verify_ShouldReturnFailed_WhenIterationsBelowMin()
    {
        var hashString = CraftHash("secret!", PasswordHasher.MinIterations - 1);

        PasswordHasher.Verify("secret!", hashString).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Verify_ShouldReturnFailed_WhenIterationsAboveMax()
    {
        // Iterations beyond MaxIterations are rejected before any PBKDF2 work is done.
        var hashString = $"{PasswordHasher.MaxIterations + 1}.{Convert.ToHexString(new byte[PasswordHasher.SaltLengthBytes])}.{Convert.ToHexString(new byte[PasswordHasher.HashLengthBytes])}";

        PasswordHasher.Verify("secret!", hashString).Should().Be(PasswordVerificationResult.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("abc.def")]
    [InlineData("abc.def.ghi.jkl")]
    [InlineData("abc." + "00112233445566778899AABBCCDDEEFF" + "." + "00112233445566778899AABBCCDDEEFF")]
    public void Verify_ShouldReturnFailed_WhenHashStringMalformed(string hashString)
    {
        PasswordHasher.Verify("secret!", hashString).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Verify_ShouldReturnFailed_WhenHexInvalid()
    {
        var hashString = $"{PasswordHasher.MinIterations}.ZZZZ.{'0' + new string('0', 63)}";

        PasswordHasher.Verify("secret!", hashString).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Verify_ShouldReturnFailed_WhenSaltLengthWrong()
    {
        var hashString = CraftHash("secret!", PasswordHasher.MinIterations, saltLength: 8);

        PasswordHasher.Verify("secret!", hashString).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Verify_ShouldReturnFailed_WhenHashLengthWrong()
    {
        var hashString = CraftHash("secret!", PasswordHasher.MinIterations, hashLength: 16);

        PasswordHasher.Verify("secret!", hashString).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Verify_ShouldThrow_WhenArgumentsNull()
    {
        var actNullPassword = () => PasswordHasher.Verify(null!, PasswordHasher.Hash("x"));
        var actNullHash = () => PasswordHasher.Verify("x", null!);

        actNullPassword.Should().Throw<ArgumentNullException>();
        actNullHash.Should().Throw<ArgumentNullException>();
    }
}
