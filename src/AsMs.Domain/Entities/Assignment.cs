using AsMs.Domain.Enums;

namespace AsMs.Domain.Entities;

public class Assignment
{
    public int Id { get; set; }
    public int TeacherClassSubjectId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime DeadlineUtc { get; set; }
    public decimal MaximumMarks { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public TeacherClassSubject TeacherClassSubject { get; set; } = null!;
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
