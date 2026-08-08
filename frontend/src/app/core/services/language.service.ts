import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { DOCUMENT } from '@angular/common';
import { BehaviorSubject } from 'rxjs';
import { skip } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private readonly LANG_KEY = 'shieldon_lang';
  private translate = inject(TranslateService);
  private document = inject(DOCUMENT);

  // Reactive stream - components subscribe to auto-reload translated data.
  // skip(1) is used by consumers to ignore the initial "boot" emission.
  private readonly _languageChange = new BehaviorSubject<string>('en');
  /** Emits the new language code every time the user switches language. */
  public readonly languageChange$ = this._languageChange.asObservable().pipe(skip(1));

  constructor() {
    this.initLanguage();
  }

  private initLanguage(): void {
    const savedLang = localStorage.getItem(this.LANG_KEY) || 'en';
    this.setLanguage(savedLang);
  }

  public setLanguage(lang: string): void {
    const validLang = lang === 'ar' ? 'ar' : 'en';

    // Set NGX-Translate active language
    this.translate.use(validLang);

    // Save to LocalStorage
    localStorage.setItem(this.LANG_KEY, validLang);

    // Update HTML attributes for DOM/CSS (RTL support)
    const htmlElement = this.document.documentElement;
    htmlElement.setAttribute('lang', validLang);
    htmlElement.setAttribute('dir', validLang === 'ar' ? 'rtl' : 'ltr');

    // Emit reactive event to all subscribed components (skip(1) will pass this for explicit switches)
    this._languageChange.next(validLang);
  }

  public getCurrentLanguage(): string {
    return localStorage.getItem(this.LANG_KEY) || 'en';
  }

  public toggleLanguage(): void {
    const current = this.getCurrentLanguage();
    const next = current === 'en' ? 'ar' : 'en';
    this.setLanguage(next);
  }
}
