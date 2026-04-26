using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM;

/// <summary>
/// Seed em Development: garante as <see cref="FixedCategoryCount"/> categorias fixas do catálogo; em seguida, para cada uma (nesta ordem),
/// insere até 10 <see cref="Product"/> coerentes (títulos únicos) e <see cref="Inventory"/> com quantidade entre 50 e 100.
/// Deve coincidir com a migration <c>20260425120000_RestrictCategoriesToTenFixed</c>.
/// </summary>
public static class CatalogInventorySeed
{
    /// <summary>Número de categorias fixas (Artigos esportivos �?� Automotivo, sem Bebidas / Alimentos e Bebidas / Unclassified).</summary>
    public const int FixedCategoryCount = 10;

    /// <summary>Ordem e nomes exatos das categorias (alinhado à migration <c>20260425120000_RestrictCategoriesToTenFixed</c>).</summary>
    public static readonly string[] FixedCategoryNamesOrdered =
    [
        "Artigos esportivos",
        "Eletrônicos",
        "Moda Masculina",
        "Moda Feminina",
        "Casa e Decoração",
        "Beleza e Cuidados Pessoais",
        "Esportes e Fitness",
        "Informática e Acessórios",
        "Brinquedos e Jogos",
        "Automotivo"
    ];

    public sealed record SeedResult(
        int CategoriesInserted,
        int ProductsInserted,
        int InventoriesCreated,
        int InventoriesUpdatedAbove50);

    /// <summary>
    /// Garante as 10 categorias fixas; para cada uma (nesta ordem), insere produtos+estoque em falta (idempotente por título).
    /// </summary>
    public static SeedResult SeedCatalogAndInventoryIfNeeded(DefaultContext db)
    {
        var rng = new Random(20260425);
        var categoriesInserted = 0;
        var productsInserted = 0;
        var inventoriesCreated = 0;

        foreach (var name in FixedCategoryNamesOrdered)
        {
            if (db.Categories.Any(c => c.Name == name))
                continue;

            db.Categories.Add(new Category { Name = name });
            categoriesInserted++;
        }

        if (categoriesInserted > 0)
            db.SaveChanges();

        var existingTitles = db.Products.AsNoTracking().Select(p => p.Title).ToHashSet(StringComparer.Ordinal);

        var byName = db.Categories.AsNoTracking().ToDictionary(c => c.Name, StringComparer.Ordinal);
        var categories = new List<Category>();
        foreach (var name in FixedCategoryNamesOrdered)
        {
            if (byName.TryGetValue(name, out var category))
                categories.Add(category);
        }

        if (categories.Count == 0)
            return new SeedResult(categoriesInserted, 0, 0, 0);

        var catalogIndex = 0;
        foreach (var category in categories)
        {
            var titles = ResolveTitlesForCategory(category);
            foreach (var title in titles)
            {
                if (existingTitles.Contains(title))
                    continue;

                var qty = rng.Next(50, 101);
                // MinimumStockAlert: 10% da quantidade inicial, mínimo 5 — demo para alertas visíveis.
                var minAlert = Math.Max(5, qty / 10);
                db.Products.Add(new Product
                {
                    Title = title,
                    Description = $"Produto seed: {title}.",
                    Price = 9.99m + catalogIndex * 1.37m,
                    CategoryId = category.Id,
                    Image = $"https://picsum.photos/id/{(catalogIndex % 80) + 1}/400/300",
                    Inventory = new Inventory { AvailableQuantity = qty, MinimumStockAlert = minAlert }
                });
                existingTitles.Add(title);
                productsInserted++;
                inventoriesCreated++;
                catalogIndex++;
            }

            db.SaveChanges();
        }

        return new SeedResult(categoriesInserted, productsInserted, inventoriesCreated, 0);
    }

    private static IReadOnlyList<string> ResolveTitlesForCategory(Category category)
    {
        if (TitlesByCategoryName.TryGetValue(category.Name, out var titles))
            return titles;

        return Enumerable.Range(1, 10)
            .Select(i => $"{category.Name} - item catálogo {category.Id:D2}-{i:D2}")
            .ToList();
    }

    /// <summary>Títulos fixos por nome de categoria (10�-10 = 100 strings distintas).</summary>
    private static readonly Dictionary<string, string[]> TitlesByCategoryName = new(StringComparer.Ordinal)
    {
        ["Artigos esportivos"] =
        [
            "Bola de futebol society costurada à mão",
            "Raquete de tênis alumínio encordoamento padrão",
            "Par de halteres revestidos neoprene 8 kg",
            "Esteira elétrica dobrável motor 1 HP",
            "Conjunto de cones para treino de agilidade 10 unidades",
            "Prancha abdominal curvada antiderrapante",
            "Caneleira de peso ajustável velcro 5 kg par",
            "Corda de pular rolamento esfera cabo PVC",
            "Tabela de basquete infantil ajustável 2,20 m",
            "Rede de vôlei de praia com fitas e estacas"
        ],
        ["Eletrônicos"] =
        [
            "Smartphone Android 6,5 polegadas 128 GB 5G",
            "Smart TV LED 50 polegadas 4K HDR10",
            "Fone de ouvido Bluetooth cancelamento ativo de ruído",
            "Soundbar 2.1 canais subwoofer wireless",
            "Caixa de som Bluetooth resistente à água IPX7",
            "Tablet Wi-Fi 11 polegadas 256 GB caneta inclusa",
            "Carregador USB-C turbo 65 W compatível PD",
            "Relógio smartwatch GPS oxímetro de pulso",
            "Roteador Wi-Fi 6 banda dupla 1800 Mbps",
            "Webcam Full HD 60 fps com microfone estéreo"
        ],
        ["Moda Masculina"] =
        [
            "Camiseta básica algodão penteado preta",
            "Calça jeans slim stretch azul índigo",
            "Bermuda cargo sarja cáqui com bolsos laterais",
            "Camisa social manga longa branca slim fit",
            "Jaqueta puffer nylon com capuz removível",
            "Tênis casual couro sintético marrom café",
            "Cinto masculino couro fivela escovada",
            "Kit meias cano médio algodão pack 3 pares",
            "Boné trucker aba curva regulagem snapback",
            "Moletom canguru capuz forro fleece cinza"
        ],
        ["Moda Feminina"] =
        [
            "Vestido midi fluido estampa botânica",
            "Blusa crepe feminina gola V manga curta",
            "Calça pantalona cintura alta off-white",
            "Saia lápis alfaiataria preto clássico",
            "Cardigan tricot misto lã tom rosé",
            "Sandália salto bloco couro sintético nude",
            "Bolsa tiracolo transversal corrente dourada",
            "Conjunto pijama short doll algodão estampado",
            "Legging compressão cintura alta preta",
            "Brinco argola média folheado ouro 18 mm"
        ],
        ["Casa e Decoração"] =
        [
            "Jogo de lençol queen 400 fios algodão",
            "Cortina blackout sala 3,00 x 2,60 m cinza",
            "Almofada decorativa fibra siliconizada 50x50",
            "Tapete sisal sala antiderrapante 2 x 3 m",
            "Abajur cerâmica cúpula linho cru",
            "Organizador gavetas acrílico kit 10 peças",
            "Jogo potes herméticos bambu tampa 7 peças",
            "Quadro canvas abstrato moldura 80 x 120 cm",
            "Vaso cerâmico esmaltado cachepô 25 cm",
            "Manta sofá sherpa microfibra 2 x 2,3 m"
        ],
        ["Beleza e Cuidados Pessoais"] =
        [
            "Shampoo hidratante queratina frasco 400 ml",
            "Condicionador reparador pontas duplas 400 ml",
            "Sérum facial vitamina C 10% frasco 30 ml",
            "Protetor solar facial FPS 70 oil-free 50 g",
            "Loção hidratante corporal karité 400 ml",
            "Batom líquido matte tom terracota",
            "Máscara para cílios à prova d'água volume",
            "Kit pincéis maquiagem profissional 15 peças",
            "Creme facial noturno retinol 50 g",
            "Escova secadora modeladora íon negativo"
        ],
        ["Esportes e Fitness"] =
        [
            "Colchonete yoga PVC antiderrapante 10 mm",
            "Bola suíça pilates 65 cm anti-estouro",
            "Faixa elástica resistência nível médio kit 3",
            "Garrafa squeeze inox térmica isolada 750 ml",
            "Tênis corrida amortecimento gel feminino",
            "Luvas musculação dedo curto com munhequeira",
            "Corda de pular PVC cabo reforçado rolamentos",
            "Step aeróbico regulável altura 3 níveis",
            "Caneleira tornozelo 2 kg par removível",
            "Camisa compressão UV50+ manga longa dry fit"
        ],
        ["Informática e Acessórios"] =
        [
            "Mouse sem fio ergonômico 1600 DPI 6 botões",
            "Teclado mecânico ABNT2 switches brown",
            "Monitor IPS 24 polegadas 75 Hz HDMI DisplayPort",
            "SSD interno NVMe PCIe 4.0 1 TB leitura 5000 MB-s",
            "Pendrive USB 3.2 128 GB corpo metálico",
            "Headset over-ear 7.1 microfone destacável USB",
            "Hub USB-C 11 em 1 HDMI leitor SD Ethernet",
            "Cabo HDMI 2.1 fibra óptica 3 metros 8K",
            "Suporte monitor braço articulado clamp mesa",
            "Base notebook cooler seis ventoinhas LED"
        ],
        ["Brinquedos e Jogos"] =
        [
            "Boneca fashion 28 cm 18 pontos de articulação",
            "Carrinho controle remoto escala 1:16 4WD",
            "Jogo de tabuleiro estratégia 2 a 6 jogadores",
            "Blocos de montar compatíveis caixa 600 peças",
            "Pelúcia urso costura manual enchimento fibra 35 cm",
            "Massinha modelar cores neon kit 12 potes",
            "Quebra-cabeça paisagem fosco 1500 peças",
            "Bola futebol infantil nº 5 costura reforçada",
            "Kit médico brinquedo maleta estetoscópio",
            "Pista corrida loop duplo com 2 carrinhos die-cast"
        ],
        ["Automotivo"] =
        [
            "Limpador parabrisa concentrado diluição 100:1 500 ml",
            "Tapete automotivo PVC universal jogo 4 peças",
            "Capa banco couro sintético costura lateral airbag",
            "Organizador porta-malas colapsável 55 litros",
            "Compressor ar portátil 12 V manômetro digital",
            "Suporte celular magnético grade ventilação 360°",
            "Cera automotiva carnaúba proteção UV 300 g",
            "Cabo chupeta partida bateria 2,5 m 400 A",
            "Aromatizante veicular clip cítrico kit 4 unidades",
            "Kit emergência lanterna LED luvas triângulo colete"
        ]
    };

    static CatalogInventorySeed()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var title in TitlesByCategoryName.Values.SelectMany(t => t))
        {
            if (!seen.Add(title))
                throw new InvalidOperationException($"CatalogInventorySeed: título duplicado no dicionário - '{title}'.");
        }

        if (FixedCategoryNamesOrdered.Length != FixedCategoryCount)
            throw new InvalidOperationException($"CatalogInventorySeed: {nameof(FixedCategoryNamesOrdered)} deve ter {FixedCategoryCount} itens.");

        var fixedSet = new HashSet<string>(FixedCategoryNamesOrdered, StringComparer.Ordinal);
        if (TitlesByCategoryName.Count != fixedSet.Count)
            throw new InvalidOperationException($"CatalogInventorySeed: número de categorias no dicionário de títulos difere de {FixedCategoryCount}.");

        foreach (var name in FixedCategoryNamesOrdered)
        {
            if (!TitlesByCategoryName.ContainsKey(name))
                throw new InvalidOperationException($"CatalogInventorySeed: falta lista de títulos para a categoria '{name}'.");
        }

        foreach (var key in TitlesByCategoryName.Keys)
        {
            if (!fixedSet.Contains(key))
                throw new InvalidOperationException($"CatalogInventorySeed: categoria extra no dicionário de títulos - '{key}'.");
        }
    }
}

