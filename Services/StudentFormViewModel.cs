using System.ComponentModel.DataAnnotations;

namespace wirtualny_dziekanat.Services;

public sealed class StudentFormViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Imię jest wymagane.")]
    [StringLength(100)]
    public string Imie { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane.")]
    [StringLength(100)]
    public string Nazwisko { get; set; } = string.Empty;

    [Required(ErrorMessage = "Numer indeksu jest wymagany.")]
    [StringLength(20)]
    public string NumerIndeksu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email jest wymagany.")]
    [EmailAddress(ErrorMessage = "Podaj prawidłowy adres email.")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string Haslo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Wybierz kierunek.")]
    public int KierunekId { get; set; }

    [Range(1, 20, ErrorMessage = "Semestr musi mieścić się w zakresie od 1 do 20.")]
    public int AktualnySemestr { get; set; } = 1;

    [Required(ErrorMessage = "Tryb studiów jest wymagany.")]
    [StringLength(20)]
    public string TrybStudiow { get; set; } = "Dzienne";

    public bool CzyCzesneOplacone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id is null && string.IsNullOrWhiteSpace(Haslo))
        {
            yield return new ValidationResult(
                "Hasło jest wymagane przy dodawaniu studenta.",
                [nameof(Haslo)]);
        }
    }
}
