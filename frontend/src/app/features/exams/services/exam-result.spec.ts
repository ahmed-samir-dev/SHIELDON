import { TestBed } from '@angular/core/testing';

import { ExamResult } from './exam-result';

describe('ExamResult', () => {
  let service: ExamResult;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ExamResult);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
