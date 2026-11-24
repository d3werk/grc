const assert = require("node:assert/strict");
const { randomBytes } = require("node:crypto");
const { mkdir, readFile } = require("node:fs/promises");
const path = require("node:path");
const { chromium } = require("playwright");

const address = new URL(process.env.GRC_BROWSER_URL || "http://127.0.0.1:8080");
assert.equal(address.hostname, "127.0.0.1", "Run the test against a local synthetic database");
assert.match(address.protocol, /^https?:$/);
const origin = address.origin;
const output = process.env.GRC_BROWSER_ARTIFACTS_DIR;
const setupKey = process.env.GRC_BOOTSTRAP_TOKEN;
const slug = "grc-browser-" + randomBytes(4).toString("hex");
const email = slug + "@example.invalid";
const password = randomBytes(24).toString("hex");
const workspaceName = "D3 GRC Browsertest";
const scope = "Synthetischer Betrieb";
let stage = "initialize";

async function run() {
  assert.ok(setupKey, "Set GRC_BOOTSTRAP_TOKEN for the test application");
  if (output) await mkdir(output, { recursive: true, mode: 0o700 });
  const browser = await chromium.launch({ executablePath: process.env.GRC_BROWSER_EXECUTABLE, headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 960 },
    serviceWorkers: "block", acceptDownloads: true, locale: "de-DE" });
  await context.route("**/*", route => new URL(route.request().url()).origin === origin
    ? route.continue() : route.abort());
  const page = await context.newPage();
  page.setDefaultTimeout(8000);
  const browserErrors = [];
  page.on("pageerror", error => browserErrors.push(error.name));
  const apiFailures = [];
  page.on("response", response => {
    const url = new URL(response.url());
    if (url.pathname.startsWith("/api/") && response.status() >= 400)
      apiFailures.push({ method: response.request().method(), status: response.status(), path: url.pathname });
  });

  const button = (name, parent = page) => parent.getByRole("button", { name, exact: true });
  const heading = name => page.getByRole("heading", { name, exact: true });
  const field = (name, parent = page) => parent.getByLabel(name, { exact: true });
  const editorField = (name, parent = page) => parent.getByRole("textbox", { name, exact: true });
  const formatText = async (label, action, parent = page) => {
    await editorField(label, parent).press("ControlOrMeta+a");
    await button(action, parent.getByRole("group", { name: `Formatierung für ${label}`, exact: true })).click();
  };
  const screenshot = async name => output && page.screenshot({ path: path.join(output, name + ".png"), fullPage: true,
    style: ".user-actions > span { visibility: hidden !important; }" });
  const navigate = async (label, title = label) => {
    const sidebar = page.locator("aside");
    await button(label, sidebar).click();
    await heading(title).first().waitFor();
  };

  async function create(nav, singular, code, title, attributes = {}, unit = null, headingLabel = nav) {
    stage = "create " + nav;
    await navigate(nav, headingLabel);
    await button(singular + " anlegen").click();
    const form = page.getByRole("form", { name: "Neuer Eintrag" });
    await field("Titel", form).fill(title);
    await field("Kürzel", form).fill(code);
    await field("Geltungsbereich", form).selectOption(scope);
    await field("Verantwortung", form).fill("Testkoordination");
    await field("Status", form).selectOption("active");
    if (unit) await field("Organisation", form).selectOption(unit);
    await field("Beschreibung", form).fill("Synthetischer Nachweis des realen Browserablaufs.");
    for (const [label, value] of Object.entries(attributes)) await field(label, form).fill(value);
    const [response] = await Promise.all([
      page.waitForResponse(response => new URL(response.url()).pathname === "/api/v1/catalog" && response.request().method() === "POST"),
      form.locator('button[type="submit"]').click()
    ]);
    assert.equal(response.status(), 201);
    const data = await response.json();
    await heading(title).waitFor();
    return data.id;
  }

  async function addLink(type, target, rationale, italic = false) {
    stage = "typed link " + type;
    await button("Bezug anlegen").click();
    await field("Beziehung").selectOption(type);
    await field("Bestehender Eintrag").selectOption(target);
    await field("Begründung").fill(rationale);
    if (italic) await formatText("Begründung", "Kursiv");
    const [response] = await Promise.all([
      page.waitForResponse(response => new URL(response.url()).pathname === "/api/v1/links" && response.request().method() === "POST"),
      button("Bezug speichern").click()
    ]);
    assert.equal(response.status(), 201);
    if (italic) {
      const result = await response.json();
      assert.equal(result.rationale, rationale);
      assert.equal(result.rationale_rich_text.content[0].content[0].marks[0].type, "italic");
    }
    await page.getByText(rationale, { exact: true }).waitFor();
  }

  async function login() {
    stage = "login";
    await heading("Anmelden").waitFor();
    await field("Mandantenkürzel").fill(slug);
    await field("E-Mail").fill(email);
    await field("Passwort").fill(password);
    await button("Arbeitsbereich öffnen").click();
    await page.getByText(workspaceName, { exact: true }).first().waitFor();
  }

  try {
    stage = "tenant bootstrap";
    const arrived = await page.goto(origin, { waitUntil: "networkidle" });
    assert.equal(arrived.status(), 200);
    await heading("Anmelden").waitFor();
    await screenshot("login-desktop");
    await button("Neuen Mandanten einrichten").click();
    await field("Name des Mandanten").fill(workspaceName);
    await field("Mandantenkürzel").fill(slug);
    await field("E-Mail der Administration").fill(email);
    await field("Passwort").fill(password);
    await field("Einrichtungsschlüssel").fill(setupKey);
    await button("Mandant anlegen").click();
    await page.getByText(workspaceName, { exact: true }).first().waitFor();
    console.log("Tenant bootstrap and authentication passed.");
    for (const name of [scope, "Leerer Testbereich"]) {
      await button("Geltungsbereich anlegen").click();
      const scopeForm = page.getByRole("form", { name: "Geltungsbereich anlegen" });
      await field("Name des Geltungsbereichs", scopeForm).fill(name);
      await button("Geltungsbereich speichern", scopeForm).click();
      await scopeForm.waitFor({ state: "hidden" });
    }

    const unit = await create("Organisation", "Organisationseinheit", "SMK-ORG", "SMK Standort Berlin",
      { "Art der Einheit": "Standort" });
    const asset = await create("Assets", "Asset", "SMK-ASSET", "SMK Asset Kundendaten",
      { "Art des Informationswerts": "Datenbank", "Schutzklasse": "Vertraulich" }, unit);
    const service = await create("IT-Dienste", "IT-Dienst", "SMK-SVC", "SMK Microsoft 365",
      { "Art des Dienstes": "SaaS", "Kritikalität": "Hoch" }, unit);
    const risk = await create("Risiken", "Risiko", "SMK-RISK", "SMK Risiko Testdienstausfall",
      { "Risikoszenario": "Ausfall des synthetischen Testdienstes" }, unit);
    await addLink("risk_asset", asset, "Verknüpft das Risiko mit dem konkreten Informationswert.");
    await addLink("risk_it_service", service, "Der gemeinsame IT-Dienst ist vom Risiko betroffen.");
    stage = "assess risk";
    await button("Risiko bewerten").click();
    await field("Eintrittswahrscheinlichkeit").selectOption("5");
    await field("Auswirkung").selectOption("4");
    await field("Restrisiko ergänzen").check();
    await field("Verbleibende Eintrittswahrscheinlichkeit").selectOption("2");
    await field("Verbleibende Auswirkung").selectOption("3");
    await field("Risikobehandlung").selectOption("mitigate");
    await field("Begründung").fill("Die Wiederanlaufprobe senkt Eintritt und Auswirkung.");
    await formatText("Begründung", "Fett");
    await button("Bewertung speichern").click();
    await page.getByText("5 × 4 · 20 · Kritisch", { exact: true }).waitFor();
    await page.getByText("2 × 3 · 6 · Mittel", { exact: true }).waitFor();
    await screenshot("risk-assessment");

    const control = await create("Controls", "Control", "SMK-CTRL", "SMK Control Wiederanlauf",
      { "Kontrollziel": "Wiederanlauf prüfen" }, unit);
    await addLink("risk_control", risk, "Der Wiederanlauf adressiert dieses Risiko.", true);
    await addLink("control_it_service", service, "Das Control schützt den gemeinsamen IT-Dienst.");
    const measure = await create("Maßnahmen", "Maßnahme", "SMK-MEAS", "SMK Maßnahme Wiederanlaufprobe",
      { "Erfolgskriterium": "Der Dienst ist im Testbetrieb verfügbar." }, unit);
    await addLink("action_risk", risk, "Die Maßnahme mindert das dokumentierte Ausfallszenario.");
    await addLink("action_control", control, "Die Maßnahme setzt das Control um.");
    console.log("Organization, asset, common service, risk, control, measure, typed relationships and assessment passed.");

    stage = "evidence upload and preview";
    await navigate("Nachweise");
    await button("Nachweis erfassen").click();
    const evidenceForm = page.locator("form.edit-panel");
    await field("Titel", evidenceForm).fill("SMK Prüfnachweis");
    await field("Herkunft", evidenceForm).fill("Synthetische Wiederanlaufprobe");
    await field("Verknüpfter Eintrag", evidenceForm).selectOption(control);
    assert.equal(await field("Geltungsbereich", evidenceForm).inputValue(), scope);
    const bytes = Buffer.from("SMK Prüfprotokoll: Dienst im Test verfügbar.\n");
    await field("Datei", evidenceForm).setInputFiles({ name: "smk-protokoll.txt", mimeType: "text/plain", buffer: bytes });
    await button("Nachweis speichern").click();
    await heading("SMK Prüfnachweis").waitFor();
    await page.getByText("Control · SMK Control Wiederanlauf", { exact: true }).waitFor();
    await field("Eintrag zuordnen").selectOption(measure);
    await button("Zuordnung speichern").click();
    await page.getByText("Maßnahme · SMK Maßnahme Wiederanlaufprobe", { exact: true }).waitFor();
    await button("Vorschau").click();
    await page.getByText("SMK Prüfprotokoll: Dienst im Test verfügbar.", { exact: true }).waitFor();
    await screenshot("evidence-preview");
    const downloadPromise = page.waitForEvent("download");
    await button("Datei speichern").click();
    const download = await downloadPromise;
    assert.equal(download.suggestedFilename(), "smk-protokoll.txt");
    assert.deepEqual(await readFile(await download.path()), bytes);

    stage = "manual reference";
    await button("Nachweis erfassen").click();
    await field("Art", evidenceForm).selectOption("external");
    await field("Titel", evidenceForm).fill("SMK Externe Referenz");
    await field("Herkunft", evidenceForm).fill("Synthetisches Testarchiv");
    await field("Verknüpfter Eintrag", evidenceForm).selectOption(control);
    await field("HTTPS-Referenz", evidenceForm).fill("https://example.invalid/grc-nachweis");
    await button("Nachweis speichern").click();
    await heading("SMK Externe Referenz").waitFor();
    const reference = page.getByRole("link", { name: "https://example.invalid/grc-nachweis", exact: true });
    assert.equal(await reference.getAttribute("href"), "https://example.invalid/grc-nachweis");

    stage = "risk matrix";
    await navigate("Risikomatrix");
    const matrix = page.getByRole("table", { name: "Risikomatrix nach Wahrscheinlichkeit und Auswirkung" });
    let count = matrix.getByRole("row", { name: /^4 Hoch/ }).getByRole("cell").nth(4);
    await count.getByText("1", { exact: true }).waitFor();
    await field("Bewertungssicht").selectOption("residual");
    count = matrix.getByRole("row", { name: /^3 Mittel/ }).getByRole("cell").nth(1);
    await count.getByText("1", { exact: true }).waitFor();
    await screenshot("risk-matrix");
    console.log("TXT upload, byte-identical download, linked preview, manual reference and risk matrix passed.");

    stage = "privacy activity and shared TOM";
    await create("Verarbeitungsverzeichnis", "Verarbeitung", "SMK-VVT", "SMK Personalverwaltung",
      { "Zweck der Verarbeitung": "Verwaltung der Personalakten", "Rechtsgrundlage": "Art. 6 Abs. 1 lit. b DSGVO" }, unit,
      "Verzeichnis der Verarbeitungen");
    await addLink("processing_it_service", service, "Die Verarbeitung nutzt den vorhandenen IT-Dienst.");
    await addLink("processing_asset", asset, "Die Verarbeitung nutzt den vorhandenen Informationswert.");
    await addLink("processing_control", control, "Die Verarbeitung nutzt das bestehende Control als TOM.");
    await navigate("TOM", "Technische und organisatorische Maßnahmen");
    await page.getByRole("button", { name: /SMK Control Wiederanlauf/ }).first().click();
    await heading("SMK Control Wiederanlauf").waitFor();
    await page.getByText("Die Verarbeitung nutzt das bestehende Control als TOM.", { exact: true }).waitFor();

    stage = "management and privacy exports";
    await navigate("Berichte");
    await button("Bericht anzeigen").click();
    await page.getByText(/Registerbestand am/).waitFor();
    await button("Datenschutz").click();
    await button("Bericht anzeigen").click();
    await page.getByText("SMK Microsoft 365", { exact: true }).waitFor();
    await page.getByText("SMK Asset Kundendaten", { exact: true }).waitFor();
    await page.getByText("SMK Control Wiederanlauf", { exact: true }).waitFor();
    await page.getByText("SMK Personalverwaltung", { exact: true }).waitFor();
    const exportPromise = page.waitForEvent("download");
    await button("CSV speichern").click();
    const privacyDownload = await exportPromise;
    assert.equal(privacyDownload.suggestedFilename(), "D3-GRC-Datenschutz.csv");
    const report = await readFile(await privacyDownload.path(), "utf8");
    for (const term of ["SMK Personalverwaltung", "SMK Microsoft 365", "SMK Control Wiederanlauf"])
      assert.ok(report.includes(term));
    await screenshot("privacy-report");
    console.log("Shared service, asset and TOM in the VVT, privacy export and management report passed.");

    stage = "dashboard and responsive navigation";
    const nav = page.getByRole("navigation", { name: "Arbeitsbereiche" });
    await button("Übersicht", nav).click();
    await heading("Register im Überblick").waitFor();
    await button("Assets öffnen, 1 Eintrag").waitFor();
    const help = button("Berechnung der Risikokennzahlen");
    await help.focus();
    await page.getByRole("tooltip").filter({ hasText: "letzte Bewertung" }).waitFor({ state: "visible" });
    await page.keyboard.press("Escape");
    await page.getByRole("tooltip").filter({ hasText: "letzte Bewertung" }).waitFor({ state: "hidden" });
    await page.mouse.move(305, 28);
    await screenshot("dashboard-desktop");
    await page.setViewportSize({ width: 390, height: 844 });
    await button("Navigation öffnen").waitFor();
    assert.equal(await page.evaluate(() => window.innerWidth), 390);
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1),
      "Mobile workspace has page-wide horizontal overflow");
    await screenshot("dashboard-mobile");
    await button("Navigation öffnen").click();
    await button("Risiken", nav).click();
    await heading("Risiken").waitFor();
    await button("Navigation öffnen").waitFor();
    await page.getByRole("button", { name: /SMK Risiko Testdienstausfall/ }).click();
    await heading("SMK Risiko Testdienstausfall").waitFor();
    await screenshot("risk-mobile");

    stage = "scope filtering";
    await page.setViewportSize({ width: 1440, height: 960 });
    await field("Geltungsbereich", page.locator("form.context-bar")).selectOption("Leerer Testbereich");
    await button("Anwenden").click();
    await heading("Noch keine Einträge").waitFor();
    assert.equal(await page.getByText("SMK Risiko Testdienstausfall", { exact: true }).count(), 0);
    await field("Geltungsbereich", page.locator("form.context-bar")).selectOption(scope);
    await button("Anwenden").click();
    await page.getByRole("button", { name: /SMK Risiko Testdienstausfall/ }).waitFor();

    stage = "leave only changed editors";
    const editRisk = async () => {
      await navigate("Risiken");
      await page.getByRole("button", { name: /SMK Risiko Testdienstausfall/ }).click();
      await button("Bearbeiten").click();
      return page.getByRole("form", { name: "Eintrag bearbeiten" });
    };
    const dirtyDialog = page.getByRole("dialog", { name: "Ungespeicherte Änderungen" });
    await editRisk();
    await navigate("IT-Dienste");
    assert.equal(await dirtyDialog.count(), 0);
    let editor = await editRisk();
    const baselineDescription = "Synthetischer Nachweis des realen Browserablaufs.";
    await formatText("Beschreibung", "Fett", editor);
    assert.equal(await editorField("Beschreibung", editor).innerText(), baselineDescription);
    await editorField("Beschreibung", editor).locator("strong").getByText(baselineDescription, { exact: true }).waitFor();
    await button("IT-Dienste", nav).click();
    await dirtyDialog.waitFor();
    await button("Verwerfen", dirtyDialog).click();
    await heading("IT-Dienste").waitFor();
    editor = await editRisk();
    assert.equal(await editorField("Beschreibung", editor).innerText(), baselineDescription);
    assert.equal(await editorField("Beschreibung", editor).locator("strong").count(), 0);
    await field("Beschreibung", editor).fill("Im Browsertest gespeicherte Änderung.");
    await formatText("Beschreibung", "Fett", editor);
    await formatText("Beschreibung", "Aufzählung", editor);
    await editorField("Beschreibung", editor).locator("ul strong").getByText("Im Browsertest gespeicherte Änderung.", { exact: true }).waitFor();
    await screenshot("rich-text-editor");
    await button("IT-Dienste", nav).click();
    await dirtyDialog.waitFor();
    await button("Speichern", dirtyDialog).click();
    await heading("IT-Dienste").waitFor();
    console.log("Scope filtering and leaving unchanged or changed editors passed.");

    stage = "logout and reopen";
    await page.setViewportSize({ width: 1440, height: 960 });
    await button("Abmelden").click();
    await login();
    await navigate("Risiken");
    await page.getByRole("button", { name: /SMK Risiko Testdienstausfall/ }).click();
    await page.getByText("5 × 4 · 20 · Kritisch", { exact: true }).waitFor();
    await page.locator(".rich-text-preview strong").getByText("Die Wiederanlaufprobe senkt Eintritt und Auswirkung.", { exact: true }).waitFor();
    await page.locator(".rich-text-preview em").getByText("Der Wiederanlauf adressiert dieses Risiko.", { exact: true }).waitFor();
    await page.locator(".rich-text-preview ul strong").getByText("Im Browsertest gespeicherte Änderung.", { exact: true }).waitFor();
    assert.deepEqual(browserErrors, []);
    assert.deepEqual(apiFailures, []);
    console.log("390px workspace navigation, logout/login and persisted records passed.");
    if (output) console.log("Screenshots:", output);
  } catch (error) {
    if (stage !== "tenant bootstrap" && stage !== "login") {
      await screenshot("failed-" + stage.replace(/[^a-z0-9-]+/gi, "-")).catch(() => {});
    }
    const message = error.message.replaceAll(setupKey, "[redacted]").replaceAll(password, "[redacted]");
    console.error("Stage:", stage, "|", message);
    console.error("API failures:", JSON.stringify(apiFailures));
    throw new Error("Browser smoke failed");
  } finally {
    await browser.close();
  }
}

run().catch(error => {
  console.error("Browser test failed:", error.message);
  process.exitCode = 1;
});
