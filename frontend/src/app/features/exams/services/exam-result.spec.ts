import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ExamResultService } from './exam-result';

describe('ExamResultService', () => {
  let service: ExamResultService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ExamResultService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(ExamResultService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
