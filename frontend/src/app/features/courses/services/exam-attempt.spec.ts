import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ExamAttemptService } from './exam-attempt';

describe('ExamAttemptService', () => {
  let service: ExamAttemptService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ExamAttemptService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(ExamAttemptService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
