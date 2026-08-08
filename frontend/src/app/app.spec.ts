import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { LanguageService } from './core/services/language.service';
import { App } from './app';

describe('App Root Component', () => {
  beforeEach(async () => {
    const languageServiceMock = {
      toggleLanguage: vi.fn(),
      setLanguage: vi.fn(),
      getCurrentLanguage: vi.fn().mockReturnValue('en'),
      languageChange$: of('en')
    };

    await TestBed.configureTestingModule({
      imports: [App, TranslateModule.forRoot()],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: LanguageService, useValue: languageServiceMock }
      ]
    }).compileComponents();
  });

  it('should create the app root component', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});
