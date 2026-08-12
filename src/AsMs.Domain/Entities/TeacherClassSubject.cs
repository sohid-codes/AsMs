namespace AsMs.Domain.Entities;

public class TeacherClassSubject
{
    public int Id { get; set; }
    public string TeacherId { get; set; } = null!;
    public int AcademicClassId { get; set; }
    public int SubjectId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public AcademicClass AcademicClass { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
