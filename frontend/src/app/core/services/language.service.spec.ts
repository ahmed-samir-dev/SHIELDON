import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from './language.service';

describe('LanguageService', () => {
  let service: LanguageService;
  let translateMock: Partial<TranslateService>;

  beforeEach(() => {
    translateMock = {
      setDefaultLang: vi.fn(),
      use: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        LanguageService,
        { provide: TranslateService, useValue: translateMock }
      ]
    });

    service = TestBed.inject(LanguageService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should set language to ar and update dir attribute to rtl', () => {
    service.setLanguage('ar');
    expect(service.getCurrentLanguage()).toBe('ar');
    expect(document.documentElement.getAttribute('dir')).toBe('rtl');
  });

  it('should toggle language from default to next language', () => {
    const current = service.getCurrentLanguage();
    service.toggleLanguage();
    expect(service.getCurrentLanguage()).not.toBe(current);
  });
});
