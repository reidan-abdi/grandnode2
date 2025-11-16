using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using MediatR;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetPriceByCustomerProductQueryHandler : IRequestHandler<GetPriceByCustomerProductQuery, double?>
{
    private readonly ICache _cache;
    private readonly IRepository<CustomerProductPrice> _customerProductPriceRepository;

    public GetPriceByCustomerProductQueryHandler(ICache cache,
        IRepository<CustomerProductPrice> customerProductPriceRepository)
    {
        _cache = cache;
        _customerProductPriceRepository = customerProductPriceRepository;
    }

    public async Task<double?> Handle(GetPriceByCustomerProductQuery request, CancellationToken cancellationToken)
    {
        var key = string.Format(CacheKey.CUSTOMER_PRODUCT_PRICE_KEY_ID, request.CustomerId, request.ProductId);
        var productPrice = _cache.Get(key, () =>
        {
            var pp = _customerProductPriceRepository.Table
                .FirstOrDefault(x => x.CustomerId == request.CustomerId && x.ProductId == request.ProductId);
            return pp == null ? (null, false) : (pp, true);
        });

        if (!productPrice.Item2)
            return await Task.FromResult(default(double?));
        return await Task.FromResult(productPrice.pp.Price);
    }
}