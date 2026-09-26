namespace wirtualny_dziekanat.Services;

public sealed class StudentGradeDto
{
    public int EnrollmentId { get; set; }
    public string Imie { get; set; } = string.Empty;
    public string Nazwisko { get; set; } = string.Empty;
    public string NumerIndeksu { get; set; } = string.Empty;
    public decimal? WartoscOceny { get; set; }
    public int? GradeId { get; set; }
}
