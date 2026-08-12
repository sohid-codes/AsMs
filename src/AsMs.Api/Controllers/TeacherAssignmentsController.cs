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
[Authorize(Roles = IdentityRoleNames.Teacher)]
[Route("api/teacher")]
public class TeacherAssignmentsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("allocations")]
    public async Task<IActionResult> GetAllocations() => Ok(await db.TeacherClassSubjects.Include(x => x.AcademicClass).Include(x => x.Subject).Where(x => x.TeacherId == UserId && x.IsActive).ToListAsync());

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments() => Ok(await db.Assignments.Include(x => x.TeacherClassSubject).Where(x => x.TeacherClassSubject.TeacherId == UserId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync());

    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment(AssignmentRequest request)
    {
        if (request.DeadlineUtc <= DateTime.UtcNow || request.MaximumMarks <= 0) return BadRequest("Deadline must be future and maximum marks must be positive.");
        var allocation = await db.TeacherClassSubjects.FirstOrDefaultAsync(x => x.Id == request.TeacherClassSubjectId && x.TeacherId == UserId && x.IsActive);
        if (allocation is null) return Forbid();
        var item = new Assignment { TeacherClassSubjectId = allocation.Id, Title = request.Title, Description = request.Description, DeadlineUtc = request.DeadlineUtc, MaximumMarks = request.MaximumMarks, Status = AssignmentStatus.Draft, CreatedAtUtc = DateTime.UtcNow };
        db.Assignments.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetAssignments), new { item.Id }, item);
    }

    [HttpPut("assignments/{id:int}")]
    public async Task<IActionResult> UpdateAssignment(int id, AssignmentRequest request)
    {
        var item = await OwnedAssignment(id); if (item is null) return NotFound();
        if (request.DeadlineUtc <= DateTime.UtcNow || request.MaximumMarks <= 0) return BadRequest("Deadline must be future and maximum marks must be positive.");
        item.Title = request.Title; item.Description = request.Description; item.DeadlineUtc = request.DeadlineUtc; item.MaximumMarks = request.MaximumMarks; item.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(); return Ok(item);
    }

    [HttpDelete("assignments/{id:int}")]
    public async Task<IActionResult> DeleteAssignment(int id)
    {
        var item = await OwnedAssignment(id); if (item is null) return NotFound();
        if (await db.Submissions.AnyAsync(x => x.AssignmentId == id)) return Conflict("Assignments with submissions cannot be deleted.");
        db.Assignments.Remove(item); await db.SaveChangesAsync(); return NoContent();
    }

    [HttpPost("assignments/{id:int}/publish")]
    public async Task<IActionResult> PublishAssignment(int id)
    {
        var item = await OwnedAssignment(id); if (item is null) return NotFound();
        item.Status = AssignmentStatus.Published; item.PublishedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(); return Ok(item);
    }

    [HttpGet("assignments/{id:int}/submissions")]
    public async Task<IActionResult> GetSubmissions(int id)
    {
        if (await OwnedAssignment(id) is null) return NotFound();
        return Ok(await db.Submissions.Where(x => x.AssignmentId == id).OrderByDescending(x => x.SubmittedAtUtc).ToListAsync());
    }

    [HttpPost("submissions/{id:int}/grade")]
    public async Task<IActionResult> GradeSubmission(int id, GradeRequest request)
    {
        var submission = await db.Submissions.Include(x => x.Assignment).ThenInclude(x => x.TeacherClassSubject).FirstOrDefaultAsync(x => x.Id == id);
        if (submission is null) return NotFound();
        if (submission.Assignment.TeacherClassSubject.TeacherId != UserId) return Forbid();
        if (request.Marks < 0 || request.Marks > submission.Assignment.MaximumMarks) return BadRequest("Marks are outside the assignment range.");
        submission.Marks = request.Marks; submission.Feedback = request.Feedback; submission.MarkedAtUtc = DateTime.UtcNow; submission.MarkedByTeacherId = UserId; submission.Status = SubmissionStatus.Graded; await db.SaveChangesAsync(); return Ok(submission);
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private Task<Assignment?> OwnedAssignment(int id) => db.Assignments.Include(x => x.TeacherClassSubject).FirstOrDefaultAsync(x => x.Id == id && x.TeacherClassSubject.TeacherId == UserId);
    public sealed record AssignmentRequest(int TeacherClassSubjectId, string Title, string Description, DateTime DeadlineUtc, decimal MaximumMarks);
    public sealed record GradeRequest(decimal Marks, string? Feedback);
}
