import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ExamResultPage } from './exam-result-page';

describe('ExamResultPage Component (Exams Vertical Slice)', () => {
  let component: ExamResultPage;
  let fixture: ComponentFixture<ExamResultPage>;

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

    await TestBed.configureTestingModule({
      imports: [ExamResultPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastrService, useValue: toastrMock },
        { provide: TranslateService, useValue: translateMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ExamResultPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the exam-result-page component', () => {
    expect(component).toBeTruthy();
  });
});
