import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TutorResultsPanel } from './tutor-results-panel';

describe('TutorResultsPanel', () => {
  let component: TutorResultsPanel;
  let fixture: ComponentFixture<TutorResultsPanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TutorResultsPanel],
    }).compileComponents();

    fixture = TestBed.createComponent(TutorResultsPanel);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
