import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExamResultPage } from './exam-result-page';

describe('ExamResultPage', () => {
  let component: ExamResultPage;
  let fixture: ComponentFixture<ExamResultPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExamResultPage],
    }).compileComponents();

    fixture = TestBed.createComponent(ExamResultPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
