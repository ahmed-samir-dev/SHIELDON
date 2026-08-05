import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { LinkPreviewData } from '../models/chat.model';

@Injectable({
  providedIn: 'root'
})
export class LinkPreviewService {
  private http = inject(HttpClient);
  private cache = new Map<string, LinkPreviewData | null>();

  private urlRegex = /(https?:\/\/[^\s<]+[^<.,:;"')\]\s])/i;

  /**
   * Extracts the first URL found in a block of text.
   */
  extractFirstUrl(text: string): string | null {
    if (!text) return null;
    const match = text.match(this.urlRegex);
    if (!match) return null;
    let url = match[0];
    // Clean up trailing punctuation if any escaped regex
    url = url.replace(/[.,;!?)]+$/, '');
    return url;
  }

  /**
   * Returns cached preview data synchronously, or `undefined` if not yet fetched.
   * Use this for instant cache reads before falling back to fetchPreview().
   */
  getCachedPreview(url: string): LinkPreviewData | null | undefined {
    if (!url) return undefined;
    let targetUrl = url;
    if (!/^https?:\/\//i.test(targetUrl)) {
      targetUrl = 'https://' + targetUrl;
    }
    return this.cache.has(targetUrl) ? this.cache.get(targetUrl) : undefined;
  }

  /**
   * Fetches Open Graph preview metadata for a URL via Microlink API or YouTube oEmbed.
   * Returns cached data if available.
   */
  fetchPreview(url: string): Observable<LinkPreviewData | null> {
    if (!url) return of(null);

    // Normalize URL
    let targetUrl = url;
    if (!/^https?:\/\//i.test(targetUrl)) {
      targetUrl = 'https://' + targetUrl;
    }

    if (this.cache.has(targetUrl)) {
      return of(this.cache.get(targetUrl) || null);
    }

    const domain = this.getDomainName(targetUrl);
    const ytVideoId = this.extractYouTubeVideoId(targetUrl);

    // Dedicated fast-path for YouTube URLs with direct video thumbnail extraction
    if (ytVideoId) {
      const ytThumbnail = `https://img.youtube.com/vi/${ytVideoId}/hqdefault.jpg`;
      const oembedUrl = `https://www.youtube.com/oembed?url=${encodeURIComponent(targetUrl)}&format=json`;

      return this.http.get<any>(oembedUrl).pipe(
        map(res => {
          const preview: LinkPreviewData = {
            url: targetUrl,
            title: res.title || 'YouTube Video',
            description: res.author_name ? `Channel: ${res.author_name}` : 'Watch on YouTube',
            image: res.thumbnail_url || ytThumbnail,
            siteName: 'YouTube'
          };
          this.cache.set(targetUrl, preview);
          return preview;
        }),
        catchError(() => {
          const fallback: LinkPreviewData = {
            url: targetUrl,
            title: 'YouTube Video',
            description: 'Watch on YouTube',
            image: ytThumbnail,
            siteName: 'YouTube'
          };
          this.cache.set(targetUrl, fallback);
          return of(fallback);
        })
      );
    }

    const apiUrl = `https://api.microlink.io/?url=${encodeURIComponent(targetUrl)}`;

    return this.http.get<any>(apiUrl).pipe(
      map(res => {
        if (res && res.status === 'success' && res.data) {
          const data = res.data;
          const image = this.extractImageUrl(data, domain);
          const preview: LinkPreviewData = {
            url: targetUrl,
            title: data.title || domain,
            description: data.description || '',
            image: image,
            siteName: data.publisher || domain
          };
          this.cache.set(targetUrl, preview);
          return preview;
        }

        
        const fallback: LinkPreviewData = {
          url: targetUrl,
          title: domain,
          siteName: domain,
          image: `https://www.google.com/s2/favicons?domain=${domain}&sz=128`
        };
        this.cache.set(targetUrl, fallback);
        return fallback;
      }),
      catchError(err => {
        console.warn('Link preview fetch failed for:', targetUrl, err);
        const fallback: LinkPreviewData = {
          url: targetUrl,
          title: domain,
          siteName: domain,
          image: `https://www.google.com/s2/favicons?domain=${domain}&sz=128`
        };
        this.cache.set(targetUrl, fallback);
        return of(fallback);
      })
    );
  }

  private extractYouTubeVideoId(url: string): string | null {
    const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|shorts\/|watch\?v=|\&v=)([^#\&\?]*).*/;
    const match = url.match(regExp);
    return (match && match[2].length === 11) ? match[2] : null;
  }

  private extractImageUrl(data: any, domain: string): string {

    let imgUrl: string | undefined;

    if (typeof data.image === 'string' && data.image.trim()) {
      imgUrl = data.image;
    } else if (data.image?.url && typeof data.image.url === 'string') {
      imgUrl = data.image.url;
    } else if (typeof data.logo === 'string' && data.logo.trim()) {
      imgUrl = data.logo;
    } else if (data.logo?.url && typeof data.logo.url === 'string') {
      imgUrl = data.logo.url;
    } else if (typeof data.banner === 'string' && data.banner.trim()) {
      imgUrl = data.banner;
    } else if (data.banner?.url && typeof data.banner.url === 'string') {
      imgUrl = data.banner.url;
    }

    // High quality fallback favicon if no OG image/logo was found
    if (!imgUrl || imgUrl.trim() === '') {
      imgUrl = `https://www.google.com/s2/favicons?domain=${domain}&sz=128`;
    }

    return imgUrl;
  }

  private getDomainName(urlStr: string): string {
    try {
      const parsed = new URL(urlStr);
      return parsed.hostname.replace(/^www\./, '');
    } catch {
      return urlStr;
    }
  }
}

