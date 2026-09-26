using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using wirtualny_dziekanat.Data;
using wirtualny_dziekanat.Data.Entities;

namespace wirtualny_dziekanat.Services;

public sealed class AdminService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IAdminService
{
    private const string MainAdminEmail = "admin@uczelnia.pl";

    public async Task<List<ApplicationUser>> GetAllAdminsAsync()
    {
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        return admins
            .OrderBy(user => user.Email)
            .ToList();
    }

    public async Task CreateAdminAsync(string email, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        email = email.Trim();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
            EnsureIdentitySuccess(roleResult, "utworzenia roli Admin");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        EnsureIdentitySuccess(createResult, "utworzenia konta administratora");

        try
        {
            var roleResult = await userManager.AddToRoleAsync(user, "Admin");
            EnsureIdentitySuccess(roleResult, "przypisania roli Admin");
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }
    }

    public async Task DeleteAdminAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return;
        }

        if (string.Equals(user.Email, MainAdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Nie można usunąć głównego administratora systemu.");
        }

        if (await userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await userManager.DeleteAsync(user);
            EnsureIdentitySuccess(result, "usunięcia konta administratora");
        }
    }

    public async Task<TeacherFormViewModel?> GetTeacherForEditAsync(int teacherId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(teacherId);

        await using var context = await contextFactory.CreateDbContextAsync();
        var teacher = await context.Teachers
            .AsNoTracking()
            .Include(item => item.ApplicationUser)
            .SingleOrDefaultAsync(item => item.Id == teacherId);

        return teacher is null
            ? null
            : new TeacherFormViewModel
            {
                Id = teacher.Id,
                Email = teacher.ApplicationUser.Email ?? string.Empty,
                Imie = teacher.Imie,
                Nazwisko = teacher.Nazwisko,
                TytulNaukowy = teacher.TytulNaukowy
            };
    }

    public async Task SaveTeacherAsync(TeacherFormViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var email = model.Email.Trim();
        var firstName = model.Imie.Trim();
        var lastName = model.Nazwisko.Trim();
        var title = model.TytulNaukowy.Trim();
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (model.Id is null)
        {
            if (string.IsNullOrWhiteSpace(model.Haslo))
            {
                throw new InvalidOperationException("Hasło jest wymagane przy tworzeniu nauczyciela.");
            }

            if (!await roleManager.RoleExistsAsync("Teacher"))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole("Teacher"));
                EnsureIdentitySuccess(roleResult, "utworzenia roli Teacher");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, model.Haslo);
            EnsureIdentitySuccess(createResult, "utworzenia konta nauczyciela");

            try
            {
                var roleResult = await userManager.AddToRoleAsync(user, "Teacher");
                EnsureIdentitySuccess(roleResult, "przypisania roli Teacher");

                await using var context = await contextFactory.CreateDbContextAsync();
                context.Teachers.Add(new Teacher
                {
                    ApplicationUserId = user.Id,
                    Imie = firstName,
                    Nazwisko = lastName,
                    TytulNaukowy = title
                });
                await context.SaveChangesAsync();
            }
            catch
            {
                await userManager.DeleteAsync(user);
                throw;
            }

            return;
        }

        string applicationUserId;
        string? currentEmail;

        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            var teacherToEdit = await context.Teachers
                .SingleOrDefaultAsync(item => item.Id == model.Id.Value);

            if (teacherToEdit is null)
            {
                throw new InvalidOperationException("Nie znaleziono nauczyciela.");
            }

            applicationUserId = teacherToEdit.ApplicationUserId;

            var applicationUser = await userManager.FindByIdAsync(applicationUserId);
            if (applicationUser is null)
            {
                throw new InvalidOperationException("Nauczyciel nie ma powiązanego konta Identity.");
            }

            currentEmail = applicationUser.Email;
            teacherToEdit.Imie = firstName;
            teacherToEdit.Nazwisko = lastName;
            teacherToEdit.TytulNaukowy = title;
            await context.SaveChangesAsync();
        }

        var userToUpdate = await userManager.FindByIdAsync(applicationUserId);
        if (userToUpdate is null)
        {
            throw new InvalidOperationException("Nauczyciel nie ma powiązanego konta Identity.");
        }

        if (!string.Equals(currentEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            var userWithEmail = await userManager.FindByEmailAsync(email);
            if (userWithEmail is not null && userWithEmail.Id != userToUpdate.Id)
            {
                throw new InvalidOperationException("Podany adres email jest już zajęty.");
            }

            var emailResult = await userManager.SetEmailAsync(userToUpdate, email);
            EnsureIdentitySuccess(emailResult, "zmiany adresu email");

            var userNameResult = await userManager.SetUserNameAsync(userToUpdate, email);
            EnsureIdentitySuccess(userNameResult, "zmiany loginu");
        }

        if (!string.IsNullOrWhiteSpace(model.Haslo))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(userToUpdate);
            var passwordResult = await userManager.ResetPasswordAsync(userToUpdate, resetToken, model.Haslo);
            EnsureIdentitySuccess(passwordResult, "zmiany hasła");
        }
    }

    public async Task<StudentFormViewModel?> GetStudentForEditAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(studentId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var student = await context.Students
            .AsNoTracking()
            .Include(item => item.ApplicationUser)
            .SingleOrDefaultAsync(item => item.Id == studentId, cancellationToken);

        return student is null
            ? null
            : new StudentFormViewModel
            {
                Id = student.Id,
                Imie = student.Imie,
                Nazwisko = student.Nazwisko,
                NumerIndeksu = student.NumerIndeksu,
                Email = student.ApplicationUser?.Email ?? string.Empty,
                KierunekId = student.KierunekId,
                AktualnySemestr = student.AktualnySemestr,
                TrybStudiow = student.TrybStudiow,
                CzyCzesneOplacone = student.CzyCzesneOplacone
            };
    }

    public async Task SaveStudentAsync(
        StudentFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var firstName = model.Imie.Trim();
        var lastName = model.Nazwisko.Trim();
        var indexNumber = model.NumerIndeksu.Trim();
        var email = model.Email.Trim();
        var studyMode = model.TrybStudiow.Trim();

        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(studyMode);

        if (model.KierunekId <= 0)
        {
            throw new InvalidOperationException("Wybierz kierunek studiów.");
        }

        if (model.Id is null)
        {
            if (string.IsNullOrWhiteSpace(model.Haslo))
            {
                throw new InvalidOperationException("Hasło jest wymagane przy tworzeniu studenta.");
            }

            ApplicationUser user;
            await using (var context = await contextFactory.CreateDbContextAsync(cancellationToken))
            {
                if (!await context.Kierunki.AnyAsync(item => item.Id == model.KierunekId, cancellationToken))
                {
                    throw new InvalidOperationException("Wybrany kierunek nie istnieje.");
                }

                if (await context.Students.AnyAsync(item => item.NumerIndeksu == indexNumber, cancellationToken))
                {
                    throw new InvalidOperationException("Podany numer indeksu jest już zajęty.");
                }

                var groupId = await context.Grupy
                    .Where(item => item.KierunekId == model.KierunekId)
                    .OrderBy(item => item.Id)
                    .Select(item => (int?)item.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (groupId is null)
                {
                    throw new InvalidOperationException("Wybrany kierunek nie ma przypisanej grupy.");
                }

                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, model.Haslo);
                EnsureIdentitySuccess(createResult, "utworzenia konta studenta");

                try
                {
                    if (!await roleManager.RoleExistsAsync("Student"))
                    {
                        var roleResult = await roleManager.CreateAsync(new IdentityRole("Student"));
                        EnsureIdentitySuccess(roleResult, "utworzenia roli Student");
                    }

                    var roleAssignmentResult = await userManager.AddToRoleAsync(user, "Student");
                    EnsureIdentitySuccess(roleAssignmentResult, "przypisania roli Student");

                    context.Students.Add(new Student
                    {
                        ApplicationUserId = user.Id,
                        NumerIndeksu = indexNumber,
                        Imie = firstName,
                        Nazwisko = lastName,
                        Semestr = 1,
                        KierunekId = model.KierunekId,
                        GrupaId = groupId.Value,
                        TrybStudiow = studyMode,
                        CzyCzesneOplacone = model.CzyCzesneOplacone
                    });
                    await context.SaveChangesAsync(cancellationToken);

                    var createdStudentId = await context.Students
                        .AsNoTracking()
                        .Where(item => item.ApplicationUserId == user.Id)
                        .Select(item => (int?)item.Id)
                        .SingleAsync(cancellationToken);

                    await AutoEnrollStudentAsync(createdStudentId.Value, cancellationToken);
                }
                catch
                {
                    await userManager.DeleteAsync(user);
                    throw;
                }
            }

            return;
        }

        string applicationUserId;
        string? currentEmail;

        await using (var context = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            var student = await context.Students
                .SingleOrDefaultAsync(item => item.Id == model.Id.Value, cancellationToken);

            if (student is null)
            {
                throw new InvalidOperationException("Nie znaleziono studenta.");
            }

            if (!await context.Kierunki.AnyAsync(item => item.Id == model.KierunekId, cancellationToken))
            {
                throw new InvalidOperationException("Wybrany kierunek nie istnieje.");
            }

            if (await context.Students.AnyAsync(
                    item => item.Id != student.Id && item.NumerIndeksu == indexNumber,
                    cancellationToken))
            {
                throw new InvalidOperationException("Podany numer indeksu jest już zajęty.");
            }

            applicationUserId = student.ApplicationUserId;
            var user = await userManager.FindByIdAsync(applicationUserId);
            if (user is null)
            {
                throw new InvalidOperationException("Student nie ma powiązanego konta Identity.");
            }

            currentEmail = user.Email;
            student.Imie = firstName;
            student.Nazwisko = lastName;
            student.NumerIndeksu = indexNumber;
            student.KierunekId = model.KierunekId;
            student.AktualnySemestr = model.AktualnySemestr;
            student.TrybStudiow = studyMode;
            student.CzyCzesneOplacone = model.CzyCzesneOplacone;
            await context.SaveChangesAsync(cancellationToken);
        }

        var userToUpdate = await userManager.FindByIdAsync(applicationUserId);
        if (userToUpdate is null)
        {
            throw new InvalidOperationException("Student nie ma powiązanego konta Identity.");
        }

        if (!string.Equals(currentEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            var userWithEmail = await userManager.FindByEmailAsync(email);
            if (userWithEmail is not null && userWithEmail.Id != userToUpdate.Id)
            {
                throw new InvalidOperationException("Podany adres email jest już zajęty.");
            }

            var emailResult = await userManager.SetEmailAsync(userToUpdate, email);
            EnsureIdentitySuccess(emailResult, "zmiany adresu email");

            var userNameResult = await userManager.SetUserNameAsync(userToUpdate, email);
            EnsureIdentitySuccess(userNameResult, "zmiany loginu");
        }

        if (!string.IsNullOrWhiteSpace(model.Haslo))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(userToUpdate);
            var passwordResult = await userManager.ResetPasswordAsync(userToUpdate, resetToken, model.Haslo);
            EnsureIdentitySuccess(passwordResult, "zmiany hasła");
        }
    }

    public async Task AutoEnrollStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(studentId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var student = await context.Students
            .AsNoTracking()
            .Where(item => item.Id == studentId)
            .Select(item => new { item.Id, item.KierunekId })
            .SingleOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            throw new InvalidOperationException("Nie znaleziono studenta.");
        }

        var subjectIds = await context.Subjects
            .AsNoTracking()
            .Where(item => item.KierunekId == student.KierunekId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        if (subjectIds.Count == 0)
        {
            return;
        }

        var enrolledSubjectIds = await context.Enrollments
            .AsNoTracking()
            .Where(item => item.StudentId == student.Id && subjectIds.Contains(item.SubjectId))
            .Select(item => item.SubjectId)
            .ToListAsync(cancellationToken);

        var missingEnrollments = subjectIds
            .Except(enrolledSubjectIds)
            .Select(subjectId => new Enrollment
            {
                StudentId = student.Id,
                SubjectId = subjectId
            })
            .ToList();

        if (missingEnrollments.Count == 0)
        {
            return;
        }

        context.Enrollments.AddRange(missingEnrollments);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(studentId);

        string applicationUserId;
        await using (var context = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            var student = await context.Students
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == studentId, cancellationToken);

            if (student is null)
            {
                return;
            }

            applicationUserId = student.ApplicationUserId;
            context.Students.Remove(student);
            await context.SaveChangesAsync(cancellationToken);
        }

        var user = await userManager.FindByIdAsync(applicationUserId);
        if (user is not null)
        {
            var deleteResult = await userManager.DeleteAsync(user);
            EnsureIdentitySuccess(deleteResult, "usunięcia konta Identity studenta");
        }
    }

    public async Task DeleteTeacherAsync(int teacherId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(teacherId);

        string applicationUserId;
        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            var teacher = await context.Teachers
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == teacherId);

            if (teacher is null)
            {
                return;
            }

            applicationUserId = teacher.ApplicationUserId;
            context.Teachers.Remove(teacher);
            await context.SaveChangesAsync();
        }

        var user = await userManager.FindByIdAsync(applicationUserId);
        if (user is not null)
        {
            var deleteResult = await userManager.DeleteAsync(user);
            EnsureIdentitySuccess(deleteResult, "usunięcia konta Identity nauczyciela");
        }
    }

    public async Task<List<Teacher>> GetAllTeachersAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Teachers
            .AsNoTracking()
            .Include(teacher => teacher.ApplicationUser)
            .OrderBy(teacher => teacher.Nazwisko)
            .ThenBy(teacher => teacher.Imie)
            .ToListAsync();
    }

    public async Task<List<Subject>> GetAllSubjectsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Subjects
            .AsNoTracking()
            .Include(subject => subject.Teacher)
                .ThenInclude(teacher => teacher.ApplicationUser)
            .Include(subject => subject.Kierunek)
            .OrderBy(subject => subject.Nazwa)
            .ToListAsync();
    }

    public async Task AddSubjectAsync(Subject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject.Nazwa);

        await using var context = await contextFactory.CreateDbContextAsync();

        if (!await context.Teachers.AnyAsync(teacher => teacher.Id == subject.TeacherId))
        {
            throw new InvalidOperationException("Wybrany nauczyciel nie istnieje.");
        }

        if (!await context.Kierunki.AnyAsync(kierunek => kierunek.Id == subject.KierunekId))
        {
            throw new InvalidOperationException("Wybrany kierunek nie istnieje.");
        }

        if (subject.PunktyECTS is < 1 or > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(subject.PunktyECTS), "ECTS musi mieścić się w zakresie od 1 do 30.");
        }

        if (subject.Semestr is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(subject.Semestr), "Semestr musi mieścić się w zakresie od 1 do 20.");
        }

        subject.Nazwa = subject.Nazwa.Trim();
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();
    }

    public async Task DeleteSubjectAsync(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        await using var context = await contextFactory.CreateDbContextAsync();
        var subject = await context.Subjects.FindAsync(id);
        if (subject is null)
        {
            return;
        }

        context.Subjects.Remove(subject);
        await context.SaveChangesAsync();
    }

    public async Task<CreatedTeacherResult> CreateTeacherAsync(
        CreateTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var firstName = request.Imie.Trim();
        var lastName = request.Nazwisko.Trim();
        var title = request.TytulNaukowy.Trim();
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var email = $"{ToEmailPart(firstName)}.{ToEmailPart(lastName)}@uczelnia.pl";
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException($"Konto o adresie {email} już istnieje.");
        }

        if (!await roleManager.RoleExistsAsync("Teacher"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("Teacher"));
            EnsureIdentitySuccess(roleResult, "utworzenia roli Teacher");
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, temporaryPassword);
        EnsureIdentitySuccess(createResult, "utworzenia konta nauczyciela");

        try
        {
            var roleResult = await userManager.AddToRoleAsync(user, "Teacher");
            EnsureIdentitySuccess(roleResult, "przypisania roli Teacher");

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            context.Teachers.Add(new Teacher
            {
                ApplicationUserId = user.Id,
                Imie = firstName,
                Nazwisko = lastName,
                TytulNaukowy = title
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }

        return new CreatedTeacherResult(email, temporaryPassword);
    }

    public async Task<Subject> AddSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Nazwa);

        if (request.GodzinaZakonczenia <= request.GodzinaRozpoczecia)
        {
            throw new ArgumentException("Godzina zakończenia musi być późniejsza niż rozpoczęcia.");
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        if (!await context.Teachers.AnyAsync(teacher => teacher.Id == request.TeacherId, cancellationToken))
        {
            throw new InvalidOperationException("Wybrany nauczyciel nie istnieje.");
        }

        if (!await context.Grupy.AnyAsync(group => group.Id == request.GrupaId, cancellationToken))
        {
            throw new InvalidOperationException("Wybrana grupa nie istnieje.");
        }

        if (!await context.Kierunki.AnyAsync(kierunek => kierunek.Id == request.KierunekId, cancellationToken))
        {
            throw new InvalidOperationException("Wybrany kierunek nie istnieje.");
        }

        var subject = new Subject
        {
            Nazwa = request.Nazwa.Trim(),
            TeacherId = request.TeacherId,
            PunktyECTS = request.PunktyECTS,
            KierunekId = request.KierunekId
        };
        context.Subjects.Add(subject);
        context.Schedules.Add(new Schedule
        {
            GrupaId = request.GrupaId,
            Subject = subject,
            DzienTygodnia = request.DzienTygodnia,
            GodzinaRozpoczecia = request.GodzinaRozpoczecia,
            GodzinaZakonczenia = request.GodzinaZakonczenia,
            Sala = request.Sala.Trim()
        });

        await context.SaveChangesAsync(cancellationToken);
        return subject;
    }

    public async Task<List<Grade>> GetAllGradesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Grades
            .AsNoTracking()
            .Include(grade => grade.Enrollment)
                .ThenInclude(enrollment => enrollment.Student)
                .ThenInclude(student => student.Kierunek)
            .Include(grade => grade.Enrollment)
                .ThenInclude(enrollment => enrollment.Subject)
            .OrderBy(grade => grade.Enrollment.Student.Nazwisko)
            .ThenBy(grade => grade.Enrollment.Student.Imie)
            .ThenBy(grade => grade.Enrollment.Subject.Nazwa)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminSystemStats> GetSystemStatsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return new AdminSystemStats(
            await context.Students.CountAsync(),
            await context.Teachers.CountAsync(),
            await context.Kierunki.CountAsync());
    }

    public Task<IReadOnlyList<Student>> GetAllStudentsAsync(
        CancellationToken cancellationToken = default) =>
        GetStudentsAsync(cancellationToken);

    public async Task<IReadOnlyList<Student>> GetStudentsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Students
            .AsNoTracking()
            .Include(student => student.ApplicationUser)
            .Include(student => student.Kierunek)
            .Include(student => student.Grupa)
            .OrderBy(student => student.Nazwisko)
            .ThenBy(student => student.Imie)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Kierunek>> GetAllKierunkiAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Kierunki
            .AsNoTracking()
            .OrderBy(kierunek => kierunek.Nazwa)
            .ToListAsync();
    }

    public async Task AddKierunekAsync(Kierunek kierunek)
    {
        ArgumentNullException.ThrowIfNull(kierunek);
        ArgumentException.ThrowIfNullOrWhiteSpace(kierunek.Nazwa);

        await using var context = await contextFactory.CreateDbContextAsync();

        kierunek.Nazwa = kierunek.Nazwa.Trim();
        context.Kierunki.Add(kierunek);
        await context.SaveChangesAsync();
    }

    public async Task DeleteKierunekAsync(int id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        await using var context = await contextFactory.CreateDbContextAsync();

        var kierunek = await context.Kierunki.FindAsync(id);
        if (kierunek is null)
        {
            return;
        }

        context.Kierunki.Remove(kierunek);
        await context.SaveChangesAsync();
    }

    private static string ToEmailPart(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var characters = normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .ToArray();

        var result = new string(characters).ToLowerInvariant();
        return string.IsNullOrWhiteSpace(result)
            ? throw new ArgumentException("Imię i nazwisko muszą zawierać litery lub cyfry.")
            : result;
    }

    private static string GenerateTemporaryPassword() =>
        $"Teacher{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}!9";

    private static void EnsureIdentitySuccess(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Błąd podczas {operation}: {errors}");
        }
    }
}
