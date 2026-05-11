import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

@Pipe({
  name: 'markdown',
  standalone: true
})
export class MarkdownPipe implements PipeTransform {
  constructor(private sanitizer: DomSanitizer) {}

  transform(value: string | undefined): SafeHtml {
    if (!value) return '';
    const parsed = marked.parse(value) as string;
    const sanitized = DOMPurify.sanitize(parsed);
    return this.sanitizer.bypassSecurityTrustHtml(sanitized);
  }
}
