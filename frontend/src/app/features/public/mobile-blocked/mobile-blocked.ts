import { Component } from '@angular/core';

@Component({
  selector: 'app-mobile-blocked',
  standalone: true,
  imports: [],
  template: `
    <div class="blocked-container">
      <div class="glass-dash-mockup">
        <div class="icon-wrapper">
          <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="lucide lucide-monitor-x"><path d="m14.5 12.5-5-5"/><path d="m9.5 12.5 5-5"/><rect width="20" height="14" x="2" y="3" rx="2"/><path d="M12 17v4"/><path d="M8 21h8"/></svg>
        </div>
        
        <h1 class="font-outfit text-center">Mobile & Tablet Access Restricted</h1>
        
        <p class="text-center">
          SHIELDON is an advanced anti-cheating LMS that requires continuous 
          desktop tracking (fullscreen, keyboard mapping, mouse monitoring).<br><br>
          Please access your exam or dashboard via a laptop or desktop computer.
        </p>

        <div class="requirements-box">
          <h3>System Requirements:</h3>
          <ul>
            <li>Windows / macOS / Linux Desktop OS</li>
            <li>Minimum screen resolution: 1024x768</li>
            <li>Modern Web Browser (Chrome, Firefox, Edge)</li>
            <li>Working Webcam & Microphone</li>
          </ul>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .blocked-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #1898A1 0%, #0B1120 100%);
      padding: 1rem;
    }

    .glass-dash-mockup {
      background: rgba(255, 255, 255, 0.95);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.3);
      border-radius: 1.5rem;
      padding: 3rem 2rem;
      max-width: 500px;
      width: 100%;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
      animation: slideUp 0.5s ease-out forwards;
    }

    .icon-wrapper {
      width: 80px;
      height: 80px;
      background: #FEE2E2;
      color: #EF4444;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 2rem;
    }

    h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: #111827;
      margin-bottom: 1rem;
      line-height: 1.2;
    }

    p {
      color: #4B5563;
      line-height: 1.6;
      margin-bottom: 2rem;
    }

    .requirements-box {
      background: #F3F4F6;
      padding: 1.5rem;
      border-radius: 1rem;
      border-left: 4px solid #215DAE;
      
      h3 {
        font-family: 'Outfit', sans-serif;
        font-size: 1.125rem;
        color: #1F2937;
        margin-bottom: 1rem;
      }
      
      ul {
        list-style: none;
        padding: 0;
        margin: 0;
        
        li {
          position: relative;
          padding-left: 1.5rem;
          margin-bottom: 0.5rem;
          color: #4B5563;
          font-size: 0.875rem;
          
          &::before {
            content: "•";
            color: #215DAE;
            position: absolute;
            left: 0;
            font-weight: bold;
          }
        }
      }
    }
  `]
})
export class MobileBlocked {}
