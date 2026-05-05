using EmployeeOrderApi.Domain;
using Microsoft.AspNetCore.Identity;

namespace EmployeeOrderApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // ── Roles ─────────────────────────────────────────────────────────────
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── Default Admin ─────────────────────────────────────────────────────
        const string adminEmail = "admin@system.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName    = "admin",
                Email       = adminEmail,
                DisplayName = "System Admin",
                IsActive    = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123456");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        // ── Default Supervisor ────────────────────────────────────────────────
        const string supEmail = "supervisor@system.com";
        if (await userManager.FindByEmailAsync(supEmail) is null)
        {
            var sup = new ApplicationUser
            {
                UserName    = "supervisor",
                Email       = supEmail,
                DisplayName = "Default Supervisor",
                IsActive    = true
            };
            var result = await userManager.CreateAsync(sup, "Supervisor@123456");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(sup, Roles.Supervisor);
        }
    }
}
