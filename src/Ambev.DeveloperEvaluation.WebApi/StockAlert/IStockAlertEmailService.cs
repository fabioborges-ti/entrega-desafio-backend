using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.WebApi.StockAlert;

/// <summary>
/// Contrato para envio de alertas de estoque baixo por e-mail.
/// </summary>
public interface IStockAlertEmailService
{
    /// <summary>
    /// Envia um e-mail de alerta para o administrador listando os produtos
    /// cujo estoque está igual ou abaixo do limiar configurado.
    /// </summary>
    Task SendLowStockAlertAsync(IReadOnlyList<Inventory> items, CancellationToken cancellationToken = default);
}
