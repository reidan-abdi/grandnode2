using Grand.Domain.Blogs;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Web.Events.Cache;

public class BlogPostNotificatioHandler :
    INotificationHandler<EntityInserted<BlogPost>>,
    INotificationHandler<EntityUpdated<BlogPost>>,
    INotificationHandler<EntityDeleted<BlogPost>>
{
    private readonly ICache _cache;

    public BlogPostNotificatioHandler(ICache cache)
    {
        _cache = cache;
    }

    public async Task Handle(EntityDeleted<BlogPost> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<BlogPost> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<BlogPost> eventMessage, CancellationToken cancellationToken)
    {
        await _cache.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
    }
}