using Ambev.DeveloperEvaluation.Common.Security;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact(DisplayName = "Hash gerado é diferente da senha original")]
    public void HashPassword_WhenCalled_ReturnsHashDifferentFromOriginal()
    {
        const string password = "P@ssw0rd!";

        var hash = _sut.HashPassword(password);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Should().StartWith("$2");
    }

    [Fact(DisplayName = "Hashes diferentes são gerados para a mesma senha (salt aleatório)")]
    public void HashPassword_CalledTwice_ProducesDifferentHashes()
    {
        const string password = "Same@Password1";

        var hash1 = _sut.HashPassword(password);
        var hash2 = _sut.HashPassword(password);

        hash1.Should().NotBe(hash2);
    }

    [Fact(DisplayName = "VerifyPassword retorna true para senha correta")]
    public void VerifyPassword_WithMatchingPassword_ReturnsTrue()
    {
        const string password = "Match@Password1";
        var hash = _sut.HashPassword(password);

        var result = _sut.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact(DisplayName = "VerifyPassword retorna false para senha incorreta")]
    public void VerifyPassword_WithDifferentPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("Original@123");

        var result = _sut.VerifyPassword("Different@123", hash);

        result.Should().BeFalse();
    }
}

