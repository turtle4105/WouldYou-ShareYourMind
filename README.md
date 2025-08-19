<p align="center">
  <!-- 로고 자리: 레포에 추가 후 경로만 바꾸세요 -->
  <img src="Resources/images/logo.png" alt="우주÷마음 로고" width="250">
</p>

# Would you share your mind?
**불안한 밤, 마음을 조용히 털어놓고 — 공감 응답과 우주 사운드로 편안한 잠을 돕는 로컬 WPF 앱 (MVP)**

---

## <img src="Resources/images/Home_SM.png" width="40" height="40" alt=""> 소개
WouldYou ÷ ShareMind(우주÷마음)는 **WPF + MVVM (.NET 6)** 기반의 로컬 데스크톱 앱입니다. 사용자가 글로 마음을 나누면 **AI가 공감 메시지**를 반환하고, **우주 테마의 백색소리**와 **호흡 휴리스틱(마이크 RMS 기반)**으로 **안정감 있는 수면 진입**을 돕습니다. 모든 기록은 **SQLite**에 로컬 저장됩니다.

> ※ 본 앱은 의료기기가 아니며, 의료적 진단/치료를 제공하지 않습니다.

---

## <img src="Resources/images/Home_sleep.png" width="40" height="40" alt=""> 핵심 기능
- **마음 나누기**: 입력 즉시 로컬 DB 저장 → OpenAI 호출 → 공감/인정 응답을 같은 행에 업데이트 → 팝업 표시  
- **수면 유도 모드**: 우주 사운드 재생(페이드 인/아웃) + 마이크 캡처(16kHz mono) → 호흡수(BPM)·변동성(CV) 기반 **Awake/Drowsy/Asleep** 상태 추정 및 10초 간격 로그  
- **보관함(아카이브)**: 최근/전체 기록 열람, **흘려보내기(삭제 아님, 상태 플래그)**  
- **홈**: 최근 마음 기록 카드 목록(날짜/요약)  
- **완전 로컬 우선**: 서버 없이 동작, AI 응답만 선택적으로 네트워크 사용


---

## <img src="Resources/images/Home_Release.png" width="40" height="40" alt=""> 프로젝트 구조



```markdown

WouldYou-ShareMind/
├─ src/
│  ├─ Views/                 # HomeView, ShareMindView, ArchiveView, SleepModeView, SettingView
│  ├─ ViewModels/            # MainViewModel 등 (MVVM)
│  ├─ Models/                # DTO/Record
│  ├─ Services/              # EmotionService, DbService, AudioService, BreathingDetector, SettingsService
│  ├─ Resources/
│  │  ├─ images/             # UI 이미지/아이콘
│  │  └─ audio/              # interstellar\_plasma.mp3 등 (Build Action: Content / Copy to Output: if newer)
│  ├─ App.xaml, App.xaml.cs  # DI/스타일/리소스
│  └─ MainWindow\.xaml\*       # 셸/네비게이션
├─ Data/                     # (선택) 개발용 로컬 DB 위치
├─ docs/
│  ├─ assets/                # README용 로고/아이콘 (logo.png, icons/\*.svg)
│  └─ screens/               # 스크린샷
└─ README.md


```
