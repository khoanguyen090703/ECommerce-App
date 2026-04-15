using ECommerce.Application.DTOs.Response;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public async Task<List<ProductResponse4List>> GetAllAsync()
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                return products.Select(p => new ProductResponse4List
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Category = p.Category.Name ?? "",
                    Image = p.Images.FirstOrDefault()?.Url ?? "",
                    CreatedDate = p.CreatedDate,
                    UpdatedDate = p.UpdatedDate

                }).ToList();
            }
            catch
            {
                throw;
            }
        }
    }
}
