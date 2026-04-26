using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.ORM;

namespace Ambev.DeveloperEvaluation.WebApi.Hosting;

public static class StartupTasksExtensions
{
    public static async Task RunStartupTasksAsync(this WebApplication app)
    {
        await app.ApplyPendingMigrationsAsync(app.Lifetime.ApplicationStopping);

        using var seedScope = app.Services.CreateScope();
        var db = seedScope.ServiceProvider.GetRequiredService<DefaultContext>();
        var passwordHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        SeedAdminUser(app, db, passwordHasher);
        SeedCustomersAndBranches(app, db);
        SeedCatalogInDevelopment(app, db);
    }

    private static void SeedAdminUser(WebApplication app, DefaultContext db, IPasswordHasher passwordHasher)
    {
        var apiPassword = app.Configuration["ApiSecrets:Password"];
        if (string.IsNullOrWhiteSpace(apiPassword))
        {
            app.Logger.LogWarning("ApiSecrets:Password não está definida; seed do usuário de API ignorado.");
            return;
        }

        var apiUsername = app.Configuration["ApiSecrets:Username"];
        var adminSeed = db.SeedAdminUserIfMissing(passwordHasher, apiPassword, apiUsername);
        if (adminSeed.Created)
        {
            app.Logger.LogInformation(
                "Seed admin (DbContext): criado login={Username}, email={Email}. Autentique com ApiSecrets (username/password) em appsettings ou secrets.",
                adminSeed.Username,
                adminSeed.Email);
        }
        else if (adminSeed.PasswordUpdated)
        {
            app.Logger.LogInformation(
                "Seed admin (DbContext): senha sincronizada para {Username} conforme ApiSecrets:Password.",
                adminSeed.Username);
        }
    }

    private static void SeedCustomersAndBranches(WebApplication app, DefaultContext db)
    {
        var customerBranchSeed = CustomerBranchSeed.SeedIfNeeded(db);
        if (customerBranchSeed.CustomersInserted > 0 || customerBranchSeed.BranchesInserted > 0)
        {
            app.Logger.LogInformation(
                "Seed Customers/Branches: customers={Customers}, branches={Branches}.",
                customerBranchSeed.CustomersInserted,
                customerBranchSeed.BranchesInserted);
        }
    }

    private static void SeedCatalogInDevelopment(WebApplication app, DefaultContext db)
    {
        if (!app.Environment.IsDevelopment())
            return;

        var catalogSeed = CatalogInventorySeed.SeedCatalogAndInventoryIfNeeded(db);
        if (catalogSeed.CategoriesInserted > 0 || catalogSeed.ProductsInserted > 0
            || catalogSeed.InventoriesCreated > 0 || catalogSeed.InventoriesUpdatedAbove50 > 0)
        {
            app.Logger.LogInformation(
                "Seed catálogo (Development): categorias={Categories}, produtos={Products}, inventários criados={InvCreated}, inventários ajustados acima de 50={InvUpdated}.",
                catalogSeed.CategoriesInserted,
                catalogSeed.ProductsInserted,
                catalogSeed.InventoriesCreated,
                catalogSeed.InventoriesUpdatedAbove50);
        }
    }
}

