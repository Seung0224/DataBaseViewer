# DataBaseViewr_PROJECT

SQLite 기반 WinForms 응용 프로그램으로, 날짜별로 저장된 데이터베이스를 기반으로
**그래프 또는 표 형태로 시각화 및 분석**할 수 있는 툴입니다.

---

<img width="1262" height="773" alt="2" src="https://github.com/user-attachments/assets/4938a7cb-b2b3-4d1b-a422-8f413f2f0d37" />

## 📦 프로젝트 개요

- **플랫폼:** Windows Forms (.NET Framework 4.8.1)
- **목적:** 도킹 기반 UI에서 날짜 기반 SQLite 데이터를 자유롭게 시각화 (그래프/표)
- **데이터 소스:** `D:\TEST_MODEL\YYYYMMDD\History.db`

---

## ✅ 주요 기능

### 1. 📌 도킹(Docking) 인터페이스
- DockPanel Suite를 활용한 **유연한 UI 배치**
- 종료 시 레이아웃 자동 저장 및 복원

### 2. 🧮 데이터 조회 및 시각화
- 테이블을 선택하여 **그리드 뷰** 또는 **차트 뷰** 생성 가능
- AlignInfos, ProductInfos 등 특정 테이블에 **전용 차트** 제공

### 3. ✍️ 데이터 편집 기능 (그리드 뷰)
- 열 이름 바꾸기, 열 삭제, 열 추가 기능
- 쉼표 구분 데이터 자동 분해: `NUM_0`, `NUM_1`, ... 열 생성

### 4. 🔄 AutoDate 모드
- 체크 시, 단일 날짜인 경우 항상 **오늘 날짜로 자동 갱신**
- DockWindow에 AutoDate 설정 상태 저장

### 5. 📅 날짜 범위 선택
- 캘린더에서 날짜를 드래그하여 기간 설정 시,
  - 해당 기간 내 모든 DB에서 데이터를 병합하여 출력

### 6. 📓 메모 도킹 창
- 각 항목에 대한 **개별 노트 작성** 가능
- 노트는 파일로 저장되어 앱 재시작 후에도 유지

### 7. 📈 전용 차트 뷰
- **AlignInfos:** X, Y, T 값 기반 정적 선형 그래프
- **ProductInfos:** 날짜별 OK/NG 카운트 기반 막대 그래프

### 8. 🌲 TreeView 미리보기
- 모든 SQLite 테이블 자동 탐색
- 최대 10개 샘플 행 미리보기 포함 (ObjectListView 기반)

---

## 🧰 사용 방법

1. `D:\TEST_MODEL\` 경로에 폴더 생성
2. 하위에 날짜 폴더 생성: 예) `D:\TEST_MODEL\20250806\`
3. SQLite DB 파일(`History.db`)을 해당 날짜 폴더에 넣음
4. 프로그램 실행 후 좌측 트리뷰에서 항목 우클릭하여 기능 사용

---

## 🔧 개발 환경 및 라이브러리

| 구성 요소 | 내용 |
|------------|------|
| 언어 | C# (.NET Framework 4.8.1) |
| UI 프레임워크 | WinForms |
| DB 라이브러리 | `Microsoft.Data.Sqlite` |
| 도킹 시스템 | `WeifenLuo.WinFormsUI.Docking` |
| 데이터 그리드 | `Zuby.ADGV (AdvancedDataGridView)` |
| 트리뷰 구성 | `BrightIdeasSoftware.ObjectListView` |
| 차트 라이브러리 | `LiveCharts.WinForms` |

---

## 📁 예제 데이터

- 기본 구조:
```
D:\TEST_MODEL\20250806\History.db
```
- 내부 테이블 예시:
  - AlignInfos (AlignX, AlignY, AlignT)
  - ProductInfos (Judge, MaterialInputTime, ProcessingTimeMs)

---
## 👨‍💻 개발자

- **Seung0224**  
- GitHub: [https://github.com/Seung0224](https://github.com/Seung0224)
