using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SHIELDON.Domain.Common;
using SHIELDON.Application.Interfaces;
using System.Text.Json;
using System.Reflection;

namespace SHIELDON.Infrastructure.Data.Interceptors;

public class TranslationMaterializationInterceptor : IMaterializationInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public TranslationMaterializationInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (entity is ITranslatable translatable && !string.IsNullOrEmpty(translatable.Translations))
        {
            // Resolve the current language provider using a scope, since this interceptor is registered globally
            using var scope = _serviceProvider.CreateScope();
            var languageProvider = scope.ServiceProvider.GetService<ICurrentLanguageProvider>();
            
            if (languageProvider != null && languageProvider.CurrentLanguage == "ar")
            {
                try
                {
                    var translationsDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(translatable.Translations);
                    if (translationsDict != null && translationsDict.TryGetValue("ar", out var arTranslations))
                    {
                        var entityType = entity.GetType();
                        var translatableProperties = entityType.GetProperties()
                            .Where(p => p.GetCustomAttribute<TranslatableAttribute>() != null && p.PropertyType == typeof(string));

                        foreach (var prop in translatableProperties)
                        {
                            if (arTranslations.TryGetValue(prop.Name, out var arabicText) && !string.IsNullOrEmpty(arabicText))
                            {
                                // Overwrite the English property with the Arabic one in memory
                                prop.SetValue(entity, arabicText);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors and fallback to English
                }
            }
        }

        return entity;
    }
}
