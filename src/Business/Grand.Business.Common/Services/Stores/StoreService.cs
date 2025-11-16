using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Data;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Common.Services.Stores;

/// <summary>
///     Store service
/// </summary>
public class StoreService : IStoreService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cache">Cache manager</param>
    /// <param name="storeRepository">Store repository</param>
    /// <param name="mediator">Mediator</param>
    public StoreService(ICache cache,
        IRepository<Store> storeRepository,
        IMediator mediator)
    {
        _cache = cache;
        _storeRepository = storeRepository;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<Store> _storeRepository;
    private readonly IMediator _mediator;
    private readonly ICache _cache;

    private List<Store> _allStores;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all stores
    /// </summary>
    /// <returns>Stores</returns>
    public virtual async Task<IList<Store>> GetAllStores()
    {
        return _allStores ??= await _cache.GetAsync(CacheKey.STORES_ALL_KEY, async () =>
        {
            return await Task.FromResult(_storeRepository.Table.OrderBy(x => x.DisplayOrder).ToList());
        });
    }

    /// <summary>
    ///     Gets all stores
    /// </summary>
    /// <returns>Stores</returns>
    public virtual IList<Store> GetAll()
    {
        return _allStores ??= _cache.Get(CacheKey.STORES_ALL_KEY, () =>
        {
            return _storeRepository.Table.OrderBy(x => x.DisplayOrder).ToList();
        });
    }

    /// <summary>
    ///     Gets a store
    /// </summary>
    /// <param name="storeId">Store identifier</param>
    /// <returns>Store</returns>
    public virtual Task<Store> GetStoreById(string storeId)
    {
        var key = string.Format(CacheKey.STORES_BY_ID_KEY, storeId);
        return _cache.GetAsync(key, () => _storeRepository.GetByIdAsync(storeId));
    }

    /// <summary>
    ///     Inserts a store
    /// </summary>
    /// <param name="store">Store</param>
    public virtual async Task InsertStore(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        await _storeRepository.InsertAsync(store);

        //clear cache
        await _cache.Clear();

        //event notification
        await _mediator.EntityInserted(store);
    }

    /// <summary>
    ///     Updates the store
    /// </summary>
    /// <param name="store">Store</param>
    public virtual async Task UpdateStore(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        await _storeRepository.UpdateAsync(store);

        //clear cache
        await _cache.Clear();

        //event notification
        await _mediator.EntityUpdated(store);
    }

    /// <summary>
    ///     Deletes a store
    /// </summary>
    /// <param name="store">Store</param>
    public virtual async Task DeleteStore(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var allStores = await GetAllStores();
        if (allStores.Count == 1)
            throw new Exception("You cannot delete the only configured store");

        await _storeRepository.DeleteAsync(store);

        //clear cache
        await _cache.Clear();

        //event notification
        await _mediator.EntityDeleted(store);
    }

    #endregion
}