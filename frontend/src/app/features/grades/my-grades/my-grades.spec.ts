import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { LanguageService } from '../../../core/services/language.service';
import { MyGrades } from './my-grades';

describe('MyGrades Component (Grades Vertical Slice)', () => {
  let component: MyGrades;
  let fixture: ComponentFixture<MyGrades>;

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
      imports: [MyGrades],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastrService, useValue: toastrMock },
        { provide: TranslateService, useValue: translateMock },
        { provide: LanguageService, useValue: languageServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MyGrades);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the my-grades component', () => {
    expect(component).toBeTruthy();
  });
});
