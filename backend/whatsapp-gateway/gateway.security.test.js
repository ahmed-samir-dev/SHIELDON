import { test } from 'node:test';
import assert from 'node:assert/strict';

// Validation Regex from index.js
const PHONE_REGEX = /^\+[1-9]\d{6,14}$/;
const CODE_REGEX = /^\d{6}$/;

test('WhatsApp Gateway: E.164 Phone Format Validation', async (t) => {
  await t.test('accepts valid E.164 Egyptian phone number', () => {
    assert.strictEqual(PHONE_REGEX.test('+201012345678'), true);
  });

  await t.test('accepts valid US phone number', () => {
    assert.strictEqual(PHONE_REGEX.test('+14155552671'), true);
  });

  await t.test('rejects phone missing leading plus', () => {
    assert.strictEqual(PHONE_REGEX.test('201012345678'), false);
  });

  await t.test('rejects malformed non-numeric characters', () => {
    assert.strictEqual(PHONE_REGEX.test('+2010123ABCD8'), false);
  });

  await t.test('rejects empty string', () => {
    assert.strictEqual(PHONE_REGEX.test(''), false);
  });
});

test('WhatsApp Gateway: 6-Digit OTP Code Validation', async (t) => {
  await t.test('accepts exactly 6 numeric digits', () => {
    assert.strictEqual(CODE_REGEX.test('849201'), true);
    assert.strictEqual(CODE_REGEX.test('000000'), true);
  });

  await t.test('rejects 5 digits', () => {
    assert.strictEqual(CODE_REGEX.test('12345'), false);
  });

  await t.test('rejects 7 digits', () => {
    assert.strictEqual(CODE_REGEX.test('1234567'), false);
  });

  await t.test('rejects alphanumeric codes', () => {
    assert.strictEqual(CODE_REGEX.test('12345A'), false);
  });

  await t.test('rejects empty code', () => {
    assert.strictEqual(CODE_REGEX.test(''), false);
  });
});
