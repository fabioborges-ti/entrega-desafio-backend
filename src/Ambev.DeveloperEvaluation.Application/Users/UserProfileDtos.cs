namespace Ambev.DeveloperEvaluation.Application.Users;

/// <summary>
/// DTOs compartilhados para comandos/resultados de usuário (alinhados ao contrato users-api.md).
/// </summary>
public class UserPersonNameDto
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}

public class AddressGeolocationDto
{
    public string Lat { get; set; } = string.Empty;

    public string Long { get; set; } = string.Empty;
}

public class UserAddressDto
{
    public string City { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public int Number { get; set; }

    public string Zipcode { get; set; } = string.Empty;

    public AddressGeolocationDto Geolocation { get; set; } = new();
}

