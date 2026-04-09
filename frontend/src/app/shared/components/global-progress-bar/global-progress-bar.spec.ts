import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GlobalProgressBar } from './global-progress-bar';

describe('GlobalProgressBar', () => {
  let component: GlobalProgressBar;
  let fixture: ComponentFixture<GlobalProgressBar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GlobalProgressBar],
    }).compileComponents();

    fixture = TestBed.createComponent(GlobalProgressBar);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
