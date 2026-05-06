import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MyGrades } from './my-grades';

describe('MyGrades', () => {
  let component: MyGrades;
  let fixture: ComponentFixture<MyGrades>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyGrades],
    }).compileComponents();

    fixture = TestBed.createComponent(MyGrades);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
