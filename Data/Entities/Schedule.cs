using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Data.Entities;

public class Schedule
{
    public int Id { get; set; }
    public int GrupaId { get; set; }
    public int SubjectId { get; set; }
    public DayOfWeek DzienTygodnia { get; set; }
    public TimeOnly GodzinaRozpoczecia { get; set; }
    public TimeOnly GodzinaZakonczenia { get; set; }

    [Required, StringLength(50)]
    public string Sala { get; set; } = string.Empty;

    public Grupa Grupa { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
