using System.Security.Claims;
using AsMs.Application.Services;
using AsMs.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsMs.Api.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoleNames.Student)]
[Route("api/student")]
public class StudentAssignmentsController(IStudentAssignmentService assignmentService) : ControllerBase
{
    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments() => Ok(await assignmentService.GetAssignmentsAsync(UserId));
   
    [HttpGet("assignments/{id:int}")]
    public async Task<IActionResult> GetAssignment(int id) => Ok(await assignmentService.GetAssignmentAsync(UserId, id));
   
    [HttpGet("submissions")]
    public async Task<IActionResult> GetMySubmissions() => Ok(await assignmentService.GetSubmissionsAsync(UserId));
   
    [HttpPost("assignments/{assignmentId:int}/submissions")]
    public async Task<IActionResult> CreateSubmission(int assignmentId, SubmissionRequest request)
    {
        var item = await assignmentService.CreateSubmissionAsync(UserId, assignmentId, request.AnswerText);
        return CreatedAtAction(nameof(GetMySubmissions), new { item.Id }, item);
    }
   
    [HttpPut("submissions/{id:int}")]
    public async Task<IActionResult> UpdateSubmission(int id, SubmissionRequest request) => Ok(await assignmentService.UpdateSubmissionAsync(UserId, id, request.AnswerText));
   
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
   
    public sealed record SubmissionRequest(string AnswerText);
}
