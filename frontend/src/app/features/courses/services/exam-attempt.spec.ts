import { TestBed } from '@angular/core/testing';

import { ExamAttempt } from './exam-attempt';

describe('ExamAttempt', () => {
  let service: ExamAttempt;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ExamAttempt);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
