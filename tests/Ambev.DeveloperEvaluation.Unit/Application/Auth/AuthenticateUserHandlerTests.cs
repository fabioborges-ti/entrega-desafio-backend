using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Auth;

public class AuthenticateUserHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();
    private readonly AuthenticateUserHandler _sut;

    public AuthenticateUserHandlerTests()
    {
        _sut = new AuthenticateUserHandler(_users, _hasher, _jwt, NullLogger<AuthenticateUserHandler>.Instance);
    }

    private static User ActiveUser(string firstName = "Ana", string lastName = "Silva", string username = "ana") => new()
    {
        Id = 10,
        Username = username,
        Email = "ana@example.com",
        Password = "hash",
        Name = new UserPersonName { FirstName = firstName, LastName = lastName },
        Role = UserRole.Customer,
        Status = UserStatus.Active
    };

    [Fact(DisplayName = "Usuário inexistente lança UnauthorizedAccessException")]
    public async Task Handle_UserNotFound_ThrowsUnauthorized()
    {
        _users.GetByUsernameAsync("ana", Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await _sut.Handle(new AuthenticateUserCommand { Username = "ana", Password = "1234" }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Invalid credentials*");
    }

    [Fact(DisplayName = "Senha inválida lança UnauthorizedAccessException")]
    public async Task Handle_InvalidPassword_ThrowsUnauthorized()
    {
        _users.GetByUsernameAsync("ana", Arg.Any<CancellationToken>()).Returns(ActiveUser());
        _hasher.VerifyPassword("wrong", "hash").Returns(false);

        var act = async () => await _sut.Handle(new AuthenticateUserCommand { Username = "ana", Password = "wrong" }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Invalid credentials*");
    }

    [Fact(DisplayName = "Usuário inativo lança UnauthorizedAccessException por especificação")]
    public async Task Handle_InactiveUser_ThrowsUnauthorized()
    {
        var user = ActiveUser();
        user.Status = UserStatus.Suspended;
        _users.GetByUsernameAsync("ana", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("ok", "hash").Returns(true);

        var act = async () => await _sut.Handle(new AuthenticateUserCommand { Username = "ana", Password = "ok" }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not active*");
    }

    [Fact(DisplayName = "Sucesso retorna token e DisplayName a partir do nome composto")]
    public async Task Handle_Success_ReturnsResultWithDisplayName()
    {
        var user = ActiveUser();
        _users.GetByUsernameAsync("ana", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("ok", "hash").Returns(true);
        _jwt.GenerateToken(user).Returns("jwt-token");

        var result = await _sut.Handle(new AuthenticateUserCommand { Username = "ana", Password = "ok" }, CancellationToken.None);

        result.Token.Should().Be("jwt-token");
        result.Id.Should().Be(10);
        result.Name.Should().Be("Ana Silva");
        result.Email.Should().Be("ana@example.com");
        result.Role.Should().Be("Customer");
    }

    [Fact(DisplayName = "Sucesso usa username quando nome composto está vazio")]
    public async Task Handle_Success_FallsBackToUsername()
    {
        var user = ActiveUser(firstName: string.Empty, lastName: string.Empty, username: "ananym");
        _users.GetByUsernameAsync("ananym", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.VerifyPassword("ok", "hash").Returns(true);
        _jwt.GenerateToken(user).Returns("jwt");

        var result = await _sut.Handle(new AuthenticateUserCommand { Username = "ananym", Password = "ok" }, CancellationToken.None);

        result.Name.Should().Be("ananym");
    }
}

public class AuthenticateUserValidatorTests
{
    [Theory(DisplayName = "AuthenticateUserValidator regras")]
    [InlineData("ana", "1234", true)]
    [InlineData("", "1234", false)]
    [InlineData("ab", "1234", false)]
    [InlineData("ana", "", false)]
    public void AuthenticateUserValidator_Validates(string user, string pass, bool expected)
    {
        var v = new AuthenticateUserValidator();
        v.Validate(new AuthenticateUserCommand { Username = user, Password = pass }).IsValid.Should().Be(expected);
    }

    [Fact(DisplayName = "AuthenticateUserValidator: Username acima de 50 caracteres é inválido")]
    public void AuthenticateUserValidator_TooLongUsername_Invalid()
    {
        var v = new AuthenticateUserValidator();
        v.Validate(new AuthenticateUserCommand { Username = new string('a', 51), Password = "ok" }).IsValid.Should().BeFalse();
    }
}

