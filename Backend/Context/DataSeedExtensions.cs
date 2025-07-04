using Microsoft.AspNetCore.Identity;

namespace Backend.Context;

public static class DataSeedExtensions
{
    public static async Task IdentitySeed(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        var adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        string adminEmail = "admin@gmail.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "123456");
            await userManager.AddToRoleAsync(admin, adminRole);
        }
        
        string userRole = "User";
        if (!await roleManager.RoleExistsAsync(userRole))
        {
            await roleManager.CreateAsync(new IdentityRole(userRole));
        }
    }
}