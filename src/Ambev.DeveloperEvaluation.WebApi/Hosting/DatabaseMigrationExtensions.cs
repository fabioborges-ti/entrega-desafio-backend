using System.Net.Sockets;
using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ambev.DeveloperEvaluation.WebApi.Hosting;

/// <summary>
/// Aplica migrations EF Core pendentes na subida, com retentativas para falhas transitórias (ex.: PostgreSQL ainda não aceitando conexões).
/// </summary>
public static class DatabaseMigrationExtensions
{
    private const int MaxAttempts = 12;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    public static async Task ApplyPendingMigrationsAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        var logger = app.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        var delay = InitialDelay;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();

                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count > 0)
                {
                    logger.LogInformation(
                        "Aplicando {Count} migration(s) pendente(s): {Migrations}",
                        pending.Count,
                        string.Join(", ", pending));
                }

                await db.Database.MigrateAsync(cancellationToken);

                if (pending.Count > 0)
                    logger.LogInformation("Migrations aplicadas com sucesso.");

                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsLikelyTransientDatabaseFailure(ex))
            {
                logger.LogWarning(
                    ex,
                    "Tentativa {Attempt}/{MaxAttempts} ao aplicar migrations falhou (erro transitório). Próxima tentativa em {Delay}s.",
                    attempt,
                    MaxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxDelay.Ticks));
            }
        }
    }

    private static bool IsLikelyTransientDatabaseFailure(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException!)
        {
            switch (current)
            {
                case NpgsqlException npg when npg.IsTransient:
                    return true;
                case TimeoutException:
                    return true;
                case IOException:
                    return true;
                case SocketException:
                    return true;
            }
        }

        return false;
    }
}

