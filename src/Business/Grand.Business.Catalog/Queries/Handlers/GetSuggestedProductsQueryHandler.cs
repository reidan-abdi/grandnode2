using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using MediatR;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetSuggestedProductsQueryHandler : IRequestHandler<GetSuggestedProductsQuery, IList<Product>>
{
    public GetSuggestedProductsQueryHandler(
        IProductService productService,
        ICache cache,
        IRepository<CustomerTagProduct> customerTagProductRepository)
    {
        _productService = productService;
        _cache = cache;
        _customerTagProductRepository = customerTagProductRepository;
    }

    public async Task<IList<Product>> Handle(GetSuggestedProductsQuery request, CancellationToken cancellationToken)
    {
        return await _cache.GetAsync(
            string.Format(CacheKey.PRODUCTS_CUSTOMER_TAG, string.Join(",", request.CustomerTagIds)), async () =>
            {
                var query = from cr in _customerTagProductRepository.Table
                    where request.CustomerTagIds.Contains(cr.CustomerTagId)
                    orderby cr.DisplayOrder
                    select cr.ProductId;

                var productIds = query.Take(request.ProductsNumber).ToList();

                var ids = await _productService.GetProductsByIds(productIds.Distinct().ToArray());

                return ids.Where(product => product.Published).ToList();
            });
    }

    #region Fields

    private readonly IProductService _productService;
    private readonly ICache _cache;
    private readonly IRepository<CustomerTagProduct> _customerTagProductRepository;

    #endregion
}