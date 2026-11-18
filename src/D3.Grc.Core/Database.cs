using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace D3.Grc.Core;
public static class Database
{
    private static readonly Lazy<bool> Init=new(()=>{SQLitePCL.Batteries_V2.Init();return true;});
    public static SqliteConnection Open(string path)
    {
        _=Init.Value;var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false,ForeignKeys=true}.ToString());c.Open();return c;
    }
    public static SqliteCommand Command(SqliteConnection c,SqliteTransaction? t,string sql,params object?[] args)
    {var cmd=c.CreateCommand();cmd.Transaction=t;cmd.CommandText=sql;for(int i=0;i<args.Length;i++)cmd.Parameters.AddWithValue("$p"+i,args[i]??DBNull.Value);return cmd;}
    public static int Exec(SqliteConnection c,SqliteTransaction? t,string sql,params object?[] args)
    {using var cmd=Command(c,t,sql,args);return cmd.ExecuteNonQuery();}
    public static string Scalar(SqliteConnection c,SqliteTransaction? t,string sql,params object?[] args)
    {using var cmd=Command(c,t,sql,args);return Convert.ToString(cmd.ExecuteScalar())??"";}
    public static List<T> Query<T>(SqliteConnection c,SqliteTransaction? t,string sql,Func<SqliteDataReader,T> read,params object?[] args)
    {using var cmd=Command(c,t,sql,args);using var reader=cmd.ExecuteReader();var rows=new List<T>();while(reader.Read())rows.Add(read(reader));return rows;}
    public static string Resource(string version)
    {using var stream=typeof(Database).Assembly.GetManifestResourceStream("D3.Grc.Core.Migrations."+version+".sql")??throw new InvalidDataException("Migrationsressource fehlt.");using var reader=new StreamReader(stream);return reader.ReadToEnd();}
    public static async Task MigrateAsync(string path)
    {
        using(var c=Open(path))
        {
            var engine=Version.Parse(Scalar(c,null,"SELECT sqlite_version()"));if(engine<new Version(3,50,2))throw new InvalidOperationException("Native SQLite-Version zu alt: "+engine);
        }
        var options=new DbContextOptionsBuilder<SchemaDb>().UseSqlite(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false}.ToString()).Options;
        await using var db=new SchemaDb(options);var known=db.Database.GetMigrations().ToHashSet();var applied=(await db.Database.GetAppliedMigrationsAsync()).ToList();
        if(applied.Any(x=>!known.Contains(x)))throw new InvalidDataException("Unbekannte neuere Migration. Kein Downgrade.");
        await db.Database.MigrateAsync();
        using var check=Open(path);if(Scalar(check,null,"PRAGMA quick_check")!="ok")throw new InvalidDataException("Datenbank beschädigt.");
        if(Query(check,null,"PRAGMA foreign_key_check",r=>r.GetValue(0)).Count!=0)throw new InvalidDataException("Fremdschlüsselprüfung fehlgeschlagen.");
    }
}
// EF is responsible for versioned schema SQL; data access is parameterized ADO.NET.
public sealed class SchemaDb(DbContextOptions<SchemaDb> options):DbContext(options) { }
[DbContext(typeof(SchemaDb))][Migration("20261118120000_BrandWorkspace")]
public sealed class Initial:Migration
{
    protected override void Up(MigrationBuilder m)=>m.Sql(Database.Resource("005"));
    protected override void Down(MigrationBuilder m)=>throw new NotSupportedException("Backup wiederherstellen.");
}
[DbContext(typeof(SchemaDb))][Migration("20261118210000_TopicsAndSessions")]
public sealed class TopicsMigration:Migration
{
    protected override void Up(MigrationBuilder m)=>m.Sql(Database.Resource("006"));
    protected override void Down(MigrationBuilder m)=>throw new NotSupportedException("Kein destruktiver Downgrade.");
}
[DbContext(typeof(SchemaDb))]
public sealed class Snapshot:ModelSnapshot
{protected override void BuildModel(ModelBuilder b)=>b.HasAnnotation("ProductVersion","10.0.14");}
