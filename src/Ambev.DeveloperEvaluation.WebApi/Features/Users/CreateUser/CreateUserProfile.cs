using Ambev.DeveloperEvaluation.Application.Users;
using Ambev.DeveloperEvaluation.Application.Users.CreateUser;
using Ambev.DeveloperEvaluation.Application.Users.GetUser;
using Ambev.DeveloperEvaluation.Application.Users.UpdateUser;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.WebApi.Features.Users;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.GetUser;
using Ambev.DeveloperEvaluation.WebApi.Features.Users.UpdateUser;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users.CreateUser;

/// <summary>
/// Profile for mapping between Application and API CreateUser responses
/// </summary>
public class CreateUserProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for CreateUser feature
    /// </summary>
    public CreateUserProfile()
    {
        CreateMap<UserNameContract, UserPersonName>();
        CreateMap<GeolocationContract, AddressGeolocation>();
        CreateMap<UserAddressContract, UserAddress>();
        CreateMap<CreateUserRequest, CreateUserCommand>();
        CreateMap<UpdateUserRequest, UpdateUserCommand>()
            .ForMember(d => d.Id, opt => opt.Ignore());

        CreateMap<UserPersonNameDto, UserNameContract>();
        CreateMap<AddressGeolocationDto, GeolocationContract>();
        CreateMap<UserAddressDto, UserAddressContract>();
        CreateMap<CreateUserResult, CreateUserResponse>();
        CreateMap<GetUserResult, GetUserResponse>();
    }
}
