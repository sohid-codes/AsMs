using AsMs.Application.UnitOfWorks;
using AsMs.Data.Identity;
using AsMs.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AsMs.Application.Services;

public sealed record UserSummary(string Id, string FullName, string? Email, bool IsActive, IReadOnlyList<string> Roles);

public interface IAdminService
{
    Task<IReadOnlyList<UserSummary>> GetUsersAsync();
    Task<UserSummary> CreateUserAsync(string fullName, string email, string password, string role);
    Task<IReadOnlyList<AcademicClass>> GetClassesAsync();
    Task<AcademicClass> CreateClassAsync(string name, string code, string? description);
    Task<AcademicClass> UpdateClassAsync(int id, string name, string code, string? description);
    Task<IReadOnlyList<Subject>> GetSubjectsAsync();
    Task<Subject> CreateSubjectAsync(string name, string code, string? description);
    Task<Subject> UpdateSubjectAsync(int id, string name, string code, string? description);
    Task<TeacherClassSubject> AssignTeacherAsync(string teacherId, int academicClassId, int subjectId);
    Task<StudentEnrollment> EnrollStudentAsync(string studentId, int academicClassId);
    Task<IReadOnlyList<Assignment>> GetAssignmentsAsync();
    Task<IReadOnlyList<Submission>> GetSubmissionsAsync();
}

public sealed class AdminService(IAsmsUnitOfWork unitOfWork, UserManager<ApplicationUser> users) : IAdminService
{
    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync()
    {
        var result = new List<UserSummary>();
        foreach (var user in await users.Users.OrderBy(user => user.Email).ToListAsync())
            result.Add(new UserSummary(user.Id, user.FullName, user.Email, user.IsActive, (await users.GetRolesAsync(user)).ToList()));
        return result;
    }
    public async Task<UserSummary> CreateUserAsync(string fullName, string email, string password, string role)
    {
        if (!IdentityRoleNames.All.Contains(role)) throw new ValidationException("Invalid role.");
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new ValidationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        await users.AddToRoleAsync(user, role);
        return new UserSummary(user.Id, user.FullName, user.Email, user.IsActive, [role]);
    }
    public Task<IReadOnlyList<AcademicClass>> GetClassesAsync() => unitOfWork.AcademicClasses.ListAsync();
    public async Task<AcademicClass> CreateClassAsync(string name, string code, string? description)
    {
        if (await unitOfWork.AcademicClasses.CodeExistsAsync(code)) throw new ConflictException("Class code already exists.");
        var item = new AcademicClass { Name = name, Code = code, Description = description, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        unitOfWork.AcademicClasses.Add(item); await unitOfWork.SaveChangesAsync(); return item;
    }
    public async Task<AcademicClass> UpdateClassAsync(int id, string name, string code, string? description)
    {
        var item = await unitOfWork.AcademicClasses.FindAsync(id) ?? throw new NotFoundException("Class was not found.");
        if (await unitOfWork.AcademicClasses.CodeExistsAsync(code, id)) throw new ConflictException("Class code already exists.");
        item.Name = name; item.Code = code; item.Description = description; await unitOfWork.SaveChangesAsync(); return item;
    }
    public Task<IReadOnlyList<Subject>> GetSubjectsAsync() => unitOfWork.Subjects.ListAsync();
    public async Task<Subject> CreateSubjectAsync(string name, string code, string? description)
    {
        if (await unitOfWork.Subjects.CodeExistsAsync(code)) throw new ConflictException("Subject code already exists.");
        var item = new Subject { Name = name, Code = code, Description = description, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        unitOfWork.Subjects.Add(item); await unitOfWork.SaveChangesAsync(); return item;
    }
    public async Task<Subject> UpdateSubjectAsync(int id, string name, string code, string? description)
    {
        var item = await unitOfWork.Subjects.FindAsync(id) ?? throw new NotFoundException("Subject was not found.");
        if (await unitOfWork.Subjects.CodeExistsAsync(code, id)) throw new ConflictException("Subject code already exists.");
        item.Name = name; item.Code = code; item.Description = description; await unitOfWork.SaveChangesAsync(); return item;
    }
    public async Task<TeacherClassSubject> AssignTeacherAsync(string teacherId, int academicClassId, int subjectId)
    {
        await EnsureRoleAsync(teacherId, IdentityRoleNames.Teacher, "TeacherId must belong to a Teacher.");
        if (!await unitOfWork.AcademicClasses.ExistsAsync(academicClassId) || !await unitOfWork.Subjects.ExistsAsync(subjectId)) throw new NotFoundException("Class or subject was not found.");
        if (await unitOfWork.TeacherClassSubjects.ExistsAsync(teacherId, academicClassId, subjectId)) throw new ConflictException("Allocation already exists.");
        var item = new TeacherClassSubject { TeacherId = teacherId, AcademicClassId = academicClassId, SubjectId = subjectId, AssignedAtUtc = DateTime.UtcNow, IsActive = true };
        unitOfWork.TeacherClassSubjects.Add(item); await unitOfWork.SaveChangesAsync(); return item;
    }
    public async Task<StudentEnrollment> EnrollStudentAsync(string studentId, int academicClassId)
    {
        await EnsureRoleAsync(studentId, IdentityRoleNames.Student, "StudentId must belong to a Student.");
        if (!await unitOfWork.AcademicClasses.ExistsAsync(academicClassId)) throw new NotFoundException("Class was not found.");
        if (await unitOfWork.StudentEnrollments.ExistsAsync(studentId, academicClassId)) throw new ConflictException("Enrollment already exists.");
        var item = new StudentEnrollment { StudentId = studentId, AcademicClassId = academicClassId, EnrolledAtUtc = DateTime.UtcNow, IsActive = true };
        unitOfWork.StudentEnrollments.Add(item); await unitOfWork.SaveChangesAsync(); return item;
    }
    public Task<IReadOnlyList<Assignment>> GetAssignmentsAsync() => unitOfWork.Assignments.ListAsync();
    public Task<IReadOnlyList<Submission>> GetSubmissionsAsync() => unitOfWork.Submissions.ListAsync();
    private async Task EnsureRoleAsync(string userId, string role, string message) { var user = await users.FindByIdAsync(userId); if (user is null || !await users.IsInRoleAsync(user, role)) throw new ValidationException(message); }
}
