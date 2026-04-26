using Ambev.DeveloperEvaluation.Application.Users.Seeding;

using Ambev.DeveloperEvaluation.Common.Security;

using Ambev.DeveloperEvaluation.Domain.Repositories;



namespace Ambev.DeveloperEvaluation.WebApi.Hosting;



/// <summary>

/// Executa o seed do administrador padrão na subida da API (<see cref="DefaultAdminUserSeed"/>).

/// </summary>

public sealed class EnsureInitialAdminHostedService : IHostedService

{

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<EnsureInitialAdminHostedService> _logger;



    public EnsureInitialAdminHostedService(

        IServiceScopeFactory scopeFactory,

        ILogger<EnsureInitialAdminHostedService> logger)

    {

        _scopeFactory = scopeFactory;

        _logger = logger;

    }



    public async Task StartAsync(CancellationToken cancellationToken)

    {

        try

        {

            using var scope = _scopeFactory.CreateScope();

            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();



            var created = await DefaultAdminUserSeed.SeedIfMissingAsync(users, hasher, cancellationToken);

            if (created)

            {

                _logger.LogInformation(

                    "Usuário administrador seed '{Username}' criado com sucesso.",

                    DefaultAdminUserSeed.Username);

            }

        }

        catch (Exception ex)

        {

            _logger.LogError(

                ex,

                "Seed do administrador: não foi possível conectar ao banco ou gravar o usuário. " +

                "A API continuará; verifique PostgreSQL e a connection string.");

        }

    }



    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}



