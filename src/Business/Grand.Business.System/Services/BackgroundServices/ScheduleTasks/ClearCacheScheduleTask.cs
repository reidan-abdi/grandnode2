using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Infrastructure.Caching;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks;

/// <summary>
///     Clear cache scheduled task implementation
/// </summary>
public class ClearCacheScheduleTask : IScheduleTask
{
    private readonly ICache _cache;

    public ClearCacheScheduleTask(ICache cache)
    {
        _cache = cache;
    }

    /// <summary>
    ///     Executes a task
    /// </summary>
    public async Task Execute()
    {
        await _cache.Clear();
    }
}