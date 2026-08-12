import { Injectable, inject, DOCUMENT } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

export interface SeoConfig {
  title?: string;
  description?: string;
  keywords?: string;
  author?: string;
  canonicalUrl?: string;
  ogType?: string;
  ogTitle?: string;
  ogDescription?: string;
  ogImage?: string;
  twitterCard?: string;
  twitterTitle?: string;
  twitterDescription?: string;
  twitterImage?: string;
  noIndex?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SeoService {
  private titleService = inject(Title);
  private metaService = inject(Meta);
  private router = inject(Router);
  private document = inject(DOCUMENT);

  private readonly defaultTitle = 'SHIELDON';
  private readonly defaultDescription =
    'SHIELDON is a premium hybrid Learning Management System featuring an integrated, zero-download Anti-Cheating Engine for absolute academic integrity, proctored exams, and automated grading.';
  private readonly defaultKeywords =
    'LMS, Anti-Cheat, Exam Engine, Online Proctoring, Academic Integrity, E-Learning, Automated Grading, Distance Learning, EdTech, Hybrid Learning';
  private readonly defaultOgImage = '/assets/images/shieldon-og-banner.png';

  constructor() {
    // Automatically update canonical and hreflang on navigation end
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updateCanonicalAndHreflang();
      });
  }

  public updateSeoData(config: SeoConfig = {}): void {
    const title = config.title
      ? (config.title.includes('SHIELDON') ? config.title : `${config.title} - SHIELDON`)
      : this.defaultTitle;
    const description = config.description || this.defaultDescription;
    const keywords = config.keywords || this.defaultKeywords;
    const ogImage = config.ogImage || this.getAbsoluteUrl(this.defaultOgImage);
    const currentUrl = config.canonicalUrl || this.getCurrentUrl();

    // 1. Page Title & Basic Meta
    this.titleService.setTitle(title);
    this.metaService.updateTag({ name: 'description', content: description });
    this.metaService.updateTag({ name: 'keywords', content: keywords });
    this.metaService.updateTag({ name: 'author', content: 'SHIELDON EdTech' });

    // 2. Robots Directive
    if (config.noIndex) {
      this.metaService.updateTag({ name: 'robots', content: 'noindex, nofollow' });
    } else {
      this.metaService.updateTag({
        name: 'robots',
        content: 'index, follow, max-snippet:-1, max-image-preview:large, max-video-preview:-1'
      });
    }

    // 3. Open Graph Tags
    this.metaService.updateTag({ property: 'og:site_name', content: 'SHIELDON' });
    this.metaService.updateTag({ property: 'og:type', content: config.ogType || 'website' });
    this.metaService.updateTag({ property: 'og:title', content: config.ogTitle || title });
    this.metaService.updateTag({ property: 'og:description', content: config.ogDescription || description });
    this.metaService.updateTag({ property: 'og:url', content: currentUrl });
    this.metaService.updateTag({ property: 'og:image', content: ogImage });
    this.metaService.updateTag({ property: 'og:image:width', content: '1200' });
    this.metaService.updateTag({ property: 'og:image:height', content: '630' });

    // 4. Twitter Card Tags
    this.metaService.updateTag({ name: 'twitter:card', content: config.twitterCard || 'summary_large_image' });
    this.metaService.updateTag({ name: 'twitter:title', content: config.twitterTitle || title });
    this.metaService.updateTag({ name: 'twitter:description', content: config.twitterDescription || description });
    this.metaService.updateTag({ name: 'twitter:image', content: config.twitterImage || ogImage });

    // 5. Canonical & Hreflang Tags
    this.updateCanonicalAndHreflang(currentUrl);
  }

  public setJsonLdSchema(schema: object, schemaId = 'seo-json-ld'): void {
    let scriptElement = this.document.getElementById(schemaId) as HTMLScriptElement | null;
    if (!scriptElement) {
      scriptElement = this.document.createElement('script');
      scriptElement.id = schemaId;
      scriptElement.type = 'application/ld+json';
      this.document.head.appendChild(scriptElement);
    }
    scriptElement.text = JSON.stringify(schema);
  }

  public updateCanonicalAndHreflang(customUrl?: string): void {
    const targetUrl = customUrl || this.getCurrentUrl();

    // Canonical link tag
    let canonicalLink = this.document.querySelector("link[rel='canonical']") as HTMLLinkElement | null;
    if (!canonicalLink) {
      canonicalLink = this.document.createElement('link');
      canonicalLink.setAttribute('rel', 'canonical');
      this.document.head.appendChild(canonicalLink);
    }
    canonicalLink.setAttribute('href', targetUrl);

    // Bilingual Hreflang link tags (EN & AR)
    this.setOrUpdateHrefLang('en', targetUrl);
    this.setOrUpdateHrefLang('ar', targetUrl);
    this.setOrUpdateHrefLang('x-default', targetUrl);
  }

  private setOrUpdateHrefLang(lang: string, url: string): void {
    let hrefLangLink = this.document.querySelector(`link[hreflang='${lang}']`) as HTMLLinkElement | null;
    if (!hrefLangLink) {
      hrefLangLink = this.document.createElement('link');
      hrefLangLink.setAttribute('rel', 'alternate');
      hrefLangLink.setAttribute('hreflang', lang);
      this.document.head.appendChild(hrefLangLink);
    }
    hrefLangLink.setAttribute('href', url);
  }

  private getCurrentUrl(): string {
    const origin = this.document.location?.origin || 'https://shieldon-lms.com';
    return `${origin}${this.router.url.split('?')[0]}`;
  }

  private getAbsoluteUrl(relativeOrAbsolute: string): string {
    if (relativeOrAbsolute.startsWith('http://') || relativeOrAbsolute.startsWith('https://')) {
      return relativeOrAbsolute;
    }
    const origin = this.document.location?.origin || 'https://shieldon-lms.com';
    const cleanPath = relativeOrAbsolute.startsWith('/') ? relativeOrAbsolute : `/${relativeOrAbsolute}`;
    return `${origin}${cleanPath}`;
  }
}
