using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Dapper;

namespace ProcureMax.Core;

// SQLite connection factory + simple file-based SQL migrator.
// Each .sql in Core/Migrations is named NN_description.sql and applied in order.
// Track applied scripts in __migrations table. Connection string comes from config.
public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    public SqliteConnectionFactory(string connectionString) => _connectionString = connectionString;

    public IDbConnection Create()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        // Enforce FKs per connection (off by default in SQLite).
        conn.Execute("PRAGMA foreign_keys = ON;");
        return conn;
    }
}

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDbConnectionFactory factory, ILogger<DatabaseInitializer> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public void Initialize()
    {
        using var conn = _factory.Create();
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS __migrations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                script_name TEXT NOT NULL UNIQUE,
                applied_at TEXT NOT NULL
            );
            """);

        var applied = conn.Query<string>("SELECT script_name FROM __migrations;").ToHashSet();
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Migrations.") && n.EndsWith(".sql"))
            .OrderBy(n => n)
            .ToList();

        if (resourceNames.Count == 0)
        {
            _logger.LogWarning("No embedded migration resources found. Make sure Core/Migrations/*.sql have Build Action=EmbeddedResource.");
            return;
        }

        foreach (var name in resourceNames)
        {
            // We just use the resource name's tail as the canonical script name.
            if (applied.Contains(name)) continue;
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            _logger.LogInformation("Applying migration {Name}", name);
            conn.Execute(sql);
            conn.Execute("INSERT INTO __migrations (script_name, applied_at) VALUES (@n, @now);",
                new { n = name, now = DateTime.UtcNow.ToString("O") });
        }
    }
}
