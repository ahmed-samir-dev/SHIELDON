import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AntiCheatService } from '../anti-cheat/anti-cheat.service';
import { ViolationService } from '../../core/services/violation.service';

describe('AntiCheat Service Security Specs', () => {
  let service: AntiCheatService;
  let violationServiceMock: any;

  beforeEach(() => {
    violationServiceMock = {
      logViolationBatch: vi.fn(),
      sendHeartbeat: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        AntiCheatService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ViolationService, useValue: violationServiceMock }
      ]
    });

    service = TestBed.inject(AntiCheatService);
    service.resetState();
  });

  it('should start with a clean state (0 strikes)', () => {
    expect(service.strikeScore()).toBe(0);
    expect(service.strikeLevel()).toBe(0);
  });

  it('should reset state cleanly when resetState() is called', () => {
    service.dismissStrikeOne();
    service.dismissStrikeTwo();
    service.resetState();

    expect(service.strikeScore()).toBe(0);
    expect(service.strikeLevel()).toBe(0);
    expect(service.strikeOneAcknowledged()).toBe(false);
  });

  it('should format strike count correctly', () => {
    expect(service.getFormattedStrikeCount()).toBe('0');
  });
});
