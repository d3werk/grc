using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
namespace D3.Grc.Core;
public static class Files
{
    public static void PrivateDirectory(string path)
    {if(OperatingSystem.IsWindows())Directory.CreateDirectory(path);else{var mode=UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute;Directory.CreateDirectory(path,mode);File.SetUnixFileMode(path,mode);}}
    public static FileStream Create(string path)
    {var o=new FileStreamOptions{Mode=FileMode.CreateNew,Access=FileAccess.Write,Share=FileShare.None,Options=FileOptions.WriteThrough};if(!OperatingSystem.IsWindows())o.UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite;return new FileStream(path,o);}
    public static async Task<byte[]> ReadAsync(string path,int max)
    {await using var f=File.OpenRead(path);if(f.Length>max)throw new IOException("Datei überschreitet Größenlimit.");var bytes=new byte[checked((int)f.Length)];await f.ReadExactlyAsync(bytes);return bytes;}
    public static async Task AtomicAsync(string path,byte[] bytes)
    {
        if(new FileInfo(path).LinkTarget is not null||new FileInfo(path+".bak").LinkTarget is not null)throw new IOException("Symlink-Ausgabe nicht erlaubt.");var tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{await using(var f=Create(tmp)){await f.WriteAsync(bytes);f.Flush(true);}if(File.Exists(path))File.Copy(path,path+".bak",true);File.Move(tmp,path,true);}finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
}
public sealed class Cipher:IDisposable
{
    public const int Limit=128*1024*1024;private byte[]? _key,_salt;
    public static async Task<byte[]> DeriveAsync(string password,byte[] salt)
    {
        if(password.Length is <1 or >1024||salt.Length!=16)throw new ArgumentException("Passwort oder Salt ungültig.");var bytes=Encoding.UTF8.GetBytes(password);
        try{return await Task.Run(()=>{using var a=new Argon2id(bytes){Salt=salt,MemorySize=65536,Iterations=3,DegreeOfParallelism=2};return a.GetBytes(32);});}finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public async Task InitializeAsync(string password,byte[]? salt=null)
    {var s=salt??RandomNumberGenerator.GetBytes(16);var key=await DeriveAsync(password,s);Dispose();_key=key;_salt=s.ToArray();}
    public static byte[] Salt(byte[] file)
    {if(file.Length<52||file.Length>Limit+52||!file.AsSpan(0,8).SequenceEqual("D3GRC005"u8))throw new InvalidDataException("Unterstützt wird das D3GRC005-Format aus 0.5/0.6.");return file.AsSpan(8,16).ToArray();}
    public byte[] Encrypt(byte[] plain)
    {
        if(_key is null||_salt is null)throw new InvalidOperationException("Projekt gesperrt.");if(plain.Length>Limit)throw new IOException("Projekt größer als 128 MiB.");
        var result=new byte[plain.Length+52];"D3GRC005"u8.CopyTo(result);_salt.CopyTo(result,8);RandomNumberGenerator.Fill(result.AsSpan(24,12));
        using var aes=new AesGcm(_key,16);aes.Encrypt(result.AsSpan(24,12),plain,result.AsSpan(36,plain.Length),result.AsSpan(36+plain.Length,16),result.AsSpan(0,36));return result;
    }
    public byte[] Decrypt(byte[] file)
    {
        var salt=Salt(file);if(_key is null||_salt is null||!salt.AsSpan().SequenceEqual(_salt))throw new CryptographicException("Schlüsselkontext ungültig.");var plain=new byte[file.Length-52];
        try{using var aes=new AesGcm(_key,16);aes.Decrypt(file.AsSpan(24,12),file.AsSpan(36,plain.Length),file.AsSpan(36+plain.Length,16),plain,file.AsSpan(0,36));return plain;}catch{CryptographicOperations.ZeroMemory(plain);throw;}
    }
    public void Dispose(){if(_key is not null)CryptographicOperations.ZeroMemory(_key);_key=null;_salt=null;}
}
public sealed class LocalIdentity
{
    private readonly string _path;private readonly SemaphoreSlim _gate=new(1,1);
    public string? User {get;private set;}
    public LocalIdentity(string? directory=null)
    {
        var dir=directory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"D3GRC","identity-v1");Files.PrivateDirectory(dir);_path=Path.Combine(dir,"profiles.db");
        using var c=Database.Open(_path);Database.Exec(c,null,"CREATE TABLE IF NOT EXISTS Profiles(Name TEXT PRIMARY KEY,Salt BLOB NOT NULL,Verifier BLOB NOT NULL,Failures INTEGER NOT NULL DEFAULT 0,LockedUntil TEXT NOT NULL DEFAULT '')");
        if(!OperatingSystem.IsWindows())File.SetUnixFileMode(_path,UnixFileMode.UserRead|UnixFileMode.UserWrite);
    }
    public async Task RegisterAsync(string name,string password)
    {
        name=name.Trim().ToUpperInvariant();if(name.Length is <3 or >80||password.Length<12)throw new ArgumentException("Profilname: 3–80 Zeichen; Passwort: mindestens 12 Zeichen.");
        await _gate.WaitAsync();try{var salt=RandomNumberGenerator.GetBytes(16);var hash=await Cipher.DeriveAsync(password,salt);try{using var c=Database.Open(_path);Database.Exec(c,null,"INSERT INTO Profiles(Name,Salt,Verifier) VALUES($p0,$p1,$p2)",name,salt,hash);}finally{CryptographicOperations.ZeroMemory(hash);}}finally{_gate.Release();}
    }
    public async Task<bool> LoginAsync(string name,string password)
    {
        name=name.Trim().ToUpperInvariant();await _gate.WaitAsync();try{
            using var c=Database.Open(_path);var row=Database.Query(c,null,"SELECT Salt,Verifier,Failures,LockedUntil FROM Profiles WHERE Name=$p0",r=>(Salt:(byte[])r[0],Hash:(byte[])r[1],Failures:r.GetInt32(2),Until:r.GetString(3)),name).SingleOrDefault();
            if(row.Salt is null){var dummy=await Cipher.DeriveAsync(password,new byte[16]);CryptographicOperations.ZeroMemory(dummy);return false;}
            if(DateTimeOffset.TryParse(row.Until,out var until)&&until>DateTimeOffset.UtcNow)throw new InvalidOperationException("Profil vorübergehend gesperrt. Später erneut versuchen.");
            var actual=await Cipher.DeriveAsync(password,row.Salt);bool ok;try{ok=CryptographicOperations.FixedTimeEquals(actual,row.Hash);}finally{CryptographicOperations.ZeroMemory(actual);}
            if(ok){Database.Exec(c,null,"UPDATE Profiles SET Failures=0,LockedUntil='' WHERE Name=$p0",name);User=name;return true;}
            var failures=row.Failures+1;Database.Exec(c,null,"UPDATE Profiles SET Failures=$p0,LockedUntil=$p1 WHERE Name=$p2",failures,failures>=5?DateTimeOffset.UtcNow.AddMinutes(5).ToString("O"):"",name);return false;
        }finally{_gate.Release();}
    }
    public void Demand(){if(User is null)throw new UnauthorizedAccessException("Bitte zuerst anmelden.");}
    public void Logout()=>User=null;
}
