using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SHIELDON.Domain.Common;
using SHIELDON.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Reflection;

namespace SHIELDON.Infrastructure.Data.Interceptors;

public class AutoTranslationInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public AutoTranslationInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = eventData.Context.ChangeTracker.Entries<ITranslatable>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var translatableProperties = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<TranslatableAttribute>() != null && p.PropertyType == typeof(string))
                .ToList();

            if (!translatableProperties.Any()) continue;

            // Load existing translations if any
            var existingTranslationsJson = entry.Entity.Translations;
            var translationsDict = new Dictionary<string, Dictionary<string, string>>();
            
            if (!string.IsNullOrEmpty(existingTranslationsJson))
            {
                try 
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(existingTranslationsJson);
                    if (parsed != null) translationsDict = parsed;
                }
                catch { /* Ignore invalid JSON */ }
            }

            if (!translationsDict.ContainsKey("ar"))
            {
                translationsDict["ar"] = new Dictionary<string, string>();
            }

            bool hasChanges = false;
            ITranslationService? translationService = null;
            IServiceScope? scope = null;

            foreach (var prop in translatableProperties)
            {
                // Only translate if the property was modified or it's a new entity
                if (entry.State == EntityState.Added || entry.Property(prop.Name).IsModified)
                {
                    var englishText = prop.GetValue(entry.Entity) as string;
                    if (!string.IsNullOrWhiteSpace(englishText))
                    {
                        if (translationService == null)
                        {
                            scope = _serviceProvider.CreateScope();
                            translationService = scope.ServiceProvider.GetRequiredService<ITranslationService>();
                        }

                        var arabicTranslation = await translationService.TranslateAsync(englishText, "ar", "en", cancellationToken);
                        translationsDict["ar"][prop.Name] = arabicTranslation;
                        hasChanges = true;
                    }
                }
            }

            if (scope != null)
            {
                scope.Dispose();
            }

            if (hasChanges)
            {
                entry.Entity.Translations = JsonSerializer.Serialize(translationsDict);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
