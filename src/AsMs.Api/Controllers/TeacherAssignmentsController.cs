using System.Security.Claims;
using AsMs.Application.Services;
using AsMs.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsMs.Api.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoleNames.Teacher)]
[Route("api/teacher")]
public class TeacherAssignmentsController(ITeacherAssignmentService assignmentService) : ControllerBase
{
    [HttpGet("allocations")]
    public async Task<IActionResult> GetAllocations() => Ok(await assignmentService.GetAllocationsAsync(UserId));
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments() => Ok(await assignmentService.GetAssignmentsAsync(UserId));
    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment(AssignmentRequest request)
    {
        var item = await assignmentService.CreateAssignmentAsync(UserId, request.TeacherClassSubjectId, request.Title, request.Description, request.DeadlineUtc, request.MaximumMarks);
        return CreatedAtAction(nameof(GetAssignments), new { item.Id }, item);
    }
    [HttpPut("assignments/{id:int}")]
    public async Task<IActionResult> UpdateAssignment(int id, AssignmentRequest request) => Ok(await assignmentService.UpdateAssignmentAsync(UserId, id, request.Title, request.Description, request.DeadlineUtc, request.MaximumMarks));
    [HttpDelete("assignments/{id:int}")]
    public async Task<IActionResult> DeleteAssignment(int id) { await assignmentService.DeleteAssignmentAsync(UserId, id); return NoContent(); }
    [HttpPost("assignments/{id:int}/publish")]
    public async Task<IActionResult> PublishAssignment(int id) => Ok(await assignmentService.PublishAssignmentAsync(UserId, id));
    [HttpGet("assignments/{id:int}/submissions")]
    public async Task<IActionResult> GetSubmissions(int id) => Ok(await assignmentService.GetSubmissionsAsync(UserId, id));
    [HttpPost("submissions/{id:int}/grade")]
    public async Task<IActionResult> GradeSubmission(int id, GradeRequest request) => Ok(await assignmentService.GradeSubmissionAsync(UserId, id, request.Marks, request.Feedback));
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public sealed record AssignmentRequest(int TeacherClassSubjectId, string Title, string Description, DateTime DeadlineUtc, decimal MaximumMarks);
    public sealed record GradeRequest(decimal Marks, string? Feedback);
}
