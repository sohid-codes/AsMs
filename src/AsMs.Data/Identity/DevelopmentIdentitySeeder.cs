using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AsMs.Data.Identity;

public class DevelopmentIdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ILogger<DevelopmentIdentitySeeder> logger)
{
    private static readonly SeedUser[] SeedUsers =
    [
        new("admin@asms.local", "Admin User", "Admin@123", IdentityRoleNames.Admin),
        new("teacher@asms.local", "Teacher User", "Teacher@123", IdentityRoleNames.Teacher),
        new("student@asms.local", "Student User", "Student@123", IdentityRoleNames.Student)
    ];

    public async Task SeedAsync()
    {
        foreach (var roleName in IdentityRoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            EnsureSucceeded(roleResult, $"create role '{roleName}'");
            logger.LogInformation("Created development role {RoleName}", roleName);
        }

        foreach (var seedUser in SeedUsers)
        {
            var user = await userManager.FindByEmailAsync(seedUser.Email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = seedUser.Email,
                    Email = seedUser.Email,
                    EmailConfirmed = true,
                    FullName = seedUser.FullName,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var userResult = await userManager.CreateAsync(user, seedUser.Password);
                EnsureSucceeded(userResult, $"create development user '{seedUser.Email}'");
                logger.LogInformation("Created development user {Email}", seedUser.Email);
            }

            if (!await userManager.IsInRoleAsync(user, seedUser.Role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, seedUser.Role);
                EnsureSucceeded(roleResult, $"assign role '{seedUser.Role}' to '{seedUser.Email}'");
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Unable to {operation}: {errors}");
        }
    }

    private sealed record SeedUser(string Email, string FullName, string Password, string Role);
}
