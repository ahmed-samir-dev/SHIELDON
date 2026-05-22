import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { ThemeService } from '../../core/services/theme.service';
import { LanguageService } from '../../core/services/language.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, TranslateModule],
  template: `
    <div class="public-layout-container">
      <nav class="top-nav">
        <div class="nav-content container">
          <a routerLink="/" class="logo animate-fade-in">
            SHIELDON
          </a>
          <div class="nav-links">
            <!-- Language Toggle -->
            <button class="theme-toggle-btn language-toggle-btn" (click)="languageService.toggleLanguage()" [attr.title]="languageService.getCurrentLanguage() === 'en' ? 'Switch to Arabic' : 'Switch to English'" style="font-weight: 600; font-size: 14px;">
              {{ languageService.getCurrentLanguage() === 'en' ? 'AR' : 'EN' }}
            </button>
            <button class="theme-toggle-btn" (click)="themeService.toggleTheme()" [attr.title]="themeService.activeTheme() === 'dark' ? 'Switch to Light Mode' : 'Switch to Dark Mode'">
              @if (themeService.activeTheme() === 'dark') {
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4"/></svg>
              } @else {
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/></svg>
              }
            </button>
            <a routerLink="/login" class="nav-link">{{ 'PUBLIC.LOGIN' | translate }}</a>
            <a routerLink="/register" class="nav-btn">{{ 'PUBLIC.GET_STARTED' | translate }}</a>
          </div>
        </div>
      </nav>

      <main class="main-content">
        <router-outlet></router-outlet>
      </main>

      <footer class="footer">
        <div class="container footer-content">
          <div class="brand">
            SHIELDON
          </div>
          <p>{{ 'PUBLIC.FOOTER_COPYRIGHT' | translate }}</p>
        </div>
      </footer>
    </div>
  `,
  styles: [`
    .public-layout-container {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
      background-color: var(--theme-bg-main);
    }

    .top-nav {
      background: var(--theme-bg-surface);
      backdrop-filter: blur(10px);
      -webkit-backdrop-filter: blur(10px);
      box-shadow: var(--theme-shadow-sm);
      position: sticky;
      top: 0;
      z-index: 1000;
      padding: 1rem 0;
    }

    .nav-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .logo {
      font-size: 1.5rem;
      font-weight: 800;
      background: linear-gradient(90deg, #215DAE, #1898A1);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      text-decoration: none;
    }

    .nav-links {
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }

    .nav-link {
      color: var(--theme-text-secondary);
      font-weight: 500;
      text-decoration: none;
      transition: color 0.2s;
      
      &:hover {
        color: var(--color-primary-base, #215DAE);
      }
    }

    .theme-toggle-btn {
      background: none;
      border: none;
      color: var(--theme-text-muted);
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: color 0.2s;
      
      &:hover {
        color: var(--theme-text-main);
      }
    }

    .nav-btn {
      background: var(--color-primary-base, #215DAE);
      color: var(--theme-text-inverse);
      padding: 0.5rem 1.25rem;
      border-radius: 9999px;
      font-weight: 600;
      text-decoration: none;
      transition: transform 0.2s, background 0.2s;

      &:hover {
        transform: translateY(-2px);
        background: var(--color-primary-dark, #16407B);
      }
    }

    .main-content {
      flex: 1;
    }

    .footer {
      background: linear-gradient(135deg, #F0F4F8 0%, #E2E8F0 100%);
      color: #475569;
      padding: 1.25rem 0;
      font-size: 0.9rem;
      border-top: 1px solid #CBD5E1;
      position: relative;

      :host-context([data-theme="dark"]) & {
        background: linear-gradient(135deg, #071324 0%, #030812 100%);
        color: #94A3B8;
        border-top: 1px solid rgba(33, 93, 174, 0.3);
      }

      /* Premium Top Gradient Line */
      &::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 2px;
        background: linear-gradient(90deg, #0B315B, #1898A1, #215DAE, #1898A1, #0B315B);
        background-size: 200% auto;
        animation: gradientSweep 6s linear infinite;
      }

      .footer-content {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        gap: 0.25rem;
        
        .brand {
          font-family: 'Outfit', sans-serif;
          font-weight: 700;
          font-size: 1.1rem;
          color: #0F172A;
          letter-spacing: -0.01em;

          :host-context([data-theme="dark"]) & {
            color: var(--theme-text-main);
          }
        }
        
        p {
          margin: 0;
          font-weight: 500;
          letter-spacing: 0.01em;
        }
      }
    }
    
    @keyframes gradientSweep {
      0% { background-position: 0% center; }
      100% { background-position: 200% center; }
    }
  `]
})
export class PublicLayout {
  themeService = inject(ThemeService);
  languageService = inject(LanguageService);
}
