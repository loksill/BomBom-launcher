// Taken from Musya

using System;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;

namespace SS14.Launcher.Models.Data.Migrations;

public sealed class Script0008_AddHwidsToLogin : Migrator.IMigrationScript
{
    public string Up(SqliteConnection connection)
    {
        var tables = connection.Query<string>("SELECT name FROM sqlite_master WHERE type = 'table'")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (tables.Contains("LoginHwid"))
            return "SELECT 1;";

        return """
            CREATE TABLE IF NOT EXISTS LoginHwid (
                UserId TEXT PRIMARY KEY NOT NULL,
                ModernHWId TEXT,
                LegacyHWId TEXT
            );
            """;
    }
}
