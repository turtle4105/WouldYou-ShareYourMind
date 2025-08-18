using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.Services
{
    /// <summary>
    /// SQLite 접근을 추상화한 인터페이스.
    /// - 앱 시작 시 Init()로 테이블을 보장
    /// - 공통 SQL 실행(Exec/Query)
    /// - MVP 편의 메서드 포함
    /// </summary>
    public interface IDbService
    {
        //앱 시작 시 1회 호출: DB 파일/테이블 생성
        void Init();

        //INSERT/UPDATE/DELETE 등 영향행 수 반환
        Task<int> ExecAsync(string sql, params object[] args);

        //SELECT 전용. 데이터리더를 map 함수로 변환
        Task<List<T>> QueryAsync<T>(string sql, Func<Microsoft.Data.Sqlite.SqliteDataReader, T> map, params object[] args);

        // ===== 편의 메서드(MVP) =====
        Task<int> InsertMindAsync(string content, string? aiReply, bool isLetGo = false);
        Task<int> UpdateMindAiReplyAsync(int id, string aiReply);  

        Task<long> InsertSleepLogStartAsync(int durationMin);
        Task<int> UpdateSleepLogEndAsync(long id);
        Task<long> InsertBreathingLogAsync(int durationSec, int sampleRate, double? brBpm, double? stability, string? badge);

        Task<IReadOnlyList<MindLogPreviewDto>> GetRecentMindAsync(int limit);
    }
}
