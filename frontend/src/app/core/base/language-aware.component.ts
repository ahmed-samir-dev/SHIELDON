import { Directive, OnDestroy, OnInit, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { LanguageService } from '../services/language.service';

/**
 * Base class for components that load translated server data.
 *
 * Extend this class and implement `loadData()` - it will be called
 * automatically whenever the user switches the app language.
 *
 * @example
 * export class MyComponent extends LanguageAwareComponent {
 *   override loadData() { this.myService.getItems().subscribe(...); }
 * }
 */
@Directive()
export abstract class LanguageAwareComponent implements OnDestroy {
  private readonly _languageService = inject(LanguageService);
  private _langSub!: Subscription;

  /**
   * Call this from `ngOnInit()` to subscribe to language changes.
   * The `loadData()` method is invoked immediately and on every toggle.
   */
  protected initLanguageReload(): void {
    this.loadData();
    this._langSub = this._languageService.languageChange$.subscribe(() => {
      this.loadData();
    });
  }

  /**
   * Override in the subclass to re-fetch API data.
   * Called once on init and again whenever language is toggled.
   */
  abstract loadData(): void;

  ngOnDestroy(): void {
    this._langSub?.unsubscribe();
  }
}
