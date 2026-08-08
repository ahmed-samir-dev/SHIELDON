import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { LanguageService } from '../../../core/services/language.service';
import { ExamEngine } from './exam-engine';

describe('ExamEngine Component (Anti-Cheat & EMS Vertical Slice)', () => {
  let component: ExamEngine;
  let fixture: ComponentFixture<ExamEngine>;

  beforeEach(async () => {
    const toastrMock = {
      success: vi.fn(),
      error: vi.fn(),
      info: vi.fn(),
      warning: vi.fn()
    };

    const translateMock = {
      get: vi.fn().mockReturnValue(of('')),
      instant: vi.fn().mockReturnValue(''),
      use: vi.fn().mockReturnValue(of({}))
    };

    const languageServiceMock = {
      toggleLanguage: vi.fn(),
      setLanguage: vi.fn(),
      getCurrentLanguage: vi.fn().mockReturnValue('en'),
      languageChange$: of('en')
    };

    await TestBed.configureTestingModule({
      imports: [ExamEngine],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastrService, useValue: toastrMock },
        { provide: TranslateService, useValue: translateMock },
        { provide: LanguageService, useValue: languageServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ExamEngine);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the exam-engine component', () => {
    expect(component).toBeTruthy();
  });
});
