import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { LanguageService } from '../../core/services/language.service';
import { ChatService } from '../../core/services/chat.service';
import { SecuritySignalrService } from '../../core/services/security-signalr.service';
import { DashboardLayout } from './dashboard-layout';

describe('DashboardLayout Component', () => {
  let component: DashboardLayout;
  let fixture: ComponentFixture<DashboardLayout>;

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

    const languageServiceMock = {
      toggleLanguage: vi.fn(),
      setLanguage: vi.fn(),
      getCurrentLanguage: vi.fn().mockReturnValue('en'),
      languageChange$: of('en')
    };

    const chatServiceMock = {
      unreadCount: vi.fn().mockReturnValue(0),
      incomingCall$: of(null),
      startConnection: vi.fn(),
      stopConnection: vi.fn()
    };

    const securitySignalrMock = {
      startConnection: vi.fn(),
      stopConnection: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [DashboardLayout],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastrService, useValue: toastrMock },
        { provide: TranslateService, useValue: translateMock },
        { provide: LanguageService, useValue: languageServiceMock },
        { provide: ChatService, useValue: chatServiceMock },
        { provide: SecuritySignalrService, useValue: securitySignalrMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardLayout);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create the dashboard layout component', () => {
    expect(component).toBeTruthy();
  });
});
