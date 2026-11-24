const assert = require('node:assert/strict');
const { randomBytes } = require('node:crypto');
const { mkdir } = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');

const address = new URL(process.env.GRC_BROWSER_URL || 'http://127.0.0.1:8080');
assert.equal(address.hostname, '127.0.0.1', 'Run the test against a local synthetic database');
assert.match(address.protocol, /^https?:$/);
const suffix = randomBytes(4).toString('hex');
const email = `switch-${suffix}@example.invalid`;
const first = { slug: `tenant-a-${suffix}`, name: 'Projekt Nord', password: randomBytes(24).toString('hex') };
const second = { slug: `tenant-b-${suffix}`, name: 'Projekt Süd', password: randomBytes(24).toString('hex') };
const secondAdmin = { email: `admin-${suffix}@example.invalid`, password: randomBytes(24).toString('hex') };

async function run() {
  const setupKey = process.env.GRC_BOOTSTRAP_TOKEN;
  assert.ok(setupKey, 'Set GRC_BOOTSTRAP_TOKEN for the local test application');
  const browser = await chromium.launch({ executablePath: process.env.GRC_BROWSER_EXECUTABLE, headless: true });
  const context = await browser.newContext({ baseURL: address.origin, viewport: { width: 390, height: 844 }, locale: 'de-DE' });

  const api = (url, options) => context.request.fetch(`/api/v1${url}`, options);
  const authorization = token => ({ Authorization: `Bearer ${token}` });

  async function bootstrap(tenant, admin) {
    const reply = await api('/tenants', { method: 'POST', headers: { 'X-Bootstrap-Token': setupKey }, data: {
      slug: tenant.slug, name: tenant.name, admin_email: admin.email, admin_password: admin.password
    } });
    assert.equal(reply.status(), 201);
    const token = await api('/auth/login', { method: 'POST', data: {
      tenant_slug: tenant.slug, email: admin.email, password: admin.password
    } });
    assert.equal(token.status(), 200);
    return (await token.json()).access_token;
  }

  async function record(token, title, scope) {
    const reply = await api('/catalog', { method: 'POST', headers: authorization(token), data: { kind: 'asset', title, scope } });
    assert.equal(reply.status(), 201);
    return (await reply.json()).id;
  }

  try {
    const source = await bootstrap(first, { email, password: first.password });
    const admin = await bootstrap(second, secondAdmin);
    assert.equal((await api('/members', { method: 'POST', headers: authorization(admin), data: {
      email, password: second.password, role: 'viewer'
    } })).status(), 201);
    const northId = await record(source, 'Nord · geschütztes Asset', 'Nord');
    await record(admin, 'Süd · freigegebenes Asset', 'Süd');

    const page = await context.newPage();
    page.setDefaultTimeout(8_000);
    const button = (name, container = page) => container.getByRole('button', { name, exact: true });
    const heading = name => page.getByRole('heading', { name, exact: true });
    const dialog = name => page.getByRole('dialog', { name, exact: true });
    const chooser = dialog('Mandant wechseln');
    const guard = dialog('Ungespeicherte Änderungen');
    const slugField = container => container.getByLabel('Mandantenkürzel', { exact: true });
    const logins = [];
    const profileChecks = [];
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.pathname === '/api/v1/auth/login') logins.push(request);
      if (url.pathname === '/api/v1/auth/me') profileChecks.push(request);
    });

    async function authenticate(tenant, target = page, password = tenant.password) {
      await slugField(target).fill(tenant.slug);
      await target.getByLabel('E-Mail', { exact: true }).fill(email);
      await target.getByLabel('Passwort', { exact: true }).fill(password);
      const expectedButton = target === page ? 'Arbeitsbereich öffnen' : 'Mandant verbinden';
      const reply = page.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/auth/login');
      await button(expectedButton, target).click();
      return reply;
    }

    async function tenantMenu() {
      await button('Mandant wechseln').click();
      await chooser.waitFor();
    }

    async function navigate(label) {
      await button('Navigation öffnen').click();
      await button(label, page.locator('aside')).click();
      await heading(label).waitFor();
    }

    async function pick(tenant) {
      await chooser.getByRole('button', { name: new RegExp(tenant.name) }).click();
    }

    async function current(tenant) {
      await heading('Projektüberblick').waitFor();
      assert.ok((await button('Mandant wechseln').getAttribute('title')).includes(tenant.name));
    }

    await page.goto(address.origin);
    const loginA = await authenticate(first);
    assert.equal(loginA.status(), 200);
    const tokenA = (await loginA.json()).access_token;
    await current(first);
    await page.getByLabel('Geltungsbereich', { exact: true }).selectOption('Nord');
    await button('Anwenden').click();
    await navigate('Assets');
    await button('Nord · geschütztes Asset').click();
    await button('Bearbeiten').click();
    const title = page.getByRole('form', { name: 'Eintrag bearbeiten' }).getByLabel('Titel', { exact: true });
    await title.fill('Nord · gespeicherter Entwurf');

    await tenantMenu();
    await button('Weiteren Mandanten verbinden', chooser).click();
    assert.equal((await authenticate(second, chooser, 'Falsches synthetisches Kennwort')).status(), 401);
    await chooser.getByRole('alert').waitFor();
    assert.ok((await button('Mandant wechseln').getAttribute('title')).includes(first.name));
    const loginB = await authenticate(second, chooser);
    assert.equal(loginB.status(), 200);
    const tokenB = (await loginB.json()).access_token;
    await guard.waitFor();
    await button('Weiter bearbeiten', guard).click();
    assert.equal(await title.inputValue(), 'Nord · gespeicherter Entwurf');

    await tenantMenu();
    const output = process.env.GRC_BROWSER_ARTIFACTS_DIR;
    if (output) {
      await mkdir(output, { recursive: true, mode: 0o700 });
      await page.screenshot({ path: path.join(output, 'mandantenwechsel-mobil.png'), style: '.user-actions > span { display: none !important; }' });
    }
    await pick(second);
    await guard.waitFor();
    const checksBeforeSwitch = profileChecks.length;
    await button('Speichern', guard).click();
    await current(second);
    assert.equal(profileChecks.length, checksBeforeSwitch + 1);
    assert.equal(await page.getByLabel('Geltungsbereich', { exact: true }).inputValue(), '');
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth));
    await navigate('Assets');
    await button('Süd · freigegebenes Asset').waitFor();
    assert.equal(await button('Nord · gespeicherter Entwurf').count(), 0);
    assert.equal(await button('Asset anlegen').count(), 0);
    assert.equal(await button('Zugriffsverwaltung').count(), 0);
    assert.equal((await api(`/catalog/${northId}`, { headers: authorization(tokenB) })).status(), 404);
    assert.deepEqual(await page.evaluate(() => [localStorage.length, sessionStorage.length]), [0, 0]);

    const secondTab = await context.newPage();
    await secondTab.goto(address.origin);
    await secondTab.getByRole('heading', { name: 'Anmelden', exact: true }).waitFor();
    await secondTab.close();

    await tenantMenu();
    await pick(first);
    await current(first);
    assert.equal(logins.length, 3, 'Switching between connected tenants does not log in again');
    await navigate('Assets');
    await button('Nord · gespeicherter Entwurf').waitFor();
    assert.equal(await button('Süd · freigegebenes Asset').count(), 0);

    assert.equal((await api('/auth/logout', { method: 'POST', headers: authorization(tokenB) })).status(), 204);
    await tenantMenu();
    await pick(second);
    await chooser.getByRole('alert').waitFor();
    assert.match(await chooser.getByRole('alert').innerText(), /Mandantensitzung ist abgelaufen oder wurde beendet/);
    assert.equal(await chooser.getByRole('button', { name: new RegExp(second.name) }).count(), 0);
    await button('Weiteren Mandanten verbinden', chooser).click();
    const renewed = await authenticate(second, chooser);
    assert.equal(renewed.status(), 200);
    const renewedToken = (await renewed.json()).access_token;
    await current(second);

    const logouts = [];
    page.on('response', response => {
      if (new URL(response.url()).pathname === '/api/v1/auth/logout') logouts.push(response.status());
    });
    await button('Abmelden').click();
    await heading('Anmelden').waitFor();
    await page.waitForFunction(async () => await document.querySelectorAll('dialog[open]').length === 0);
    // The browser clears its local state immediately and revokes both server sessions in parallel.
    for (const token of [tokenA, renewedToken]) {
      for (let attempt = 0; attempt < 20; attempt++) {
        const status = (await api('/auth/me', { headers: authorization(token) })).status();
        if (status === 401) break;
        assert.ok(attempt < 19, 'All connected sessions must be rejected after logout');
        await new Promise(resolve => setTimeout(resolve, 50));
      }
    }
    assert.equal(logouts.filter(status => status === 204).length, 2);
    console.log('Mandantenwechsel geprüft: getrennte Konten, erneute Sitzungsprüfung, Entwurfsdialog, Rollen, Widerruf und gemeinsame Abmeldung.');
  } finally {
    await browser.close();
  }
}

run().catch(error => { console.error(error); process.exitCode = 1; });
