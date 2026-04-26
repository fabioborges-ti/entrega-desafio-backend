namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>Mensagens de validação ao registrar ou alterar vendas (cliente, filial, carrinho, produtos).</summary>
public static class SaleSubmissionMessages
{
    public const string PropertyCustomerId = "customer.id";

    public const string PropertyBranchId = "branch.id";

    public const string PropertyCartId = "cartId";

    public const string PropertyProductId = "items";

    public static string CustomerNotRegistered(int customerId) =>
        $"Não encontramos um cliente cadastrado com o identificador informado (id: {customerId}). " +
        "Confira o campo customer.id, cadastre o cliente na API de clientes, se necessário, e tente novamente.";

    public static string BranchNotRegistered(int branchId) =>
        $"Não encontramos uma filial (loja) cadastrada com o identificador informado (id: {branchId}). " +
        "Confira o campo branch.id, cadastre a filial na API de filiais, se necessário, e tente novamente.";

    public static string CartNotRegistered(int cartId) =>
        $"Não encontramos um carrinho com o identificador informado (cartId: {cartId}). " +
        "Confira se o número do carrinho está correto e se ele ainda existe no sistema.";

    public static string CartAlreadyHasSale(int cartId) =>
        $"Este carrinho (cartId: {cartId}) já foi utilizado em outra venda. " +
        "Cada carrinho pode gerar apenas uma venda; utilize um carrinho novo ou verifique o histórico.";

    public static string CartHasNoLineItems(int cartId) =>
        $"O carrinho (cartId: {cartId}) não possui itens. Adicione produtos ao carrinho antes de finalizar a venda.";

    public static string SaleHasNoLinkedCart(int saleId) =>
        $"A venda (id: {saleId}) não está vinculada a um carrinho; não é possível recalcular os itens. " +
        "Apenas vendas com carrinho de origem podem ser atualizadas por este endpoint.";

    public static string ProductNotRegistered(int productId) =>
        $"Um dos itens da venda referencia um produto que não existe no catálogo (productId: {productId}). " +
        "Revise a lista de itens e utilize apenas produtos cadastrados.";
}


