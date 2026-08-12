using System.Security.Claims;
using AsMs.Data.Identity;
using AsMs.Data.Persistence;
using AsMs.Domain.Entities;
using AsMs.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMs.Api.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoleNames.Student)]
[Route("api/student")]
public class StudentAssignmentsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments() => Ok(await VisibleAssignments().OrderBy(x => x.DeadlineUtc).ToListAsync());

    [HttpGet("assignments/{id:int}")]
    public async Task<IActionResult> GetAssignment(int id) { var item = await VisibleAssignments().FirstOrDefaultAsync(x => x.Id == id); return item is null ? NotFound() : Ok(item); }

    [HttpGet("submissions")]
    public async Task<IActionResult> GetMySubmissions() => Ok(await db.Submissions.Where(x => x.StudentId == UserId).OrderByDescending(x => x.SubmittedAtUtc).ToListAsync());

    [HttpPost("assignments/{assignmentId:int}/submissions")]
    public async Task<IActionResult> CreateSubmission(int assignmentId, SubmissionRequest request)
    {
        var assignment = await VisibleAssignments().FirstOrDefaultAsync(x => x.Id == assignmentId);
        if (assignment is null) return NotFound();
        if (assignment.DeadlineUtc <= DateTime.UtcNow) return BadRequest("The deadline has passed.");
        if (await db.Submissions.AnyAsync(x => x.AssignmentId == assignmentId && x.StudentId == UserId)) return Conflict("A submission already exists; update it instead.");
        var item = new Submission { AssignmentId = assignmentId, StudentId = UserId, AnswerText = request.AnswerText, Status = SubmissionStatus.Submitted, SubmittedAtUtc = DateTime.UtcNow };
        db.Submissions.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetMySubmissions), new { item.Id }, item);
    }

    [HttpPut("submissions/{id:int}")]
    public async Task<IActionResult> UpdateSubmission(int id, SubmissionRequest request)
    {
        var item = await db.Submissions.Include(x => x.Assignment).FirstOrDefaultAsync(x => x.Id == id && x.StudentId == UserId);
        if (item is null) return NotFound();
        if (item.Assignment.DeadlineUtc <= DateTime.UtcNow) return BadRequest("The deadline has passed.");
        item.AnswerText = request.AnswerText; item.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(); return Ok(item);
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Assignment> VisibleAssignments() => db.Assignments.Include(x => x.TeacherClassSubject).Where(x => x.Status == AssignmentStatus.Published && x.TeacherClassSubject.AcademicClass.StudentEnrollments.Any(e => e.StudentId == UserId && e.IsActive));
    public sealed record SubmissionRequest(string AnswerText);
}
