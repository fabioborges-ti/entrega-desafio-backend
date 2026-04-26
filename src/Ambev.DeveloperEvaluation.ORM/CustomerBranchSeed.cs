using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;

namespace Ambev.DeveloperEvaluation.ORM;

/// <summary>
/// Seed runtime para <see cref="Customer"/> e <see cref="Branch"/>.
/// As migrations de seed originais (<c>20260424145810_SeedRandomCustomersTen</c> e
/// <c>20260424145853_SeedRandomBranchesTen</c>) só inserem se já existir algum
/// <see cref="User"/>; como o admin é criado depois de <c>ApplyPendingMigrationsAsync</c>,
/// na primeira subida essas tabelas ficam vazias. Este seed complementa, de forma
/// idempotente, populando até <see cref="DefaultTarget"/> linhas em cada tabela.
/// </summary>
public static class CustomerBranchSeed
{
    public const int DefaultTarget = 10;

    public sealed record SeedResult(int CustomersInserted, int BranchesInserted);

    /// <summary>
    /// Insere até <paramref name="target"/> Customers (Id+Name) e Branches coerentes (com CNPJ único)
    /// quando as respectivas tabelas estiverem vazias. Branches requerem um admin existente.
    /// </summary>
    public static SeedResult SeedIfNeeded(DefaultContext db, int target = DefaultTarget)
    {
        if (target <= 0)
            return new SeedResult(0, 0);

        var customersInserted = 0;
        var branchesInserted = 0;

        var rng = new Random(20260424);

        if (!db.Customers.Any())
        {
            for (var i = 1; i <= target; i++)
            {
                db.Customers.Add(new Customer
                {
                    Name = $"Cliente {RandomNameToken(rng, 6, 14)}"
                });
                customersInserted++;
            }

            db.SaveChanges();
        }

        if (!db.Branches.Any())
        {
            var adminId = db.Users
                .Where(u => u.Role == UserRole.Admin)
                .OrderBy(u => u.Id)
                .Select(u => (int?)u.Id)
                .FirstOrDefault();

            if (adminId is null or 0)
                return new SeedResult(customersInserted, 0);

            var now = DateTime.UtcNow;
            for (var i = 1; i <= target; i++)
            {
                db.Branches.Add(new Branch
                {
                    Name = $"Filial {RandomNameToken(rng, 6, 16)}",
                    Cnpj = (91000000000000L + i).ToString().PadLeft(14, '0'),
                    CreatedByUserId = adminId.Value,
                    CreatedAt = now - TimeSpan.FromDays(rng.Next(0, 120)),
                    LastModifiedAt = now
                });
                branchesInserted++;
            }

            db.SaveChanges();
        }

        return new SeedResult(customersInserted, branchesInserted);
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
}

