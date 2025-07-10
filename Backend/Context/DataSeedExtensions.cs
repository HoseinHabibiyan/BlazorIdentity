using Backend.Identity;
using Microsoft.AspNetCore.Identity;

namespace Backend.Context;

public static class DataSeedExtensions
{
    public static async Task IdentitySeed(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        string adminEmail = "admin@gmail.com";
        ApplicationUser? admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
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
        
        string userEmail = "user@gmail.com";
        ApplicationUser? user = await userManager.FindByEmailAsync(userEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "123456");
            await userManager.AddToRoleAsync(user, userRole);
        }
    }
}