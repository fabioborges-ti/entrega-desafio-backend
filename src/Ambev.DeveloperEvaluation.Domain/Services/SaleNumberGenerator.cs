namespace Ambev.DeveloperEvaluation.Domain.Services;

public static class SaleNumberGenerator
{
    public static string Generate()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"VEN-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
    }
}
