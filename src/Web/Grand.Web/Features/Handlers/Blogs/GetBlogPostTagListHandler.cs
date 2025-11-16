using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Blogs;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Models.Blogs;
using MediatR;

namespace Grand.Web.Features.Handlers.Blogs;

public class GetBlogPostTagListHandler : IRequestHandler<GetBlogPostTagList, BlogPostTagListModel>
{
    private readonly IBlogService _blogService;

    private readonly BlogSettings _blogSettings;
    private readonly ICache _cache;
    private readonly IWorkContext _workContext;

    public GetBlogPostTagListHandler(IBlogService blogService, ICache cache, IWorkContext workContext,
        BlogSettings blogSettings)
    {
        _blogService = blogService;
        _cache = cache;
        _workContext = workContext;
        _blogSettings = blogSettings;
    }

    public async Task<BlogPostTagListModel> Handle(GetBlogPostTagList request, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(CacheKeyConst.BLOG_TAGS_MODEL_KEY, _workContext.WorkingLanguage.Id,
            _workContext.CurrentStore.Id);
        var cachedModel = await _cache.GetAsync(cacheKey, async () =>
        {
            var model = new BlogPostTagListModel();

            //get tags
            var tags = await _blogService.GetAllBlogPostTags(_workContext.CurrentStore.Id);
            tags = tags.OrderByDescending(x => x.BlogPostCount)
                .Take(_blogSettings.NumberOfTags)
                .ToList();
            //sorting
            tags = tags.OrderBy(x => x.Name).ToList();

            foreach (var tag in tags)
                model.Tags.Add(new BlogPostTagModel {
                    Name = tag.Name,
                    BlogPostCount = tag.BlogPostCount
                });
            return model;
        });
        return cachedModel;
    }
}