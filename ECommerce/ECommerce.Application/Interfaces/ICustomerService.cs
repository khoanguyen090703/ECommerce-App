using ECommerce.Domain.Common;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Response;

namespace ECommerce.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<PagedResult<CustomerResponse>> GetCustomersAsync(CustomerQueryParams parameters);
    }
}
