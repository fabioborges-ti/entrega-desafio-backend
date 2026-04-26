namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// Coordenadas no endereço (lat / long como string, conforme contrato da API).
/// </summary>
public class AddressGeolocation
{
    public string Lat { get; set; } = string.Empty;

    public string Long { get; set; } = string.Empty;
}

