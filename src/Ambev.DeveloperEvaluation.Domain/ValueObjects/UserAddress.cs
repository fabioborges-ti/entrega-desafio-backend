namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

public class UserAddress
{
    public string City { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public int Number { get; set; }

    public string Zipcode { get; set; } = string.Empty;

    public AddressGeolocation Geolocation { get; set; } = new();
}
