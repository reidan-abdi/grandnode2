using Grand.Domain.News;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Web.Events.Cache;

public class NewsItemNotificatioHandler :
    INotificationHandler<EntityInserted<NewsItem>>,
    INotificationHandler<EntityUpdated<NewsItem>>,
    INotificationHandler<EntityDeleted<NewsItem>>
{
    private readonly ICache _cache;

    public NewsItemNotificatioHandler(ICache cache)
    {
        _cache = cache;
    }

    public async Task Handle(EntityDeleted<NewsItem> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.NEWS_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<NewsItem> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.NEWS_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<NewsItem> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.NEWS_PATTERN_KEY);
    }
}