import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MobileBlocked } from './mobile-blocked';

describe('MobileBlocked', () => {
  let component: MobileBlocked;
  let fixture: ComponentFixture<MobileBlocked>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MobileBlocked],
    }).compileComponents();

    fixture = TestBed.createComponent(MobileBlocked);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
