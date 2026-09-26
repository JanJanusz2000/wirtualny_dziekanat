using wirtualny_dziekanat.Data.Entities;

namespace wirtualny_dziekanat.Services;

public interface IStudentService
{
    Task PayTuitionAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default);

    Task AdvanceToNextSemestrAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentSubjectDto>> GetAvailableSubjectsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentSubjectDto>> GetMySubjectsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task EnrollInSubjectAsync(
        string userId,
        int subjectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentGradeInfoDto>> GetMyGradesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<StudentDashboardInfo?> GetStudentDashboardInfoAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Student?> GetStudentProfileAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Schedule>> GetStudentScheduleAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Grade>> GetStudentGradesAsync(
        string applicationUserId,
        CancellationToken cancellationToken = default);
}
