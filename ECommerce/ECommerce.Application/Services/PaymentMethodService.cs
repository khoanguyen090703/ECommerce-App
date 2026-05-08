using ECommerce.Domain.Interfaces;
using ECommerce.SharedViewModels.DTOs.Response;
using ECommerce.Application.Mappings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Application.Services
{
    public interface IPaymentMethodService
    {
        Task<List<PaymentMethodResponse>> GetAllAsync();
        Task<List<PaymentMethodResponse>> GetAllAsync(bool includeInactive);
    }

    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly IPaymentMethodRepository _repo;

        public PaymentMethodService(IPaymentMethodRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<PaymentMethodResponse>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            var res = list.Select(pm => pm.ToResponse()).ToList();
            return res;
        }

        // Overload to allow including inactive methods
        public async Task<List<PaymentMethodResponse>> GetAllAsync(bool includeInactive)
        {
            var list = await _repo.GetAllAsync(includeInactive);
            return list.Select(pm => pm.ToResponse()).ToList();
        }
    }
}
