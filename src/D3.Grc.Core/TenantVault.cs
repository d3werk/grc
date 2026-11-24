using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace D3.Grc.Core;

public sealed record ManagedTenant(string Id, string Name, bool IsDemo)
{
    public override string ToString() => Name;
}

public sealed class TenantVault(LocalIdentity identity)
{
    private const string ManifestName = "mandanten.d3vault";
    private const int ManifestLimit = 8 * 1024 * 1024;
    private const int MaxTenants = 1000;
    private sealed record TenantFile(ManagedTenant Tenant, string Password);
    private sealed record BackupEntry(TenantFile File, string Sha256);
    private sealed record BackupManifest(int Version, BackupEntry[] Entries);

    private string Root
    {
        get
        {
            identity.Demand();
            var name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.User!)));
            return Path.Combine(identity.DataDirectory, "tenants-v1", name);
        }
    }

    private FileStream OpenVault()
    {
        Files.PrivateDirectory(Root);
        return new FileStream(Path.Combine(Root, "vault.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    private string IndexPath => Path.Combine(Root, "index.d3vault");
    private string TenantPath(string id) => Path.Combine(Root, id + ".d3grc");

    private static bool TenantId(string id) => Guid.TryParseExact(id, "N", out var value) && value.ToString("N") == id;
    private static string NewSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    private async Task<List<TenantFile>> ReadIndexAsync()
    {
        if (!File.Exists(IndexPath))
            return [];
        using var cipher = identity.CreateVaultCipher();
        return Decode<List<TenantFile>>(cipher, await Files.ReadAsync(IndexPath, ManifestLimit)) ?? [];
    }

    private async Task WriteIndexAsync(List<TenantFile> index)
    {
        using var cipher = identity.CreateVaultCipher();
        await Files.AtomicAsync(IndexPath, Encode(cipher, index));
    }

    private static byte[] Encode<T>(Cipher cipher, T value)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(value);
        try
        {
            if (plain.Length > ManifestLimit - 52)
                throw new IOException("Das Mandantenverzeichnis überschreitet die zulässige Größe.");
            return cipher.Encrypt(plain);
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    private static T? Decode<T>(Cipher cipher, byte[] bytes)
    {
        var plain = cipher.Decrypt(bytes);
        try { return JsonSerializer.Deserialize<T>(plain); }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }

    public async Task<IReadOnlyList<ManagedTenant>> ListAsync()
    {
        using var lease = OpenVault();
        return (await ReadIndexAsync()).Select(row => row.Tenant)
            .OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(row => row.Id).ToArray();
    }

    public async Task<ManagedTenant> CreateAsync(string name, CompanyProfile profile, bool demo = false, string? catalog = null)
    {
        using var lease = OpenVault();
        var index = await ReadIndexAsync();
        if (index.Count >= MaxTenants)
            throw new IOException("Das Mandantenlimit dieses Profils ist erreicht.");
        var existingDemo = demo ? index.SingleOrDefault(item => item.Tenant.IsDemo) : null;
        if (existingDemo is not null)
            return existingDemo.Tenant;

        var tenant = new ManagedTenant(Guid.NewGuid().ToString("N"), UniqueName(index, name.Trim()), demo);
        var password = NewSecret();
        var path = TenantPath(tenant.Id);
        try
        {
            using (var project = new Workspace(identity))
            {
                await project.CreateAsync(path, tenant.Name, password, catalog, profile);
                if (demo)
                    await project.SeedDemoAsync();
            }
            index.Add(new TenantFile(tenant, password));
            await WriteIndexAsync(index);
            return tenant;
        }
        catch
        {
            RemoveNewFile(path);
            throw;
        }
    }

    public async Task<ManagedTenant> ImportLegacyAsync(string source, string password, bool recovery = false)
    {
        using var lease = OpenVault();
        var index = await ReadIndexAsync();
        if (index.Count >= MaxTenants)
            throw new IOException("Das Mandantenlimit dieses Profils ist erreicht.");
        var id = Guid.NewGuid().ToString("N");
        var staged = Path.Combine(Root, "import-" + id + ".d3grc");
        var newPassword = NewSecret();
        var path = TenantPath(id);
        try
        {
            var bytes = await Files.ReadAsync(recovery ? source + ".recovery" : source, Cipher.Limit + 52);
            await Files.AtomicAsync(staged, bytes);
            using var project = new Workspace(identity);
            await project.OpenAsync(staged, password);
            var tenant = new ManagedTenant(id, UniqueName(index, project.Name), false);
            await project.CopyWithPasswordAsync(path, newPassword);
            index.Add(new TenantFile(tenant, newPassword));
            await WriteIndexAsync(index);
            return tenant;
        }
        catch
        {
            RemoveNewFile(path);
            throw;
        }
        finally
        {
            RemoveNewFile(staged);
            foreach (var backup in Directory.EnumerateFiles(Root, Path.GetFileName(staged) + ".pre*.bak"))
                TryRemove(backup);
        }
    }

    public async Task<bool> OpenAsync(Workspace workspace, string id, bool? recovery = null)
    {
        using var lease = OpenVault();
        var file = (await ReadIndexAsync()).FirstOrDefault(row => row.Tenant.Id == id)
            ?? throw new ArgumentException("Mandant fehlt.");
        var path = TenantPath(file.Tenant.Id);
        return await workspace.SwitchAsync(path, file.Password, recovery ?? File.Exists(path + ".recovery"));
    }

    public async Task<bool> HasRecoveryAsync(string id)
    {
        using var lease = OpenVault();
        return (await ReadIndexAsync()).Any(row => row.Tenant.Id == id && File.Exists(TenantPath(row.Tenant.Id) + ".recovery"));
    }

    public async Task<string> ExportAsync(Workspace workspace, string target, string? id = null)
    {
        using var lease = OpenVault();
        var files = (await ReadIndexAsync()).Where(row => id is null || row.Tenant.Id == id).ToArray();
        if (files.Length == 0)
            throw new InvalidOperationException("Kein Mandant für die Sicherung vorhanden.");
        target = Path.GetFullPath(target);
        if (!Path.GetExtension(target).Equals(".d3grcbackup", StringComparison.OrdinalIgnoreCase) ||
            new FileInfo(target).LinkTarget is not null)
            throw new IOException("Bitte einen sicheren Dateipfad mit der Endung .d3grcbackup wählen.");

        var key = NewSecret();
        var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using var safeguard = new Cipher();
            await safeguard.InitializeAsync(key);
            await using (var stream = Files.Create(temp))
            {
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var items = new List<BackupEntry>();
                    foreach (var file in files)
                    {
                        var path = TenantPath(file.Tenant.Id);
                        if (workspace.IsCurrentProject(path) && workspace.IsDirty)
                            await workspace.SaveAsync();
                        var snapshot = File.Exists(path + ".recovery") ? path + ".recovery" : path;
                        var bytes = await Files.ReadAsync(snapshot, Cipher.Limit + 52);
                        Cipher.Salt(bytes);
                        await WriteZipEntry(zip, "projects/" + file.Tenant.Id + ".d3grc", bytes);
                        items.Add(new BackupEntry(file, Convert.ToHexString(SHA256.HashData(bytes))));
                    }
                    await WriteZipEntry(zip, ManifestName, Encode(safeguard, new BackupManifest(1, items.ToArray())));
                }
                stream.Flush(true);
            }
            File.Move(temp, target, true);
            return key;
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    public async Task<IReadOnlyList<ManagedTenant>> RestoreAsync(string source, string recoveryKey)
    {
        using var lease = OpenVault();
        var index = await ReadIndexAsync();
        recoveryKey = recoveryKey.Trim().ToUpperInvariant();
        if (recoveryKey.Length != 48 || !recoveryKey.All(Uri.IsHexDigit))
            throw new ArgumentException("Bitte den 48-stelligen Sicherungsschlüssel eingeben.");
        using var zip = ZipFile.OpenRead(source);
        if (zip.Entries.Count is < 2 or > MaxTenants + 1 ||
            zip.Entries.Select(entry => entry.FullName).Distinct(StringComparer.Ordinal).Count() != zip.Entries.Count)
            throw new InvalidDataException("Das Archiv enthält eine ungültige Anzahl oder doppelte Einträge.");
        var keyData = await ReadZipEntry(zip, ManifestName, ManifestLimit);
        using var safeguard = new Cipher();
        await safeguard.InitializeAsync(recoveryKey, Cipher.Salt(keyData));
        var manifest = Decode<BackupManifest>(safeguard, keyData);
        if (manifest is not { Version: 1, Entries.Length: > 0 } || zip.Entries.Count != manifest.Entries.Length + 1 ||
            manifest.Entries.Length + index.Count > MaxTenants)
            throw new InvalidDataException("Mandantenarchiv oder Mandantenanzahl ungültig.");

        var added = new List<TenantFile>();
        try
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in manifest.Entries)
            {
                if (item?.File?.Tenant is not { } old || old.Id is null || !TenantId(old.Id) || !ids.Add(old.Id) ||
                    string.IsNullOrWhiteSpace(old.Name) || old.Name.Length > 240 ||
                    item.File.Password is not { Length: 48 } password || !password.All(Uri.IsHexDigit))
                    throw new InvalidDataException("Das Mandantenverzeichnis ist ungültig.");
                var bytes = await ReadZipEntry(zip, "projects/" + old.Id + ".d3grc", Cipher.Limit + 52);
                if (!string.Equals(item.Sha256, Convert.ToHexString(SHA256.HashData(bytes)), StringComparison.Ordinal))
                    throw new InvalidDataException("Die Prüfsumme eines Projekts stimmt nicht.");
                using (var projectCipher = new Cipher())
                {
                    await projectCipher.InitializeAsync(password, Cipher.Salt(bytes));
                    var plain = projectCipher.Decrypt(bytes);
                    try
                    {
                        if (!plain.AsSpan().StartsWith("SQLite format 3\0"u8))
                            throw new InvalidDataException("Ein gesichertes Projekt besitzt kein gültiges Datenbankformat.");
                    }
                    finally { CryptographicOperations.ZeroMemory(plain); }
                }

                var copy = old with
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = UniqueName(index.Concat(added), old.Name),
                    IsDemo = old.IsDemo && !index.Concat(added).Any(row => row.Tenant.IsDemo)
                };
                added.Add(new TenantFile(copy, password));
                await Files.AtomicAsync(TenantPath(copy.Id), bytes);
            }
            index.AddRange(added);
            await WriteIndexAsync(index);
            return added.Select(row => row.Tenant).ToArray();
        }
        catch
        {
            foreach (var row in added)
                RemoveNewFile(TenantPath(row.Tenant.Id));
            throw;
        }
    }

    private static async Task<byte[]> ReadZipEntry(ZipArchive zip, string name, int limit)
    {
        var entry = zip.GetEntry(name) ?? throw new InvalidDataException("Es fehlt eine für den Import benötigte Datei.");
        if (entry.Length < 52 || entry.Length > limit)
            throw new InvalidDataException("Ein Archiveintrag überschreitet die zulässige Größe.");
        var bytes = new byte[checked((int)entry.Length)];
        await using var stream = entry.Open();
        await stream.ReadExactlyAsync(bytes);
        if (stream.ReadByte() != -1)
            throw new InvalidDataException("Die Länge eines Archiveintrags stimmt nicht.");
        return bytes;
    }

    private static async Task WriteZipEntry(ZipArchive zip, string name, byte[] bytes)
    {
        await using var stream = zip.CreateEntry(name, CompressionLevel.NoCompression).Open();
        await stream.WriteAsync(bytes);
    }

    private static void RemoveNewFile(string path)
    {
        foreach (var suffix in new[] { "", ".bak", ".recovery", ".recovery.bak", ".lock" })
            TryRemove(path + suffix);
    }

    private static void TryRemove(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static string UniqueName(IEnumerable<TenantFile> index, string name)
    {
        var names = index.Select(row => row.Tenant.Name).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        for (var copy = 2; names.Contains(name); copy++)
        {
            var suffix = $" · Kopie {copy}";
            var candidate = name[..Math.Min(name.Length, 240 - suffix.Length)] + suffix;
            if (!names.Contains(candidate))
                return candidate;
        }
        return name;
    }
}
