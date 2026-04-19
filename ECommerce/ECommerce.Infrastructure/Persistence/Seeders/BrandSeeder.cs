using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Persistence.Seeders
{
    public static class BrandSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (!await context.Brands.AnyAsync())
            {
                var brands = new List<Brand>
                {
                    new Brand { Name = "Afnan", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/fragnance-logo-text-afnan.jpg" },
                    new Brand { Name = "Armaf", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-armaf.webp" },
                    new Brand { Name = "Burberry", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-burberry.webp" },
                    new Brand { Name = "Bvlgari", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-bvlgary.webp" },
                    new Brand { Name = "Calvin Klein", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/04/logo-brand-calvin-klein.webp" },
                    new Brand { Name = "Carolina Herrera", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-carolina-herrera.webp" },
                    new Brand { Name = "Chanel", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-chanel.webp" },
                    new Brand { Name = "Chloé", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-chloe.webp" },
                    new Brand { Name = "Creed", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/fragnance-logo-text-creed.jpg" },
                    new Brand { Name = "Dior", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-dior.webp" },
                    new Brand { Name = "Dolce & Gabbana", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/fragnance-logo-text-dolce-and-gabbana.jpg" },
                    new Brand { Name = "Giorgio Armani", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/04/logo-brand-giorgio-armani.webp" },
                    new Brand { Name = "Gucci", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-gucci.webp" },
                    new Brand { Name = "Hermès", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-hermes.webp" },
                    new Brand { Name = "Jean Paul Gaultier", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-jean-paul-gaultier.webp" },
                    new Brand { Name = "Kenzo", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/fragnance-logo-text-kenzo.jpg" },
                    new Brand { Name = "Kilian", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-kilian.webp" },
                    new Brand { Name = "Lalique", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-lalique.webp" },
                    new Brand { Name = "Lancôme", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-lancome.webp" },
                    new Brand { Name = "Tommy Hilfiger", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/fragnance-logo-text-tommy-hilfiger.jpg" },
                    new Brand { Name = "Le Labo", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-le-labo.webp" },
                    new Brand { Name = "Marc Jacobs", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-marc-jacobs.webp" },
                    new Brand { Name = "Montblanc", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-montblanc.webp" },
                    new Brand { Name = "Moschino", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/fragnance-logo-text-moschino.jpg" },
                    new Brand { Name = "Narciso Rodriguez", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/04/logo-brand-narciso-rodriguez.webp" },
                    new Brand { Name = "Nautica", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-nautica.webp" },
                    new Brand { Name = "Paco Rabanne", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/fragnance-logo-text-paco-rabanne.jpg" },
                    new Brand { Name = "Roja", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-roja.webp" },
                    new Brand { Name = "Ralph Lauren", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-ralph-lauren.webp" },
                    new Brand { Name = "Victoria's Secret", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/07/fragnance-logo-text-victoria-s-secret.jpg" },
                    new Brand { Name = "Tom Ford", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-tom-ford.webp" },
                    new Brand { Name = "Valentino", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-valentino.webp" },
                    new Brand { Name = "Versace", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-versace.webp" },
                    new Brand { Name = "Viktor & Rolf", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-viktor-rolf.webp" },
                    new Brand { Name = "Yves Saint Laurent", 
                        ImageUrl = "https://orchard.vn/wp-content/uploads/2024/06/fragnance-logo-text-ysl.webp" }
                };

                await context.Brands.AddRangeAsync(brands);
                await context.SaveChangesAsync();
            }
        }
    }
}
