namespace AsMs.Domain.Entities;

public class StudentEnrollment
{
    public int Id { get; set; }
    public string StudentId { get; set; } = null!;
    public int AcademicClassId { get; set; }
    public DateTime EnrolledAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public AcademicClass AcademicClass { get; set; } = null!;
}
