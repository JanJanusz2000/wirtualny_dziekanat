using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Data.Entities;

public class Subject
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Nazwa { get; set; } = string.Empty;

    public int TeacherId { get; set; }

    public Teacher Teacher { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
