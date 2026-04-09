import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

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
          <p>&copy; 2026 SHIELDON LMS & Anti-Cheating Engine. All rights reserved.</p>
        </div>
      </footer>
    </div>
  `,
  styles: [`
    .public-layout-container {
      display: flex;
      flex-direction: column;
      min-height: 100vh;
      background-color: var(--color-neutral-50, #F9FAFB);
    }

    .top-nav {
      background: rgba(255, 255, 255, 0.9);
      backdrop-filter: blur(10px);
      -webkit-backdrop-filter: blur(10px);
      box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
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
      color: #4B5563;
      font-weight: 500;
      text-decoration: none;
      transition: color 0.2s;
      
      &:hover {
        color: #215DAE;
      }
    }

    .nav-btn {
      background: #215DAE;
      color: white;
      padding: 0.5rem 1.25rem;
      border-radius: 9999px;
      font-weight: 600;
      text-decoration: none;
      transition: transform 0.2s, background 0.2s;

      &:hover {
        transform: translateY(-2px);
        background: #16407B;
      }
    }

    .main-content {
      flex: 1;
    }

    .footer {
      background: #0B1120;
      color: #9CA3AF;
      padding: 2rem 0;
      text-align: center;
      font-size: 0.875rem;
    }
  `]
})
export class PublicLayout {}
