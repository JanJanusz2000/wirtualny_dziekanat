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

        await GetOrCreateUserAsync(userManager, "admin@uczelnia.pl", "Admin123!", "Admin");
        var student = await GetOrCreateUserAsync(userManager, "student@dziekanat.local", "Student123!", "Student");
        var teacher = await GetOrCreateUserAsync(userManager, "teacher@dziekanat.local", "Teacher123!", "Teacher");
        var janKowalski = await GetOrCreateUserAsync(userManager, "jan.kowalski@uczelnia.pl", "Jan123!", "Teacher");

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

        if (!await context.Teachers.AnyAsync(item => item.ApplicationUserId == janKowalski.Id))
        {
            context.Teachers.Add(new Teacher
            {
                ApplicationUserId = janKowalski.Id,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                TytulNaukowy = "dr"
            });
        }

        await context.SaveChangesAsync();

        var janProfile = await context.Teachers
            .SingleAsync(item => item.ApplicationUserId == janKowalski.Id);

        await SeedSubjectAsync(
            context,
            janProfile.Id,
            group.Id,
            direction.Id,
            "Bazy Danych",
            5,
            DayOfWeek.Monday,
            new TimeOnly(8, 0),
            new TimeOnly(9, 30),
            "A-101");

        await SeedSubjectAsync(
            context,
            janProfile.Id,
            group.Id,
            direction.Id,
            "Programowanie Obiektowe",
            6,
            DayOfWeek.Wednesday,
            new TimeOnly(10, 0),
            new TimeOnly(11, 30),
            "B-204");
    }

    private static async Task SeedSubjectAsync(
        ApplicationDbContext context,
        int teacherId,
        int groupId,
        int directionId,
        string name,
        int ects,
        DayOfWeek day,
        TimeOnly start,
        TimeOnly end,
        string room)
    {
        var subject = await context.Subjects
            .SingleOrDefaultAsync(item => item.Nazwa == name);

        if (subject is null)
        {
            subject = new Subject
            {
                Nazwa = name,
                TeacherId = teacherId,
                KierunekId = directionId,
                PunktyECTS = ects
            };
            context.Subjects.Add(subject);
            await context.SaveChangesAsync();
        }
        else
        {
            if (subject.KierunekId == 0)
            {
                subject.KierunekId = directionId;
            }

            if (subject.PunktyECTS == 0)
            {
                subject.PunktyECTS = ects;
            }

            await context.SaveChangesAsync();
        }

        if (!await context.Schedules.AnyAsync(item => item.SubjectId == subject.Id && item.GrupaId == groupId))
        {
            context.Schedules.Add(new Schedule
            {
                GrupaId = groupId,
                SubjectId = subject.Id,
                DzienTygodnia = day,
                GodzinaRozpoczecia = start,
                GodzinaZakonczenia = end,
                Sala = room
            });
            await context.SaveChangesAsync();
        }
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
