namespace Ambev.DeveloperEvaluation.Application.Branches;

public static class CnpjDigits
{
    public static string Normalize(string cnpj) =>
        string.IsNullOrWhiteSpace(cnpj)
            ? string.Empty
            : new string(cnpj.Where(char.IsAsciiDigit).ToArray());

    public static bool HasValidLength(string digits) => digits.Length == 14;
}
