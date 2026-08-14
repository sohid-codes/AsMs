using AsMs.Data.Identity;
using AsMs.Application.Repositories;
using AsMs.Application.UnitOfWorks;
using AsMs.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsMs.Api.Controllers;

[ApiController]
[Authorize(Roles = IdentityRoleNames.Admin)]
[Route("api/admin")]
public class AdminController(IAsmsUnitOfWork unitOfWork, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = new List<object>();

        foreach (var user in await users.Users.OrderBy(user => user.Email).ToListAsync())
        {
            result.Add(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive,
                Roles = await users.GetRolesAsync(user)
            });
        }

        return Ok(result);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        if (!IdentityRoleNames.All.Contains(request.Role))
        {
            return BadRequest("Invalid role.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await users.AddToRoleAsync(user, request.Role);
        return CreatedAtAction(nameof(GetUsers), new { user.Id }, new { user.Id, user.Email, request.Role });
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        return Ok(await unitOfWork.AcademicClasses.ListAsync());
    }

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass(ClassRequest request)
    {
        if (await unitOfWork.AcademicClasses.CodeExistsAsync(request.Code))
        {
            return Conflict("Class code already exists.");
        }

        var academicClass = new AcademicClass
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        unitOfWork.AcademicClasses.Add(academicClass);
        await unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClasses), new { academicClass.Id }, academicClass);
    }

    [HttpPut("classes/{id:int}")]
    public async Task<IActionResult> UpdateClass(int id, ClassRequest request)
    {
        var academicClass = await unitOfWork.AcademicClasses.FindAsync(id);
        if (academicClass is null)
        {
            return NotFound();
        }

        if (await unitOfWork.AcademicClasses.CodeExistsAsync(request.Code, id))
        {
            return Conflict("Class code already exists.");
        }

        academicClass.Name = request.Name;
        academicClass.Code = request.Code;
        academicClass.Description = request.Description;

        await unitOfWork.SaveChangesAsync();
        return Ok(academicClass);
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        return Ok(await unitOfWork.Subjects.ListAsync());
    }

    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject(SubjectRequest request)
    {
        if (await unitOfWork.Subjects.CodeExistsAsync(request.Code))
        {
            return Conflict("Subject code already exists.");
        }

        var subject = new Subject
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        unitOfWork.Subjects.Add(subject);
        await unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSubjects), new { subject.Id }, subject);
    }

    [HttpPut("subjects/{id:int}")]
    public async Task<IActionResult> UpdateSubject(int id, SubjectRequest request)
    {
        var subject = await unitOfWork.Subjects.FindAsync(id);
        if (subject is null)
        {
            return NotFound();
        }

        if (await unitOfWork.Subjects.CodeExistsAsync(request.Code, id))
        {
            return Conflict("Subject code already exists.");
        }

        subject.Name = request.Name;
        subject.Code = request.Code;
        subject.Description = request.Description;

        await unitOfWork.SaveChangesAsync();
        return Ok(subject);
    }

    [HttpPost("teacher-assignments")]
    public async Task<IActionResult> AssignTeacher(TeacherAssignmentRequest request)
    {
        if (!await IsInRole(request.TeacherId, IdentityRoleNames.Teacher))
        {
            return BadRequest("TeacherId must belong to a Teacher.");
        }

        if (!await unitOfWork.AcademicClasses.ExistsAsync(request.AcademicClassId) ||
            !await unitOfWork.Subjects.ExistsAsync(request.SubjectId))
        {
            return NotFound();
        }

        if (await unitOfWork.TeacherClassSubjects.ExistsAsync(
                request.TeacherId,
                request.AcademicClassId,
                request.SubjectId))
        {
            return Conflict("Allocation already exists.");
        }

        var allocation = new TeacherClassSubject
        {
            TeacherId = request.TeacherId,
            AcademicClassId = request.AcademicClassId,
            SubjectId = request.SubjectId,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        unitOfWork.TeacherClassSubjects.Add(allocation);
        await unitOfWork.SaveChangesAsync();

        return Ok(allocation);
    }

    [HttpPost("student-enrollments")]
    public async Task<IActionResult> EnrollStudent(StudentEnrollmentRequest request)
    {
        if (!await IsInRole(request.StudentId, IdentityRoleNames.Student))
        {
            return BadRequest("StudentId must belong to a Student.");
        }

        if (!await unitOfWork.AcademicClasses.ExistsAsync(request.AcademicClassId))
        {
            return NotFound();
        }

        if (await unitOfWork.StudentEnrollments.ExistsAsync(request.StudentId, request.AcademicClassId))
        {
            return Conflict("Enrollment already exists.");
        }

        var enrollment = new StudentEnrollment
        {
            StudentId = request.StudentId,
            AcademicClassId = request.AcademicClassId,
            EnrolledAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        unitOfWork.StudentEnrollments.Add(enrollment);
        await unitOfWork.SaveChangesAsync();

        return Ok(enrollment);
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAllAssignments()
    {
        return Ok(await unitOfWork.Assignments.ListAsync());
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> GetAllSubmissions()
    {
        return Ok(await unitOfWork.Submissions.ListAsync());
    }

    private async Task<bool> IsInRole(string userId, string role)
    {
        var user = await users.FindByIdAsync(userId);
        return user is not null && await users.IsInRoleAsync(user, role);
    }

    public sealed record CreateUserRequest(string FullName, string Email, string Password, string Role);

    public sealed record ClassRequest(string Name, string Code, string? Description);

    public sealed record SubjectRequest(string Name, string Code, string? Description);

    public sealed record TeacherAssignmentRequest(string TeacherId, int AcademicClassId, int SubjectId);

    public sealed record StudentEnrollmentRequest(string StudentId, int AcademicClassId);
}

