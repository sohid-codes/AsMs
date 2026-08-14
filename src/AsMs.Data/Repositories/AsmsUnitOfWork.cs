using AsMs.Data.Persistence;
using AsMs.Domain.Entities;
using AsMs.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AsMs.Data.Repositories;

public sealed class AsmsUnitOfWork : UnitOfWork, IAsmsUnitOfWork
{
    public AsmsUnitOfWork(
        ApplicationDbContext dbContext,
        IAcademicClassRepository academicClasses,
        ISubjectRepository subjects,
        ITeacherClassSubjectRepository teacherClassSubjects,
        IStudentEnrollmentRepository studentEnrollments,
        IAssignmentRepository assignments,
        ISubmissionRepository submissions) : base(dbContext)
    {
        AcademicClasses = academicClasses;
        Subjects = subjects;
        TeacherClassSubjects = teacherClassSubjects;
        StudentEnrollments = studentEnrollments;
        Assignments = assignments;
        Submissions = submissions;
    }

    public IAcademicClassRepository AcademicClasses { get; }
    public ISubjectRepository Subjects { get; }
    public ITeacherClassSubjectRepository TeacherClassSubjects { get; }
    public IStudentEnrollmentRepository StudentEnrollments { get; }
    public IAssignmentRepository Assignments { get; }
    public ISubmissionRepository Submissions { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}

public sealed class AcademicClassRepository(ApplicationDbContext dbContext) : IAcademicClassRepository
{
    public async Task<IReadOnlyList<AcademicClass>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AcademicClasses.OrderBy(item => item.Name).ToListAsync(cancellationToken);
    public Task<AcademicClass?> FindAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.AcademicClasses.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.AcademicClasses.AnyAsync(item => item.Id == id, cancellationToken);
    public Task<bool> CodeExistsAsync(string code, int? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.AcademicClasses.AnyAsync(item => item.Code == code && (!excludingId.HasValue || item.Id != excludingId), cancellationToken);
    public void Add(AcademicClass entity) => dbContext.AcademicClasses.Add(entity);
}

public sealed class SubjectRepository(ApplicationDbContext dbContext) : ISubjectRepository
{
    public async Task<IReadOnlyList<Subject>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Subjects.OrderBy(item => item.Name).ToListAsync(cancellationToken);
    public Task<Subject?> FindAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Subjects.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Subjects.AnyAsync(item => item.Id == id, cancellationToken);
    public Task<bool> CodeExistsAsync(string code, int? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.Subjects.AnyAsync(item => item.Code == code && (!excludingId.HasValue || item.Id != excludingId), cancellationToken);
    public void Add(Subject entity) => dbContext.Subjects.Add(entity);
}

public sealed class TeacherClassSubjectRepository(ApplicationDbContext dbContext) : ITeacherClassSubjectRepository
{
    public async Task<IReadOnlyList<TeacherClassSubject>> ListForTeacherAsync(string teacherId, CancellationToken cancellationToken = default) =>
        await dbContext.TeacherClassSubjects.Include(item => item.AcademicClass).Include(item => item.Subject)
            .Where(item => item.TeacherId == teacherId && item.IsActive).ToListAsync(cancellationToken);
    public Task<TeacherClassSubject?> FindActiveForTeacherAsync(int id, string teacherId, CancellationToken cancellationToken = default) =>
        dbContext.TeacherClassSubjects.FirstOrDefaultAsync(item => item.Id == id && item.TeacherId == teacherId && item.IsActive, cancellationToken);
    public Task<bool> ExistsAsync(string teacherId, int academicClassId, int subjectId, CancellationToken cancellationToken = default) =>
        dbContext.TeacherClassSubjects.AnyAsync(item => item.TeacherId == teacherId && item.AcademicClassId == academicClassId && item.SubjectId == subjectId, cancellationToken);
    public void Add(TeacherClassSubject entity) => dbContext.TeacherClassSubjects.Add(entity);
}

public sealed class StudentEnrollmentRepository(ApplicationDbContext dbContext) : IStudentEnrollmentRepository
{
    public Task<bool> ExistsAsync(string studentId, int academicClassId, CancellationToken cancellationToken = default) =>
        dbContext.StudentEnrollments.AnyAsync(item => item.StudentId == studentId && item.AcademicClassId == academicClassId, cancellationToken);
    public void Add(StudentEnrollment entity) => dbContext.StudentEnrollments.Add(entity);
}

public sealed class AssignmentRepository(ApplicationDbContext dbContext) : IAssignmentRepository
{
    public async Task<IReadOnlyList<Assignment>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Assignments.Include(item => item.TeacherClassSubject).OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Assignment>> ListForTeacherAsync(string teacherId, CancellationToken cancellationToken = default) =>
        await dbContext.Assignments.Include(item => item.TeacherClassSubject).Where(item => item.TeacherClassSubject.TeacherId == teacherId)
            .OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
    public Task<Assignment?> FindOwnedByTeacherAsync(int id, string teacherId, CancellationToken cancellationToken = default) =>
        dbContext.Assignments.Include(item => item.TeacherClassSubject).FirstOrDefaultAsync(item => item.Id == id && item.TeacherClassSubject.TeacherId == teacherId, cancellationToken);
    public async Task<IReadOnlyList<Assignment>> ListVisibleToStudentAsync(string studentId, CancellationToken cancellationToken = default) =>
        await VisibleToStudent(studentId).OrderBy(item => item.DeadlineUtc).ToListAsync(cancellationToken);
    public Task<Assignment?> FindVisibleToStudentAsync(int id, string studentId, CancellationToken cancellationToken = default) =>
        VisibleToStudent(studentId).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    public void Add(Assignment entity) => dbContext.Assignments.Add(entity);
    public void Remove(Assignment entity) => dbContext.Assignments.Remove(entity);
    private IQueryable<Assignment> VisibleToStudent(string studentId) => dbContext.Assignments.Include(item => item.TeacherClassSubject)
        .Where(item => item.Status == AssignmentStatus.Published && item.TeacherClassSubject.AcademicClass.StudentEnrollments.Any(enrollment => enrollment.StudentId == studentId && enrollment.IsActive));
}

public sealed class SubmissionRepository(ApplicationDbContext dbContext) : ISubmissionRepository
{
    public async Task<IReadOnlyList<Submission>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Submissions.Include(item => item.Assignment).OrderByDescending(item => item.SubmittedAtUtc).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Submission>> ListForStudentAsync(string studentId, CancellationToken cancellationToken = default) =>
        await dbContext.Submissions.Where(item => item.StudentId == studentId).OrderByDescending(item => item.SubmittedAtUtc).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Submission>> ListForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default) =>
        await dbContext.Submissions.Where(item => item.AssignmentId == assignmentId).OrderByDescending(item => item.SubmittedAtUtc).ToListAsync(cancellationToken);
    public Task<Submission?> FindForStudentAsync(int id, string studentId, CancellationToken cancellationToken = default) =>
        dbContext.Submissions.Include(item => item.Assignment).FirstOrDefaultAsync(item => item.Id == id && item.StudentId == studentId, cancellationToken);
    public Task<Submission?> FindForGradingAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Submissions.Include(item => item.Assignment).ThenInclude(item => item.TeacherClassSubject).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<bool> ExistsForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default) =>
        dbContext.Submissions.AnyAsync(item => item.AssignmentId == assignmentId, cancellationToken);
    public Task<bool> ExistsForStudentAndAssignmentAsync(string studentId, int assignmentId, CancellationToken cancellationToken = default) =>
        dbContext.Submissions.AnyAsync(item => item.StudentId == studentId && item.AssignmentId == assignmentId, cancellationToken);
    public void Add(Submission entity) => dbContext.Submissions.Add(entity);
}
