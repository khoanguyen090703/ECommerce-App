using ECommerce.Application.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductResponse4List>> GetAllAsync();
    }
}
