using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace WouldYou_ShareMind.ViewModels
{
    public class ArchiveItem
    {
        public string Title { get; set; } = "";   // ⬅ AI가 뽑아줄 '제목'
        public string Summary { get; set; } = ""; // 본문/요약
        public string DateText { get; set; } = "";
        public bool IsSelected { get; set; }
    }

    public partial class ArchiveViewModel : ObservableObject
    {
        private const int PageSize = 5; // 한 페이지에 5개씩

        public ObservableCollection<ArchiveItem> AllItems { get; } = new();
        public ObservableCollection<ArchiveItem> Items { get; } = new();

        [ObservableProperty] private int currentPage = 1;
        public ObservableCollection<int> PageNumbers { get; } = new();

        [ObservableProperty] private ArchiveItem? selectedItem;

        public ArchiveViewModel()
        {
            // --- 더미 10개 ---
            var dummy = new[]
            {
                "작은 실수 하나가 자꾸 마음에 남아요",
                "오늘은 내가 참 잘했다고 느껴졌어요",
                "새로운 시작이 설레면서도 두려워요",
                "걱정이 꼬리에 꼬리를 물어요",
                "좋아하는 사람과 오랜 시간 대화를 했어요",
                "아직 정리되지 않은 감정이 복잡해요",
                "나 자신을 좀 더 믿어주고 싶어요",
                "꿈에서 깬 듯한 허무함이 있어요",
                "소중한 순간이 다시 떠올라 기뻤어요",
                "내일은 더 잘할 수 있기를 바라요",
            };

            int day = 8;
            foreach (var (text, idx) in dummy.Select((t, i) => (t, i)))
            {
                AllItems.Add(new ArchiveItem
                {
                    Title = MakeTitle(text), // ⬅ 제목 생성 (간단 규칙/이후 LLM 대체)
                    Summary = text,
                    DateText = $"2025.08.{day - idx:D2} ({GetDayOfWeek(idx)}) {23 - idx:00}:{14 + idx:00}"
                });
            }

            RebuildPagination();
            LoadPage(CurrentPage);
        }

        // ===== Helpers =====
        private string GetDayOfWeek(int offset)
        {
            var date = new DateTime(2025, 8, 8).AddDays(-offset);
            return date.ToString("ddd", CultureInfo.GetCultureInfo("ko-KR"));
        }

        // 간단 제목 생성: 18자 cut + 말줄임 (Title이 비지 않도록 안전망)
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

            // 현재 페이지 항목 중 선택 유지(있다면)
            if (SelectedItem is not null && Items.Contains(SelectedItem) && SelectedItem.IsSelected)
            {
                // nothing
            }
            else
            {
                // 페이지 변경 시 선택 초기화
                foreach (var it in Items) it.IsSelected = false;
                SelectedItem = null;
            }
        }

        private void RebuildPagination()
        {
            PageNumbers.Clear();
            int totalPages = Math.Max(1, (int)Math.Ceiling(AllItems.Count / (double)PageSize));
            for (int i = 1; i <= totalPages; i++) PageNumbers.Add(i);
            if (CurrentPage > totalPages) CurrentPage = totalPages;
        }

        // SelectedItem 변경되면 IsSelected 동기화 (단일 선택 유지)
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
            System.Windows.MessageBox.Show(
                $"제목: {item.Title}\n날짜: {item.DateText}\n\n{item.Summary}",
                "상세");
        }

        [RelayCommand]
        private void ReleaseSelected()
        {
            // SelectedItem 우선, 없으면 IsSelected 항목 검색
            var selected = SelectedItem ?? AllItems.FirstOrDefault(x => x.IsSelected);
            if (selected == null)
            {
                System.Windows.MessageBox.Show("선택된 마음이 없어요.", "알림");
                return;
            }

            System.Windows.MessageBox.Show($"흘려보낸 마음: {selected.Title}", "완료");

            // 삭제 후 페이지/선택 갱신
            int indexBefore = AllItems.IndexOf(selected);
            AllItems.Remove(selected);

            RebuildPagination();

            // 삭제된 인덱스 기준으로 같은 페이지 유지가 자연스럽다
            int newPage = Math.Clamp((indexBefore / PageSize) + 1, 1, PageNumbers.Count);
            CurrentPage = newPage;
            LoadPage(CurrentPage);
        }
    }
}
