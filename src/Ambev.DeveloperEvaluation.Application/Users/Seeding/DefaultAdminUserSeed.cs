using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Application.Users.Seeding;

/// <summary>
/// Seed idempotente do usuário administrador padrão (login na API).
/// </summary>
public static class DefaultAdminUserSeed
{
    public const string Username = "admin";

    public const string Password = "Admin@123456";

    /// <summary>
    /// Cria o administrador padrão se ainda não existir usuário com <see cref="Username"/>.
    /// </summary>
    /// <returns><c>true</c> se inseriu o usuário; <c>false</c> se já existia.</returns>
    /// <exception cref="InvalidOperationException">Entidade inválida segundo regras de domínio (não esperado para estes dados fixos).</exception>
    public static async Task<bool> SeedIfMissingAsync(
        IUserRepository users,
        IPasswordHasher hasher,
        CancellationToken cancellationToken = default)
    {
        if (await users.GetByUsernameAsync(Username, cancellationToken) != null)
            return false;

        var user = new User
        {
            Username = Username,
            Email = "admin@localhost",
            Phone = "(11) 98765-4321",
            Password = hasher.HashPassword(Password),
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            Name = new UserPersonName { FirstName = "System", LastName = "Administrator" },
            Address = new UserAddress
            {
                City = "São Paulo",
                Street = "Rua Inicial",
                Number = 1,
                Zipcode = "01000-000",
                Geolocation = new AddressGeolocation { Lat = "0", Long = "0" }
            }
        };

        var validation = user.Validate();
        if (!validation.IsValid)
        {
            var msg = string.Join("; ", validation.Errors.Select(e => string.IsNullOrEmpty(e.Detail) ? e.Error : e.Detail));
            throw new InvalidOperationException($"DefaultAdminUserSeed: dados inválidos: {msg}");
        }

        await users.CreateAsync(user, cancellationToken);
        return true;
    }
}

