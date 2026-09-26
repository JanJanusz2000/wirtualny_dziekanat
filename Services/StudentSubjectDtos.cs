namespace wirtualny_dziekanat.Services;

public sealed record StudentSubjectDto(
    int Id,
    string Nazwa,
    int PunktyECTS,
    int Semestr);

public sealed record StudentGradeInfoDto(
    int EnrollmentId,
    string SubjectName,
    int PunktyECTS,
    string TeacherFirstName,
    string TeacherLastName,
    decimal? WartoscOceny);
