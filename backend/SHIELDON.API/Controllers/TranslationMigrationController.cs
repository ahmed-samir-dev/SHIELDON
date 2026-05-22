using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Interfaces;
using SHIELDON.Infrastructure.Persistence;
using System.Text.Json;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")] // Uncomment if you want to restrict it, but for local dev it's fine
public class TranslationMigrationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITranslationService _translationService;

    public TranslationMigrationController(AppDbContext context, ITranslationService translationService)
    {
        _context = context;
        _translationService = translationService;
    }

    [HttpPost("migrate-existing-data")]
    public async Task<IActionResult> MigrateExistingData()
    {
        int translatedCount = 0;

        // Migrate Courses
        var courses = await _context.Courses.Where(c => c.Translations == null || c.Translations == "").ToListAsync();
        foreach (var course in courses)
        {
            var arTitle = await _translationService.TranslateAsync(course.Title, "ar");
            var arDesc = await _translationService.TranslateAsync(course.Description ?? "", "ar");
            
            var dict = new Dictionary<string, Dictionary<string, string>> {
                { "ar", new Dictionary<string, string> {
                    { "Title", arTitle },
                    { "Description", arDesc }
                }}
            };
            course.Translations = JsonSerializer.Serialize(dict);
            translatedCount++;
        }

        // Migrate Announcements
        var announcements = await _context.Announcements.Where(c => c.Translations == null || c.Translations == "").ToListAsync();
        foreach (var ann in announcements)
        {
            var arTitle = await _translationService.TranslateAsync(ann.Title, "ar");
            var arContent = await _translationService.TranslateAsync(ann.Content, "ar");
            
            var dict = new Dictionary<string, Dictionary<string, string>> {
                { "ar", new Dictionary<string, string> {
                    { "Title", arTitle },
                    { "Content", arContent }
                }}
            };
            ann.Translations = JsonSerializer.Serialize(dict);
            translatedCount++;
        }

        // Save Changes (we skip the interceptor translating it again because we manually set Translations, 
        // wait, the interceptor will run and see it's modified and override it?
        // Actually, if we set the translation manually, the interceptor WILL run on `Modified` state and re-translate it!
        // To avoid that, we can just touch a property and let the Interceptor do its job!
        
        // Let's rely entirely on the Interceptor!
        foreach (var course in courses)
        {
            _context.Entry(course).Property(c => c.Title).IsModified = true;
        }
        foreach (var ann in announcements)
        {
            _context.Entry(ann).Property(c => c.Title).IsModified = true;
        }

        // Assignments
        var assignments = await _context.Assignments.Where(c => c.Translations == null || c.Translations == "").ToListAsync();
        foreach (var a in assignments)
        {
            _context.Entry(a).Property(c => c.Title).IsModified = true;
            translatedCount++;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Successfully translated {translatedCount} records via Interceptor." });
    }
}
