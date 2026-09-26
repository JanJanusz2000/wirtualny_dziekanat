using wirtualny_dziekanat.Data;
using wirtualny_dziekanat.Data.Entities;

namespace wirtualny_dziekanat.Services;

public interface IAdminService
{
    Task<List<ApplicationUser>> GetAllAdminsAsync();

    Task CreateAdminAsync(string email, string password);

    Task DeleteAdminAsync(string userId);

    Task<TeacherFormViewModel?> GetTeacherForEditAsync(int teacherId);

    Task SaveTeacherAsync(TeacherFormViewModel model);

    Task DeleteTeacherAsync(int teacherId);

    Task<List<Teacher>> GetAllTeachersAsync();

    Task<IReadOnlyList<Student>> GetAllStudentsAsync(
        CancellationToken cancellationToken = default);

    Task<StudentFormViewModel?> GetStudentForEditAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task SaveStudentAsync(
        StudentFormViewModel model,
        CancellationToken cancellationToken = default);

    Task DeleteStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task AutoEnrollStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task<List<Subject>> GetAllSubjectsAsync();

    Task AddSubjectAsync(Subject subject);

    Task DeleteSubjectAsync(int id);

    Task<CreatedTeacherResult> CreateTeacherAsync(
        CreateTeacherRequest request,
        CancellationToken cancellationToken = default);

    Task<Subject> AddSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default);

    Task<List<Grade>> GetAllGradesAsync(
        CancellationToken cancellationToken = default);

    Task<AdminSystemStats> GetSystemStatsAsync();

    Task<IReadOnlyList<Student>> GetStudentsAsync(
        CancellationToken cancellationToken = default);

    Task<List<Kierunek>> GetAllKierunkiAsync();

    Task AddKierunekAsync(Kierunek kierunek);

    Task DeleteKierunekAsync(int id);
}
