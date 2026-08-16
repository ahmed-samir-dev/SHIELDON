import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { Login } from './login';

describe('Login Component Security Specs', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;

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
      imports: [Login],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastrService, useValue: toastrMock },
        { provide: TranslateService, useValue: translateMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should mark invalid email format as invalid form control', () => {
    component.loginForm.patchValue({ email: 'not-an-email', password: 'ValidPassword123!' });
    expect(component.loginForm.controls['email'].valid).toBe(false);
    expect(component.loginForm.valid).toBe(false);
  });

  it('should invalidate empty form', () => {
    component.loginForm.patchValue({ email: '', password: '' });
    expect(component.loginForm.valid).toBe(false);
  });

  it('should have password form control initialized', () => {
    expect(component.loginForm.contains('password')).toBe(true);
  });
});
