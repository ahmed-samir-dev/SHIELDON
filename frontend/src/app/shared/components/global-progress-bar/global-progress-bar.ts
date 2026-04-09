import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingService } from '../../../core/services/loading.service';

@Component({
  selector: 'app-global-progress-bar',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (isLoading()) {
      <div class="global-progress-container">
        <div class="global-progress-bar"></div>
      </div>
    }
  `,
  styles: [`
    .global-progress-container {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 4px;
      z-index: 9999;
      background-color: transparent;
      overflow: hidden;
    }

    .global-progress-bar {
      width: 50%;
      height: 100%;
      /* Brand gradient for wow factor */
      background: linear-gradient(90deg, #215DAE, #1898A1, #215DAE);
      background-size: 200% auto;
      border-radius: 0 4px 4px 0;
      animation: indeterminate 1.5s infinite linear;
    }

    @keyframes indeterminate {
      0% {
        transform: translateX(-100%);
        width: 30%;
        background-position: 0% 50%;
      }
      50% {
        width: 60%;
        background-position: 100% 50%;
      }
      100% {
        transform: translateX(200%);
        width: 30%;
        background-position: 0% 50%;
      }
    }
  `]
})
export class GlobalProgressBar {
  private loadingService = inject(LoadingService);
  
  // Expose the signal to the template
  isLoading = this.loadingService.isLoading;
}
