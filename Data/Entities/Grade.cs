using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Data.Entities;

public class Grade
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }

    [Range(2.0, 5.0)]
    public decimal WartoscOceny { get; set; }

    public DateTime DataWystawienia { get; set; }

    public Enrollment Enrollment { get; set; } = null!;
}
