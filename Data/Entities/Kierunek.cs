using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Data.Entities;

public class Kierunek
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Nazwa { get; set; } = string.Empty;

    public ICollection<Grupa> Grupy { get; set; } = new List<Grupa>();
    public ICollection<Student> Studenci { get; set; } = new List<Student>();
}
