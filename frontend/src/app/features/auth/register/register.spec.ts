import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { Register } from './register';

describe('Register Component (Auth Vertical Slice)', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;

  beforeEach(async () => {
    const toastrMock = {
      success: vi.fn(),
      error: vi.fn(),
      info: vi.fn(),
      warning: vi.fn()
    };

    const translateMock = {
      get: vi.fn().mockReturnValue(of('')),
      instant: vi.fn().mockReturnValue(''),
      use: vi.fn().mockReturnValue(of({}))
    };

    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastrService, useValue: toastrMock },
        { provide: TranslateService, useValue: translateMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the register component', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize registerForm with controls', () => {
    expect(component.registerForm).toBeDefined();
    expect(component.registerForm.contains('firstName')).toBe(true);
    expect(component.registerForm.contains('lastName')).toBe(true);
    expect(component.registerForm.contains('email')).toBe(true);
    expect(component.registerForm.contains('password')).toBe(true);
    expect(component.registerForm.contains('confirmPassword')).toBe(true);
  });
});
