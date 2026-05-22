using Microsoft.AspNetCore.Http;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.Infrastructure.Services;

public class CurrentLanguageProvider : ICurrentLanguageProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentLanguageProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentLanguage
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "en";

            var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
            if (string.IsNullOrEmpty(acceptLanguage)) return "en";

            // Expecting something like "ar" or "ar-EG" or "en,ar;q=0.9"
            if (acceptLanguage.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            {
                return "ar";
            }

            return "en";
        }
    }
}
