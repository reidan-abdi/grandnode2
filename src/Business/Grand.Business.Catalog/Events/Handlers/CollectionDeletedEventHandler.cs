using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Business.Catalog.Events.Handlers;

public class CollectionDeletedEventHandler : INotificationHandler<EntityDeleted<Collection>>
{
    private readonly ICache _cache;
    private readonly IRepository<EntityUrl> _entityUrlRepository;
    private readonly IRepository<Product> _productRepository;

    public CollectionDeletedEventHandler(
        IRepository<EntityUrl> entityUrlRepository,
        IRepository<Product> productRepository,
        ICache cache)
    {
        _entityUrlRepository = entityUrlRepository;
        _productRepository = productRepository;
        _cache = cache;
    }

    public async Task Handle(EntityDeleted<Collection> notification, CancellationToken cancellationToken)
    {
        //delete url
        await _entityUrlRepository.DeleteManyAsync(x =>
            x.EntityId == notification.Entity.Id && x.EntityName == EntityTypes.Collection);

        //delete on the product
        await _productRepository.PullFilter(string.Empty, x => x.ProductCollections, z => z.CollectionId,
            notification.Entity.Id);

        //clear cache
        await _cache.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }
}