import { HttpInterceptorFn } from '@angular/common/http';

export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  // Read directly from localStorage to avoid circular DI dependency
  // HttpClient -> Interceptor -> LanguageService -> TranslateService -> HttpClient
  const currentLang = localStorage.getItem('shieldon_lang') || 'en';

  const clonedRequest = req.clone({
    setHeaders: {
      'Accept-Language': currentLang
    }
  });

  return next(clonedRequest);
};
