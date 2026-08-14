using AsMs.Application.Services;
using AsMs.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsMs.Api.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoleNames.Admin)]
[Route("api/admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() => Ok(await adminService.GetUsersAsync());

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var user = await adminService.CreateUserAsync(request.FullName, request.Email, request.Password, request.Role);
        return CreatedAtAction(nameof(GetUsers), new { user.Id }, user);
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses() => Ok(await adminService.GetClassesAsync());

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass(ClassRequest request)
    {
        var item = await adminService.CreateClassAsync(request.Name, request.Code, request.Description);
        return CreatedAtAction(nameof(GetClasses), new { item.Id }, item);
    }

    [HttpPut("classes/{id:int}")]
    public async Task<IActionResult> UpdateClass(int id, ClassRequest request) =>
        Ok(await adminService.UpdateClassAsync(id, request.Name, request.Code, request.Description));

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects() => Ok(await adminService.GetSubjectsAsync());

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(SubjectRequest request)
    {
        var item = await adminService.CreateSubjectAsync(request.Name, request.Code, request.Description);
        return CreatedAtAction(nameof(GetSubjects), new { item.Id }, item);
    }

    [HttpPut("subjects/{id:int}")]
    public async Task<IActionResult> UpdateSubject(int id, SubjectRequest request) =>
        Ok(await adminService.UpdateSubjectAsync(id, request.Name, request.Code, request.Description));

    [HttpPost("teacher-assignments")]
    public async Task<IActionResult> AssignTeacher(TeacherAssignmentRequest request) =>
        Ok(await adminService.AssignTeacherAsync(request.TeacherId, request.AcademicClassId, request.SubjectId));

    [HttpPost("student-enrollments")]
    public async Task<IActionResult> EnrollStudent(StudentEnrollmentRequest request) =>
        Ok(await adminService.EnrollStudentAsync(request.StudentId, request.AcademicClassId));

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAllAssignments() => Ok(await adminService.GetAssignmentsAsync());

    [HttpGet("submissions")]
    public async Task<IActionResult> GetAllSubmissions() => Ok(await adminService.GetSubmissionsAsync());

    public sealed record CreateUserRequest(string FullName, string Email, string Password, string Role);
    public sealed record ClassRequest(string Name, string Code, string? Description);
    public sealed record SubjectRequest(string Name, string Code, string? Description);
    public sealed record TeacherAssignmentRequest(string TeacherId, int AcademicClassId, int SubjectId);
    public sealed record StudentEnrollmentRequest(string StudentId, int AcademicClassId);
}
