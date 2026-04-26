using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Application.Users.UpdateUser;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Users;

/// <summary>
/// Cobertura adicional para <see cref="UpdateUserHandler"/>: e-mail/username
/// duplicados, troca de senha, validação de entidade e fluxo bem sucedido.
/// </summary>
public class UpdateUserHandlerExtraTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateUserHandler _handler;

    public UpdateUserHandlerExtraTests()
    {
        _handler = new UpdateUserHandler(_users, _mapper, _hasher);
    }

    private static UpdateUserCommand ValidCommand(int id = 10) => new()
    {
        Id = id,
        Username = "validuser",
        Email = "user@host.com",
        Phone = "+5511987654321",
        Status = UserStatus.Active,
        Role = UserRole.Customer,
        Password = "Test@123",
        Name = new UserPersonName { FirstName = "A", LastName = "B" },
        Address = new UserAddress
        {
            City = "C",
            Street = "S",
            Number = 1,
            Zipcode = "01000-000",
            Geolocation = new AddressGeolocation { Lat = "0", Long = "0" }
        }
    };

    private static User TrackedUser(int id = 10) => new()
    {
        Id = id,
        Username = "olduser",
        Email = "old@host.com",
        Phone = "+5511000000000",
        Password = "Old@Hash1",
        Status = UserStatus.Active,
        Role = UserRole.Customer,
        Name = new UserPersonName { FirstName = "Old", LastName = "User" },
        Address = new UserAddress
        {
            City = "C",
            Street = "S",
            Number = 1,
            Zipcode = "01000-000",
            Geolocation = new AddressGeolocation { Lat = "0", Long = "0" }
        }
    };

    [Fact(DisplayName = "UpdateUser: e-mail já em uso por outro usuário lança InvalidOperation")]
    public async Task EmailTakenByOther_Throws()
    {
        var user = TrackedUser();
        _users.GetTrackedByIdAsync(10, Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByEmailAsync("user@host.com", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 999 });

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*user@host.com*");
    }

    [Fact(DisplayName = "UpdateUser: username já em uso por outro usuário lança InvalidOperation")]
    public async Task UsernameTakenByOther_Throws()
    {
        var user = TrackedUser();
        _users.GetTrackedByIdAsync(10, Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.GetByUsernameAsync("validuser", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 999 });

        var act = async () => await _handler.Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*validuser*");
    }

    [Fact(DisplayName = "UpdateUser: e-mail/username pertencendo ao próprio usuário não bloqueia")]
    public async Task EmailAndUsernameOwnedBySameUser_DoNotBlock()
    {
        var user = TrackedUser();
        _users.GetTrackedByIdAsync(10, Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.HashPassword("Test@123").Returns("Hashed@Pwd1");

        var dto = new GetUserResult { Id = user.Id };
        _mapper.Map<GetUserResult>(user).Returns(dto);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.Should().BeSameAs(dto);
        user.Username.Should().Be("validuser");
        user.Email.Should().Be("user@host.com");
        user.Password.Should().Be("Hashed@Pwd1");
        user.UpdatedAt.Should().NotBeNull();
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "UpdateUser: senha em branco mantém senha atual (não chama hasher)")]
    public async Task BlankPassword_DoesNotChangePassword()
    {
        var user = TrackedUser();
        _users.GetTrackedByIdAsync(10, Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _mapper.Map<GetUserResult>(user).Returns(new GetUserResult { Id = user.Id });

        var cmd = ValidCommand();
        cmd.Password = "   ";

        await _handler.Handle(cmd, CancellationToken.None);

        user.Password.Should().Be("Old@Hash1");
        _hasher.DidNotReceiveWithAnyArgs().HashPassword(default!);
    }

    [Fact(DisplayName = "UpdateUser: validação de entidade falha (senha persistida fraca) lança ValidationException")]
    public async Task EntityValidationFails_ThrowsValidationException()
    {
        // Cenário: usuário existente possui senha já fraca em "Password" (não passa no
        // PasswordValidator do User). Como o command não envia Password (Password = null),
        // a senha atual é mantida e o user.Validate() falha.
        var user = TrackedUser();
        user.Password = "weak"; // não atende às regras do PasswordValidator
        _users.GetTrackedByIdAsync(10, Arg.Any<CancellationToken>()).Returns(user);
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var cmd = ValidCommand();
        cmd.Password = null;

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "UpdateUserCommandValidator: regras com Status/Role inválidos")]
    [InlineData(UserStatus.Unknown, UserRole.Customer, false)]
    [InlineData(UserStatus.Active, UserRole.None, false)]
    [InlineData(UserStatus.Active, UserRole.Customer, true)]
    public void UpdateUserCommandValidator_StatusRole(UserStatus status, UserRole role, bool expected)
    {
        var v = new UpdateUserCommandValidator();
        var cmd = ValidCommand();
        cmd.Status = status;
        cmd.Role = role;
        v.Validate(cmd).IsValid.Should().Be(expected);
    }

    [Fact(DisplayName = "UpdateUserCommandValidator: senha fraca dispara erro quando informada")]
    public void UpdateUserCommandValidator_WeakPassword_Invalid()
    {
        var v = new UpdateUserCommandValidator();
        var cmd = ValidCommand();
        cmd.Password = "abc";
        v.Validate(cmd).IsValid.Should().BeFalse();
    }
}

public class CreateUserCommandValidateTests
{
    [Fact(DisplayName = "CreateUserCommand.Validate retorna sucesso para comando válido")]
    public void Validate_Valid_ReturnsTrue()
    {
        var cmd = CreateValidCommand();
        var result = cmd.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact(DisplayName = "CreateUserCommand.Validate captura erros e converte para ValidationErrorDetail")]
    public void Validate_Invalid_ReturnsErrors()
    {
        var cmd = new CreateUserCommand();
        var result = cmd.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    private static CreateUserCommand CreateValidCommand() => new()
    {
        Username = "validuser",
        Email = "user@host.com",
        Password = "Test@123",
        Phone = "+5511987654321",
        Status = UserStatus.Active,
        Role = UserRole.Customer,
        Name = new UserPersonName { FirstName = "A", LastName = "B" },
        Address = new UserAddress
        {
            City = "C",
            Street = "S",
            Number = 1,
            Zipcode = "01000-000",
            Geolocation = new AddressGeolocation { Lat = "0", Long = "0" }
        }
    };
}

