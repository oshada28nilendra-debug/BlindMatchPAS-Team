using BlindMatchPAS.Web.Models;
using BlindMatchPAS.Web.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlindMatchPAS.Web.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                await context.Database.MigrateAsync();

                // Seed Roles
                foreach (var role in Roles.AllRoles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Created role: {Role}", role);
                    }
                }

                // Seed Research Areas
                if (!await context.ResearchAreas.AnyAsync())
                {
                    var areas = new[]
                    {
                        new ResearchArea { Name = "Artificial Intelligence", Description = "Machine Learning, Deep Learning, NLP", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ResearchArea { Name = "Cybersecurity", Description = "Network Security, Cryptography, Ethical Hacking", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ResearchArea { Name = "Software Engineering", Description = "Agile, DevOps, Software Architecture", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ResearchArea { Name = "Data Science", Description = "Big Data, Analytics, Visualization", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ResearchArea { Name = "Human-Computer Interaction", Description = "UX, Accessibility, UI Design", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ResearchArea { Name = "Cloud Computing", Description = "Distributed Systems, Microservices, Serverless", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ResearchArea { Name = "Internet of Things", Description = "Embedded Systems, Edge Computing, Smart Devices", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new ResearchArea { Name = "Blockchain", Description = "Distributed Ledger, Smart Contracts, DeFi", IsActive = true, CreatedAt = DateTime.UtcNow },
                    };
                    context.ResearchAreas.AddRange(areas);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} research areas", areas.Length);
                }

                // Seed Admin User
                await SeedUserAsync(userManager, roleManager, logger,
                    email: "admin@pas.edu",
                    fullName: "System Administrator",
                    password: "Admin@1234!",
                    role: Roles.SystemAdmin,
                    department: "IT Administration");

                // Seed Module Leader
                await SeedUserAsync(userManager, roleManager, logger,
                    email: "leader@pas.edu",
                    fullName: "Dr. Module Leader",
                    password: "Leader@1234!",
                    role: Roles.ModuleLeader,
                    department: "Computer Science");

                // Seed Demo Supervisor
                await SeedUserAsync(userManager, roleManager, logger,
                    email: "supervisor@pas.edu",
                    fullName: "Prof. Demo Supervisor",
                    password: "Supervisor@1234!",
                    role: Roles.Supervisor,
                    department: "Computer Science");

                // Seed Demo Student
                await SeedUserAsync(userManager, roleManager, logger,
                    email: "student@pas.edu",
                    fullName: "Demo Student",
                    password: "Student@1234!",
                    role: Roles.Student,
                    department: "Computer Science");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private static async Task SeedUserAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger logger,
            string email, string fullName, string password, string role, string? department = null)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    Department = department,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));

                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation("Seeded user {Email} with role {Role}", email, role);
                }
                else
                {
                    logger.LogError("Failed to seed user {Email}: {Errors}", email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
