using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using System;

namespace SHIELDON.Tests.Builders;

public class ExamBuilder
{
    private Exam _exam;

    public ExamBuilder()
    {
        var courseId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _exam = new Exam
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = "Midterm Examination Security 2026",
            Instructions = "No external aids allowed.",
            TimeLimit = 60,
            PassScore = 60,
            Weight = 30,
            Status = ExamStatus.Published,
            ResultVisibility = ResultVisibility.Immediate,
            ScheduledAt = DateTime.UtcNow.AddDays(-1),
            ScheduledEndAt = DateTime.UtcNow.AddDays(7),
            CreatedByUserId = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public ExamBuilder WithTitle(string title)
    {
        _exam.Title = title;
        return this;
    }

    public ExamBuilder WithCourse(Guid courseId)
    {
        _exam.CourseId = courseId;
        return this;
    }

    public Exam Build() => _exam;
}
