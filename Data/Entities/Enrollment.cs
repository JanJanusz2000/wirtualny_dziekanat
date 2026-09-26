namespace wirtualny_dziekanat.Data.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int SubjectId { get; set; }

    public Student Student { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
