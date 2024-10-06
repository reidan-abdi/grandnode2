﻿using Grand.Api.Constants;
using Grand.Api.DTOs.Catalog;
using Grand.Api.DTOs.Common;
using Grand.Api.DTOs.Customers;
using Grand.Api.DTOs.Shipping;
using Grand.Api.Infrastructure.DependencyManagement;
using Grand.Api.Queries.Handlers.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.TypeSearch;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Grand.Api.Infrastructure;

public class ODataStartup : IStartupApplication
{
    public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
    {
        var apiConfig = application.ApplicationServices.GetService<BackendAPIConfig>();
        if (apiConfig.Enabled) application.UseCors(Configurations.CorsPolicyName);
    }

    public void ConfigureServices(IServiceCollection services,
        IConfiguration configuration)
    {
        var apiConfig = services.BuildServiceProvider().GetService<BackendAPIConfig>();
        if (apiConfig.Enabled)
        {
            //register RequestHandler
            RegisterRequestHandler(services);

            //cors
            services.AddCors(options =>
            {
                options.AddPolicy(Configurations.CorsPolicyName,
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });
            //Add OData
            services.AddControllers(options =>
            {
                options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
            }).AddOData(opt =>
            {
                opt.EnableQueryFeatures(Configurations.MaxLimit);
                opt.AddRouteComponents(Configurations.ODataRoutePrefix, GetEdmModel(apiConfig));
                opt.Select().Filter().OrderBy().Expand().Count().SetMaxTop(Configurations.MaxLimit);
            });

            services.AddScoped<ModelValidationAttribute>();
        }
    }

    public int Priority => 505;
    public bool BeforeConfigure => false;

    private static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
    {
        var builder = new ServiceCollection()
            .AddLogging()
            .AddMvc()
            .AddNewtonsoftJson()
            .Services.BuildServiceProvider();

        return builder
            .GetRequiredService<IOptions<MvcOptions>>()
            .Value
            .InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>()
            .First();
    }


    private IEdmModel GetEdmModel(BackendAPIConfig apiConfig)
    {
        var builder = new ODataConventionModelBuilder {
            Namespace = Configurations.ODataModelBuilderNamespace
        };
        RegisterDependencies(builder, apiConfig);
        return builder.GetEdmModel();
    }

    private void RegisterDependencies(ODataConventionModelBuilder builder, BackendAPIConfig apiConfig)
    {
        var typeFinder = new TypeSearcher();

        //find dependency provided by other assemblies
        var dependencyInject = typeFinder.ClassesOfType<IDependencyEdmModel>();

        //create and sort instances of dependency inject
        var instances = dependencyInject
            .Select(di => (IDependencyEdmModel)Activator.CreateInstance(di))
            .OrderBy(di => di!.Order);

        //register all provided dependencies
        foreach (var dependencyRegistrar in instances)
            dependencyRegistrar!.Register(builder, apiConfig);
    }

    private void RegisterRequestHandler(IServiceCollection services)
    {
        var handlerTypes = new (Type dto, Type entity)[]
        {
            (typeof(CountryDto), typeof(Country)),
            (typeof(CurrencyDto), typeof(Currency)),
            (typeof(BrandDto), typeof(Brand)),
            (typeof(CategoryDto), typeof(Category)),
            (typeof(CollectionDto), typeof(Collection)),
            (typeof(ProductAttributeDto), typeof(ProductAttribute)),
            (typeof(ProductDto), typeof(Product)),
            (typeof(SpecificationAttributeDto), typeof(SpecificationAttribute)),
            (typeof(WarehouseDto), typeof(Warehouse)),
            (typeof(ShippingMethodDto), typeof(ShippingMethod)),
            (typeof(PickupPointDto), typeof(PickupPoint)),
            (typeof(DeliveryDateDto), typeof(DeliveryDate)),
            (typeof(VendorDto), typeof(Vendor)),
            (typeof(CustomerGroupDto), typeof(CustomerGroup)),
            (typeof(StoreDto), typeof(Store)),
            (typeof(LanguageDto), typeof(Language)),
            (typeof(PictureDto), typeof(Picture))
        };

        foreach (var (dto, entity) in handlerTypes)
        {
            var requestHandlerType = typeof(IRequestHandler<,>).MakeGenericType(
                typeof(GetGenericQuery<,>).MakeGenericType(dto, entity),
                typeof(IQueryable<>).MakeGenericType(dto)
            );
            var handlerImplementationType = typeof(GetGenericQueryHandler<,>).MakeGenericType(dto, entity);
            services.AddScoped(requestHandlerType, handlerImplementationType);
        }
    }

}