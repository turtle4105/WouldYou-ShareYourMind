using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WouldYou_ShareMind.Services;

namespace WouldYou_ShareMind.ViewModels
{
    public class ArchiveItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";     // 제목(본문에서 잘라 만든 요약 제목)
        public string Summary { get; set; } = "";   // 본문 전체 또는 요약
        public string DateText { get; set; } = "";  // yyyy.MM.dd (ddd) HH:mm
        public string? AiReply { get; set; }        // AI 응답(있다면)
        public bool IsSelected { get; set; }
    }

    public partial class ArchiveViewModel : ObservableObject
    {
        private const int PageSize = 5; // 한 페이지 5개
        private readonly IDbService _db;

        public ObservableCollection<ArchiveItem> AllItems { get; } = new();
        public ObservableCollection<ArchiveItem> Items { get; } = new();

        [ObservableProperty] private int currentPage = 1;
        public ObservableCollection<int> PageNumbers { get; } = new();

        [ObservableProperty] private ArchiveItem? selectedItem;

        public ArchiveViewModel(IDbService db)
        {
            _db = db;
            // 시작 시 DB에서 로드
            _ = LoadFromDbAsync();
        }

        // ===== 데이터 로드 =====
        private async Task LoadFromDbAsync()
        {
            AllItems.Clear();

            // 필요하면 limit 조정 (예: 200개까지)
            var rows = await _db.GetRecentMindAsync(limit: 200);

            var ko = CultureInfo.GetCultureInfo("ko-KR");
            foreach (var r in rows)
            {
                var created = r.CreatedAt;
                var title = MakeTitle(r.Content);

                AllItems.Add(new ArchiveItem
                {
                    Id = r.Id,
                    Title = title,
                    Summary = r.Content,
                    AiReply = r.AiReply,
                    DateText = created == DateTime.MinValue
                        ? ""
                        : created.ToString("yyyy.MM.dd (ddd) HH:mm", ko)
                });
            }

            RebuildPagination();
            LoadPage(CurrentPage);
        }

        // ===== Helpers =====
        private string MakeTitle(string? body)
        {
            var t = (body ?? "").Trim();
            if (string.IsNullOrEmpty(t)) return "(제목 없음)";
            return t.Length <= 28 ? t : t[..28] + "…";
        }

        private void LoadPage(int page)
        {
            Items.Clear();
            foreach (var item in AllItems.Skip((page - 1) * PageSize).Take(PageSize))
                Items.Add(item);

            // 페이지 변경 시 선택 상태 초기화
            foreach (var it in Items) it.IsSelected = false;
            SelectedItem = null;
        }

        private void RebuildPagination()
        {
            PageNumbers.Clear();
            int totalPages = Math.Max(1, (int)Math.Ceiling(AllItems.Count / (double)PageSize));
            for (int i = 1; i <= totalPages; i++) PageNumbers.Add(i);
            if (CurrentPage > totalPages) CurrentPage = totalPages;
        }

        // 단일 선택 유지
        partial void OnSelectedItemChanged(ArchiveItem? value)
        {
            foreach (var it in AllItems) it.IsSelected = false;
            if (value != null) value.IsSelected = true;
        }

        // ===== Commands =====
        [RelayCommand]
        private void PrevPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadPage(CurrentPage);
            }
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < PageNumbers.Count)
            {
                CurrentPage++;
                LoadPage(CurrentPage);
            }
        }

        [RelayCommand]
        private void GoToPage(int page)
        {
            if (page < 1 || page > PageNumbers.Count) return;
            CurrentPage = page;
            LoadPage(CurrentPage);
        }

        [RelayCommand]
        private void OpenDetail(ArchiveItem item)
        {
            // 필요 시 상세 팝업을 View로 분리 가능. 우선 간단히 메시지박스.
            var ai = string.IsNullOrWhiteSpace(item.AiReply) ? "(AI 응답 없음)" : item.AiReply;
            System.Windows.MessageBox.Show(
                $"제목: {item.Title}\n날짜: {item.DateText}\n\n[본문]\n{item.Summary}\n\n[AI]\n{ai}",
                "상세");
        }

        // DB에서도 is_let_go=1 업데이트하고 목록에서 제거
        [RelayCommand]
        private async Task ReleaseSelected()
        {
            var selected = SelectedItem ?? AllItems.FirstOrDefault(x => x.IsSelected);
            if (selected == null)
            {
                System.Windows.MessageBox.Show("선택된 마음이 없어요.", "알림");
                return;
            }

            // DB 업데이트
            await _db.ExecAsync("UPDATE mind_log SET is_let_go = 1 WHERE id = @p0;", selected.Id);

            // UI에서 제거
            int indexBefore = AllItems.IndexOf(selected);
            AllItems.Remove(selected);

            RebuildPagination();
            int newPage = Math.Clamp((indexBefore / PageSize) + 1, 1, PageNumbers.Count);
            CurrentPage = newPage;
            LoadPage(CurrentPage);

            System.Windows.MessageBox.Show($"흘려보낸 마음: {selected.Title}", "완료");
        }
    }
}
