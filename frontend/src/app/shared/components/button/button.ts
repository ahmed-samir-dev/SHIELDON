import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [type]="type"
      [disabled]="disabled || loading"
      [ngClass]="[
        'btn',
        'btn-' + variant,
        'btn-' + size,
        fullWidth ? 'w-100' : ''
      ]"
      (click)="onClick($event)"
    >
      <span *ngIf="loading" class="spinner-border" aria-hidden="true"></span>
      <span [class.opacity-0]="loading">
        <ng-content></ng-content>
      </span>
    </button>
  `,
  styles: [`
    .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      font-weight: 600;
      border-radius: 8px; /* $border-radius-md */
      transition: all 0.2s ease;
      position: relative;
      overflow: hidden;
    }
    
    .btn:active:not(:disabled) {
      transform: scale(0.98);
    }
    
    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    /* Primary Variant */
    .btn-primary {
      background: linear-gradient(135deg, #215DAE, #1898A1);
      color: white;
      box-shadow: 0 4px 6px -1px rgba(33, 93, 174, 0.2);
    }
    .btn-primary:hover:not(:disabled) {
      box-shadow: 0 6px 10px -1px rgba(33, 93, 174, 0.3);
      filter: brightness(1.1);
    }

    /* Secondary Variant */
    .btn-secondary {
      background: white;
      color: #374151; /* neutral-700 */
      border: 1px solid #D1D5DB; /* neutral-300 */
    }
    .btn-secondary:hover:not(:disabled) {
      background: #F9FAFB; /* neutral-50 */
      border-color: #9CA3AF;
    }

    /* Sizes */
    .btn-sm {
      padding: 0.5rem 1rem;
      font-size: 0.875rem;
    }
    .btn-md {
      padding: 0.75rem 1.5rem;
      font-size: 1rem;
    }
    .btn-lg {
      padding: 1rem 2rem;
      font-size: 1.125rem;
    }

    .spinner-border {
      width: 1.2em;
      height: 1.2em;
      border: 2px solid rgba(255,255,255,0.3);
      border-top-color: currentColor;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      position: absolute;
    }

    .opacity-0 { opacity: 0; }
    
    @keyframes spin { 
      to { transform: rotate(360deg); } 
    }
  `]
})
export class Button {
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() variant: 'primary' | 'secondary' | 'danger' = 'primary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() fullWidth = false;

  onClick(event: Event) {
    if (this.disabled || this.loading) {
      event.preventDefault();
      event.stopPropagation();
    }
  }
}
