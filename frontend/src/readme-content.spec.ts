// @vitest-environment node
/**
 * Tests for PR documentation changes:
 * - README.md (root)
 * - backend/README.md
 * - frontend/README.md
 *
 * These tests verify the emoji additions and structural changes introduced
 * in the post-graduation enhancements update.
 */

import { readFileSync } from 'fs';
import { resolve } from 'path';

const ROOT = resolve(__dirname, '../../..');

function readReadme(relativePath: string): string {
  return readFileSync(resolve(ROOT, relativePath), 'utf-8');
}

// ---------------------------------------------------------------------------
// Root README.md
// ---------------------------------------------------------------------------
describe('README.md — root', () => {
  let content: string;

  beforeAll(() => {
    content = readReadme('README.md');
  });

  // System Roles table: new three-column layout with dedicated Emoji column
  describe('System Roles table', () => {
    it('has a three-column header: Role | Emoji | Description', () => {
      expect(content).toContain('| Role | Emoji | Description |');
    });

    it('contains the Admin emoji 🔑 in the Roles table', () => {
      expect(content).toMatch(/\|\s*\*\*Admin\*\*\s*\|\s*🔑\s*\|/);
    });

    it('contains the Tutor emoji 👨‍🏫 in the Roles table', () => {
      expect(content).toMatch(/\|\s*\*\*Tutor\*\*\s*\|\s*👨‍🏫\s*\|/);
    });

    it('contains the Student emoji 🎓 in the Roles table', () => {
      expect(content).toMatch(/\|\s*\*\*Student\*\*\s*\|\s*🎓\s*\|/);
    });

    // Regression: the old two-column header must no longer exist
    it('does NOT use the old two-column Role | Description header', () => {
      // Old format had exactly two columns; new format has three
      const oldHeader = '| Role | Description |';
      // The three-column variant superseded the two-column one
      expect(content).not.toContain(oldHeader);
    });
  });

  // Anti-Cheating Engine sub-feature IDs changed from "1–9" to "17-1 – 17-9"
  describe('Anti-Cheating Engine sub-feature IDs', () => {
    it('uses "17-1" as the first sub-feature ID', () => {
      expect(content).toContain('| 17-1 |');
    });

    it('uses "17-9" as the last sub-feature ID', () => {
      expect(content).toContain('| 17-9 |');
    });

    it('contains all nine sub-feature IDs 17-1 through 17-9', () => {
      for (let i = 1; i <= 9; i++) {
        expect(content).toContain(`| 17-${i} |`);
      }
    });

    // Regression: bare numeric IDs "| 1 |" through "| 9 |" must no longer appear
    // in the anti-cheat table section
    it('does NOT use bare numeric IDs 1–9 for anti-cheat sub-features', () => {
      const antiCheatSection = content.split('### 🛡️ Anti-Cheating Engine')[1];
      expect(antiCheatSection).toBeDefined();
      // The old format was "| 1 |", "| 2 |", etc.
      for (let i = 1; i <= 9; i++) {
        expect(antiCheatSection).not.toMatch(new RegExp(`\\|\\s*${i}\\s*\\|`));
      }
    });
  });

  // Frontend technology stack emojis
  describe('Frontend technology stack emojis', () => {
    it('uses 🅰️ emoji for Angular 21', () => {
      expect(content).toContain('🅰️ **Angular 21**');
    });

    it('uses 🔷 emoji for TypeScript', () => {
      expect(content).toContain('🔷 **TypeScript**');
    });

    it('uses 🎨 emoji for SCSS', () => {
      expect(content).toContain('🎨 **SCSS**');
    });

    it('uses 📊 emoji for Apache ECharts', () => {
      expect(content).toContain('📊 **Apache ECharts**');
    });

    it('uses 🎉 emoji for canvas-confetti', () => {
      expect(content).toContain('🎉 **canvas-confetti**');
    });

    it('uses 🧭 emoji for Shepherd.js', () => {
      expect(content).toContain('🧭 **Shepherd.js**');
    });
  });

  // Backend technology stack emojis
  describe('Backend technology stack emojis', () => {
    it('uses 🟣 emoji for .NET 9 ASP.NET Core', () => {
      expect(content).toContain('🟣 **.NET 9 ASP.NET Core**');
    });

    it('uses 🗄️ emoji for Entity Framework Core 9', () => {
      expect(content).toContain('🗄️ **Entity Framework Core 9**');
    });

    it('uses ✅ emoji for FluentValidation', () => {
      expect(content).toContain('✅ **FluentValidation**');
    });

    it('uses 💳 emoji for Stripe.net', () => {
      expect(content).toContain('💳 **Stripe.net**');
    });

    it('uses 🤖 emoji for Google Gemini API', () => {
      expect(content).toContain('🤖 **Google Gemini API**');
    });
  });

  // Database technology stack emojis
  describe('Database technology stack emojis', () => {
    it('uses 🗃️ emoji for Microsoft SQL Server 2022', () => {
      expect(content).toContain('🗃️ **Microsoft SQL Server 2022**');
    });
  });

  // Architecture layer table emojis
  describe('Architecture layer table emojis', () => {
    it('uses 🌐 emoji for SHIELDON.API layer', () => {
      expect(content).toContain('🌐 **SHIELDON.API**');
    });

    it('uses 📦 emoji for SHIELDON.Application layer', () => {
      expect(content).toContain('📦 **SHIELDON.Application**');
    });

    it('uses 🔌 emoji for SHIELDON.Infrastructure layer', () => {
      expect(content).toContain('🔌 **SHIELDON.Infrastructure**');
    });

    it('uses 🧱 emoji for SHIELDON.Domain layer', () => {
      expect(content).toContain('🧱 **SHIELDON.Domain**');
    });

    it('uses 🧪 emoji for SHIELDON.Tests layer', () => {
      expect(content).toContain('🧪 **SHIELDON.Tests**');
    });
  });

  // Feature list (F1–F30) emojis
  describe('Feature list emojis (F1–F30)', () => {
    it('uses 🔐 emoji for F1 Secure Login', () => {
      expect(content).toContain('F1 | 🔐 **Secure Login');
    });

    it('uses 🛡️ emoji for F17 Anti-Cheating Engine', () => {
      expect(content).toContain('F17 | 🛡️ **Anti-Cheating Engine**');
    });

    it('uses 💳 emoji for F27 Online Payment Gateway', () => {
      expect(content).toContain('F27 | 💳 **Online Payment Gateway');
    });

    it('uses 🌓 emoji for F28 Dark / Light Mode', () => {
      expect(content).toContain('F28 | 🌓 **Dark / Light Mode**');
    });

    it('uses 🌍 emoji for F29 English / Arabic i18n', () => {
      expect(content).toContain('F29 | 🌍 **English / Arabic (i18n)**');
    });
  });

  // API section header emojis
  describe('API section header emojis', () => {
    it('uses 🔐 emoji for Authentication section header', () => {
      expect(content).toContain('### 🔐 Authentication (`/api/auth`)');
    });

    it('uses 👤 emoji for Profile section header', () => {
      expect(content).toContain('### 👤 Profile (`/api/profile`)');
    });

    it('uses 📚 emoji for Courses section header', () => {
      expect(content).toContain('### 📚 Courses (`/api/courses`)');
    });

    it('uses 🤖 emoji for AI Assistant section header', () => {
      expect(content).toContain('### 🤖 AI Assistant (`/api/ai`)');
    });

    it('uses 💳 emoji for Payments section header', () => {
      expect(content).toContain('### 💳 Payments (`/api/payment`)');
    });

    it('uses 🔗 emoji for Stripe Webhook section header', () => {
      expect(content).toContain('### 🔗 Stripe Webhook (`/api/webhooks/stripe`)');
    });
  });

  // Prerequisites section emojis
  describe('Prerequisites section emojis', () => {
    it('uses 🟢 emoji for Node.js prerequisite', () => {
      expect(content).toContain('🟢 **Node.js**');
    });

    it('uses 🟣 emoji for .NET 9 SDK prerequisite', () => {
      expect(content).toContain('🟣 **.NET 9 SDK**');
    });

    it('uses 🌿 emoji for Git prerequisite', () => {
      expect(content).toContain('🌿 **Git**');
    });

    it('uses 💳 emoji for Stripe CLI optional prerequisite', () => {
      expect(content).toContain('💳 **Stripe CLI**');
    });
  });

  // Inline emojis in setup instructions
  describe('Setup instruction inline emojis', () => {
    it('uses ⚠️ emoji to flag the crucial database configuration step', () => {
      expect(content).toContain('⚠️ Before running the backend');
    });

    it('uses 🔥 emoji next to hot-reloading tip', () => {
      expect(content).toContain('hot-reloading 🔥');
    });

    it('uses 🎉 emoji on the Congratulations line', () => {
      expect(content).toContain('🎉 **Congratulations!');
    });
  });
});

// ---------------------------------------------------------------------------
// backend/README.md
// ---------------------------------------------------------------------------
describe('backend/README.md', () => {
  let content: string;

  beforeAll(() => {
    content = readReadme('backend/README.md');
  });

  describe('Responsibilities bullet emojis', () => {
    it('uses 🔐 emoji for JWT authentication bullet', () => {
      expect(content).toContain('🔐 Secure JWT authentication');
    });

    it('uses ⚡ emoji for Exam Engine bullet', () => {
      expect(content).toContain('⚡ Orchestrating the Exam Engine');
    });

    it('uses 🛡️ emoji for anti-cheat violation logs bullet', () => {
      expect(content).toContain('🛡️ Persisting and analyzing anti-cheat violation logs');
    });

    it('uses 💳 emoji for payment processing bullet', () => {
      expect(content).toContain('💳 Online payment processing via Stripe');
    });

    it('uses 🤖 emoji for AI assistant proxy bullet', () => {
      expect(content).toContain('🤖 AI assistant proxy (Google Gemini API)');
    });

    it('uses 📱 emoji for QR attendance tracking bullet', () => {
      expect(content).toContain('📱 Dynamic QR attendance tracking');
    });
  });

  describe('Technology stack emojis', () => {
    it('uses 🟣 emoji for .NET 9 ASP.NET Core', () => {
      expect(content).toContain('🟣 **.NET 9 ASP.NET Core**');
    });

    it('uses 🏛️ emoji for Clean Architecture', () => {
      expect(content).toContain('🏛️ **Clean Architecture**');
    });

    it('uses 💽 emoji for Microsoft SQL Server 2022', () => {
      expect(content).toContain('💽 **Microsoft SQL Server 2022**');
    });

    it('uses 🔐 emoji for JWT Bearer Tokens', () => {
      expect(content).toContain('🔐 **JWT Bearer Tokens**');
    });

    it('uses 🔄 emoji for AutoMapper', () => {
      expect(content).toContain('🔄 **AutoMapper**');
    });

    it('uses 📝 emoji for Serilog', () => {
      expect(content).toContain('📝 **Serilog**');
    });
  });

  describe('Architecture layer emojis', () => {
    it('uses 🌐 emoji for SHIELDON.API layer', () => {
      expect(content).toContain('🌐 **SHIELDON.API**');
    });

    it('uses 📦 emoji for SHIELDON.Application layer', () => {
      expect(content).toContain('📦 **SHIELDON.Application**');
    });

    it('uses 🔌 emoji for SHIELDON.Infrastructure layer', () => {
      expect(content).toContain('🔌 **SHIELDON.Infrastructure**');
    });

    it('uses 🧱 emoji for SHIELDON.Domain layer', () => {
      expect(content).toContain('🧱 **SHIELDON.Domain**');
    });

    it('uses 🧪 emoji for SHIELDON.Tests layer', () => {
      expect(content).toContain('🧪 **SHIELDON.Tests**');
    });
  });

  describe('Anti-Cheating Engine sub-feature IDs', () => {
    it('uses "17-1" as first sub-feature ID', () => {
      expect(content).toContain('| 17-1 |');
    });

    it('uses "17-9" as last sub-feature ID', () => {
      expect(content).toContain('| 17-9 |');
    });

    it('contains all nine sub-feature IDs 17-1 through 17-9', () => {
      for (let i = 1; i <= 9; i++) {
        expect(content).toContain(`| 17-${i} |`);
      }
    });
  });

  describe('API reference controller emojis', () => {
    it('uses 🔐 emoji for Auth controller', () => {
      expect(content).toContain('| 🔐 Auth |');
    });

    it('uses 📚 emoji for Courses controller', () => {
      expect(content).toContain('| 📚 Courses |');
    });

    it('uses 🛡️ emoji for Violations controller', () => {
      expect(content).toContain('| 🛡️ Violations |');
    });

    it('uses 💳 emoji for Payment controller', () => {
      expect(content).toContain('| 💳 Payment |');
    });

    it('uses 🤖 emoji for AI controller', () => {
      expect(content).toContain('| 🤖 AI |');
    });

    it('uses 🖼️ emoji for Files controller', () => {
      expect(content).toContain('| 🖼️ Files |');
    });
  });

  describe('Setup instruction inline emojis', () => {
    it('uses ⚠️ emoji to flag the crucial database configuration step', () => {
      expect(content).toContain('⚠️ Before running the backend');
    });

    it('uses ✅ emoji in the "already installed" tip', () => {
      expect(content).toContain("that's perfect! ✅");
    });

    it('uses 🚨 emoji for the keep-terminal-open warning', () => {
      expect(content).toContain('🚨 Keep this terminal window open');
    });
  });
});

// ---------------------------------------------------------------------------
// frontend/README.md
// ---------------------------------------------------------------------------
describe('frontend/README.md', () => {
  let content: string;

  beforeAll(() => {
    content = readReadme('frontend/README.md');
  });

  describe('Responsibilities bullet emojis', () => {
    it('uses 📚 emoji for LMS experience bullet', () => {
      expect(content).toContain('📚 Delivering the Learning Management System experience');
    });

    it('uses 🛡️ emoji for Anti-Cheating Engine bullet', () => {
      expect(content).toContain('🛡️ Enforcing the **Anti-Cheating Engine**');
    });

    it('uses 🤖 emoji for AI assistant bullet', () => {
      expect(content).toContain('🤖 Providing an interactive AI assistant');
    });

    it('uses 💬 emoji for chat system bullet', () => {
      expect(content).toContain('💬 Real-time chat system between users');
    });

    it('uses 💳 emoji for payment interface bullet', () => {
      expect(content).toContain('💳 Online payment interface via Stripe Checkout');
    });

    it('uses 🌓 emoji for Dark/Light mode bullet', () => {
      expect(content).toContain('🌓 Dark / Light mode with CSS custom properties');
    });

    it('uses 🌍 emoji for i18n bullet', () => {
      expect(content).toContain('🌍 Full English / Arabic (RTL) internationalization');
    });
  });

  describe('Technology stack emojis', () => {
    it('uses 🅰️ emoji for Angular 21', () => {
      expect(content).toContain('🅰️ **Angular 21**');
    });

    it('uses 🔷 emoji for TypeScript', () => {
      expect(content).toContain('🔷 **TypeScript**');
    });

    it('uses 🎨 emoji for SCSS', () => {
      expect(content).toContain('🎨 **SCSS**');
    });

    it('uses 🌍 emoji for ngx-translate', () => {
      expect(content).toContain('🌍 **ngx-translate**');
    });

    it('uses 💳 emoji for Stripe.js', () => {
      expect(content).toContain('💳 **Stripe.js**');
    });
  });

  describe('Anti-Cheating Engine sub-feature IDs', () => {
    it('uses "17-1" as first sub-feature ID', () => {
      expect(content).toContain('| 17-1 |');
    });

    it('uses "17-9" as last sub-feature ID', () => {
      expect(content).toContain('| 17-9 |');
    });

    it('contains all nine sub-feature IDs 17-1 through 17-9', () => {
      for (let i = 1; i <= 9; i++) {
        expect(content).toContain(`| 17-${i} |`);
      }
    });

    it('does NOT use bare numeric IDs 1–9 for anti-cheat sub-features', () => {
      const antiCheatSection = content.split('### 🛡️ Anti-Cheating Engine')[1];
      expect(antiCheatSection).toBeDefined();
      for (let i = 1; i <= 9; i++) {
        expect(antiCheatSection).not.toMatch(new RegExp(`\\|\\s*${i}\\s*\\|`));
      }
    });
  });

  describe('Setup instruction inline emojis', () => {
    it('uses ⚠️ emoji for the critical backend requirement notice', () => {
      expect(content).toContain('⚠️ **CRITICAL REQUIREMENT:**');
    });

    it('uses ✅ emoji for Verify SQL Server step', () => {
      expect(content).toContain('✅ Verify that your SQL Server is running.');
    });

    it('uses 🚨 emoji for keep-backend-terminal-open warning', () => {
      expect(content).toContain('🚨 **Keep the backend terminal open**');
    });

    it('uses 🎉 emoji on the Congratulations line', () => {
      expect(content).toContain('🎉 **Congratulations!');
    });
  });

  describe('Git workflow section emojis', () => {
    it('uses 🌿 emoji for Feature Branches workflow item', () => {
      expect(content).toContain('🌿 **Feature Branches**');
    });

    it('uses 🔀 emoji for Pull Requests workflow item', () => {
      expect(content).toContain('🔀 **Pull Requests**');
    });
  });

  describe('Feature list emojis (F1–F30)', () => {
    it('uses 🔐 emoji for F1 Secure Login', () => {
      expect(content).toContain('F1 | 🔐 **Secure Login');
    });

    it('uses 🛡️ emoji for F17 Anti-Cheating Engine', () => {
      expect(content).toContain('F17 | 🛡️ **Anti-Cheating Engine**');
    });

    it('uses 🌍 emoji for F29 English / Arabic i18n', () => {
      expect(content).toContain('F29 | 🌍 **English / Arabic (i18n)**');
    });
  });

  // Edge / boundary cases
  describe('edge cases and regression checks', () => {
    it('still contains all 30 feature entries (F1–F30)', () => {
      for (let i = 1; i <= 30; i++) {
        expect(content).toContain(`| F${i} |`);
      }
    });

    it('the frontend README still references the backend README for full setup', () => {
      expect(content).toContain('../README.md');
    });

    it('the frontend README still lists the correct dev server port 4201', () => {
      expect(content).toContain('4201');
    });
  });
});
