import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-exam-device-blocked',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="blocked-container">
      <div class="glass-card">
        <div class="icon-wrapper">
          <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect width="20" height="14" x="2" y="3" rx="2"/>
            <line x1="8" x2="16" y1="21" y2="21"/>
            <line x1="12" x2="12" y1="17" y2="21"/>
            <path d="m15 9-6 6"/>
            <path d="m9 9 6 6"/>
          </svg>
        </div>

        <h1 class="title">Desktop Required for Exams</h1>

        <p class="description">
          Taking exams on <strong>SHIELDON</strong> requires a laptop or desktop computer 
          (screen width &ge; 1024px) to enforce browser-based anti-cheating, keyboard tracking, 
          and full-screen proctoring.
        </p>

        <div class="notice-box">
          <h3>Good News:</h3>
          <p>
            You can use your phone or tablet for all other SHIELDON modules including 
            <strong>Announcements, Course Materials, Assignments, Attendance Scanning, Profile & Payments</strong>.
            Only the <em>Exam Engine</em> requires a larger screen.
          </p>
        </div>

        <div class="requirements-box">
          <h3>Exam System Requirements:</h3>
          <ul>
            <li>Laptop or Desktop Computer (Windows, macOS, Linux)</li>
            <li>Minimum screen resolution: 1024 &times; 768</li>
            <li>Modern Web Browser (Chrome, Firefox, Edge)</li>
            <li>Keyboard & Mouse / Trackpad</li>
          </ul>
        </div>

        <div class="actions">
          <a routerLink="/courses" class="btn btn-primary">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="m12 19-7-7 7-7"/>
              <path d="M19 12H5"/>
            </svg>
            Return to Dashboard
          </a>
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
      background: linear-gradient(135deg, #215DAE 0%, #0B1120 100%);
      padding: 1.5rem;
    }

    .glass-card {
      background: rgba(255, 255, 255, 0.96);
      backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.3);
      border-radius: 1.5rem;
      padding: 2.5rem 2rem;
      max-width: 520px;
      width: 100%;
      box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
      animation: slideUp 0.4s ease-out forwards;
    }

    .icon-wrapper {
      width: 72px;
      height: 72px;
      background: #FEE2E2;
      color: #DC2626;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1.5rem;
    }

    .title {
      font-size: 1.6rem;
      font-weight: 700;
      color: #111827;
      text-align: center;
      margin-bottom: 1rem;
      line-height: 1.25;
    }

    .description {
      color: #4B5563;
      font-size: 0.95rem;
      line-height: 1.6;
      text-align: center;
      margin-bottom: 1.5rem;

      strong {
        color: #1F2937;
      }
    }

    .notice-box {
      background: #EFF6FF;
      border-left: 4px solid #215DAE;
      padding: 1rem 1.25rem;
      border-radius: 0.75rem;
      margin-bottom: 1.25rem;

      h3 {
        font-size: 0.95rem;
        font-weight: 700;
        color: #1E40AF;
        margin-bottom: 0.35rem;
      }

      p {
        font-size: 0.875rem;
        color: #1E3A8A;
        line-height: 1.5;
        margin: 0;
      }
    }

    .requirements-box {
      background: #F9FAFB;
      border: 1px solid #E5E7EB;
      padding: 1.25rem;
      border-radius: 0.75rem;
      margin-bottom: 1.5rem;

      h3 {
        font-size: 0.9rem;
        font-weight: 700;
        color: #374151;
        margin-bottom: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }

      ul {
        list-style: none;
        padding: 0;
        margin: 0;

        li {
          position: relative;
          padding-left: 1.25rem;
          margin-bottom: 0.4rem;
          color: #4B5563;
          font-size: 0.85rem;

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

    .actions {
      display: flex;
      justify-content: center;
    }

    .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      border-radius: 0.75rem;
      font-weight: 600;
      font-size: 0.95rem;
      text-decoration: none;
      transition: all 0.2s ease;
      width: 100%;
    }

    .btn-primary {
      background: linear-gradient(135deg, #215DAE 0%, #16407B 100%);
      color: #FFFFFF;
      box-shadow: 0 4px 12px rgba(33, 93, 174, 0.3);

      &:hover {
        transform: translateY(-1px);
        box-shadow: 0 6px 16px rgba(33, 93, 174, 0.4);
      }
    }

    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }
  `]
})
export class ExamDeviceBlocked {}
