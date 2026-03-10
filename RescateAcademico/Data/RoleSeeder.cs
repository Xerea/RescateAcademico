using Microsoft.AspNetCore.Identity;
using RescateAcademico.Models;

namespace RescateAcademico.Data
{
    public static class RoleSeeder
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Administrador", "Tutor", "Alumno" };

            // Seed Roles
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Default Admin User
            string adminEmail = "admin@ipn.mx";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrador");
                }
            }
            
            // Seed Default Tutor
            string tutorEmail = "tutor@ipn.mx";
            if (await userManager.FindByEmailAsync(tutorEmail) == null)
            {
                var tut = new ApplicationUser { UserName = tutorEmail, Email = tutorEmail, IsActive = true, EmailConfirmed = true };
                var r2 = await userManager.CreateAsync(tut, "Tutor123!");
                if (r2.Succeeded) await userManager.AddToRoleAsync(tut, "Tutor");
            }
            
            // Seed Default Alumno
            string alumnoEmail = "alumno@alumno.ipn.mx";
            if (await userManager.FindByEmailAsync(alumnoEmail) == null)
            {
                var alum = new ApplicationUser { UserName = alumnoEmail, Email = alumnoEmail, IsActive = true, EmailConfirmed = true };
                var r3 = await userManager.CreateAsync(alum, "Alumno123!");
                if (r3.Succeeded) await userManager.AddToRoleAsync(alum, "Alumno");
            }
        }
    }
}
