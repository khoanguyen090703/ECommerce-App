using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Seeders
{
    public static class ScentFamilySeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (!await context.ScentFamilies.AnyAsync())
            {
                var scentFamilies = new List<ScentFamily>
                {
                    new ScentFamily { Name = "Floral (Hoa cỏ)", Description = "Dịu dàng, nữ tính. Phù hợp với nữ giới yêu sự thanh lịch." },
                    new ScentFamily { Name = "Fruity (Trái cây)", Description = "Ngọt ngào, trẻ trung. Phù hợp với nữ tuổi teen – U30, năng động." },
                    new ScentFamily { Name = "Citrus (Cam chanh)", Description = "Tươi mát, tỉnh táo. Phù hợp với nam/nữ thích sự sạch sẽ, đơn giản." },
                    new ScentFamily { Name = "Green (Xanh lá)", Description = "Tự nhiên, thư giãn. Phù hợp với người yêu thiên nhiên, thích sự mộc mạc." },
                    new ScentFamily { Name = "Aquatic (Biển cả)", Description = "Mát lạnh, hiện đại. Phù hợp với người trẻ, thích sự tự do." },
                    new ScentFamily { Name = "Oriental (Phương Đông)", Description = "Nồng nàn, quyến rũ. Phù hợp với người trưởng thành, yêu sự sang trọng." },
                    new ScentFamily { Name = "Woody (Gỗ)", Description = "Trầm ấm, nam tính. Phù hợp với nam giới chín chắn, lịch lãm." },
                    new ScentFamily { Name = "Spicy (Gia vị)", Description = "Ấm nồng, cá tính. Phù hợp với người mạnh mẽ, yêu phong cách khác biệt." },
                    new ScentFamily { Name = "Leather (Da thuộc)", Description = "Mạnh mẽ, cổ điển. Phù hợp với nam giới trưởng thành, phong cách cổ điển." },
                    new ScentFamily { Name = "Gourmand (Ẩm thực)", Description = "Ngọt như kẹo, quyến rũ. Phù hợp với người cá tính, thích hương ngọt." }
                };

                await context.ScentFamilies.AddRangeAsync(scentFamilies);
                await context.SaveChangesAsync();
            }
        }
    }
}
