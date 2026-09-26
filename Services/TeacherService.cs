using Microsoft.EntityFrameworkCore;
using wirtualny_dziekanat.Data;
using wirtualny_dziekanat.Data.Entities;

namespace wirtualny_dziekanat.Services;

public sealed class TeacherService(IDbContextFactory<ApplicationDbContext> contextFactory) : ITeacherService
{
    public async Task<List<Subject>> GetTeacherSubjectsWithDetailsAsync(
        string teacherUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teacherUserId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Subjects
            .AsNoTracking()
            .Where(subject => subject.Teacher.ApplicationUserId == teacherUserId)
            .Include(subject => subject.Enrollments)
                .ThenInclude(enrollment => enrollment.Student)
            .Include(subject => subject.Enrollments)
                .ThenInclude(enrollment => enrollment.Grades)
            .OrderBy(subject => subject.Nazwa)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherSubjectDto>> GetTeacherSubjectsAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationUserId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Subjects
            .AsNoTracking()
            .Where(subject => subject.Teacher.ApplicationUserId == applicationUserId)
            .OrderBy(subject => subject.Nazwa)
            .Select(subject => new TeacherSubjectDto(subject.Id, subject.Nazwa))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentGradeDto>> GetStudentsForSubjectAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subjectId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.SubjectId == subjectId)
            .OrderBy(enrollment => enrollment.Student.Nazwisko)
            .ThenBy(enrollment => enrollment.Student.Imie)
            .Select(enrollment => new StudentGradeDto
            {
                EnrollmentId = enrollment.Id,
                Imie = enrollment.Student.Imie,
                Nazwisko = enrollment.Student.Nazwisko,
                NumerIndeksu = enrollment.Student.NumerIndeksu,
                GradeId = enrollment.Grades
                    .OrderByDescending(grade => grade.DataWystawienia)
                    .Select(grade => (int?)grade.Id)
                    .FirstOrDefault(),
                WartoscOceny = enrollment.Grades
                    .OrderByDescending(grade => grade.DataWystawienia)
                    .Select(grade => (decimal?)grade.WartoscOceny)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task SaveGradeAsync(
        int enrollmentId,
        decimal wartoscOceny,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(enrollmentId);

        if (wartoscOceny < 2.0m || wartoscOceny > 5.0m)
        {
            throw new ArgumentOutOfRangeException(nameof(wartoscOceny), "Ocena musi mieścić się w zakresie od 2,0 do 5,0.");
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var enrollmentExists = await context.Enrollments
            .AsNoTracking()
            .AnyAsync(enrollment => enrollment.Id == enrollmentId, cancellationToken);

        if (!enrollmentExists)
        {
            throw new InvalidOperationException("Nie znaleziono zapisu studenta na przedmiot.");
        }

        var grade = await context.Grades
            .OrderByDescending(item => item.DataWystawienia)
            .FirstOrDefaultAsync(item => item.EnrollmentId == enrollmentId, cancellationToken);

        if (grade is null)
        {
            context.Grades.Add(new Grade
            {
                EnrollmentId = enrollmentId,
                WartoscOceny = wartoscOceny,
                DataWystawienia = DateTime.UtcNow
            });
        }
        else
        {
            grade.WartoscOceny = wartoscOceny;
            grade.DataWystawienia = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherDashboardInfo>> GetTeacherDashboardInfoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var subjects = await context.Subjects
            .AsNoTracking()
            .Where(subject => subject.Teacher.ApplicationUserId == userId)
            .Include(subject => subject.Schedules)
                .ThenInclude(schedule => schedule.Grupa)
            .OrderBy(subject => subject.Nazwa)
            .ToListAsync(cancellationToken);

        return subjects
            .Select(subject => new TeacherDashboardInfo(
                subject.Id,
                subject.Nazwa,
                subject.Schedules
                    .Select(schedule => schedule.Grupa.Nazwa)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .ToList()))
            .ToList();
    }
}
