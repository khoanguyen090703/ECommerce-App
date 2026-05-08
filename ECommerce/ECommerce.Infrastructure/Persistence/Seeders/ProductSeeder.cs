using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Persistence.Seeders
{
    public static class ProductSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (!await context.Products.AnyAsync())
            {
                var seedData = new[]
                {
                    new {
                        Name = "Bleu de Chanel EDP",
                        Description = "Hương thơm gỗ nồng nàn, mang lại vẻ ngoài sang trọng, lịch lãm và đầy bí ẩn cho phái mạnh.",
                        Image = "https://fimgs.net/mdimg/perfume-thumbs/dark-375x500.25967.avif",
                        BrandId = 2,
                        CategoryIds = new[] { 1 },
                        Line = "Bleu de Chanel",
                        ReleaseYear = 2014,
                        Concentration = ProductConcentration.EDP,
                        ScentFamilyIds = new[] { 2, 5, 8 },
                        VariantPrice = 3800000m,
                        VariantVolumn = 100,
                        VariantStock = 15
                    },
                    new {
                        Name = "Creed Aventus",
                        Description = "Tôn vinh sức mạnh, tầm nhìn và sự thành công. Mùi hương trái cây hoàng gia vô cùng cuốn hút.",
                        Image = "https://fimgs.net/mdimg/perfume-thumbs/dark-375x500.9828.avif",
                        BrandId = 15,
                        CategoryIds = new[] { 3, 4 },
                        Line = "Aventus",
                        ReleaseYear = 2010,
                        Concentration = ProductConcentration.EDP,
                        ScentFamilyIds = new[] { 1, 7 },
                        VariantPrice = 7500000m,
                        VariantVolumn = 100,
                        VariantStock = 5
                    },
                    new {
                        Name = "Tom Ford Oud Wood",
                        Description = "Hương gỗ trầm hương quý hiếm, mang đến vẻ đẹp bí ẩn, gợi cảm và vô cùng quyền lực.",
                        Image = "https://fimgs.net/mdimg/perfume-thumbs/dark-375x500.1826.avif",
                        BrandId = 8,
                        CategoryIds = new[] { 2 },
                        Line = "Private Blend",
                        ReleaseYear = 2007,
                        Concentration = ProductConcentration.EDP,
                        ScentFamilyIds = new[] { 8, 9, 10 },
                        VariantPrice = 6200000m,
                        VariantVolumn = 50,
                        VariantStock = 8
                    },
                    new {
                        Name = "YSL Y EDP",
                        Description = "Sự pha trộn tinh tế giữa sự tươi mát của táo xanh và chiều sâu của gỗ sồi, đại diện cho người đàn ông tự tin.",
                        Image = "https://fimgs.net/mdimg/perfume-thumbs/dark-375x500.79243.avif",
                        BrandId = 5,
                        CategoryIds = new[] { 1 },
                        Line = "Y",
                        ReleaseYear = 2018,
                        Concentration = ProductConcentration.EDP,
                        ScentFamilyIds = new[] { 3, 4 },
                        VariantPrice = 3100000m,
                        VariantVolumn = 100,
                        VariantStock = 20
                    },
                    new {
                        Name = "Versace Eros EDT",
                        Description = "Tình yêu, đam mê và khao khát được thể hiện qua hương thơm phương Đông tươi mát và bạc hà sảng khoái.",
                        Image = "https://fimgs.net/mdimg/perfume-thumbs/dark-375x500.16657.avif",
                        BrandId = 12,
                        CategoryIds = new[] { 1, 2 },
                        Line = "Eros",
                        ReleaseYear = 2012,
                        Concentration = ProductConcentration.EDT,
                        ScentFamilyIds = new[] { 2, 6 },
                        VariantPrice = 2200000m,
                        VariantVolumn = 100,
                        VariantStock = 25
                    },
                    new {
                        Name = "Acqua di Gio Profumo",
                        Description = "Sự hòa quyện giữa hương biển cả thanh mát và sự gai góc của đá núi lửa, mãnh liệt và nam tính.",
                        Image = "https://fimgs.net/mdimg/perfume-thumbs/dark-375x500.29727.avif",
                        BrandId = 3,
                        CategoryIds = new[] { 1 },
                        Line = "Acqua di Gio",
                        ReleaseYear = 2015,
                        Concentration = ProductConcentration.Parfum,
                        ScentFamilyIds = new[] { 1, 5, 8 },
                        VariantPrice = 2800000m,
                        VariantVolumn = 75,
                        VariantStock = 10
                    },
                    new {
                        Name = "Jo Malone Wood Sage & Sea Salt",
                        Description = "Mang theo không khí biển trong lành hòa quyện cùng vị muối khoáng và nét ấm áp của gỗ xô thơm.",
                        Image = "https://fimgs.net/mdimg/perfume-thumbs/dark-375x500.25529.avif",
                        BrandId = 20,
                        CategoryIds = new[] { 2, 4 },
                        Line = "Cologne",
                        ReleaseYear = 2014,
                        Concentration = ProductConcentration.EDC,
                        ScentFamilyIds = new[] { 5, 9 },
                        VariantPrice = 3500000m,
                        VariantVolumn = 100,
                        VariantStock = 12
                    },
                    new {
                        Name = "Le Labo Santal 33",
                        Description = "Biểu tượng của sự cá tính và độc bản. Hương gỗ đàn hương, bạch đậu khấu và da thuộc đặc trưng khó quên.",
                        Image = "https://fimgs.net/mdimg/perfume-thumbs/dark-375x500.12201.avif",
                        BrandId = 25,
                        CategoryIds = new[] { 2, 3 },
                        Line = "Classic Collection",
                        ReleaseYear = 2011,
                        Concentration = ProductConcentration.EDP,
                        ScentFamilyIds = new[] { 8, 10 },
                        VariantPrice = 5500000m,
                        VariantVolumn = 50,
                        VariantStock = 6
                    }
                };

                var products = new List<Product>();
                var allCategories = await context.Categories.ToListAsync();
                var allScentFamilies = await context.ScentFamilies.ToListAsync();
                var allBrands = await context.Brands.ToListAsync();

                foreach (var item in seedData)
                {
                    var product = new Product
                    {
                        Name = item.Name,
                        Description = item.Description,
                        Concentration = item.Concentration,
                        Line = item.Line,
                        ReleaseYear = item.ReleaseYear,
                        Status = ProductStatus.Active,
                        IsFeatured = true,
                        Images = new List<ProductImage> { new ProductImage { Url = item.Image } },
                        Categories = allCategories.Where(c => item.CategoryIds.Contains(c.Id)).ToList(),
                        ScentFamilies = allScentFamilies.Where(sf => item.ScentFamilyIds.Contains(sf.Id)).ToList(),
                        ProductVariants = new List<ProductVariant>
                        {
                            new ProductVariant
                            {
                                Name = item.Name,
                                Format = VariantFormat.FullBottle,
                                Volumn = item.VariantVolumn,
                                Price = item.VariantPrice,
                                StockQuantity = item.VariantStock,
                                IsDefault = true,
                                Status = VariantStatus.Available,
                                Images = new List<VariantImage> { new VariantImage { Url = item.Image } }
                            }
                        }
                    };

                    var brand = allBrands.FirstOrDefault(b => b.Id == item.BrandId);
                    if (brand != null)
                    {
                        product.Brand = brand;
                    }

                    products.Add(product);
                }

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
