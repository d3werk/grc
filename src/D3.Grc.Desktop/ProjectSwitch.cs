using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private TenantVault TenantStore => new(_identity);
    private ComboBox _tenantSelector = Ui.Combo();
    private string? _activeTenantId;
    private bool _updatingTenantSelector;
    private IReadOnlyList<ManagedTenant> _tenants = [];
    private static readonly FilePickerFileType BackupFileType = new("D3 GRC-Mandantenarchiv") { Patterns = ["*.d3grcbackup"] };

    private ComboBox BuildTenantSelector()
    {
        _tenantSelector = Ui.Combo();
        _tenantSelector.MinWidth = 185;
        _tenantSelector.MaxWidth = 320;
        _tenantSelector.PlaceholderText = "Mandant auswählen";
        AutomationProperties.SetName(_tenantSelector, "Mandant auswählen");
        _tenantSelector.SelectionChanged += async (_, _) =>
        {
            if (_loading || _busy || _updatingTenantSelector ||
                _tenantSelector.SelectedItem is not ManagedTenant selected || selected.Id == _activeTenantId)
                return;
            await Run(async () =>
            {
                SelectActiveTenant();
                await SwitchManagedTenantAsync(selected);
            });
        };
        return _tenantSelector;
    }

    private void SelectActiveTenant()
    {
        _updatingTenantSelector = true;
        try
        {
            _tenantSelector.ItemsSource = _tenants;
            _tenantSelector.SelectedItem = _tenants.FirstOrDefault(tenant => tenant.Id == _activeTenantId);
        }
        finally { _updatingTenantSelector = false; }
    }

    private async Task RefreshTenantsAsync()
    {
        _tenants = await TenantStore.ListAsync();
        SelectActiveTenant();
    }

    private async Task StartProfileAsync()
    {
        BuildShell();
        await RefreshTenantsAsync();
        await Render();
        if ((_tenants.FirstOrDefault(tenant => !tenant.IsDemo) ?? _tenants.FirstOrDefault()) is { } active)
            await SwitchManagedTenantAsync(active);
        else
            await CreateProjectFromSetupAsync();
    }

    private async Task SwitchManagedTenantAsync(ManagedTenant tenant)
    {
        if (_activeTenantId == tenant.Id || !await CanLeaveFormAsync())
            return;
        bool? recovery = await TenantStore.HasRecoveryAsync(tenant.Id) ? await ChooseRecoveryAsync() : false;
        if (recovery is null)
            return;
        var cleanupPending = await TenantStore.OpenAsync(_workspace, tenant.Id, recovery.Value);
        _activeTenantId = tenant.Id;
        await CompleteProjectSwitchAsync(cleanupPending);
    }

    private async Task<bool> CreateManagedTenantAsync(string name, CompanyProfile profile, bool demo = false)
    {
        var tenant = await TenantStore.CreateAsync(name, profile, demo);
        var cleanupPending = await TenantStore.OpenAsync(_workspace, tenant.Id);
        _activeTenantId = tenant.Id;
        return cleanupPending;
    }

    private async Task OpenDemoTenantAsync()
    {
        if (!await CanLeaveFormAsync())
            return;
        var tenant = _tenants.FirstOrDefault(item => item.IsDemo);
        if (tenant is not null)
        {
            await SwitchManagedTenantAsync(tenant);
            return;
        }
        var cleanupPending = await CreateManagedTenantAsync(DemoTenant.Name, DemoTenant.Profile, demo: true);
        await CompleteProjectSwitchAsync(cleanupPending);
        _status.Text = "Demo geöffnet · fiktives Unternehmen und synthetische Beispieldaten";
    }

    private async Task OpenProject()
    {
        if (!await CanLeaveFormAsync())
            return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Vorhandene verschlüsselte Projektdatei importieren",
            AllowMultiple = false,
            FileTypeFilter = [new("D3 GRC-Projekt") { Patterns = ["*.d3grc"] }]
        });
        if (files.FirstOrDefault() is not { } file)
            return;
        var path = file.TryGetLocalPath() ?? throw new IOException("Bitte eine lokale Projektdatei wählen.");
        bool? recovery = File.Exists(path + ".recovery") ? await ChooseRecoveryAsync() : false;
        if (recovery is null)
            return;
        var password = await Ui.Prompt(this, "Bisheriges Projektpasswort für den einmaligen Import", true);
        if (password is null)
            return;
        var tenant = await TenantStore.ImportLegacyAsync(path, password, recovery.Value);
        await RefreshTenantsAsync();
        await SwitchManagedTenantAsync(tenant);
        if (_activeTenantId == tenant.Id)
            _status.Text = "Projektdatei importiert · künftig über das Mandanten-Dropdown öffnen";
    }

    private async Task SelectTenantActionsAsync()
    {
        var current = _tenants.FirstOrDefault(tenant => tenant.Id == _activeTenantId);
        var actions = new List<object>
        {
            new Choice("new", "Neuen Mandanten einrichten"),
            new Choice("demo", "Demo-Unternehmen öffnen"),
            new Choice("legacy", "Bestehende Projektdatei importieren"),
            new Choice("restore", "Mandantenarchiv wiederherstellen")
        };
        if (_tenants.Count > 0)
            actions.Add(new Choice("backup", $"Alle Mandanten sichern ({_tenants.Count})"));
        if (current is not null)
            actions.Add(new Choice("export", $"Aktiven Mandanten exportieren · {current.Name}"));
        if (await Ui.Pick(this, "Mandanten verwalten", actions) is not Choice action)
            return;
        switch (action.Id)
        {
            case "new": await CreateProjectFromSetupAsync(); break;
            case "demo": await OpenDemoTenantAsync(); break;
            case "legacy": await OpenProject(); break;
            case "restore": await RestoreTenantsAsync(); break;
            case "backup": await ExportTenantsAsync(all: true); break;
            case "export": await ExportTenantsAsync(all: false); break;
        }
    }

    private async Task ExportTenantsAsync(bool all)
    {
        if (!all && _activeTenantId is null)
            throw new InvalidOperationException("Bitte zuerst einen Mandanten auswählen.");
        if (!await SaveFormAsync())
            return;
        var filename = all ? $"D3-GRC-Gesamtsicherung-{DateTime.Today:yyyy-MM-dd}" : $"D3-GRC-Mandant-{DateTime.Today:yyyy-MM-dd}";
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = all ? "Alle Mandanten sichern" : "Aktiven Mandanten exportieren",
            SuggestedFileName = filename,
            DefaultExtension = "d3grcbackup",
            FileTypeChoices = [BackupFileType],
            ShowOverwritePrompt = true
        });
        if (file is null)
            return;
        var path = file.TryGetLocalPath() ?? throw new IOException("Bitte einen lokalen Speicherort wählen.");
        var key = await TenantStore.ExportAsync(_workspace, path, all ? null : _activeTenantId);
        _status.Text = all ? "Mandantensicherung gespeichert." : "Mandant mit Einstellungen und Dokumenten exportiert.";
        await ShowBackupKeyAsync(key);
    }

    private async Task RestoreTenantsAsync()
    {
        if (!await CanLeaveFormAsync())
            return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Mandantenarchiv wiederherstellen",
            FileTypeFilter = [BackupFileType],
            AllowMultiple = false
        });
        if (files.FirstOrDefault() is not { } file)
            return;
        var path = file.TryGetLocalPath() ?? throw new IOException("Bitte ein lokales Mandantenarchiv wählen.");
        var key = await Ui.Prompt(this, "Sicherungsschlüssel des ausgewählten Archivs", true);
        if (key is null)
            return;
        IReadOnlyList<ManagedTenant> restored;
        try { restored = await TenantStore.RestoreAsync(path, key); }
        catch (System.Security.Cryptography.CryptographicException)
        {
            throw new IOException("Der Sicherungsschlüssel stimmt nicht oder das Archiv ist beschädigt.");
        }
        await RefreshTenantsAsync();
        if (_activeTenantId is null && restored.FirstOrDefault() is { } first)
            await SwitchManagedTenantAsync(first);
        _status.Text = $"{restored.Count} Mandanten als separate Kopien wiederhergestellt.";
    }

    private async Task ShowBackupKeyAsync(string key)
    {
        var dialog = new Window
        {
            Title = "Sicherungsschlüssel",
            Width = 620,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var keyBox = Ui.Input();
        keyBox.Text = key;
        keyBox.IsReadOnly = true;
        var copy = new Button { Content = "Schlüssel kopieren", MinHeight = 38 };
        var finish = new Button { Content = "Fertig", IsDefault = true, MinHeight = 38 };
        copy.Click += async (_, _) =>
        {
            if (dialog.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(key);
                copy.Content = "Kopiert";
            }
        };
        finish.Click += (_, _) => dialog.Close();
        dialog.Content = new Border
        {
            Padding = new Thickness(16),
            Child = Ui.Card(Ui.Stack(Ui.Heading("Sicherungsschlüssel separat aufbewahren", 21),
                Ui.Secondary("Zum Wiederherstellen benötigen Sie das Archiv und diesen Schlüssel."),
                Ui.Field("Sicherungsschlüssel", keyBox), Ui.Row(copy, finish)))
        };
        await dialog.ShowDialog(this);
    }

    private async Task CompleteProjectSwitchAsync(bool cleanupPending)
    {
        _refreshAfterPersistence = false;
        ResetProjectContext();
        await RefreshTenantsAsync();
        await Render();
        if (_workspace.PreservedRecoveryPath is { } preserved)
            _status.Text = "Vorheriger Wiederherstellungsstand gesichert · " + preserved;
        if (cleanupPending)
            await Ui.Confirm(this, "Der Mandant wurde gewechselt. Die bisherige Arbeitskopie kann über „Arbeitskopien bereinigen“ entfernt werden.", "Verstanden");
    }
}
