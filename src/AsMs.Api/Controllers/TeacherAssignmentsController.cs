using System.Security.Claims;
using AsMs.Data.Identity;
using AsMs.Data.Repositories;
using AsMs.Domain.Entities;
using AsMs.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsMs.Api.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoleNames.Teacher)]
[Route("api/teacher")]
public class TeacherAssignmentsController(IAsmsUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet("allocations")]
    public async Task<IActionResult> GetAllocations()
    {
        var allocations = await unitOfWork.TeacherClassSubjects.ListForTeacherAsync(UserId);
        return Ok(allocations);
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments()
    {
        var assignments = await unitOfWork.Assignments.ListForTeacherAsync(UserId);
        return Ok(assignments);
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment(AssignmentRequest request)
    {
        if (request.DeadlineUtc <= DateTime.UtcNow || request.MaximumMarks <= 0)
        {
            return BadRequest("Deadline must be future and maximum marks must be positive.");
        }

        var allocation = await unitOfWork.TeacherClassSubjects
            .FindActiveForTeacherAsync(request.TeacherClassSubjectId, UserId);
        if (allocation is null)
        {
            return Forbid();
        }

        var assignment = new Assignment
        {
            TeacherClassSubjectId = allocation.Id,
            Title = request.Title,
            Description = request.Description,
            DeadlineUtc = request.DeadlineUtc,
            MaximumMarks = request.MaximumMarks,
            Status = AssignmentStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };

        unitOfWork.Assignments.Add(assignment);
        await unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAssignments), new { assignment.Id }, assignment);
    }

    [HttpPut("assignments/{id:int}")]
    public async Task<IActionResult> UpdateAssignment(int id, AssignmentRequest request)
    {
        var assignment = await unitOfWork.Assignments.FindOwnedByTeacherAsync(id, UserId);
        if (assignment is null)
        {
            return NotFound();
        }

        if (request.DeadlineUtc <= DateTime.UtcNow || request.MaximumMarks <= 0)
        {
            return BadRequest("Deadline must be future and maximum marks must be positive.");
        }

        assignment.Title = request.Title;
        assignment.Description = request.Description;
        assignment.DeadlineUtc = request.DeadlineUtc;
        assignment.MaximumMarks = request.MaximumMarks;
        assignment.UpdatedAtUtc = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();
        return Ok(assignment);
    }

    [HttpDelete("assignments/{id:int}")]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        var assignment = await unitOfWork.Assignments.FindOwnedByTeacherAsync(id, UserId);
        if (assignment is null)
        {
            return NotFound();
        }

        if (await unitOfWork.Submissions.ExistsForAssignmentAsync(id))
        {
            return Conflict("Assignments with submissions cannot be deleted.");
        }

        unitOfWork.Assignments.Remove(assignment);
        await unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("assignments/{id:int}/publish")]
    public async Task<IActionResult> PublishAssignment(int id)
    {
        var assignment = await unitOfWork.Assignments.FindOwnedByTeacherAsync(id, UserId);
        if (assignment is null)
        {
            return NotFound();
        }

        assignment.Status = AssignmentStatus.Published;
        assignment.PublishedAtUtc = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();
        return Ok(assignment);
    }

    [HttpGet("assignments/{id:int}/submissions")]
    public async Task<IActionResult> GetSubmissions(int id)
    {
        if (await unitOfWork.Assignments.FindOwnedByTeacherAsync(id, UserId) is null)
        {
            return NotFound();
        }

        var submissions = await unitOfWork.Submissions.ListForAssignmentAsync(id);
        return Ok(submissions);
    }

    [HttpPost("submissions/{id:int}/grade")]
    public async Task<IActionResult> GradeSubmission(int id, GradeRequest request)
    {
        var submission = await unitOfWork.Submissions.FindForGradingAsync(id);
        if (submission is null)
        {
            return NotFound();
        }

        if (submission.Assignment.TeacherClassSubject.TeacherId != UserId)
        {
            return Forbid();
        }

        if (request.Marks < 0 || request.Marks > submission.Assignment.MaximumMarks)
        {
            return BadRequest("Marks are outside the assignment range.");
        }

        submission.Marks = request.Marks;
        submission.Feedback = request.Feedback;
        submission.MarkedAtUtc = DateTime.UtcNow;
        submission.MarkedByTeacherId = UserId;
        submission.Status = SubmissionStatus.Graded;

        await unitOfWork.SaveChangesAsync();
        return Ok(submission);
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public sealed record AssignmentRequest(
        int TeacherClassSubjectId,
        string Title,
        string Description,
        DateTime DeadlineUtc,
        decimal MaximumMarks);

    public sealed record GradeRequest(decimal Marks, string? Feedback);
}

