import { describe, it, expect } from 'vitest';
import DOMPurify from 'dompurify';

describe('Chat Security Specs - DOMPurify & Input Handling', () => {
  it('should strip malicious script tags from chat message content using DOMPurify', () => {
    const rawMessage = '<script>alert("xss")</script>Hello safe message';
    const cleanMessage = DOMPurify.sanitize(rawMessage);

    expect(cleanMessage).not.toContain('<script>');
    expect(cleanMessage).toContain('Hello safe message');
  });

  it('should strip dangerous event handlers from HTML elements', () => {
    const rawMessage = '<img src="x" onerror="alert(document.cookie)">Clean image text';
    const cleanMessage = DOMPurify.sanitize(rawMessage);

    expect(cleanMessage).not.toContain('onerror=');
    expect(cleanMessage).not.toContain('alert');
  });

  it('should sanitize javascript: links', () => {
    const rawMessage = '<a href="javascript:alert(1)">Click me</a>';
    const cleanMessage = DOMPurify.sanitize(rawMessage);

    expect(cleanMessage).not.toContain('javascript:');
  });
});
