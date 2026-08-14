using AsMs.Domain.Entities;

namespace AsMs.Data.Repositories;

public interface IAsmsUnitOfWork : IDisposable
{
    IAcademicClassRepository AcademicClasses { get; }
    ISubjectRepository Subjects { get; }
    ITeacherClassSubjectRepository TeacherClassSubjects { get; }
    IStudentEnrollmentRepository StudentEnrollments { get; }
    IAssignmentRepository Assignments { get; }
    ISubmissionRepository Submissions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAcademicClassRepository
{
    Task<IReadOnlyList<AcademicClass>> ListAsync(CancellationToken cancellationToken = default);
    Task<AcademicClass?> FindAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    void Add(AcademicClass entity);
}

public interface ISubjectRepository
{
    Task<IReadOnlyList<Subject>> ListAsync(CancellationToken cancellationToken = default);
    Task<Subject?> FindAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    void Add(Subject entity);
}

public interface ITeacherClassSubjectRepository
{
    Task<IReadOnlyList<TeacherClassSubject>> ListForTeacherAsync(string teacherId, CancellationToken cancellationToken = default);
    Task<TeacherClassSubject?> FindActiveForTeacherAsync(int id, string teacherId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string teacherId, int academicClassId, int subjectId, CancellationToken cancellationToken = default);
    void Add(TeacherClassSubject entity);
}

public interface IStudentEnrollmentRepository
{
    Task<bool> ExistsAsync(string studentId, int academicClassId, CancellationToken cancellationToken = default);
    void Add(StudentEnrollment entity);
}

public interface IAssignmentRepository
{
    Task<IReadOnlyList<Assignment>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> ListForTeacherAsync(string teacherId, CancellationToken cancellationToken = default);
    Task<Assignment?> FindOwnedByTeacherAsync(int id, string teacherId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assignment>> ListVisibleToStudentAsync(string studentId, CancellationToken cancellationToken = default);
    Task<Assignment?> FindVisibleToStudentAsync(int id, string studentId, CancellationToken cancellationToken = default);
    void Add(Assignment entity);
    void Remove(Assignment entity);
}

public interface ISubmissionRepository
{
    Task<IReadOnlyList<Submission>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Submission>> ListForStudentAsync(string studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Submission>> ListForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default);
    Task<Submission?> FindForStudentAsync(int id, string studentId, CancellationToken cancellationToken = default);
    Task<Submission?> FindForGradingAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForStudentAndAssignmentAsync(string studentId, int assignmentId, CancellationToken cancellationToken = default);
    void Add(Submission entity);
}
