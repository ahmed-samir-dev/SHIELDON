export interface CountryCode {
  name: string;
  code: string;
  flag: string;
  placeholder?: string;
}

export const COUNTRY_CODES: CountryCode[] = [
  // ── Middle East & North Africa (MENA) ─────────────────────────────────────
  { name: 'Egypt', code: '+20', flag: '🇪🇬', placeholder: '1012345678' },
  { name: 'Saudi Arabia', code: '+966', flag: '🇸🇦', placeholder: '512345678' },
  { name: 'United Arab Emirates', code: '+971', flag: '🇦🇪', placeholder: '501234567' },
  { name: 'Kuwait', code: '+965', flag: '🇰🇼', placeholder: '91234567' },
  { name: 'Qatar', code: '+974', flag: '🇶🇦', placeholder: '33123456' },
  { name: 'Oman', code: '+968', flag: '🇴🇲', placeholder: '91234567' },
  { name: 'Bahrain', code: '+973', flag: '🇧🇭', placeholder: '36123456' },
  { name: 'Jordan', code: '+962', flag: '🇯🇴', placeholder: '791234567' },
  { name: 'Lebanon', code: '+961', flag: '🇱🇧', placeholder: '71123456' },
  { name: 'Iraq', code: '+964', flag: '🇮🇶', placeholder: '7901234567' },
  { name: 'Palestine', code: '+970', flag: '🇵🇸', placeholder: '599123456' },
  { name: 'Morocco', code: '+212', flag: '🇲🇦', placeholder: '612345678' },
  { name: 'Algeria', code: '+213', flag: '🇩🇿', placeholder: '551234567' },
  { name: 'Tunisia', code: '+216', flag: '🇹🇳', placeholder: '20123456' },
  { name: 'Sudan', code: '+249', flag: '🇸🇩', placeholder: '912345678' },
  { name: 'Libya', code: '+218', flag: '🇱🇾', placeholder: '911234567' },
  { name: 'Yemen', code: '+967', flag: '🇾🇪', placeholder: '771234567' },
  { name: 'Syria', code: '+963', flag: '🇸🇾', placeholder: '933123456' },

  // ── North America ────────────────────────────────────────────────────────
  { name: 'United States', code: '+1', flag: '🇺🇸', placeholder: '4155550100' },
  { name: 'Canada', code: '+1', flag: '🇨🇦', placeholder: '4165550100' },

  // ── Europe ───────────────────────────────────────────────────────────────
  { name: 'United Kingdom', code: '+44', flag: '🇬🇧', placeholder: '7911123456' },
  { name: 'Germany', code: '+49', flag: '🇩🇪', placeholder: '15112345678' },
  { name: 'France', code: '+33', flag: '🇫🇷', placeholder: '612345678' },
  { name: 'Italy', code: '+39', flag: '🇮🇹', placeholder: '3123456789' },
  { name: 'Spain', code: '+34', flag: '🇪🇸', placeholder: '612345678' },
  { name: 'Netherlands', code: '+31', flag: '🇳🇱', placeholder: '612345678' },
  { name: 'Switzerland', code: '+41', flag: '🇨🇭', placeholder: '791234567' },
  { name: 'Sweden', code: '+46', flag: '🇸🇪', placeholder: '701234567' },
  { name: 'Norway', code: '+47', flag: '🇳🇴', placeholder: '41234567' },
  { name: 'Denmark', code: '+45', flag: '🇩🇰', placeholder: '20123456' },
  { name: 'Turkey', code: '+90', flag: '🇹🇷', placeholder: '5012345678' },

  // ── Asia & Pacific ───────────────────────────────────────────────────────
  { name: 'India', code: '+91', flag: '🇮🇳', placeholder: '9876543210' },
  { name: 'China', code: '+86', flag: '🇨🇳', placeholder: '13812345678' },
  { name: 'Japan', code: '+81', flag: '🇯🇵', placeholder: '9012345678' },
  { name: 'South Korea', code: '+82', flag: '🇰🇷', placeholder: '1012345678' },
  { name: 'Pakistan', code: '+92', flag: '🇵🇰', placeholder: '3001234567' },
  { name: 'Malaysia', code: '+60', flag: '🇲🇾', placeholder: '123456789' },
  { name: 'Singapore', code: '+65', flag: '🇸🇬', placeholder: '91234567' },
  { name: 'Indonesia', code: '+62', flag: '🇮🇩', placeholder: '8123456789' },
  { name: 'Australia', code: '+61', flag: '🇦🇺', placeholder: '412345678' },
  { name: 'New Zealand', code: '+64', flag: '🇳🇿', placeholder: '211234567' },

  // ── South America & Africa ──────────────────────────────────────────────
  { name: 'Brazil', code: '+55', flag: '🇧🇷', placeholder: '11912345678' },
  { name: 'Argentina', code: '+54', flag: '🇦🇷', placeholder: '91112345678' },
  { name: 'Nigeria', code: '+234', flag: '🇳🇬', placeholder: '8021234567' },
  { name: 'South Africa', code: '+27', flag: '🇿🇦', placeholder: '821234567' }
];
