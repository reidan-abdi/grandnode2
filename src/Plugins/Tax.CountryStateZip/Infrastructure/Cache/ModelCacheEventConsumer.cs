using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Tax.CountryStateZip.Domain;

namespace Tax.CountryStateZip.Infrastructure.Cache;

/// <summary>
///     Model cache event consumer (used for caching of presentation layer models)
/// </summary>
public class ModelCacheEventConsumer :
    //tax rates
    INotificationHandler<EntityInserted<TaxRate>>,
    INotificationHandler<EntityUpdated<TaxRate>>,
    INotificationHandler<EntityDeleted<TaxRate>>
{
    /// <summary>
    ///     Key for caching
    /// </summary>
    public const string ALL_TAX_RATES_MODEL_KEY = "Grand.plugins.tax.countrystatezip.all";

    public const string ALL_TAX_RATES_PATTERN_KEY = "Grand.plugins.tax.countrystatezip";

    private readonly ICache _cache;

    public ModelCacheEventConsumer(ICache cache)
    {
        _cache = cache;
    }

    public async Task Handle(EntityDeleted<TaxRate> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(ALL_TAX_RATES_PATTERN_KEY);
    }

    //tax rates
    public async Task Handle(EntityInserted<TaxRate> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(ALL_TAX_RATES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<TaxRate> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(ALL_TAX_RATES_PATTERN_KEY);
    }
}