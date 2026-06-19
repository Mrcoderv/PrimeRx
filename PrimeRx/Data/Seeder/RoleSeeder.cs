using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrimeRx.Models;

namespace PrimeRx.Data.Seeder;

public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = [AppRoles.Admin, AppRoles.Staff];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
