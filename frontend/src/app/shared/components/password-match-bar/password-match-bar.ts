import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-password-match-bar',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './password-match-bar.html',
  styleUrls: ['./password-match-bar.scss']
})
export class PasswordMatchBarComponent {
  /** Target password string */
  @Input() set password(val: string | null | undefined) {
    this.passwordSignal.set(val || '');
  }

  /** Confirm password string to compare against target */
  @Input() set confirmPassword(val: string | null | undefined) {
    this.confirmPasswordSignal.set(val || '');
  }

  readonly passwordSignal = signal<string>('');
  readonly confirmPasswordSignal = signal<string>('');

  readonly hasInput = computed(() => this.confirmPasswordSignal().length > 0);

  readonly isMatch = computed(() => {
    const pwd = this.passwordSignal();
    const conf = this.confirmPasswordSignal();
    return conf.length > 0 && pwd === conf;
  });

  readonly isMismatch = computed(() => {
    const pwd = this.passwordSignal();
    const conf = this.confirmPasswordSignal();
    return conf.length > 0 && pwd !== conf;
  });

  readonly statusClass = computed(() => {
    if (!this.hasInput()) return 'pending';
    return this.isMatch() ? 'matched' : 'mismatched';
  });

  readonly statusLabelKey = computed(() => {
    if (!this.hasInput()) return 'PASSWORD_MATCH.ENTER_CONFIRM';
    return this.isMatch() ? 'PASSWORD_MATCH.MATCH' : 'PASSWORD_MATCH.MISMATCH';
  });
}
