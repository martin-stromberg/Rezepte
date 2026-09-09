const { spawn } = require('child_process');
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const os = require('os');
const http = require('http');

const repoRoot = path.resolve(__dirname, '..', '..');
const outputDir = path.join(repoRoot, 'Docs', 'screenshots');
const appDll = path.join(repoRoot, 'Rezepte.Web', 'bin', 'Release', 'net10.0', 'publish', 'Rezepte.Web.dll');
const tempDbDir = fs.mkdtempSync(path.join(os.tmpdir(), 'rezepte-screenshots-db-'));
const tempDb = path.join(tempDbDir, 'rezepte.db');

fs.mkdirSync(outputDir, { recursive: true });

function getFreePort() {
  return new Promise((resolve, reject) => {
    const server = http.createServer();
    server.listen(0, '127.0.0.1', () => {
      const port = server.address().port;
      server.close(() => resolve(port));
    });
    server.on('error', reject);
  });
}

function waitFor(predicate, timeoutMs = 120000, intervalMs = 500) {
  return new Promise((resolve, reject) => {
    const deadline = Date.now() + timeoutMs;
    const check = async () => {
      try {
        const ok = await predicate();
        if (ok) return resolve();
      } catch { /* ignore */ }
      if (Date.now() >= deadline) return reject(new Error('Timeout while waiting for predicate'));
      setTimeout(check, intervalMs);
    };
    check();
  });
}

(async () => {
  if (!fs.existsSync(appDll)) {
    throw new Error(
      `Published application not found at ${appDll}. Run "dotnet publish Rezepte.Web -c Release" first.`
    );
  }

  const port = await getFreePort();
  const baseUrl = `http://127.0.0.1:${port}`;

  const appProcess = spawn('dotnet', [appDll], {
    cwd: path.dirname(appDll),
    env: {
      ...process.env,
      ASPNETCORE_URLS: baseUrl,
      ASPNETCORE_ENVIRONMENT: 'Production',
      ConnectionStrings__Default: `Data Source=${tempDb}`,
      Jwt__Key: 'screenshots-signing-key-0123456789',
      RateLimiting__Authentication__PermitLimit: '1000'
    },
    stdio: 'ignore'
  });

  try {
    // Wait until the application responds to HTTP requests.
    await waitFor(async () => {
      try {
        const res = await fetch(baseUrl);
        return res.status === 200;
      } catch {
        return false;
      }
    }, 120000, 250);

    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    const page = await context.newPage();

    // 1. Registration page with the demo-data checkbox.
    await page.goto(`${baseUrl}/register`, { waitUntil: 'load' });
    await page.waitForSelector('#register-username');
    await page.screenshot({ path: path.join(outputDir, 'register.png'), fullPage: false });

    const username = `demo${Date.now()}`;
    const password = 'DemoTest!123';

    await page.fill('#register-username', username);
    await page.fill('#register-email', `${username}@example.invalid`);
    await page.fill('#register-password', password);
    await page.check('#register-create-demo-data');

    await Promise.all([
      page.waitForURL(url => url.pathname.includes('/login'), { timeout: 20000 }),
      page.click('.page-auth button[type="submit"]')
    ]);

    // 2. Login and wait for the background seeding job to finish.
    await page.waitForSelector('#username');
    await page.fill('#username', username);
    await page.fill('#password', password);
    await Promise.all([
      page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 20000 }),
      page.click('.page-auth button[type="submit"]')
    ]);

    await waitFor(async () => {
      await page.goto(`${baseUrl}/cookbooks`, { waitUntil: 'load' });
      try {
        await page.waitForSelector('.cookbook-grid, .alert-info', { timeout: 3000 });
      } catch {
        return false;
      }
      const count = await page.locator('.cookbook-card').count();
      return count >= 5;
    }, 120000, 2000);

    // 3. Home
    await page.goto(`${baseUrl}/`, { waitUntil: 'load' });
    await Promise.all([
      page.waitForSelector('.latest-recipes .recipe-card:not(.skeleton-card)', { timeout: 20000 }),
      page.waitForSelector('.random-recipes .recipe-card.small-card:not(.skeleton-card)', { timeout: 20000 })
    ]);
    await page.screenshot({ path: path.join(outputDir, 'home.png'), fullPage: false });

    // 4. Cookbooks
    await page.goto(`${baseUrl}/cookbooks`, { waitUntil: 'load' });
    await page.waitForFunction(() => {
      const cards = document.querySelectorAll('.cookbook-card');
      return cards.length === 5 && Array.from(cards).every(c => c.querySelector('img, .no-image') !== null);
    });
    await page.waitForTimeout(1000);
    await page.screenshot({ path: path.join(outputDir, 'cookbooks.png'), fullPage: false });

    // 5. Recipe search
    await page.goto(`${baseUrl}/recipes/search?q=&page=1&pageSize=100`, { waitUntil: 'load' });
    await page.waitForFunction(() => {
      return document.querySelectorAll('.list-group .list-group-item').length >= 10 &&
             document.querySelectorAll('.spinner-border').length === 0;
    });
    await page.screenshot({ path: path.join(outputDir, 'recipes.png'), fullPage: false });

    // 6. Calendar
    await page.goto(`${baseUrl}/calendar`, { waitUntil: 'load' });
    await page.waitForFunction(() => {
      const events = document.querySelectorAll('.calendar-root .recipe-item, .calendar-root .event-item');
      return events.length === 5;
    });
    await page.waitForTimeout(500);
    await page.screenshot({ path: path.join(outputDir, 'calendar.png'), fullPage: false });

    // 7. Shopping list
    await page.goto(`${baseUrl}/shopping-list`, { waitUntil: 'load' });
    await page.waitForFunction(() => {
      return document.querySelectorAll('.shopping-list-grid .shopping-item').length >= 1;
    });
    await page.screenshot({ path: path.join(outputDir, 'shopping-list.png'), fullPage: false });

    // 8. First recipe detail
    await page.goto(`${baseUrl}/recipes/search?q=&page=1&pageSize=100`, { waitUntil: 'load' });
    const firstRecipe = page.locator('.list-group .list-group-item').first();
    if (await firstRecipe.count() > 0) {
      const href = await firstRecipe.getAttribute('href');
      if (href) {
        await page.goto(`${baseUrl}${href}`, { waitUntil: 'load' });
        await page.waitForTimeout(1000);
        await page.screenshot({ path: path.join(outputDir, 'recipe-detail.png'), fullPage: false });
      }
    }

    await context.close();
    await browser.close();
    console.log(`Screenshots saved to ${outputDir}`);
  } finally {
    appProcess.kill('SIGTERM');
    try {
      fs.rmSync(tempDbDir, { recursive: true, force: true });
    } catch { /* ignore */ }
  }
})();
