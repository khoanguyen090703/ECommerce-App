using ECommerce.Application.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Response;

namespace ECommerce.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<PagedResult<CustomerResponse>> GetCustomersAsync(CustomerQueryParams parameters)
        {
            var paged = await _customerRepository.GetAsync(parameters);
            var emailByIdentityId = await _customerRepository.GetEmailsByIdentityIdsAsync(
                paged.Items.Select(c => c.IdentityId));
            var mapped = paged.Items.Select(c => new CustomerResponse
            {
                Id = c.Id,
                FullName = c.FullName,
                Address = c.Address,
                AvatarUrl = c.AvatarUrl,
                Email = emailByIdentityId.TryGetValue(c.IdentityId, out var email) ? email : null,
                CreatedDate = c.CreatedDate,
                UpdatedDate = c.UpdatedDate
            }).ToList();

            return new PagedResult<CustomerResponse>(mapped, paged.TotalCount, paged.PageNumber, paged.PageSize);
        }
    }
}
