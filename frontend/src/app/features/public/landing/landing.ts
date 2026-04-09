import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  template: `
    <!-- Hero Section -->
    <section class="hero-section">
      <div class="container hero-content">
        <div class="hero-text animate-slide-up">
          <div class="badge">Next-Gen LMS</div>
          <h1 class="font-outfit">Integrity You Can Trust.</h1>
          <p>
            An advanced Learning Management System natively integrated with a robust
            Anti-Cheating Engine for absolute academic integrity.
          </p>
          <div class="cta-group">
            <a routerLink="/register" class="btn-primary-glow">Start For Free</a>
            <a href="#features" class="btn-secondary-outline">Explore Features</a>
          </div>
        </div>
        <div class="hero-visual animate-slide-up" style="animation-delay: 0.2s;">
          <div class="glass-dash-mockup">
            <div class="mockup-header">
              <span></span><span></span><span></span>
            </div>
            <div class="mockup-body">
              <div class="skeleton-chart"></div>
              <div class="skeleton-stat-grid">
                <div class="skeleton-card"></div>
                <div class="skeleton-card"></div>
                <div class="skeleton-card"></div>
              </div>
            </div>
          </div>
        </div>
      </div>
      <!-- Background Decorations -->
      <div class="blob blob-1"></div>
      <div class="blob blob-2"></div>
    </section>

    <!-- Features Section -->
    <section id="features" class="features-section bg-gradient">
      <div class="container">
        <h2 class="text-center font-outfit text-3xl mb-10">Why Choose SHIELDON?</h2>
        <div class="feature-grid">
          
          <div class="feature-card hover-lift">
            <div class="icon-wrapper">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-shield-check"><path d="M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"/><path d="m9 12 2 2 4-4"/></svg>
            </div>
            <h3>Advanced Anti-Cheat</h3>
            <p>Smart focus tracking, tab monitoring, and fullscreen persistence to secure remote testing.</p>
          </div>

          <div class="feature-card hover-lift">
            <div class="icon-wrapper">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-book-open"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>            
            </div>
            <h3>Course Management</h3>
            <p>Create intuitive course structures, share materials, and keep students engaged easily.</p>
          </div>

          <div class="feature-card hover-lift">
            <div class="icon-wrapper">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-activity"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/></svg>
            </div>
            <h3>Live Proctor Dashboard</h3>
            <p>Tutors can monitor active exam sessions and track student violations in real time.</p>
          </div>

        </div>
      </div>
    </section>
  `,
  styles: [`
    .hero-section {
      position: relative;
      overflow: hidden;
      padding: 6rem 0 8rem;
      background: #F9FAFB;
      min-height: calc(100vh - 72px);
      display: flex;
      align-items: center;
    }

    .hero-content {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 4rem;
      align-items: center;
      position: relative;
      z-index: 10;
    }

    .badge {
      display: inline-block;
      padding: 0.5rem 1rem;
      background: #E0E7FF;
      color: #3730A3;
      border-radius: 9999px;
      font-weight: 600;
      font-size: 0.875rem;
      margin-bottom: 1.5rem;
    }

    .hero-text {
      h1 {
        font-size: 4rem;
        font-weight: 800;
        line-height: 1.1;
        color: #111827;
        margin-bottom: 1.5rem;
      }
      p {
        font-size: 1.25rem;
        color: #4B5563;
        margin-bottom: 2.5rem;
        line-height: 1.6;
        max-width: 500px;
      }
    }

    .cta-group {
      display: flex;
      gap: 1rem;
      
      .btn-primary-glow {
        background: linear-gradient(135deg, #215DAE, #1898A1);
        color: white;
        padding: 0.875rem 2rem;
        border-radius: 8px;
        font-weight: 600;
        font-size: 1.125rem;
        text-decoration: none;
        box-shadow: 0 10px 15px -3px rgba(33, 93, 174, 0.4);
        transition: all 0.3s;
        
        &:hover {
          transform: translateY(-2px);
          box-shadow: 0 14px 20px -3px rgba(33, 93, 174, 0.5);
        }
      }

      .btn-secondary-outline {
        background: white;
        color: #374151;
        border: 2px solid #E5E7EB;
        padding: 0.875rem 2rem;
        border-radius: 8px;
        font-weight: 600;
        font-size: 1.125rem;
        text-decoration: none;
        transition: all 0.3s;
        
        &:hover {
          background: #F3F4F6;
          border-color: #D1D5DB;
        }
      }
    }

    // Glass Mockup
    .glass-dash-mockup {
      background: rgba(255, 255, 255, 0.6);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.8);
      border-radius: 1rem;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.1);
      padding: 1.5rem;
      transform: perspective(1000px) rotateY(-5deg) rotateX(5deg);
      transition: transform 0.3s;
      
      &:hover {
        transform: perspective(1000px) rotateY(0deg) rotateX(0deg);
      }
      
      .mockup-header {
        display: flex;
        gap: 8px;
        margin-bottom: 2rem;
        
        span {
          width: 12px;
          height: 12px;
          border-radius: 50%;
          background: #E5E7EB;
          
          &:nth-child(1) { background: #EF4444; }
          &:nth-child(2) { background: #F59E0B; }
          &:nth-child(3) { background: #10B981; }
        }
      }
      
      .skeleton-chart {
        height: 200px;
        background: linear-gradient(90deg, #F3F4F6 25%, #E5E7EB 50%, #F3F4F6 75%);
        background-size: 200% 100%;
        animation: shimmer 2s infinite linear;
        border-radius: 0.5rem;
        margin-bottom: 1.5rem;
      }
      
      .skeleton-stat-grid {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 1rem;
        
        .skeleton-card {
          height: 80px;
          background: #F3F4F6;
          border-radius: 0.5rem;
        }
      }
    }

    // Decorative Blobs
    .blob {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      z-index: 1;
      opacity: 0.5;
    }
    
    .blob-1 {
      top: -10%;
      right: -5%;
      width: 500px;
      height: 500px;
      background: #E0E7FF;
    }
    
    .blob-2 {
      bottom: -10%;
      left: 10%;
      width: 400px;
      height: 400px;
      background: #CCFBF1;
    }

    // Features Section
    .features-section {
      padding: 6rem 0;
      background: white;
      
      h2 {
        text-align: center;
        font-size: 2.25rem;
        font-weight: 700;
        margin-bottom: 4rem;
        color: #111827;
      }
    }

    .feature-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 2rem;
    }

    .feature-card {
      padding: 2rem;
      border-radius: 1rem;
      background: #F9FAFB;
      border: 1px solid #F3F4F6;
      transition: all 0.3s;
      
      .icon-wrapper {
        width: 48px;
        height: 48px;
        background: #E0F2FE;
        color: #0369A1;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 1.5rem;
      }
      
      h3 {
        font-size: 1.25rem;
        font-weight: 700;
        margin-bottom: 0.75rem;
        color: #1F2937;
      }
      
      p {
        color: #6B7280;
        line-height: 1.6;
      }
    }

    .hover-lift:hover {
      transform: translateY(-5px);
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.05);
      background: white;
    }

    @keyframes shimmer {
      0% { background-position: 200% 0; }
      100% { background-position: -200% 0; }
    }

    @media (max-width: 991px) {
      .hero-content {
        grid-template-columns: 1fr;
        text-align: center;
      }
      .hero-text h1 { font-size: 3rem; }
      .hero-text p { margin: 0 auto 2.5rem; }
      .cta-group { justify-content: center; }
    }
  `]
})
export class Landing {}
