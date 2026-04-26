namespace Ambev.DeveloperEvaluation.WebApi.Features.Inventories;



public class CreateInventoryRequest
{
    public int ProductId { get; set; }

    public int AvailableQuantity { get; set; }

    /// <summary>Quantidade mínima para disparo de alerta de estoque baixo. 0 = desativado (padrão).</summary>
    public int MinimumStockAlert { get; set; }
}

public class UpdateInventoryRequest
{
    public int AvailableQuantity { get; set; }

    /// <summary>Quantidade mínima para disparo de alerta de estoque baixo. 0 = desativado (padrão).</summary>
    public int MinimumStockAlert { get; set; }
}


