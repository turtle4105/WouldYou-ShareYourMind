using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// DB
using Microsoft.Data.Sqlite;
using System.IO;

namespace WouldYou_ShareMind.Services
{
    /// <summary>
    /// 순수 SQLite ADO.NET을 사용한 간단한 DB 서비스.
    /// - 각 호출마다 연결을 열고 닫아 동시성 문제를 최소화
    /// - WAL 모드를 사용해 읽기 동시성 향상
    /// </summary>
    public sealed class DbService : IDbService
    {
        private readonly string _dataDir;
        private readonly string _connStr;

        public DbService()
        {
            _dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(_dataDir);
            var dbPath = Path.Combine(_dataDir, "uju_mind.db");
            _connStr = $"Data Source={dbPath};Cache=Shared";
        }

        public void Init()
        {
            using var c = new SqliteConnection(_connStr);
            c.Open();

            // 성능/동시성 향상을 위한 기본 프라그마
            using (var pragma = c.CreateCommand())
            {
                pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
                pragma.ExecuteNonQuery();
            }

            // 스키마 보장
            using var cmd = c.CreateCommand();
            cmd.CommandText =
            @"
        CREATE TABLE IF NOT EXISTS mind_log (
          id         INTEGER PRIMARY KEY AUTOINCREMENT,
          content    TEXT NOT NULL,
          ai_reply   TEXT,
          is_let_go  INTEGER NOT NULL DEFAULT 0,           -- 0:아직, 1:흘려보냄
          created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS sleep_mode_log (
          id           INTEGER PRIMARY KEY AUTOINCREMENT,
          duration_min INTEGER NOT NULL,
          started_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
          ended_at     DATETIME
        );

        CREATE TABLE IF NOT EXISTS breathing_log (
          id            INTEGER PRIMARY KEY AUTOINCREMENT,
          started_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
          duration_sec  INTEGER NOT NULL,
          sample_rate   INTEGER NOT NULL,
          br_bpm        REAL,
          stability     REAL,
          badge         TEXT
        );
        ";
            cmd.ExecuteNonQuery();
        }

        public async Task<int> ExecAsync(string sql, params object[] args)
        {
            await using var c = new SqliteConnection(_connStr);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            BindParams(cmd, args);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> map, params object[] args)
        {
            var list = new List<T>();
            await using var c = new SqliteConnection(_connStr);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            BindParams(cmd, args);

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(map(r));
            return list;
        }

        public async Task<long> InsertMindLogAsync(string content, string? aiReply, int isLetGo = 0)
        {
            var sql = @"INSERT INTO mind_log(content, ai_reply, is_let_go)
                    VALUES(@p0, @p1, @p2);
                    SELECT last_insert_rowid();";
            await using var c = new SqliteConnection(_connStr);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@p0", content);
            cmd.Parameters.AddWithValue("@p1", (object?)aiReply ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p2", isLetGo);
            var id = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            return id;
        }

        public async Task<long> InsertSleepLogStartAsync(int durationMin)
        {
            var sql = @"INSERT INTO sleep_mode_log(duration_min)
                    VALUES(@p0);
                    SELECT last_insert_rowid();";
            await using var c = new SqliteConnection(_connStr);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@p0", durationMin);
            var id = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            return id;
        }

        public Task<int> UpdateSleepLogEndAsync(long id)
            => ExecAsync("UPDATE sleep_mode_log SET ended_at = CURRENT_TIMESTAMP WHERE id=@p0", id);

        public async Task<long> InsertBreathingLogAsync(int durationSec, int sampleRate, double? brBpm, double? stability, string? badge)
        {
            var sql = @"INSERT INTO breathing_log(duration_sec, sample_rate, br_bpm, stability, badge)
                    VALUES(@p0, @p1, @p2, @p3, @p4);
                    SELECT last_insert_rowid();";
            await using var c = new SqliteConnection(_connStr);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@p0", durationSec);
            cmd.Parameters.AddWithValue("@p1", sampleRate);
            cmd.Parameters.AddWithValue("@p2", (object?)brBpm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p3", (object?)stability ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p4", (object?)badge ?? DBNull.Value);
            var id = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            return id;
        }

        private static void BindParams(SqliteCommand cmd, object[] args)
        {
            for (int i = 0; i < args.Length; i++)
                cmd.Parameters.AddWithValue($"@p{i}", args[i] ?? DBNull.Value);
        }
    }
}
