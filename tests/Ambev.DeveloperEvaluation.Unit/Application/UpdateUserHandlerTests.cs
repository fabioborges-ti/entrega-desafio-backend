using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Application.Users.UpdateUser;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class UpdateUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly UpdateUserHandler _handler;

    public UpdateUserHandlerTests()
    {
        _handler = new UpdateUserHandler(_userRepository, _mapper, _passwordHasher);
    }

    [Fact(DisplayName = "UpdateUser should throw when user not found")]
    public async Task Handle_UserNotTracked_ThrowsKeyNotFound()
    {
        _userRepository.GetTrackedByIdAsync(50, Arg.Any<CancellationToken>()).Returns((User?)null);

        var cmd = new UpdateUserCommand
        {
            Id = 50,
            Username = "validuser",
            Email = "e@t.com",
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

        var act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "UpdateUser should reject invalid command")]
    public async Task Handle_InvalidCommand_ThrowsValidationException()
    {
        var user = new User { Id = 1, Username = "x", Email = "x@t.com" };
        _userRepository.GetTrackedByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await _handler.Handle(new UpdateUserCommand { Id = 1 }, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
