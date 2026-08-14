using AsMs.Application.Repositories;

namespace AsMs.Application.UnitOfWorks;

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
