using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Data.Entities;

public class Student
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string NumerIndeksu { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Imie { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Nazwisko { get; set; } = string.Empty;

    [Range(1, 20)]
    public int Semestr { get; set; }

    [Range(1, 20)]
    public int AktualnySemestr { get; set; } = 1;

    [Required, StringLength(20)]
    public string TrybStudiow { get; set; } = "Dzienne";

    public bool CzyCzesneOplacone { get; set; }

    public int KierunekId { get; set; }
    public int GrupaId { get; set; }

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public Kierunek Kierunek { get; set; } = null!;
    public Grupa Grupa { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
