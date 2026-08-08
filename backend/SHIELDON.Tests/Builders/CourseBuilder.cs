using SHIELDON.Domain.Entities;
using System;

namespace SHIELDON.Tests.Builders;

public class CourseBuilder
{
    private Course _course;

    public CourseBuilder()
    {
        var tutorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _course = new Course
        {
            Id = Guid.NewGuid(),
            Title = "Advanced Algorithms & Security",
            CourseCode = $"CS{Random.Shared.Next(100, 999)}",
            Description = "Comprehensive testing course builder data",
            AssignedTutorId = tutorId,
            CreatedByAdminId = adminId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public CourseBuilder WithTitle(string title)
    {
        _course.Title = title;
        return this;
    }

    public CourseBuilder WithCode(string code)
    {
        _course.CourseCode = code;
        return this;
    }

    public CourseBuilder WithTutor(Guid tutorId)
    {
        _course.AssignedTutorId = tutorId;
        return this;
    }

    public Course Build() => _course;
}
