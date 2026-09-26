namespace wirtualny_dziekanat.Services;

using wirtualny_dziekanat.Data.Entities;

public interface ITeacherService
{
    Task<List<Subject>> GetTeacherSubjectsWithDetailsAsync(
        string teacherUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherSubjectDto>> GetTeacherSubjectsAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentGradeDto>> GetStudentsForSubjectAsync(
        int subjectId,
        CancellationToken cancellationToken = default);

    Task SaveGradeAsync(
        int enrollmentId,
        decimal wartoscOceny,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeacherDashboardInfo>> GetTeacherDashboardInfoAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
