using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class GetUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetUserHandler _handler;

    public GetUserHandlerTests()
    {
        _handler = new GetUserHandler(_userRepository, _mapper);
    }

    [Fact(DisplayName = "GetUser should return mapped user when found")]
    public async Task Handle_ExistingId_ReturnsResult()
    {
        var user = new User
        {
            Id = 3,
            Username = "a",
            Email = "a@t.com",
            Phone = "+5511987654321",
            Password = "p",
            Status = UserStatus.Active,
            Role = UserRole.Admin,
            Name = new UserPersonName { FirstName = "X", LastName = "Y" },
            Address = new UserAddress
            {
                City = "C",
                Street = "S",
                Number = 1,
                Zipcode = "00000-000",
                Geolocation = new AddressGeolocation { Lat = "0", Long = "0" }
            }
        };

        _userRepository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(user);
        var dto = new GetUserResult { Id = 3, Username = "a", Email = "a@t.com" };
        _mapper.Map<GetUserResult>(user).Returns(dto);

        var result = await _handler.Handle(new GetUserCommand(3), CancellationToken.None);

        result.Should().BeSameAs(dto);
    }

    [Fact(DisplayName = "GetUser should throw when user missing")]
    public async Task Handle_MissingId_ThrowsKeyNotFound()
    {
        _userRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await _handler.Handle(new GetUserCommand(99), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "GetUser should reject invalid id")]
    public async Task Handle_InvalidId_ThrowsValidationException()
    {
        var act = async () => await _handler.Handle(new GetUserCommand(0), CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
