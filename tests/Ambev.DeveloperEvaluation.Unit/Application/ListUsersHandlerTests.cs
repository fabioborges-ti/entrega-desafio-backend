using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Application.Users.ListUsers;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application;

public class ListUsersHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ListUsersHandler _handler;

    public ListUsersHandlerTests()
    {
        _handler = new ListUsersHandler(_userRepository, _mapper);
        _mapper.Map<GetUserResult>(Arg.Any<User>()).Returns(ci =>
        {
            var u = ci.Arg<User>();
            return new GetUserResult { Id = u.Id, Username = u.Username, Email = u.Email };
        });
    }

    [Fact(DisplayName = "ListUsers should return paged metadata and mapped rows")]
    public async Task Handle_WithRepositoryData_ReturnsPagedList()
    {
        var user = new User
        {
            Id = 5,
            Username = "john",
            Email = "john@test.com",
            Phone = "+5511987654321",
            Password = "h",
            Status = UserStatus.Active,
            Role = UserRole.Customer,
            Name = new UserPersonName { FirstName = "J", LastName = "D" },
            Address = new UserAddress
            {
                City = "SP",
                Street = "Rua",
                Number = 10,
                Zipcode = "01000-000",
                Geolocation = new AddressGeolocation { Lat = "1", Long = "2" }
            }
        };

        _userRepository
            .ListPagedAsync(2, 5, "username asc", Arg.Any<CancellationToken>())
            .Returns((new List<User> { user }.AsReadOnly(), 12));

        var result = await _handler.Handle(new ListUsersQuery(2, 5, "username asc"), CancellationToken.None);

        result.TotalItems.Should().Be(12);
        result.CurrentPage.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.Data.Should().ContainSingle();
        result.Data[0].Id.Should().Be(5);
        await _userRepository.Received(1).ListPagedAsync(2, 5, "username asc", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "ListUsers should clamp page size to 100")]
    public async Task Handle_LargePageSize_ClampsTo100()
    {
        _userRepository.ListPagedAsync(1, 100, null, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<User>(), 0));

        await _handler.Handle(new ListUsersQuery(1, 500, null), CancellationToken.None);

        await _userRepository.Received(1).ListPagedAsync(1, 100, null, Arg.Any<CancellationToken>());
    }
}
