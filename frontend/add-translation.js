const fs = require('fs');

try {
  const keys = JSON.parse(fs.readFileSync('keys.json', 'utf8'));

  for (const lang of ['en', 'ar']) {
    const path = `src/assets/i18n/${lang}.json`;
    let data = {};
    if (fs.existsSync(path)) {
      data = JSON.parse(fs.readFileSync(path, 'utf8'));
    }
    
    // Merge keys
    for (const [section, items] of Object.entries(keys[lang])) {
      if (!data[section]) data[section] = {};
      for (const [k, v] of Object.entries(items)) {
        data[section][k] = v;
      }
    }
    
    fs.writeFileSync(path, JSON.stringify(data, null, 2));
    console.log(`Updated ${lang}.json`);
  }
} catch (e) {
  console.error("Error:", e);
  process.exit(1);
}
