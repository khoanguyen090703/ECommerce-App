using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IVariantService, VariantService>();
            services.AddScoped<IScentFamilyService, ScentFamilyService>();
            services.AddScoped<IImageService, ImageService>();

            // Register validators
            services.AddTransient<FluentValidation.IValidator<UpdateVariantRequest>, UpdateVariantRequestValidator>();
            services.AddTransient<FluentValidation.IValidator<ECommerce.SharedViewModels.DTOs.Request.CreateVariantRequest>, CreateVariantRequestValidator>();
            services.AddTransient<FluentValidation.IValidator<CreateVariantRequest>, CreateVariantRequestValidator>();

            return services;
        }
    }
}
