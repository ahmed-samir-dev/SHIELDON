import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExamEngine } from './exam-engine';

describe('ExamEngine', () => {
  let component: ExamEngine;
  let fixture: ComponentFixture<ExamEngine>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExamEngine],
    }).compileComponents();

    fixture = TestBed.createComponent(ExamEngine);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
