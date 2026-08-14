using AsMs.Application.Repositories;
using AsMs.Data;
using AsMs.Data.Persistence;

namespace AsMs.Application.UnitOfWorks;

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

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
