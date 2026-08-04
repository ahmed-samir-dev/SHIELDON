/**
 * SHIELDON WhatsApp Gateway
 * Self-hosted OTP delivery microservice using Baileys (WhatsApp Web socket library).
 *
 * Features:
 * - One-time QR code scan to pair with any WhatsApp number
 * - Session persisted in ./auth_info/ — no re-scan after restart
 * - Exposes POST /api/send-otp for the C# backend to call
 * - Sends a formatted WhatsApp message with the 6-digit OTP code
 *
 * Usage (first time only):
 *   npm install
 *   node index.js   → Scan the QR code → Done forever
 *
 * Usage (every time after):
 *   node index.js   → Reconnects automatically, no QR needed
 */

import makeWASocket, {
  useMultiFileAuthState,
  DisconnectReason,
  fetchLatestBaileysVersion,
  makeCacheableSignalKeyStore,
} from '@whiskeysockets/baileys';
import express from 'express';
import pino from 'pino';
import qrcode from 'qrcode-terminal';

// ── Config ──────────────────────────────────────────────────────────────────
const PORT = 3001;
const AUTH_FOLDER = './auth_info';

// Suppress verbose Baileys internal logs — only show errors
const logger = pino({ level: 'silent' });

// ── WhatsApp Socket State ────────────────────────────────────────────────────
let sock = null;
let isConnected = false;

// ── Connect / Reconnect Logic ────────────────────────────────────────────────
async function connectWhatsApp() {
  const { state, saveCreds } = await useMultiFileAuthState(AUTH_FOLDER);
  const { version } = await fetchLatestBaileysVersion();

  sock = makeWASocket({
    version,
    auth: {
      creds: state.creds,
      keys: makeCacheableSignalKeyStore(state.keys, logger),
    },
    logger,
    browser: ['SHIELDON Gateway', 'Chrome', '10.0'],
  });

  // Persist session credentials whenever they update
  sock.ev.on('creds.update', saveCreds);

  sock.ev.on('connection.update', ({ connection, lastDisconnect, qr }) => {
    // Render QR code in terminal manually (printQRInTerminal is deprecated in newer Baileys)
    if (qr) {
      console.log('\n══════════════════════════════════════════════════');
      console.log('  Scan this QR code with your WhatsApp phone  ');
      console.log('══════════════════════════════════════════════════\n');
      qrcode.generate(qr, { small: true });
      console.log('\n  Open WhatsApp → Linked Devices → Link a Device\n');
    }

    if (connection === 'open') {
      isConnected = true;
      console.log('WhatsApp connected! Gateway is ready to send OTP messages.\n');
    }

    if (connection === 'close') {
      isConnected = false;
      const statusCode = lastDisconnect?.error?.output?.statusCode;
      const shouldReconnect = statusCode !== DisconnectReason.loggedOut;

      if (shouldReconnect) {
        console.log(`Connection closed (code: ${statusCode}). Reconnecting in 3s...`);
        setTimeout(connectWhatsApp, 3000);
      } else {
        console.log('Logged out from WhatsApp. Delete ./auth_info/ and restart to re-pair.');
      }
    }
  });
}

// ── Express API Server ────────────────────────────────────────────────────────
const app = express();
app.use(express.json());

/**
 * Health check — GET /health
 */
app.get('/health', (req, res) => {
  res.json({
    status: isConnected ? 'connected' : 'disconnected',
    message: isConnected ? 'WhatsApp Gateway is ready.' : 'WhatsApp not connected — scan the QR code.',
  });
});

/**
 * Send OTP — POST /api/send-otp
 * Called internally by SHIELDON C# ProfileService only. Never exposed to the browser.
 * Body: { "phone": "+201012345678", "code": "849201" }
 */
app.post('/api/send-otp', async (req, res) => {
  const { phone, code } = req.body;

  if (!phone || !code) {
    return res.status(400).json({ error: 'Both "phone" and "code" fields are required.' });
  }
  if (!/^\+[1-9]\d{6,14}$/.test(phone)) {
    return res.status(400).json({ error: 'Phone must be in E.164 format (e.g. +201012345678).' });
  }
  if (!/^\d{6}$/.test(code)) {
    return res.status(400).json({ error: 'Code must be exactly 6 digits.' });
  }
  if (!isConnected || !sock) {
    return res.status(503).json({ error: 'WhatsApp not connected. Scan the QR code and try again.' });
  }

  // WhatsApp JID: strip '+' and append '@s.whatsapp.net'
  const jid = `${phone.replace('+', '')}@s.whatsapp.net`;

  const message =
    `🔐 *SHIELDON Verification Code*\n\n` +
    `Your verification code is:\n\n` +
    `*${code}*\n\n` +
    `This code is valid for *10 minutes*.\n` +
    `_Do not share this code with anyone._`;

  try {
    await sock.sendMessage(jid, { text: message });
    console.log(`[OTP Sent] → ${phone}`);
    return res.status(200).json({ success: true });
  } catch (error) {
    console.error(`[OTP Error] → ${phone}:`, error.message);
    return res.status(500).json({ error: 'Failed to send WhatsApp message. Please try again.' });
  }
});

// ── Boot ─────────────────────────────────────────────────────────────────────
app.listen(PORT, () => {
  console.log(`\nSHIELDON WhatsApp Gateway`);
  console.log(`   Port 3001  ← internal only, called by the C# backend`);
  console.log(`   Health:   http://localhost:${PORT}/health\n`);
});

connectWhatsApp().catch(err => {
  console.error('Failed to initialize WhatsApp connection:', err);
  process.exit(1);
});
