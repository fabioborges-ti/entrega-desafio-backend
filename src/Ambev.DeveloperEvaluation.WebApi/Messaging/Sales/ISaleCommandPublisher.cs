namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public interface ISaleCommandPublisher
{
    Task<string> PublishCreateAsync(CreateSaleRequestedMessage payload, CancellationToken cancellationToken);

    Task<string> PublishUpdateAsync(UpdateSaleRequestedMessage payload, CancellationToken cancellationToken);

    Task<string> PublishCancelAsync(CancelSaleRequestedMessage payload, CancellationToken cancellationToken);

    Task<string> PublishDeleteAsync(DeleteSaleRequestedMessage payload, CancellationToken cancellationToken);
}
