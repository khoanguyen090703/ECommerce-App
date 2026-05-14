using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Seeders
{
    public static class PaymentMethodSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            foreach (var name in new[] { "VnPay", "COD", "Stripe" })
            {
                if (!await context.PaymentMethods.AnyAsync(pm => pm.Name == name))
                    await context.PaymentMethods.AddAsync(new PaymentMethod { Name = name });
            }

            await context.SaveChangesAsync();
        }
    }
}
