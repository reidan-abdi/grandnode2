using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Data;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Checkout.Services.Shipping;

public class DeliveryDateService : IDeliveryDateService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public DeliveryDateService(
        IRepository<DeliveryDate> deliveryDateRepository,
        IMediator mediator,
        ICache cache)
    {
        _deliveryDateRepository = deliveryDateRepository;
        _mediator = mediator;
        _cache = cache;
    }

    #endregion

    #region Fields

    private readonly IRepository<DeliveryDate> _deliveryDateRepository;
    private readonly IMediator _mediator;
    private readonly ICache _cache;

    #endregion

    #region Delivery dates

    /// <summary>
    ///     Gets a delivery date
    /// </summary>
    /// <param name="deliveryDateId">The delivery date identifier</param>
    /// <returns>Delivery date</returns>
    public virtual Task<DeliveryDate> GetDeliveryDateById(string deliveryDateId)
    {
        var key = string.Format(CacheKey.DELIVERYDATE_BY_ID_KEY, deliveryDateId);
        return _cache.GetAsync(key, () => _deliveryDateRepository.GetByIdAsync(deliveryDateId));
    }

    /// <summary>
    ///     Gets all delivery dates
    /// </summary>
    /// <returns>Delivery dates</returns>
    public virtual async Task<IList<DeliveryDate>> GetAllDeliveryDates()
    {
        return await _cache.GetAsync(CacheKey.DELIVERYDATE_ALL, async () =>
        {
            var query = from dd in _deliveryDateRepository.Table
                orderby dd.DisplayOrder
                select dd;
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Inserts a delivery date
    /// </summary>
    /// <param name="deliveryDate">Delivery date</param>
    public virtual async Task InsertDeliveryDate(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);

        await _deliveryDateRepository.InsertAsync(deliveryDate);

        //clear cache
        await _cache.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(deliveryDate);
    }

    /// <summary>
    ///     Updates the delivery date
    /// </summary>
    /// <param name="deliveryDate">Delivery date</param>
    public virtual async Task UpdateDeliveryDate(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);

        await _deliveryDateRepository.UpdateAsync(deliveryDate);

        //clear cache
        await _cache.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(deliveryDate);
    }

    /// <summary>
    ///     Deletes a delivery date
    /// </summary>
    /// <param name="deliveryDate">The delivery date</param>
    public virtual async Task DeleteDeliveryDate(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);

        await _deliveryDateRepository.DeleteAsync(deliveryDate);

        //clear cache
        await _cache.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY);

        //clear product cache
        await _cache.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(deliveryDate);
    }

    #endregion
}