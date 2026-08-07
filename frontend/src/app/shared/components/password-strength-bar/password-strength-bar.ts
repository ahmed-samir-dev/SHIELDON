import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-password-strength-bar',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './password-strength-bar.html',
  styleUrls: ['./password-strength-bar.scss']
})
export class PasswordStrengthBarComponent {
  /** The current password string to evaluate */
  @Input() set password(val: string | null | undefined) {
    this.passwordSignal.set(val || '');
  }

  /** Whether to show requirement check pills below the bar */
  @Input() showRequirements = true;

  /** Compact mode (hides text label and shows minimal bar) */
  @Input() compact = false;

  readonly passwordSignal = signal<string>('');

  // ── Criteria Checks ────────────────────────────────────────────────────────
  readonly hasMinLength = computed(() => this.passwordSignal().length >= 8);
  readonly hasUpper = computed(() => /[A-Z]/.test(this.passwordSignal()));
  readonly hasLower = computed(() => /[a-z]/.test(this.passwordSignal()));
  readonly hasNumber = computed(() => /[0-9]/.test(this.passwordSignal()));
  readonly hasSpecial = computed(() => /[!@#$%^&*(),.?":{}|<>_\-\+=/\\]/.test(this.passwordSignal()));

  // ── Score Calculation (0 to 4) ─────────────────────────────────────────────
  readonly score = computed(() => {
    const pwd = this.passwordSignal();
    if (!pwd) return 0;

    let points = 0;

    // Minimum length criteria
    if (pwd.length >= 8) points++;
    if (pwd.length >= 12) points++; // bonus for longer passwords

    // Character diversity
    if (this.hasUpper() && this.hasLower()) points++;
    if (this.hasNumber()) points++;
    if (this.hasSpecial()) points++;

    // Cap at 4
    return Math.min(4, Math.max(1, Math.floor(points * 0.8)));
  });

  // ── Segment Width Percentage (0% to 100%) ─────────────────────────────────
  readonly percentage = computed(() => {
    const s = this.score();
    if (!this.passwordSignal()) return 0;
    return s * 25;
  });

  // ── Label Key for i18n Translation ─────────────────────────────────────────
  readonly strengthLabelKey = computed(() => {
    if (!this.passwordSignal()) return '';
    const s = this.score();
    switch (s) {
      case 1: return 'PASSWORD_STRENGTH.WEAK';
      case 2: return 'PASSWORD_STRENGTH.FAIR';
      case 3: return 'PASSWORD_STRENGTH.GOOD';
      case 4: return 'PASSWORD_STRENGTH.STRONG';
      default: return '';
    }
  });

  // ── Color Theme Class ──────────────────────────────────────────────────────
  readonly strengthClass = computed(() => {
    if (!this.passwordSignal()) return '';
    const s = this.score();
    switch (s) {
      case 1: return 'weak';
      case 2: return 'fair';
      case 3: return 'good';
      case 4: return 'strong';
      default: return '';
    }
  });
}
