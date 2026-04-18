using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Persistence.Seeders
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            // Danh sách các role cần thiết cho app nước hoa của bạn
            string[] roleNames = { "Admin", "Customer" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Tạo mới Role nếu chưa tồn tại
                    await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName, NormalizedName = roleName.ToUpper() });
                }
            }
        }
    }
}
