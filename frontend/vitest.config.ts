import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['src/test-setup.ts'],
    include: ['src/**/*.spec.ts'],
    reporters: ['default']
  },
  plugins: [
    {
      name: 'angular-component-inline-transform',
      transform(code, id) {
        if (id.endsWith('.ts') && (code.includes('templateUrl') || code.includes('styleUrl') || code.includes('styleUrls'))) {
          let transformed = code.replace(/templateUrl\s*:\s*['"]([^'"]+)['"]/g, "template: '<div>Test Component Template</div>'");
          transformed = transformed.replace(/styleUrl\s*:\s*['"][^'"]+['"]/g, "styles: []");
          transformed = transformed.replace(/styleUrls\s*:\s*\[[^\]]+\]/g, "styles: []");
          return { code: transformed, map: null };
        }
      }
    }
  ]
});
