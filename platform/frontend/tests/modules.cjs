const assert = require('node:assert/strict');
const { randomBytes } = require('node:crypto');
const { chromium } = require('playwright');

const address = new URL(process.env.GRC_BROWSER_URL || 'http://127.0.0.1:8080');
assert.equal(address.hostname, '127.0.0.1', 'Run against a local synthetic database');
const setupKey = process.env.GRC_BOOTSTRAP_TOKEN;
const suffix = randomBytes(5).toString('hex');
const scope = 'Synthetischer Fachbereich';
let step = 'initialization';

async function run() {
  assert.ok(setupKey, 'Set GRC_BOOTSTRAP_TOKEN for the test instance');
  const browser = await chromium.launch({ executablePath: process.env.GRC_BROWSER_EXECUTABLE, headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 960 } });
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.setDefaultTimeout(10_000);
  const button = (name, area = page) => area.getByRole('button', { name, exact: true });
  const field = (name, area = page) => area.getByLabel(name, { exact: true });
  const heading = (name, area = page) => area.getByRole('heading', { level: 1, name, exact: true });
  const sidebar = page.getByRole('navigation', { name: 'Arbeitsbereiche' });
  const catalogForm = page.getByRole('form', { name: 'Neuer Eintrag' });

  async function moduleSection(moduleName, section, registerName) {
    step = moduleName + ' / ' + section;
    await button(moduleName, sidebar).click();
    await heading(moduleName).waitFor();
    const nav = page.getByRole('navigation', { name: 'Ansichten für ' + moduleName });
    await button(section, nav).click();
    await heading(registerName).waitFor();
  }

  async function create(singular, title, inputs = {}) {
    step = 'create ' + singular;
    await button(singular + ' anlegen').click();
    await field('Titel', catalogForm).fill(title);
    await field('Geltungsbereich', catalogForm).selectOption(scope);
    for (const [label, value] of Object.entries(inputs)) {
      const input = field(label, catalogForm);
      if (await input.evaluate(node => node.tagName === 'SELECT')) await input.selectOption({ label: value });
      else await input.fill(value);
    }
    const response = page.waitForResponse(response =>
      new URL(response.url()).pathname === '/api/v1/catalog' && response.request().method() === 'POST');
    await button('Speichern', catalogForm).click();
    const result = await response;
    assert.equal(result.status(), 201, await result.text());
    await page.getByRole('heading', { level: 2, name: title, exact: true }).waitFor();
    return result.json();
  }

  try {
    step = 'bootstrap';
    await page.goto(address.origin);
    await button('Neuen Mandanten einrichten').click();
    await field('Name des Mandanten').fill('Fachmodule Browsertest');
    await field('Mandantenkürzel').fill('modules-' + suffix);
    await field('E-Mail der Administration').fill('module-' + suffix + '@example.invalid');
    await field('Passwort').fill(randomBytes(24).toString('hex'));
    await field('Einrichtungsschlüssel').fill(setupKey);
    await button('Mandant anlegen').click();
    await heading('Projektüberblick').waitFor();

    step = 'scope setup';
    await button('Geltungsbereich anlegen').click();
    const scopeForm = page.getByRole('form', { name: 'Geltungsbereich anlegen' });
    await field('Name des Geltungsbereichs', scopeForm).fill(scope);
    await button('Geltungsbereich speichern', scopeForm).click();
    const contextBar = page.locator('.context-bar');
    await field('Geltungsbereich', contextBar).selectOption(scope);
    await button('Anwenden', contextBar).click();

    step = 'process steps and map';
    await button('Prozessmanagement', sidebar).click();
    await button('Geschäftsprozess anlegen').click();
    await field('Titel', catalogForm).fill('Anfragen bearbeiten');
    assert.equal(await field('Geltungsbereich', catalogForm).inputValue(), scope);
    await field('Prozessart', catalogForm).selectOption({ label: 'Kernprozess' });
    await button('Schritt hinzufügen', catalogForm).click();
    await field('Tätigkeit · Schritt 1', catalogForm).fill('Anfrage entgegennehmen');
    await field('Rolle · Schritt 1', catalogForm).fill('Serviceleitung');
    await button('Speichern', catalogForm).click();
    await page.getByRole('heading', { name: 'Prozessablauf', exact: true }).waitFor();
    await page.getByText('Anfrage entgegennehmen', { exact: true }).waitFor();
    await button('Prozesslandkarte').click();
    await page.locator('.process-map').getByRole('button', { name: /Anfragen bearbeiten/ }).waitFor();

    await moduleSection('Interne Audits', 'Prüfschritte', 'Prüfschritte');
    const auditTest = await create('Prüfschritt', 'Freigabe im Stichprobentest', { 'Prüfergebnis': 'Bestanden' });
    step = 'atomic audit evidence and detail navigation';
    await button('Nachweis hinzufügen').click();
    const evidence = page.locator('form.edit-panel');
    assert.equal(await field('Verknüpfter Eintrag', evidence).inputValue(), auditTest.id);
    await field('Art', evidence).selectOption('external');
    await field('Titel', evidence).fill('Synthetisches Arbeitspapier');
    await field('Herkunft', evidence).fill('Browserprüfung');
    await field('HTTPS-Referenz', evidence).fill('https://example.invalid/arbeitspapier');
    const saved = page.waitForResponse(response =>
      new URL(response.url()).pathname === '/api/v1/evidence' && response.request().method() === 'POST');
    await button('Nachweis speichern', evidence).click();
    const proof = await saved;
    assert.equal(proof.status(), 201, await proof.text());
    assert.deepEqual((await proof.json()).linked_record_ids, [auditTest.id]);
    await button('Zurück zum Eintrag').click();
    await button('Zugeordnete Nachweise ansehen').click();
    await page.getByRole('button', { name: /Synthetisches Arbeitspapier/ }).waitFor();

    await moduleSection('Operationelle Resilienz', 'Geschäftsdienste', 'Kritische Geschäftsdienste');
    const service = await create('Geschäftsdienst', 'Annahmedienst', { 'Maximal tolerierbare Unterbrechung (Minuten)': '90' });
    assert.equal(service.attributes.tolerance_minutes, '90');

    await moduleSection('Business Continuity', 'Auswirkungsanalysen', 'Business Impact Analysen');
    const bia = await create('Business Impact Analyse', 'Ausfallfolgen für Annahmedienst', {
      'Kritikalität': 'Kritisch', 'Zielzeit für den Wiederanlauf (RTO, Minuten)': '60',
    });
    assert.equal(bia.attributes.rto_minutes, '60');

    await moduleSection('KI-Governance', 'KI-Inventar', 'KI-Systeme');
    const system = await create('KI-System', 'Assistenz für Wissenssuche', {
      'Interne Risikoklasse': 'Mittel',
      'Begründung der Risikoklasse': 'Team prüft Vorschläge vor der internen Verwendung.',
    });
    assert.equal(system.attributes.risk_class, 'Mittel');

    await moduleSection('Vertragsmanagement', 'Vertragsregister', 'Vertragsregister');
    const contract = await create('Vertrag', 'Rahmenvertrag Hosting', { 'Vertragsart': 'Rahmenvertrag' });
    assert.equal(contract.attributes.contract_type, 'Rahmenvertrag');
    assert.deepEqual(errors, []);
    console.log('Fachmodule, Prozesslandkarte, Geltungsbereich und Audit-Nachweis im Browser geprüft.');
  } finally {
    await browser.close();
  }
}

run().catch(error => {
  console.error('Fachmodule ·', step, error);
  process.exitCode = 1;
});
