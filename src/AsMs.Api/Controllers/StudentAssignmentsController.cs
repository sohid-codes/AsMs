using System.Security.Claims;
using AsMs.Data.Identity;
using AsMs.Data.Repositories;
using AsMs.Domain.Entities;
using AsMs.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsMs.Api.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoleNames.Student)]
[Route("api/student")]
public class StudentAssignmentsController(IAsmsUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments()
    {
        var assignments = await unitOfWork.Assignments.ListVisibleToStudentAsync(UserId);
        return Ok(assignments);
    }

    [HttpGet("assignments/{id:int}")]
    public async Task<IActionResult> GetAssignment(int id)
    {
        var assignment = await unitOfWork.Assignments.FindVisibleToStudentAsync(id, UserId);
        return assignment is null ? NotFound() : Ok(assignment);
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> GetMySubmissions()
    {
        var submissions = await unitOfWork.Submissions.ListForStudentAsync(UserId);
        return Ok(submissions);
    }

    [HttpPost("assignments/{assignmentId:int}/submissions")]
    public async Task<IActionResult> CreateSubmission(int assignmentId, SubmissionRequest request)
    {
        var assignment = await unitOfWork.Assignments.FindVisibleToStudentAsync(assignmentId, UserId);
        if (assignment is null)
        {
            return NotFound();
        }

        if (assignment.DeadlineUtc <= DateTime.UtcNow)
        {
            return BadRequest("The deadline has passed.");
        }

        if (await unitOfWork.Submissions.ExistsForStudentAndAssignmentAsync(UserId, assignmentId))
        {
            return Conflict("A submission already exists; update it instead.");
        }

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = UserId,
            AnswerText = request.AnswerText,
            Status = SubmissionStatus.Submitted,
            SubmittedAtUtc = DateTime.UtcNow
        };

        unitOfWork.Submissions.Add(submission);
        await unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMySubmissions), new { submission.Id }, submission);
    }

    [HttpPut("submissions/{id:int}")]
    public async Task<IActionResult> UpdateSubmission(int id, SubmissionRequest request)
    {
        var submission = await unitOfWork.Submissions.FindForStudentAsync(id, UserId);
        if (submission is null)
        {
            return NotFound();
        }

        if (submission.Assignment.DeadlineUtc <= DateTime.UtcNow)
        {
            return BadRequest("The deadline has passed.");
        }

        submission.AnswerText = request.AnswerText;
        submission.UpdatedAtUtc = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();
        return Ok(submission);
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public sealed record SubmissionRequest(string AnswerText);
}

