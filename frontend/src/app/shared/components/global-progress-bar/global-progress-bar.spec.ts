import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { GlobalProgressBar } from './global-progress-bar';

describe('GlobalProgressBar Component', () => {
  let component: GlobalProgressBar;
  let fixture: ComponentFixture<GlobalProgressBar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GlobalProgressBar, CommonModule],
    }).compileComponents();

    fixture = TestBed.createComponent(GlobalProgressBar);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the global progress bar component', () => {
    expect(component).toBeTruthy();
  });
});
