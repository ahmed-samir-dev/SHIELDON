import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { examDeviceGuard } from './exam-device.guard';

describe('examDeviceGuard', () => {
  let routerMock: Partial<Router>;

  beforeEach(() => {
    routerMock = {
      navigate: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: routerMock }
      ]
    });
  });

  it('should allow access on desktop screens (width >= 1024px)', () => {
    Object.defineProperty(window, 'innerWidth', { writable: true, configurable: true, value: 1280 });
    const result = TestBed.runInInjectionContext(() => examDeviceGuard({} as any, { url: '/courses/1/exam' } as any));
    expect(result).toBe(true);
  });
});
