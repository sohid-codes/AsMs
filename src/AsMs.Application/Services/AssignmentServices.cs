using AsMs.Application.UnitOfWorks;
using AsMs.Domain.Entities;
using AsMs.Domain.Enums;

namespace AsMs.Application.Services;

public interface ITeacherAssignmentService
{
    Task<IReadOnlyList<TeacherClassSubject>> GetAllocationsAsync(string teacherId);
    Task<IReadOnlyList<Assignment>> GetAssignmentsAsync(string teacherId);
    Task<Assignment> CreateAssignmentAsync(string teacherId, int allocationId, string title, string description, DateTime deadlineUtc, decimal maximumMarks);
    Task<Assignment> UpdateAssignmentAsync(string teacherId, int id, string title, string description, DateTime deadlineUtc, decimal maximumMarks);
    Task DeleteAssignmentAsync(string teacherId, int id);
    Task<Assignment> PublishAssignmentAsync(string teacherId, int id);
    Task<IReadOnlyList<Submission>> GetSubmissionsAsync(string teacherId, int assignmentId);
    Task<Submission> GradeSubmissionAsync(string teacherId, int id, decimal marks, string? feedback);
}

public sealed class TeacherAssignmentService(IAsmsUnitOfWork unitOfWork) : ITeacherAssignmentService
{
    public Task<IReadOnlyList<TeacherClassSubject>> GetAllocationsAsync(string teacherId) => unitOfWork.TeacherClassSubjects.ListForTeacherAsync(teacherId);
    public Task<IReadOnlyList<Assignment>> GetAssignmentsAsync(string teacherId) => unitOfWork.Assignments.ListForTeacherAsync(teacherId);
    public async Task<Assignment> CreateAssignmentAsync(string teacherId, int allocationId, string title, string description, DateTime deadlineUtc, decimal maximumMarks)
    {
        ValidateAssignment(deadlineUtc, maximumMarks);
        var allocation = await unitOfWork.TeacherClassSubjects.FindActiveForTeacherAsync(allocationId, teacherId) ?? throw new ForbiddenException("You are not assigned to this class and subject.");
        var item = new Assignment { TeacherClassSubjectId = allocation.Id, Title = title, Description = description, DeadlineUtc = deadlineUtc, MaximumMarks = maximumMarks, Status = AssignmentStatus.Draft, CreatedAtUtc = DateTime.UtcNow };
        unitOfWork.Assignments.Add(item); await unitOfWork.SaveChangesAsync(); return item;
    }
    public async Task<Assignment> UpdateAssignmentAsync(string teacherId, int id, string title, string description, DateTime deadlineUtc, decimal maximumMarks)
    {
        ValidateAssignment(deadlineUtc, maximumMarks);
        var item = await GetOwnedAssignmentAsync(teacherId, id);
        item.Title = title; item.Description = description; item.DeadlineUtc = deadlineUtc; item.MaximumMarks = maximumMarks; item.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(); return item;
    }
    public async Task DeleteAssignmentAsync(string teacherId, int id)
    {
        var item = await GetOwnedAssignmentAsync(teacherId, id);
        if (await unitOfWork.Submissions.ExistsForAssignmentAsync(id)) throw new ConflictException("Assignments with submissions cannot be deleted.");
        unitOfWork.Assignments.Remove(item); await unitOfWork.SaveChangesAsync();
    }
    public async Task<Assignment> PublishAssignmentAsync(string teacherId, int id)
    {
        var item = await GetOwnedAssignmentAsync(teacherId, id);
        item.Status = AssignmentStatus.Published; item.PublishedAtUtc = DateTime.UtcNow; await unitOfWork.SaveChangesAsync(); return item;
    }
    public async Task<IReadOnlyList<Submission>> GetSubmissionsAsync(string teacherId, int assignmentId)
    {
        await GetOwnedAssignmentAsync(teacherId, assignmentId); return await unitOfWork.Submissions.ListForAssignmentAsync(assignmentId);
    }
    public async Task<Submission> GradeSubmissionAsync(string teacherId, int id, decimal marks, string? feedback)
    {
        var item = await unitOfWork.Submissions.FindForGradingAsync(id) ?? throw new NotFoundException("Submission was not found.");
        if (item.Assignment.TeacherClassSubject.TeacherId != teacherId) throw new ForbiddenException("You do not own this submission's assignment.");
        if (marks < 0 || marks > item.Assignment.MaximumMarks) throw new ValidationException("Marks are outside the assignment range.");
        item.Marks = marks; item.Feedback = feedback; item.MarkedAtUtc = DateTime.UtcNow; item.MarkedByTeacherId = teacherId; item.Status = SubmissionStatus.Graded;
        await unitOfWork.SaveChangesAsync(); return item;
    }
    private async Task<Assignment> GetOwnedAssignmentAsync(string teacherId, int id) => await unitOfWork.Assignments.FindOwnedByTeacherAsync(id, teacherId) ?? throw new NotFoundException("Assignment was not found.");
    private static void ValidateAssignment(DateTime deadlineUtc, decimal maximumMarks) { if (deadlineUtc <= DateTime.UtcNow || maximumMarks <= 0) throw new ValidationException("Deadline must be future and maximum marks must be positive."); }
}

public interface IStudentAssignmentService
{
    Task<IReadOnlyList<Assignment>> GetAssignmentsAsync(string studentId);
    Task<Assignment> GetAssignmentAsync(string studentId, int id);
    Task<IReadOnlyList<Submission>> GetSubmissionsAsync(string studentId);
    Task<Submission> CreateSubmissionAsync(string studentId, int assignmentId, string answerText);
    Task<Submission> UpdateSubmissionAsync(string studentId, int id, string answerText);
}

public sealed class StudentAssignmentService(IAsmsUnitOfWork unitOfWork) : IStudentAssignmentService
{
    public Task<IReadOnlyList<Assignment>> GetAssignmentsAsync(string studentId) => unitOfWork.Assignments.ListVisibleToStudentAsync(studentId);
    public async Task<Assignment> GetAssignmentAsync(string studentId, int id) => await unitOfWork.Assignments.FindVisibleToStudentAsync(id, studentId) ?? throw new NotFoundException("Assignment was not found.");
    public Task<IReadOnlyList<Submission>> GetSubmissionsAsync(string studentId) => unitOfWork.Submissions.ListForStudentAsync(studentId);
    public async Task<Submission> CreateSubmissionAsync(string studentId, int assignmentId, string answerText)
    {
        var assignment = await GetAssignmentAsync(studentId, assignmentId);
        if (assignment.DeadlineUtc <= DateTime.UtcNow) throw new ValidationException("The deadline has passed.");
        if (await unitOfWork.Submissions.ExistsForStudentAndAssignmentAsync(studentId, assignmentId)) throw new ConflictException("A submission already exists; update it instead.");
        var item = new Submission { AssignmentId = assignmentId, StudentId = studentId, AnswerText = answerText, Status = SubmissionStatus.Submitted, SubmittedAtUtc = DateTime.UtcNow };
        unitOfWork.Submissions.Add(item); await unitOfWork.SaveChangesAsync(); return item;
    }
    public async Task<Submission> UpdateSubmissionAsync(string studentId, int id, string answerText)
    {
        var item = await unitOfWork.Submissions.FindForStudentAsync(id, studentId) ?? throw new NotFoundException("Submission was not found.");
        if (item.Assignment.DeadlineUtc <= DateTime.UtcNow) throw new ValidationException("The deadline has passed.");
        item.AnswerText = answerText; item.UpdatedAtUtc = DateTime.UtcNow; await unitOfWork.SaveChangesAsync(); return item;
    }
}
