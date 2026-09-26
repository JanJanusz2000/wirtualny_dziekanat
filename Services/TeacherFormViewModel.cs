using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Services;

public sealed class TeacherFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Email jest wymagany.")]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres email.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć od 6 do 100 znaków.")]
    [DataType(DataType.Password)]
    public string Haslo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Imię jest wymagane.")]
    [StringLength(100)]
    public string Imie { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane.")]
    [StringLength(100)]
    public string Nazwisko { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tytuł naukowy jest wymagany.")]
    [StringLength(100)]
    public string TytulNaukowy { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id is null && string.IsNullOrWhiteSpace(Haslo))
        {
            yield return new ValidationResult(
                "Hasło jest wymagane przy dodawaniu nauczyciela.",
                [nameof(Haslo)]);
        }
    }
}
