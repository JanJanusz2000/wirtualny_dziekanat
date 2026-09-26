using System.ComponentModel.DataAnnotations;
using System;

namespace wirtualny_dziekanat.Services;

public sealed record AdminSystemStats(
    int StudentCount,
    int TeacherCount,
    int KierunekCount);

public sealed record StudentDashboardInfo(
    string Imie,
    string Nazwisko,
    string KierunekNazwa,
    string TrybStudiow,
    bool CzyCzesneOplacone,
    int AktualnySemestr);

public sealed record TeacherDashboardInfo(
    int SubjectId,
    string SubjectName,
    IReadOnlyList<string> GroupNames);

public sealed record TeacherSubjectDto(int Id, string Nazwa);

public sealed class CreateTeacherRequest
{
    [Required(ErrorMessage = "Imiê jest wymagane.")]
    [StringLength(100)]
    public string Imie { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane.")]
    [StringLength(100)]
    public string Nazwisko { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tytu³ naukowy jest wymagany.")]
    [StringLength(100)]
    public string TytulNaukowy { get; set; } = string.Empty;
}

public sealed record CreatedTeacherResult(string Email, string TemporaryPassword);

public sealed class CreateSubjectRequest
{
    [Required, StringLength(200)]
    public string Nazwa { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int TeacherId { get; set; }

    [Range(1, 30)]
    public int PunktyECTS { get; set; }

    [Range(1, int.MaxValue)]
    public int KierunekId { get; set; }

    [Range(1, int.MaxValue)]
    public int GrupaId { get; set; }

    public DayOfWeek DzienTygodnia { get; set; } = DayOfWeek.Monday;
    public TimeOnly GodzinaRozpoczecia { get; set; } = new(8, 0);
    public TimeOnly GodzinaZakonczenia { get; set; } = new(9, 30);

    [Required, StringLength(50)]
    public string Sala { get; set; } = "Do ustalenia";
}