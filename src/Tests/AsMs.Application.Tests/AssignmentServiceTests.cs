using AsMs.Application.Repositories;
using AsMs.Application.Services;
using AsMs.Application.UnitOfWorks;
using AsMs.Domain.Entities;
using FakeItEasy;
using NUnit.Framework;

namespace AsMs.Application.Tests;

[TestFixture]
public class AssignmentServiceTests
{
    [Test]
    public void CreateSubmissionAsync_RejectsExpiredAssignment()
    {
        var unitOfWork = A.Fake<IAsmsUnitOfWork>();
        var assignments = A.Fake<IAssignmentRepository>();
        A.CallTo(() => unitOfWork.Assignments).Returns(assignments);
        A.CallTo(() => assignments.FindVisibleToStudentAsync(10, "student-1", A<CancellationToken>._))
            .Returns(Task.FromResult<Assignment?>(new Assignment { Id = 10, DeadlineUtc = DateTime.UtcNow.AddMinutes(-1) }));
        var service = new StudentAssignmentService(unitOfWork);

        Assert.ThrowsAsync<ValidationException>(() => service.CreateSubmissionAsync("student-1", 10, "My answer"));
    }

    [Test]
    public void CreateSubmissionAsync_RejectsDuplicateSubmission()
    {
        var unitOfWork = A.Fake<IAsmsUnitOfWork>();
        var assignments = A.Fake<IAssignmentRepository>();
        var submissions = A.Fake<ISubmissionRepository>();
        A.CallTo(() => unitOfWork.Assignments).Returns(assignments);
        A.CallTo(() => unitOfWork.Submissions).Returns(submissions);
        A.CallTo(() => assignments.FindVisibleToStudentAsync(10, "student-1", A<CancellationToken>._))
            .Returns(Task.FromResult<Assignment?>(new Assignment { Id = 10, DeadlineUtc = DateTime.UtcNow.AddDays(1) }));
        A.CallTo(() => submissions.ExistsForStudentAndAssignmentAsync("student-1", 10, A<CancellationToken>._))
            .Returns(Task.FromResult(true));
        var service = new StudentAssignmentService(unitOfWork);

        Assert.ThrowsAsync<ConflictException>(() => service.CreateSubmissionAsync("student-1", 10, "My answer"));
        A.CallTo(() => submissions.Add(A<Submission>._)).MustNotHaveHappened();
    }

    [Test]
    public void GradeSubmissionAsync_RejectsSubmissionOwnedByAnotherTeacher()
    {
        var unitOfWork = A.Fake<IAsmsUnitOfWork>();
        var submissions = A.Fake<ISubmissionRepository>();
        A.CallTo(() => unitOfWork.Submissions).Returns(submissions);
        A.CallTo(() => submissions.FindForGradingAsync(5, A<CancellationToken>._))
            .Returns(Task.FromResult<Submission?>(SubmissionFor("teacher-2", 100)));
        var service = new TeacherAssignmentService(unitOfWork);

        Assert.ThrowsAsync<ForbiddenException>(() => service.GradeSubmissionAsync("teacher-1", 5, 50, "Good"));
    }

    [Test]
    public void GradeSubmissionAsync_RejectsMarksAboveMaximum()
    {
        var unitOfWork = A.Fake<IAsmsUnitOfWork>();
        var submissions = A.Fake<ISubmissionRepository>();
        A.CallTo(() => unitOfWork.Submissions).Returns(submissions);
        A.CallTo(() => submissions.FindForGradingAsync(5, A<CancellationToken>._))
            .Returns(Task.FromResult<Submission?>(SubmissionFor("teacher-1", 100)));
        var service = new TeacherAssignmentService(unitOfWork);

        Assert.ThrowsAsync<ValidationException>(() => service.GradeSubmissionAsync("teacher-1", 5, 101, "Too high"));
    }

    private static Submission SubmissionFor(string teacherId, decimal maximumMarks)
    {
        return new Submission
        {
            Id = 5,
            Assignment = new Assignment
            {
                MaximumMarks = maximumMarks,
                TeacherClassSubject = new TeacherClassSubject { TeacherId = teacherId }
            }
        };
    }
}
