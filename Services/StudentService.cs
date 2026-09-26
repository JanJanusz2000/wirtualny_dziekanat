using Microsoft.EntityFrameworkCore;
using wirtualny_dziekanat.Data;
using wirtualny_dziekanat.Data.Entities;

namespace wirtualny_dziekanat.Services;

public sealed class StudentService(IDbContextFactory<ApplicationDbContext> contextFactory) : IStudentService
{
    public async Task PayTuitionAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationUserId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var student = await context.Students
            .SingleOrDefaultAsync(item => item.ApplicationUserId == applicationUserId, cancellationToken);

        if (student is null)
        {
            throw new InvalidOperationException("Nie znaleziono profilu studenta.");
        }

        student.CzyCzesneOplacone = true;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AdvanceToNextSemestrAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var student = await context.Students
            .SingleOrDefaultAsync(item => item.ApplicationUserId == userId, cancellationToken);

        if (student is null)
        {
            throw new InvalidOperationException("Nie znaleziono profilu studenta.");
        }

        if (student.AktualnySemestr >= 20)
        {
            throw new InvalidOperationException("Student znajduje się już na najwyższym obsługiwanym semestrze.");
        }

        student.AktualnySemestr++;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentSubjectDto>> GetAvailableSubjectsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var directionId = await context.Students
            .AsNoTracking()
            .Where(student => student.ApplicationUserId == userId)
            .Select(student => (int?)student.KierunekId)
            .SingleOrDefaultAsync(cancellationToken);

        if (directionId is null)
        {
            return [];
        }

        var currentSemester = await context.Students
            .AsNoTracking()
            .Where(student => student.ApplicationUserId == userId)
            .Select(student => (int?)student.AktualnySemestr)
            .SingleOrDefaultAsync(cancellationToken);

        if (currentSemester is null)
        {
            return [];
        }

        return await context.Subjects
            .AsNoTracking()
            .Where(subject => subject.KierunekId == directionId.Value)
            .Where(subject => subject.Semestr == currentSemester.Value)
            .Where(subject => !context.Enrollments.Any(enrollment =>
                enrollment.SubjectId == subject.Id && enrollment.Student.ApplicationUserId == userId))
            .OrderBy(subject => subject.Nazwa)
            .Select(subject => new StudentSubjectDto(subject.Id, subject.Nazwa, subject.PunktyECTS, subject.Semestr))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentSubjectDto>> GetMySubjectsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.Student.ApplicationUserId == userId)
            .OrderBy(enrollment => enrollment.Subject.Nazwa)
            .Select(enrollment => new StudentSubjectDto(
                enrollment.Subject.Id,
                enrollment.Subject.Nazwa,
                enrollment.Subject.PunktyECTS,
                enrollment.Subject.Semestr))
            .ToListAsync(cancellationToken);
    }

    public async Task EnrollInSubjectAsync(
        string userId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subjectId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var student = await context.Students
            .AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .Select(item => new { item.Id, item.KierunekId, item.AktualnySemestr })
            .SingleOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            throw new InvalidOperationException("Nie znaleziono profilu studenta.");
        }

        var subjectExistsForDirection = await context.Subjects
            .AsNoTracking()
            .AnyAsync(item => item.Id == subjectId
                && item.KierunekId == student.KierunekId
                && item.Semestr == student.AktualnySemestr, cancellationToken);

        if (!subjectExistsForDirection)
        {
            throw new InvalidOperationException("Wybrany przedmiot nie należy do kierunku studenta.");
        }

        var alreadyEnrolled = await context.Enrollments
            .AsNoTracking()
            .AnyAsync(item => item.StudentId == student.Id && item.SubjectId == subjectId, cancellationToken);

        if (alreadyEnrolled)
        {
            return;
        }

        context.Enrollments.Add(new Enrollment
        {
            StudentId = student.Id,
            SubjectId = subjectId
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentGradeInfoDto>> GetMyGradesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.Student.ApplicationUserId == userId)
            .OrderBy(enrollment => enrollment.Subject.Nazwa)
            .Select(enrollment => new StudentGradeInfoDto(
                enrollment.Id,
                enrollment.Subject.Nazwa,
                enrollment.Subject.PunktyECTS,
                enrollment.Subject.Teacher.Imie,
                enrollment.Subject.Teacher.Nazwisko,
                enrollment.Grades
                    .OrderByDescending(grade => grade.DataWystawienia)
                    .Select(grade => (decimal?)grade.WartoscOceny)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentDashboardInfo?> GetStudentDashboardInfoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Students
            .AsNoTracking()
            .Where(student => student.ApplicationUserId == userId)
            .Select(student => new StudentDashboardInfo(
                student.Imie,
                student.Nazwisko,
                student.Kierunek.Nazwa,
                student.TrybStudiow,
                student.CzyCzesneOplacone,
                student.AktualnySemestr))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Student?> GetStudentProfileAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Students
            .AsNoTracking()
            .Include(student => student.Kierunek)
            .Include(student => student.Grupa)
            .SingleOrDefaultAsync(
                student => student.ApplicationUserId == applicationUserId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Schedule>> GetStudentScheduleAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var groupId = await context.Students
            .AsNoTracking()
            .Where(student => student.ApplicationUserId == applicationUserId)
            .Select(student => (int?)student.GrupaId)
            .SingleOrDefaultAsync(cancellationToken);

        if (groupId is null)
        {
            return [];
        }

        return await context.Schedules
            .AsNoTracking()
            .Include(schedule => schedule.Subject)
            .Where(schedule => schedule.GrupaId == groupId.Value)
            .OrderBy(schedule => schedule.DzienTygodnia)
            .ThenBy(schedule => schedule.GodzinaRozpoczecia)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Grade>> GetStudentGradesAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Grades
            .AsNoTracking()
            .Include(grade => grade.Enrollment)
                .ThenInclude(enrollment => enrollment.Subject)
            .Where(grade => grade.Enrollment.Student.ApplicationUserId == applicationUserId)
            .OrderByDescending(grade => grade.DataWystawienia)
            .ToListAsync(cancellationToken);
    }
}
