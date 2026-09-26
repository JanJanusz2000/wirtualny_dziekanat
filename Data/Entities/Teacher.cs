using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Data.Entities;

public class Teacher
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Imie { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Nazwisko { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string TytulNaukowy { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
