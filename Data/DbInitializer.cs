using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using wirtualny_dziekanat.Data.Entities;

namespace wirtualny_dziekanat.Data;

public static class DbInitializer
{
    private static readonly string[] Roles = ["Admin", "Student", "Teacher"];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Migracje zapewniają, że baza LocalDB ma aktualny schemat Code First.
        await context.Database.MigrateAsync();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSucceeded(result, $"utworzenia roli {role}");
            }
        }

        var direction = await context.Kierunki.SingleOrDefaultAsync(item => item.Nazwa == "Informatyka")
            ?? new Kierunek { Nazwa = "Informatyka" };
        if (direction.Id == 0)
        {
            context.Kierunki.Add(direction);
            await context.SaveChangesAsync();
        }

        var group = await context.Grupy.SingleOrDefaultAsync(item => item.Nazwa == "INF-1" && item.KierunekId == direction.Id)
            ?? new Grupa { Nazwa = "INF-1", KierunekId = direction.Id, Semestr = 1 };
        if (group.Id == 0)
        {
            context.Grupy.Add(group);
            await context.SaveChangesAsync();
        }

        await GetOrCreateUserAsync(userManager, "admin@dziekanat.local", "Admin123!", "Admin");
        var student = await GetOrCreateUserAsync(userManager, "student@dziekanat.local", "Student123!", "Student");
        var teacher = await GetOrCreateUserAsync(userManager, "teacher@dziekanat.local", "Teacher123!", "Teacher");

        if (!await context.Students.AnyAsync(item => item.ApplicationUserId == student.Id))
        {
            context.Students.Add(new Student
            {
                ApplicationUserId = student.Id,
                NumerIndeksu = "100001",
                Imie = "Jan",
                Nazwisko = "Student",
                Semestr = 1,
                KierunekId = direction.Id,
                GrupaId = group.Id
            });
        }

        if (!await context.Teachers.AnyAsync(item => item.ApplicationUserId == teacher.Id))
        {
            context.Teachers.Add(new Teacher
            {
                ApplicationUserId = teacher.Id,
                Imie = "Anna",
                Nazwisko = "Nauczyciel",
                TytulNaukowy = "dr"
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> GetOrCreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            EnsureSucceeded(result, $"utworzenia użytkownika {email}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var result = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(result, $"przypisania roli {role} użytkownikowi {email}");
        }

        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Błąd podczas {operation}: {errors}");
        }
    }
}
