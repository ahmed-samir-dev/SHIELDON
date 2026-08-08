import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ThemeService]
    });
    service = TestBed.inject(ThemeService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should toggle dark/light theme signal', () => {
    const initialTheme = service.activeTheme();
    service.toggleTheme();
    expect(service.activeTheme()).not.toBe(initialTheme);
  });

  it('should set specific theme and update data-theme attribute', () => {
    service.setTheme('dark');
    expect(service.isDark).toBe(true);
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
