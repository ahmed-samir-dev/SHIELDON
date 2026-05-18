import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <div class="public-layout-container">
      <nav class="top-nav">
        <div class="nav-content container">
          <a routerLink="/" class="logo animate-fade-in">
            SHIELDON
          </a>
          <div class="nav-links">
            <button class="theme-toggle-btn" (click)="themeService.toggleTheme()" [attr.title]="themeService.activeTheme() === 'dark' ? 'Switch to Light Mode' : 'Switch to Dark Mode'">
              @if (themeService.activeTheme() === 'dark') {
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4"/></svg>
              } @else {
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/></svg>
              }
            </button>
            <a routerLink="/login" class="nav-link">Login</a>
            <a routerLink="/register" class="nav-btn">Get Started</a>
          </div>
        </div>
      </nav>

      <main class="main-content">
        <router-outlet></router-outlet>
      </main>

      <footer class="footer">
        <div class="container">
          <p>&copy; 2026 SHIELDON. All Rights Reserved.</p>
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
      background: #0B315B;
      color: #A0CFF4;
      padding: 1.5rem 0;
      text-align: center;
      font-size: 0.875rem;
      border-top: 1px solid rgba(255, 255, 255, 0.1);
      
      p {
        margin: 0;
        font-weight: 500;
        letter-spacing: 0.025em;
      }
    }
  `]
})
export class PublicLayout {
  themeService = inject(ThemeService);
}
