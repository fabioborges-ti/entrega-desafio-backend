using System.Text.Json.Serialization;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Users;

/// <summary>
/// Contrato alinhado a https://github.com/coodesh/mouts-backend-challenge/blob/main/.doc/users-api.md
/// </summary>
public class UserNameContract
{
    [JsonPropertyName("firstname")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastname")]
    public string LastName { get; set; } = string.Empty;
}

public class GeolocationContract
{
    [JsonPropertyName("lat")]
    public string Lat { get; set; } = string.Empty;

    [JsonPropertyName("long")]
    public string Long { get; set; } = string.Empty;
}

public class UserAddressContract
{
    public string City { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public int Number { get; set; }

    public string Zipcode { get; set; } = string.Empty;

    public GeolocationContract Geolocation { get; set; } = new();
}
