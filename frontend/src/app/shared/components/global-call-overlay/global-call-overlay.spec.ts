import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GlobalCallOverlay } from './global-call-overlay';

describe('GlobalCallOverlay', () => {
  let component: GlobalCallOverlay;
  let fixture: ComponentFixture<GlobalCallOverlay>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GlobalCallOverlay],
    }).compileComponents();

    fixture = TestBed.createComponent(GlobalCallOverlay);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
