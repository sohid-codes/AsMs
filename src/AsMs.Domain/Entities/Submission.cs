using AsMs.Domain.Enums;

namespace AsMs.Domain.Entities;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string StudentId { get; set; } = null!;
    public string AnswerText { get; set; } = null!;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? MarkedAtUtc { get; set; }
    public decimal? Marks { get; set; }
    public string? Feedback { get; set; }
    public string? MarkedByTeacherId { get; set; }

    public Assignment Assignment { get; set; } = null!;
}
