using Ambev.DeveloperEvaluation.Application.Users.DeleteUser;
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

public class DeleteUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly DeleteUserHandler _handler;

    public DeleteUserHandlerTests()
    {
        _handler = new DeleteUserHandler(_userRepository, _mapper);
    }

    [Fact(DisplayName = "DeleteUser should return snapshot and delete")]
    public async Task Handle_ExistingUser_ReturnsSnapshotAndDeletes()
    {
        var user = new User
        {
            Id = 8,
            Username = "del",
            Email = "del@t.com",
            Phone = "+5511987654321",
            Password = "hash",
            Status = UserStatus.Active,
            Role = UserRole.Customer,
            Name = new UserPersonName { FirstName = "D", LastName = "E" },
            Address = new UserAddress
            {
                City = "C",
                Street = "S",
                Number = 2,
                Zipcode = "01000-000",
                Geolocation = new AddressGeolocation { Lat = "0", Long = "0" }
            }
        };

        var snapshot = new GetUserResult { Id = 8, Username = "del", Email = "del@t.com" };

        _userRepository.GetByIdAsync(8, Arg.Any<CancellationToken>()).Returns(user);
        _mapper.Map<GetUserResult>(user).Returns(snapshot);
        _userRepository.DeleteAsync(8, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeleteUserCommand(8), CancellationToken.None);

        result.Should().BeSameAs(snapshot);
        await _userRepository.Received(1).DeleteAsync(8, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "DeleteUser should throw when user missing")]
    public async Task Handle_MissingUser_ThrowsKeyNotFound()
    {
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await _handler.Handle(new DeleteUserCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
