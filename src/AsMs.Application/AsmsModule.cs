using AsMs.Application.Repositories;
using AsMs.Application.UnitOfWorks;
using Autofac;

namespace AsMs.Application;

public sealed class AsmsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AcademicClassRepository>().As<IAcademicClassRepository>().InstancePerLifetimeScope();
        builder.RegisterType<SubjectRepository>().As<ISubjectRepository>().InstancePerLifetimeScope();
        builder.RegisterType<TeacherClassSubjectRepository>().As<ITeacherClassSubjectRepository>().InstancePerLifetimeScope();
        builder.RegisterType<StudentEnrollmentRepository>().As<IStudentEnrollmentRepository>().InstancePerLifetimeScope();
        builder.RegisterType<AssignmentRepository>().As<IAssignmentRepository>().InstancePerLifetimeScope();
        builder.RegisterType<SubmissionRepository>().As<ISubmissionRepository>().InstancePerLifetimeScope();
        builder.RegisterType<AsmsUnitOfWork>().As<IAsmsUnitOfWork>().InstancePerLifetimeScope();

        base.Load(builder);
    }
}
