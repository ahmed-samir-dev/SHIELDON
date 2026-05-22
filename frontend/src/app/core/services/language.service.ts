import { Injectable, Inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { DOCUMENT } from '@angular/common';
import { BehaviorSubject } from 'rxjs';
import { skip } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private readonly LANG_KEY = 'shieldon_lang';

  // Reactive stream - components subscribe to auto-reload translated data.
  // skip(1) is used by consumers to ignore the initial "boot" emission.
  private readonly _languageChange = new BehaviorSubject<string>('en');
  /** Emits the new language code every time the user switches language. */
  public readonly languageChange$ = this._languageChange.asObservable().pipe(skip(1));

  constructor(
    private translate: TranslateService,
    @Inject(DOCUMENT) private document: Document
  ) {
    this.initLanguage();
  }

  private initLanguage(): void {
    this.translate.setDefaultLang('en');
    const savedLang = localStorage.getItem(this.LANG_KEY) as 'en' | 'ar' || 'en';
    this.setLanguage(savedLang);
  }

  public setLanguage(lang: 'en' | 'ar'): void {
    localStorage.setItem(this.LANG_KEY, lang);
    this.translate.use(lang);

    // Update HTML attributes for RTL/LTR layout
    const htmlTag = this.document.documentElement;
    htmlTag.lang = lang;
    htmlTag.dir = lang === 'ar' ? 'rtl' : 'ltr';

    // Notify all subscribers so they can re-fetch translated data
    this._languageChange.next(lang);
  }

  public getCurrentLanguage(): string {
    return this.translate.currentLang || localStorage.getItem(this.LANG_KEY) || 'en';
  }

  public toggleLanguage(): void {
    const current = this.getCurrentLanguage();
    this.setLanguage(current === 'en' ? 'ar' : 'en');
  }
}
