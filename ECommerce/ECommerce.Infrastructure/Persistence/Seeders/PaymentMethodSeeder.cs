using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Seeders
{
    public static class PaymentMethodSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (!await context.PaymentMethods.AnyAsync())
            {
                var paymentMethods = new List<PaymentMethod>
                {
                    new PaymentMethod { Name = "VnPay" },
                    new PaymentMethod { Name = "COD" }
                };

                await context.PaymentMethods.AddRangeAsync(paymentMethods);
                await context.SaveChangesAsync();
            }
        }
    }
}
