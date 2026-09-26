using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Data.Entities;

public class Grupa
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Nazwa { get; set; } = string.Empty;

    public int KierunekId { get; set; }

    [Range(1, 20)]
    public int Semestr { get; set; }

    public Kierunek Kierunek { get; set; } = null!;
    public ICollection<Student> Studenci { get; set; } = new List<Student>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
