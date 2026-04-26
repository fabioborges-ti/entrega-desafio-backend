using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Validation;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Ambev.DeveloperEvaluation.ORM;

/// <summary>
/// Resultado de <see cref="DefaultContext.SeedAdminUserIfMissing"/>.
/// <see cref="PlainPassword"/> reflete o valor configurado (não é lido do banco).
/// </summary>
public sealed record AdminUserSeedResult(bool Created, bool PasswordUpdated, string Username, string Email, string PlainPassword);

public class DefaultContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Sale> Sales { get; set; }

    public DbSet<Customer> Customers { get; set; }

    public DbSet<Branch> Branches { get; set; }

    public DbSet<Product> Products { get; set; }

    public DbSet<Inventory> Inventories { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Cart> Carts { get; set; }

    public DbSet<ProductUserRating> ProductRatings { get; set; }

    public DefaultContext(DbContextOptions<DefaultContext> options) : base(options)
    {
    }

    /// <summary>
    /// Garante um administrador com a senha em texto plano informada (tipicamente vinda de configuração/secret).
    /// Se <paramref name="configuredUsername"/> existir no banco e for Admin, apenas sincroniza o hash da senha.
    /// Se não houver username na config e existir <b>exatamente um</b> Admin no banco, sincroniza a senha desse usuário
    /// (útil após seed antigo com senha aleatória).
    /// Caso contrário, cria um novo Admin (login padrão <c>admin</c> ou <see cref="configuredUsername"/>; se o login já existir, usa <c>admin_</c> + sufixo) somente se ainda não houver nenhum Admin.
    /// </summary>
    /// <param name="configuredUsername">Opcional. Quando preenchido, atualiza a senha desse login se já existir (comparação case-insensitive).</param>
    /// <exception cref="ArgumentException"><paramref name="plainPassword"/> vazio ou inválido para as regras de senha.</exception>
    /// <exception cref="InvalidOperationException">Dados inválidos ou username existente com outro papel.</exception>
    public AdminUserSeedResult SeedAdminUserIfMissing(
        IPasswordHasher passwordHasher,
        string plainPassword,
        string? configuredUsername = null)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Senha de seed não pode ser vazia.", nameof(plainPassword));

        var pwdCheck = new PasswordValidator().Validate(plainPassword);
        if (!pwdCheck.IsValid)
        {
            var msg = string.Join("; ", pwdCheck.Errors.Select(e => e.ErrorMessage));
            throw new ArgumentException($"Senha de configuração não atende às regras: {msg}", nameof(plainPassword));
        }

        var usernameKey = string.IsNullOrWhiteSpace(configuredUsername) ? null : configuredUsername.Trim();

        if (usernameKey is not null)
        {
            var existingByName = Users.FirstOrDefault(u => u.Username.ToLower() == usernameKey.ToLower());
            if (existingByName is not null)
            {
                if (existingByName.Role != UserRole.Admin)
                    throw new InvalidOperationException($"O usuário '{usernameKey}' existe mas não é Admin.");

                existingByName.Password = passwordHasher.HashPassword(plainPassword);
                existingByName.UpdatedAt = DateTime.UtcNow;
                SaveChanges();
                return new AdminUserSeedResult(false, true, existingByName.Username, existingByName.Email, plainPassword);
            }
        }
        else if (Users.Count(u => u.Role == UserRole.Admin) == 1)
        {
            var soleAdmin = Users.First(u => u.Role == UserRole.Admin);
            soleAdmin.Password = passwordHasher.HashPassword(plainPassword);
            soleAdmin.UpdatedAt = DateTime.UtcNow;
            SaveChanges();
            return new AdminUserSeedResult(false, true, soleAdmin.Username, soleAdmin.Email, plainPassword);
        }

        if (Users.Any(u => u.Role == UserRole.Admin))
            return new AdminUserSeedResult(false, false, string.Empty, string.Empty, string.Empty);

        var rng = Random.Shared;
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var desiredLogin = (usernameKey ?? "admin").Trim();
        var username = Users.Any(u => u.Username.ToLower() == desiredLogin.ToLower())
            ? $"admin_{suffix}"
            : desiredLogin;

        var user = new User
        {
            Username = username,
            Email = $"admin.{suffix}@seed.local",
            Phone = $"+55{rng.Next(11, 100)}{rng.Next(100000000, 1000000000)}",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            Name = new UserPersonName
            {
                FirstName = RandomNameToken(rng, 6, 12),
                LastName = RandomNameToken(rng, 6, 14)
            },
            Address = new UserAddress
            {
                City = RandomNameToken(rng, 5, 20),
                Street = $"Rua {RandomNameToken(rng, 6, 18)}",
                Number = rng.Next(1, 9999),
                Zipcode = $"{rng.Next(10000, 99999)}-{rng.Next(100, 999)}",
                Geolocation = new AddressGeolocation
                {
                    Lat = (rng.NextDouble() * 180 - 90).ToString("F6"),
                    Long = (rng.NextDouble() * 360 - 180).ToString("F6")
                }
            },
            Password = plainPassword
        };

        var validation = user.Validate();
        if (!validation.IsValid)
        {
            var msg = string.Join("; ", validation.Errors.Select(e =>
                string.IsNullOrEmpty(e.Detail) ? e.Error : e.Detail));
            throw new InvalidOperationException($"SeedAdminUserIfMissing: dados inválidos: {msg}");
        }

        user.Password = passwordHasher.HashPassword(plainPassword);
        Users.Add(user);
        SaveChanges();

        return new AdminUserSeedResult(true, false, user.Username, user.Email, plainPassword);
    }

    private static string RandomNameToken(Random rng, int minLen, int maxLen)
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";
        var len = rng.Next(minLen, maxLen + 1);
        var chars = new char[len];
        for (var i = 0; i < len; i++)
            chars[i] = letters[rng.Next(letters.Length)];
        chars[0] = char.ToUpperInvariant(chars[0]);
        return new string(chars);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
public class YourDbContextFactory : IDesignTimeDbContextFactory<DefaultContext>
{
    public DefaultContext CreateDbContext(string[] args)
    {
        var assemblyDir = Path.GetDirectoryName(typeof(YourDbContextFactory).Assembly.Location)!;
        var ormProjectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));
        var webApiDir = Path.GetFullPath(Path.Combine(ormProjectDir, "..", "Ambev.DeveloperEvaluation.WebApi"));

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(webApiDir)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var builder = new DbContextOptionsBuilder<DefaultContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(
            connectionString,
            b => b.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM"));

        return new DefaultContext(builder.Options);
    }
}