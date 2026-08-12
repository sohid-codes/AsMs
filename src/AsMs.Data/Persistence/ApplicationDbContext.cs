using AsMs.Data.Identity;
using AsMs.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AsMs.Data.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<AcademicClass> AcademicClasses => Set<AcademicClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherClassSubject> TeacherClassSubjects => Set<TeacherClassSubject>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
        });

        builder.Entity<AcademicClass>(entity =>
        {
            entity.ToTable("AcademicClasses");
            entity.Property(item => item.Name).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Code).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.HasIndex(item => item.Code).IsUnique();
        });

        builder.Entity<Subject>(entity =>
        {
            entity.ToTable("Subjects");
            entity.Property(item => item.Name).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Code).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.HasIndex(item => item.Code).IsUnique();
        });

        builder.Entity<TeacherClassSubject>(entity =>
        {
            entity.ToTable("TeacherClassSubjects");
            entity.HasIndex(item => new { item.TeacherId, item.AcademicClassId, item.SubjectId }).IsUnique();
            entity.HasOne(item => item.AcademicClass).WithMany(item => item.TeacherClassSubjects)
                .HasForeignKey(item => item.AcademicClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Subject).WithMany(item => item.TeacherClassSubjects)
                .HasForeignKey(item => item.SubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentEnrollment>(entity =>
        {
            entity.ToTable("StudentEnrollments");
            entity.HasIndex(item => new { item.StudentId, item.AcademicClassId }).IsUnique();
            entity.HasOne(item => item.AcademicClass).WithMany(item => item.StudentEnrollments)
                .HasForeignKey(item => item.AcademicClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Assignment>(entity =>
        {
            entity.ToTable("Assignments");
            entity.Property(item => item.Title).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.MaximumMarks).HasPrecision(5, 2);
            entity.HasOne(item => item.TeacherClassSubject).WithMany(item => item.Assignments)
                .HasForeignKey(item => item.TeacherClassSubjectId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Submission>(entity =>
        {
            entity.ToTable("Submissions");
            entity.Property(item => item.AnswerText).HasMaxLength(10000).IsRequired();
            entity.Property(item => item.Feedback).HasMaxLength(4000);
            entity.Property(item => item.Marks).HasPrecision(5, 2);
            entity.HasIndex(item => new { item.AssignmentId, item.StudentId }).IsUnique();
            entity.HasOne(item => item.Assignment).WithMany(item => item.Submissions)
                .HasForeignKey(item => item.AssignmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.MarkedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
