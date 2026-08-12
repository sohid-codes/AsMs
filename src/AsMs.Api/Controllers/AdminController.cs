using AsMs.Data.Identity;
using AsMs.Data.Persistence;
using AsMs.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMs.Api.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoleNames.Admin)]
[Route("api/admin")]
public class AdminController(ApplicationDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() => Ok(await users.Users
        .OrderBy(user => user.Email)
        .Select(user => new { user.Id, user.FullName, user.Email, user.IsActive })
        .ToListAsync());

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        if (!IdentityRoleNames.All.Contains(request.Role)) return BadRequest("Invalid role.");
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, FullName = request.FullName, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);
        await users.AddToRoleAsync(user, request.Role);
        return CreatedAtAction(nameof(GetUsers), new { user.Id }, new { user.Id, user.Email, request.Role });
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses() => Ok(await db.AcademicClasses.OrderBy(item => item.Name).ToListAsync());

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass(ClassRequest request)
    {
        if (await db.AcademicClasses.AnyAsync(item => item.Code == request.Code)) return Conflict("Class code already exists.");
        var item = new AcademicClass { Name = request.Name, Code = request.Code, Description = request.Description, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.AcademicClasses.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetClasses), new { item.Id }, item);
    }

    [HttpPut("classes/{id:int}")]
    public async Task<IActionResult> UpdateClass(int id, ClassRequest request)
    {
        var item = await db.AcademicClasses.FindAsync(id); if (item is null) return NotFound();
        if (await db.AcademicClasses.AnyAsync(x => x.Id != id && x.Code == request.Code)) return Conflict("Class code already exists.");
        item.Name = request.Name; item.Code = request.Code; item.Description = request.Description; await db.SaveChangesAsync(); return Ok(item);
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects() => Ok(await db.Subjects.OrderBy(item => item.Name).ToListAsync());

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(SubjectRequest request)
    {
        if (await db.Subjects.AnyAsync(item => item.Code == request.Code)) return Conflict("Subject code already exists.");
        var item = new Subject { Name = request.Name, Code = request.Code, Description = request.Description, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Subjects.Add(item); await db.SaveChangesAsync(); return CreatedAtAction(nameof(GetSubjects), new { item.Id }, item);
    }

    [HttpPut("subjects/{id:int}")]
    public async Task<IActionResult> UpdateSubject(int id, SubjectRequest request)
    {
        var item = await db.Subjects.FindAsync(id); if (item is null) return NotFound();
        if (await db.Subjects.AnyAsync(x => x.Id != id && x.Code == request.Code)) return Conflict("Subject code already exists.");
        item.Name = request.Name; item.Code = request.Code; item.Description = request.Description; await db.SaveChangesAsync(); return Ok(item);
    }

    [HttpPost("teacher-assignments")]
    public async Task<IActionResult> AssignTeacher(TeacherAssignmentRequest request)
    {
        if (!await IsInRole(request.TeacherId, IdentityRoleNames.Teacher)) return BadRequest("TeacherId must belong to a Teacher.");
        if (!await db.AcademicClasses.AnyAsync(x => x.Id == request.AcademicClassId) || !await db.Subjects.AnyAsync(x => x.Id == request.SubjectId)) return NotFound();
        if (await db.TeacherClassSubjects.AnyAsync(x => x.TeacherId == request.TeacherId && x.AcademicClassId == request.AcademicClassId && x.SubjectId == request.SubjectId)) return Conflict("Allocation already exists.");
        var item = new TeacherClassSubject { TeacherId = request.TeacherId, AcademicClassId = request.AcademicClassId, SubjectId = request.SubjectId, AssignedAtUtc = DateTime.UtcNow, IsActive = true };
        db.TeacherClassSubjects.Add(item); await db.SaveChangesAsync(); return Ok(item);
    }

    [HttpPost("student-enrollments")]
    public async Task<IActionResult> EnrollStudent(StudentEnrollmentRequest request)
    {
        if (!await IsInRole(request.StudentId, IdentityRoleNames.Student)) return BadRequest("StudentId must belong to a Student.");
        if (!await db.AcademicClasses.AnyAsync(x => x.Id == request.AcademicClassId)) return NotFound();
        if (await db.StudentEnrollments.AnyAsync(x => x.StudentId == request.StudentId && x.AcademicClassId == request.AcademicClassId)) return Conflict("Enrollment already exists.");
        var item = new StudentEnrollment { StudentId = request.StudentId, AcademicClassId = request.AcademicClassId, EnrolledAtUtc = DateTime.UtcNow, IsActive = true };
        db.StudentEnrollments.Add(item); await db.SaveChangesAsync(); return Ok(item);
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAllAssignments() => Ok(await db.Assignments.Include(x => x.TeacherClassSubject).OrderByDescending(x => x.CreatedAtUtc).ToListAsync());

    [HttpGet("submissions")]
    public async Task<IActionResult> GetAllSubmissions() => Ok(await db.Submissions.Include(x => x.Assignment).OrderByDescending(x => x.SubmittedAtUtc).ToListAsync());

    private async Task<bool> IsInRole(string userId, string role) { var user = await users.FindByIdAsync(userId); return user is not null && await users.IsInRoleAsync(user, role); }

    public sealed record CreateUserRequest(string FullName, string Email, string Password, string Role);
    public sealed record ClassRequest(string Name, string Code, string? Description);
    public sealed record SubjectRequest(string Name, string Code, string? Description);
    public sealed record TeacherAssignmentRequest(string TeacherId, int AcademicClassId, int SubjectId);
    public sealed record StudentEnrollmentRequest(string StudentId, int AcademicClassId);
}
