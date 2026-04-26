using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace Ambev.DeveloperEvaluation.WebApi.StockAlert;

/// <summary>
/// Background service que verifica periodicamente o estoque de todos os produtos
/// e envia um e-mail de alerta ao administrador quando algum produto está abaixo
/// do limiar mínimo configurado (<see cref="Inventory.MinimumStockAlert"/>).
/// O intervalo padrão é de 60 minutos, configurável em <see cref="StockAlertOptions.CheckIntervalMinutes"/>.
/// </summary>
public class StockAlertHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<StockAlertOptions> _options;
    private readonly ILogger<StockAlertHostedService> _logger;

    public StockAlertHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<StockAlertOptions> options,
        ILogger<StockAlertHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Max(1, _options.Value.CheckIntervalMinutes);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation(
            "StockAlertHostedService iniciado. Intervalo de verificação: {IntervalMinutes} min.",
            intervalMinutes);

        // Primeira execução logo após o start da aplicação (aguarda 30s para o banco estar pronto).
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAndAlertAsync(stoppingToken);
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CheckAndAlertAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("StockAlert: iniciando verificação de estoque.");

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var inventoryRepo = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IStockAlertEmailService>();

            var belowAlert = await inventoryRepo.ListBelowAlertAsync(cancellationToken);

            if (belowAlert.Count == 0)
            {
                _logger.LogDebug("StockAlert: nenhum produto abaixo do limiar mínimo.");
                return;
            }

            _logger.LogWarning(
                "StockAlert: {Count} produto(s) com estoque abaixo do limiar. Enviando alerta para o administrador.",
                belowAlert.Count);

            await emailService.SendLowStockAlertAsync(belowAlert, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "StockAlert: erro inesperado durante a verificação de estoque.");
        }
    }
}
