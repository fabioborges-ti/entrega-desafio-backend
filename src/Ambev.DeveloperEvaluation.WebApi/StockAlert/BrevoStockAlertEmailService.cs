using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Ambev.DeveloperEvaluation.WebApi.StockAlert;

/// <summary>
/// Implementação de <see cref="IStockAlertEmailService"/> usando a API REST do Mailjet.
/// Documentação: https://dev.mailjet.com/email/reference/send-emails/
/// </summary>
public class BrevoStockAlertEmailService : IStockAlertEmailService
{
    private const string MailjetApiUrl = "https://api.mailjet.com/v3.1/send";

    private readonly HttpClient _httpClient;
    private readonly StockAlertOptions _options;
    private readonly ILogger<BrevoStockAlertEmailService> _logger;

    public BrevoStockAlertEmailService(
        HttpClient httpClient,
        IOptions<StockAlertOptions> options,
        ILogger<BrevoStockAlertEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendLowStockAlertAsync(IReadOnlyList<Inventory> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(_options.MailjetApiKey)
            || string.IsNullOrWhiteSpace(_options.MailjetSecretKey)
            || string.IsNullOrWhiteSpace(_options.AdminEmail)
            || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogWarning(
                "StockAlert: e-mail não enviado — MailjetApiKey, MailjetSecretKey, AdminEmail ou FromEmail não configurados. " +
                "Verifique a seção '{Section}' nas variáveis de ambiente.",
                StockAlertOptions.SectionName);
            return;
        }

        var htmlBody = BuildHtmlBody(items);
        var subject = $"[Alerta de Estoque] {items.Count} produto(s) abaixo do limiar mínimo";

        var payload = new
        {
            Messages = new[]
            {
                new
                {
                    From = new { Email = _options.FromEmail, Name = _options.FromName },
                    To = new[] { new { Email = _options.AdminEmail } },
                    Subject = subject,
                    HTMLPart = htmlBody
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_options.MailjetApiKey}:{_options.MailjetSecretKey}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, MailjetApiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = content;

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "StockAlert: e-mail de alerta enviado com sucesso para {AdminEmail}. {ItemCount} produto(s) abaixo do limiar.",
                    _options.AdminEmail,
                    items.Count);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "StockAlert: falha ao enviar e-mail via Mailjet. StatusCode={StatusCode} Body={Body}",
                    (int)response.StatusCode,
                    body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockAlert: exceção ao chamar API do Mailjet.");
        }
    }

    private static string BuildHtmlBody(IReadOnlyList<Inventory> items)
    {
        var rows = new StringBuilder();
        foreach (var inv in items)
        {
            var productName = inv.Product?.Title ?? $"Produto #{inv.ProductId}";
            rows.AppendLine($"""
                <tr>
                  <td style="padding:8px;border:1px solid #ddd;">{inv.ProductId}</td>
                  <td style="padding:8px;border:1px solid #ddd;">{WebUtility.HtmlEncode(productName)}</td>
                  <td style="padding:8px;border:1px solid #ddd;color:#c0392b;font-weight:bold;">{inv.AvailableQuantity}</td>
                  <td style="padding:8px;border:1px solid #ddd;">{inv.MinimumStockAlert}</td>
                </tr>
                """);
        }

        return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8"/></head>
            <body style="font-family:Arial,sans-serif;color:#333;">
              <h2 style="color:#c0392b;">&#9888; Alerta de Estoque Baixo</h2>
              <p>Os seguintes produtos estão com estoque igual ou abaixo do limiar mínimo configurado:</p>
              <table style="border-collapse:collapse;width:100%;max-width:700px;">
                <thead>
                  <tr style="background:#f2f2f2;">
                    <th style="padding:8px;border:1px solid #ddd;text-align:left;">ID</th>
                    <th style="padding:8px;border:1px solid #ddd;text-align:left;">Produto</th>
                    <th style="padding:8px;border:1px solid #ddd;text-align:left;">Qtd. Disponível</th>
                    <th style="padding:8px;border:1px solid #ddd;text-align:left;">Limiar Mínimo</th>
                  </tr>
                </thead>
                <tbody>
                  {rows}
                </tbody>
              </table>
              <p style="margin-top:20px;font-size:12px;color:#777;">
                Gerado automaticamente em {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC.
              </p>
            </body>
            </html>
            """;
    }
}
