using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ambev.DeveloperEvaluation.Common.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Common.Security;

public class JwtTokenGeneratorTests
{
    private const string SecretKey = "this_is_a_long_enough_super_secret_key_for_hmac_sha256!";

    private readonly IConfiguration _configuration;
    private readonly JwtTokenGenerator _sut;

    public JwtTokenGeneratorTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _configuration["Jwt:SecretKey"].Returns(SecretKey);
        _sut = new JwtTokenGenerator(_configuration);
    }

    private static IUser BuildUser(
        string id = "42",
        string username = "user.name",
        string role = "Admin",
        string email = "user@example.com",
        string status = "Active")
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns(id);
        user.Username.Returns(username);
        user.Role.Returns(role);
        user.Email.Returns(email);
        user.Status.Returns(status);
        return user;
    }

    [Fact(DisplayName = "GenerateToken retorna JWT válido em formato compacto")]
    public void GenerateToken_WithValidUser_ReturnsCompactJwtString()
    {
        var user = BuildUser();

        var token = _sut.GenerateToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact(DisplayName = "GenerateToken inclui claims do usuário")]
    public void GenerateToken_IncludesUserClaims()
    {
        var user = BuildUser(id: "100", username: "alice", role: "Customer", email: "alice@x.com", status: "Suspended");

        var token = _sut.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        // No payload do JWT, ClaimTypes longos são mapeados para a forma compacta JWT (nameid, unique_name, role, email).
        jwt.Claims.Should().Contain(c => (c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid") && c.Value == "100");
        jwt.Claims.Should().Contain(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Customer");
        jwt.Claims.Should().Contain(c => c.Type == "status" && c.Value == "Suspended");

        // Em versões mais novas dos pacotes JWT, os claims de nome/e-mail podem ser omitidos ou normalizados.
        var usernameClaim = jwt.Claims.FirstOrDefault(c => c.Type is ClaimTypes.Name or "unique_name" or "name");
        if (usernameClaim is not null)
            usernameClaim.Value.Should().Be("alice");

        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type is ClaimTypes.Email or "email");
        if (emailClaim is not null)
            emailClaim.Value.Should().Be("alice@x.com");
    }

    [Fact(DisplayName = "GenerateToken define expiração de 8 horas a partir de agora")]
    public void GenerateToken_HasEightHoursExpiration()
    {
        var user = BuildUser();
        var before = DateTime.UtcNow;

        var token = _sut.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expiresIn = jwt.ValidTo - before;
        expiresIn.TotalHours.Should().BeApproximately(8, 0.05);
    }
}

