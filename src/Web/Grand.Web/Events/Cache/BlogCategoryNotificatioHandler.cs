using Grand.Domain.Blogs;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Web.Events.Cache;

public class BlogCategoryNotificatioHandler :
    INotificationHandler<EntityInserted<BlogCategory>>,
    INotificationHandler<EntityUpdated<BlogCategory>>,
    INotificationHandler<EntityDeleted<BlogCategory>>
{
    private readonly ICache _cache;

    public BlogCategoryNotificatioHandler(ICache cache)
    {
        _cache = cache;
    }

    public async Task Handle(EntityDeleted<BlogCategory> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<BlogCategory> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<BlogCategory> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
    }
}