import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CourseGrades } from './course-grades';

describe('CourseGrades', () => {
  let component: CourseGrades;
  let fixture: ComponentFixture<CourseGrades>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CourseGrades],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseGrades);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
