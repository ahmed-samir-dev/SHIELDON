using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHIELDON.Application.Features.Calendar.DTOs;
using SHIELDON.Application.Features.Calendar.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(AppDbContext db, ILogger<CalendarService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<CalendarEventDto>> GetCalendarEventsAsync(Guid userId, DateTime start, DateTime end, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

            // Determine which courses the user has access to
            var courseIds = new List<Guid>();

            if (user.Role == UserRole.Student)
            {
                courseIds = await _db.CourseEnrollments.AsNoTracking()
                    .Where(e => e.StudentId == userId && e.Status == CourseEnrollmentStatus.Approved && e.Course!.IsActive)
                    .Select(e => e.CourseId)
                    .ToListAsync(ct);
            }
            else if (user.Role == UserRole.Tutor)
            {
                courseIds = await _db.Courses.AsNoTracking()
                    .Where(c => c.AssignedTutorId == userId && c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync(ct);
            }
            else if (user.Role == UserRole.Admin)
            {
                // Admins see all active courses
                courseIds = await _db.Courses.AsNoTracking()
                    .Where(c => c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync(ct);
            }

            var events = new List<CalendarEventDto>();

            // 1. Fetch Exams
            var exams = await _db.Exams.AsNoTracking()
                .Include(e => e.Course)
                .Where(e => courseIds.Contains(e.CourseId) && e.Status == ExamStatus.Published)
                .Where(e => (e.ScheduledAt >= start && e.ScheduledAt <= end) || 
                            (e.ScheduledEndAt >= start && e.ScheduledEndAt <= end) ||
                            (e.ScheduledAt <= start && e.ScheduledEndAt >= end))
                .ToListAsync(ct);

            foreach (var exam in exams)
            {
                if (exam.ScheduledAt.HasValue)
                {
                    events.Add(new CalendarEventDto
                    {
                        Id = exam.Id,
                        Title = $"Exam: {exam.Title}",
                        Description = exam.Instructions,
                        StartDate = exam.ScheduledAt.Value,
                        EndDate = exam.ScheduledEndAt, // Nullable is fine
                        Type = EventType.Exam,
                        CourseId = exam.CourseId,
                        CourseName = exam.Course?.Title,
                        SourceEntityId = exam.Id
                    });
                }
            }

            // 2. Fetch Assignments
            var assignments = await _db.Assignments.AsNoTracking()
                .Include(a => a.Course)
                .Where(a => courseIds.Contains(a.CourseId))
                .Where(a => a.DueDate >= start && a.DueDate <= end)
                .ToListAsync(ct);

            foreach (var assignment in assignments)
            {
                if (assignment.DueDate.HasValue)
                {
                    events.Add(new CalendarEventDto
                    {
                        Id = assignment.Id,
                        Title = $"Assignment Due: {assignment.Title}",
                        Description = assignment.Instructions,
                        StartDate = assignment.DueDate.Value.AddHours(-1), // Represent as a 1-hour block ending at DueDate
                        EndDate = assignment.DueDate.Value,
                        Type = EventType.Assignment,
                        CourseId = assignment.CourseId,
                        CourseName = assignment.Course?.Title,
                        SourceEntityId = assignment.Id
                    });
                }
            }

            // 3. Fetch Custom Events (Course-specific + Global)
            var customEvents = await _db.CustomEvents.AsNoTracking()
                .Include(c => c.Course)
                .Where(c => c.CourseId == null || courseIds.Contains(c.CourseId.Value))
                .Where(c => (c.EventDate >= start && c.EventDate <= end) || 
                            (c.EventEndDate >= start && c.EventEndDate <= end) ||
                            (c.EventDate <= start && c.EventEndDate >= end))
                .ToListAsync(ct);

            foreach (var custom in customEvents)
            {
                events.Add(new CalendarEventDto
                {
                    Id = custom.Id,
                    Title = custom.Title,
                    Description = custom.Description,
                    StartDate = custom.EventDate,
                    EndDate = custom.EventEndDate,
                    Type = EventType.Custom,
                    CourseId = custom.CourseId,
                    CourseName = custom.Course?.Title,
                    SourceEntityId = custom.Id
                });
            }

        return events.OrderBy(e => e.StartDate).ToList();
    }

    public async Task<CalendarEventDto> CreateCustomEventAsync(Guid userId, CreateCustomEventRequest request, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null || user.Role == UserRole.Student)
        {
            throw new ForbiddenException("Only Tutors and Admins can create events.");
        }

        if (request.CourseId.HasValue)
        {
            var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CourseId.Value, ct);
            if (course == null) throw new NotFoundException("Course", request.CourseId.Value);
            
            // Only Assigned Tutor or Admin can create events for a course
            if (user.Role == UserRole.Tutor && course.AssignedTutorId != userId)
            {
                throw new ForbiddenException("You can only create events for your assigned courses.");
            }
        }
        else
        {
            // Only Admins can create global events (CourseId == null)
            if (user.Role != UserRole.Admin)
            {
                throw new ForbiddenException("Only Admins can create global events.");
            }
        }

            var customEvent = new CustomEvent
            {
                Title = request.Title,
                Description = request.Description,
                EventDate = request.EventDate,
                EventEndDate = request.EventEndDate,
                CourseId = request.CourseId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.CustomEvents.Add(customEvent);
            await _db.SaveChangesAsync(ct);

            // Fetch with Course included to return the name
            if (customEvent.CourseId.HasValue)
            {
                await _db.Entry(customEvent).Reference(c => c.Course).LoadAsync(ct);
            }

            var dto = new CalendarEventDto
            {
                Id = customEvent.Id,
                Title = customEvent.Title,
                Description = customEvent.Description,
                StartDate = customEvent.EventDate,
                EndDate = customEvent.EventEndDate,
                Type = EventType.Custom,
                CourseId = customEvent.CourseId,
                CourseName = customEvent.Course?.Title,
                SourceEntityId = customEvent.Id
            };

        return dto;
    }

    public async Task<CalendarEventDto> UpdateCustomEventAsync(Guid userId, Guid eventId, UpdateCustomEventRequest request, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null || user.Role == UserRole.Student)
        {
            throw new ForbiddenException("Only Tutors and Admins can update events.");
        }

        var customEvent = await _db.CustomEvents.Include(c => c.Course).FirstOrDefaultAsync(c => c.Id == eventId, ct);
        if (customEvent == null)
        {
            throw new NotFoundException("CustomEvent", eventId);
        }

        if (user.Role == UserRole.Tutor && customEvent.CreatedByUserId != userId)
        {
            throw new ForbiddenException("You can only edit your own events.");
        }

        if (request.CourseId.HasValue)
        {
            var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CourseId.Value, ct);
            if (course == null) throw new NotFoundException("Course", request.CourseId.Value);
            
            if (user.Role == UserRole.Tutor && course.AssignedTutorId != userId)
            {
                throw new ForbiddenException("You can only assign events to your assigned courses.");
            }
        }
        else
        {
            if (user.Role != UserRole.Admin)
            {
                throw new ForbiddenException("Only Admins can create global events.");
            }
        }

            customEvent.Title = request.Title;
            customEvent.Description = request.Description;
            customEvent.EventDate = request.EventDate;
            customEvent.EventEndDate = request.EventEndDate;
            customEvent.CourseId = request.CourseId;
            customEvent.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            if (customEvent.CourseId.HasValue && customEvent.Course == null)
            {
                await _db.Entry(customEvent).Reference(c => c.Course).LoadAsync(ct);
            }

            var dto = new CalendarEventDto
            {
                Id = customEvent.Id,
                Title = customEvent.Title,
                Description = customEvent.Description,
                StartDate = customEvent.EventDate,
                EndDate = customEvent.EventEndDate,
                Type = EventType.Custom,
                CourseId = customEvent.CourseId,
                CourseName = customEvent.Course?.Title,
                SourceEntityId = customEvent.Id
            };

        return dto;
    }

    public async Task<bool> DeleteCustomEventAsync(Guid userId, Guid eventId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null || user.Role == UserRole.Student)
        {
            throw new ForbiddenException("Only Tutors and Admins can delete events.");
        }

        var customEvent = await _db.CustomEvents.FirstOrDefaultAsync(c => c.Id == eventId, ct);
        if (customEvent == null)
        {
            throw new NotFoundException("CustomEvent", eventId);
        }

        if (user.Role == UserRole.Tutor && customEvent.CreatedByUserId != userId)
        {
            throw new ForbiddenException("You can only delete your own events.");
        }

        _db.CustomEvents.Remove(customEvent);
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
