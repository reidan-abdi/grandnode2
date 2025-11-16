using Grand.Business.Core.Events.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using MediatR;

namespace Grand.Business.Catalog.Events.Handlers;

public class ProductPublishEventHandler : INotificationHandler<ProductPublishEvent>
{
    private readonly ICache _cache;

    public ProductPublishEventHandler(ICache cache)
    {
        _cache = cache;
    }

    public async Task Handle(ProductPublishEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Product.ShowOnHomePage)
            await _cache.RemoveByPrefix(CacheKey.PRODUCTS_SHOWONHOMEPAGE);
    }
}