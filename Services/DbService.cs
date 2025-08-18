using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;


namespace WouldYou_ShareMind.Services
{
    public sealed class DbService : IDbService
    {
        private readonly string _dbPath;
        private readonly string _connStr;

        public DbService()
        {
            Console.WriteLine("[DB] DbService ctor");

            var baseDir = AppDomain.CurrentDomain.BaseDirectory; 
            var dataDir = Path.Combine(baseDir, "Data");
            Directory.CreateDirectory(dataDir);
            _dbPath = Path.Combine(dataDir, "app.db");
            _connStr = $"Data Source={_dbPath};Cache=Shared";
            Console.WriteLine($"[DB] Using file: {_dbPath}");

            System.Diagnostics.Debug.WriteLine($"[DB] Using file: {_dbPath}");
        }

        // 동기 시그니처란 무언인가?
        public void Init()  // 인터페이스에 동기로 정의되었을 때를 대비
        {
            // 기존 InitAsync 재사용
            InitAsync().GetAwaiter().GetResult();
        }


        public async Task InitAsync()
        {
            Console.WriteLine("[DB] InitAsync start");

            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            // 테이블 생성 SQL (이미 적용되어 있으면 그대로 둠)
            var sql = @"
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;

            CREATE TABLE IF NOT EXISTS mind_log (
              id         INTEGER PRIMARY KEY AUTOINCREMENT,
              content    TEXT    NOT NULL,
              ai_reply   TEXT    NULL,
              is_let_go  INTEGER NOT NULL DEFAULT 0,
              created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
";
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();

            Console.WriteLine("[DB] InitAsync done");
        }

        public async Task<int> InsertMindAsync(string content, string? aiReply, bool isLetGo = false)
        {
            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            using var tx = await conn.BeginTransactionAsync();
            var sql = @"INSERT INTO mind_log(content, ai_reply, is_let_go)
                        VALUES($content, $ai_reply, $is_let_go);
                        SELECT last_insert_rowid();";
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = (SqliteTransaction)tx;

            cmd.Parameters.AddWithValue("$content", content);
            cmd.Parameters.AddWithValue("$ai_reply", (object?)aiReply ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$is_let_go", isLetGo ? 1 : 0);

            var id = (long)await cmd.ExecuteScalarAsync();
            await tx.CommitAsync();
            return (int)id;
        }

        public async Task<int> UpdateMindAiReplyAsync(int id, string aiReply)
        {
            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"UPDATE mind_log SET ai_reply = @p0 WHERE id = @p1;";
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@p0", aiReply);
            cmd.Parameters.AddWithValue("@p1", id);

            return await cmd.ExecuteNonQueryAsync();
        }


        public async Task<IReadOnlyList<MindLogPreviewDto>> GetRecentMindAsync(int limit)
        {
            var list = new List<MindLogPreviewDto>();

            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"SELECT id, content, ai_reply, created_at
                        FROM mind_log
                        WHERE is_let_go = 0
                        ORDER BY datetime(created_at) DESC
                        LIMIT $limit;";
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$limit", limit);

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                // created_at이 TEXT로 저장되어도 GetDateTime이 처리 가능.
                // 혹시 포맷 문제 발생 시 DateTime.Parse로 보완.
                var created = r.IsDBNull(3) ? DateTime.MinValue : r.GetDateTime(3);

                list.Add(new MindLogPreviewDto
                {
                    Id = r.GetInt32(0),
                    Content = r.IsDBNull(1) ? "" : r.GetString(1),
                    AiReply = r.IsDBNull(2) ? null : r.GetString(2),
                    CreatedAt = created
                });
            }
            return list;
        }

        // 수면로그 Start
        public async Task<long> InsertSleepLogStartAsync(int durationMin)
        {
            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
INSERT INTO sleep_mode_log (duration_min, started_at, ended_at)
VALUES (@p0, datetime('now'), datetime('now'));
SELECT last_insert_rowid();";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@p0", durationMin);

            var id = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            return id;
        }

        // 수면로그 End
        public async Task<int> UpdateSleepLogEndAsync(long id)
        {
            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            // ended_at을 지금으로 갱신하고, 실제 duration_min 재계산
            var sql = @"
UPDATE sleep_mode_log
SET ended_at = datetime('now'),
    duration_min = CAST((julianday(datetime('now')) - julianday(started_at)) * 24 * 60 AS INTEGER)
WHERE id = @p0;";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@p0", id);

            return await cmd.ExecuteNonQueryAsync();
        }


        // 인터페이스 시그니처를 만족시키는 오버로드 (추가)
        public Task<long> InsertBreathingLogAsync(
            int line,                 // 인터페이스 첫 번째 int (현재 스키마엔 사용처 없음)
            int sleepPossible,        // 0/1
            double? breathRate,
            double? variability,
            string? measuredAt = null)
        {
            // 기존 구현(4-파라미터)으로 위임
            return InsertBreathingLogAsync(sleepPossible, breathRate, variability, measuredAt);
        }


        // 호흡 로그 Insert
        public async Task<long> InsertBreathingLogAsync(int sleepPossible, double? breathRate, double? variability, string? measuredAt = null)
        {
            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
INSERT INTO breathing_log (breath_rate, variability, sleep_possible, measured_at)
VALUES (@p0, @p1, @p2, COALESCE(@p3, CURRENT_TIMESTAMP));
SELECT last_insert_rowid();";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@p0", (object?)breathRate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p1", (object?)variability ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@p2", sleepPossible);
            cmd.Parameters.AddWithValue("@p3", (object?)measuredAt ?? DBNull.Value);

            var id = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            return id;
        }


        // 도우미 → SQL에는 @p0, @p1… 형태로 파라미터를 써줘
        public async Task<int> ExecAsync(string sql, params object[] args)
        {
            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            for (int i = 0; i < args.Length; i++)
                cmd.Parameters.AddWithValue($"@p{i}", args[i] ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> map, params object[] args)
        {
            var list = new List<T>();
            using var conn = new SqliteConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            for (int i = 0; i < args.Length; i++)
                cmd.Parameters.AddWithValue($"@p{i}", args[i] ?? DBNull.Value);

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(map(r));

            return list;
        }

    }
}
