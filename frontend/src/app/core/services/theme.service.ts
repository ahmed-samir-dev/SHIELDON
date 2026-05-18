import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'shieldon_theme';
  
  // Use Angular Signals to make the theme state reactive
  private themeSignal = signal<Theme>('light');
  public readonly activeTheme = this.themeSignal.asReadonly();

  constructor() {
    this.initializeTheme();
  }

  /**
   * Initializes the theme on application load based on localStorage
   */
  private initializeTheme(): void {
    const savedTheme = localStorage.getItem(this.THEME_KEY) as Theme;
    const initialTheme = savedTheme === 'dark' ? 'dark' : 'light';
    this.setTheme(initialTheme);
  }

  /**
   * Toggles between light and dark mode
   */
  public toggleTheme(): void {
    const newTheme = this.themeSignal() === 'light' ? 'dark' : 'light';
    this.setTheme(newTheme);
  }

  /**
   * Sets a specific theme, saves to localStorage, and updates the HTML attribute
   * @param theme The theme to set ('light' | 'dark')
   */
  public setTheme(theme: Theme): void {
    this.themeSignal.set(theme);
    localStorage.setItem(this.THEME_KEY, theme);
    
    // Apply to the document element (<html>)
    if (theme === 'dark') {
      document.documentElement.setAttribute('data-theme', 'dark');
    } else {
      document.documentElement.removeAttribute('data-theme'); // default is light
    }
  }

  /**
   * Returns true if the current theme is dark
   */
  public get isDark(): boolean {
    return this.themeSignal() === 'dark';
  }
}
