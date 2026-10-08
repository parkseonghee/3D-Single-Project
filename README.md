<div align="center">

# ⚔️ 3D-Single-Project

**군인을 키우고 편성해 뱀서라이크 전장에 투입하는 3D 군대 서바이버**

*마을과 전장을 오가며 100일을 버텨라 — 어떤 부대를 데려가느냐가 곧 빌드다.*

<br>

![Unity](https://img.shields.io/badge/Unity-6000.3-000000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![URP](https://img.shields.io/badge/URP-Universal_RP-4B32C3?style=for-the-badge)
![DOTween](https://img.shields.io/badge/DOTween-UI_Motion-6DB33F?style=for-the-badge)

![Status](https://img.shields.io/badge/status-개발_중-orange?style=flat-square)
![Period](https://img.shields.io/badge/기간-2026.09.10_~-blue?style=flat-square)
![Skills](https://img.shields.io/badge/구현_스킬-24개-red?style=flat-square)

[📄 게임 기획서](Assets/Scripts/게임_기획서_최종.md) · [🗡️ 전체 스킬 정리](Docs/Skills.md)

</div>

<br>

## 📌 목차

1. [진행 현황](#-진행-현황)
2. [개발 타임라인](#-개발-타임라인)
3. [개발일지](#-개발일지)
4. [병종별 스킬](#-병종별-스킬)
5. [스크립트 구조](#-스크립트-구조)

<br>

## 📊 진행 현황

| 영역 | 내용 | 상태 |
|:---:|---|:---:|
| 🏘️ **마을** | 건물 배치 · 업그레이드 · 벼 수확 · 병력 모집 | ✅ |
| 💾 **저장** | 건물 · 병력 · 게임 진행 저장 / 불러오기 | ✅ |
| 📅 **날짜** | 100일 진행 시스템 | ✅ |
| ⚔️ **전투** | 자동 전투 · 몬스터 추적 · 애니메이션 · 피격 효과 | ✅ |
| 📈 **성장** | 경험치 병 투자 · 병사 개별 레벨업 · 스킬 카드 | ✅ |
| ✨ **스킬** | 5개 병종, 총 24개 스킬 및 이펙트 | ✅ |
| 🎯 **지휘** | 집중 구역 명령 | ✅ |
| 🃏 **UI 연출** | 카드 애니메이션 · UI 기울기 효과 | ✅ |
| ⚖️ **밸런스** | 수치 최종 조정 | ⏳ |

<br>

## 🗓 개발 타임라인

| 주차 | 기간 | 핵심 작업 |
|:---:|:---:|---|
| **1주차** | 09.10 ~ 09.14 | 프로젝트 세팅 · 건물 시스템 · 자원 · 런 초안 |
| **2주차** | 09.16 ~ 09.24 | 전투 기초 · 저장 · 날짜 · 경험치 · 카메라 |
| **3주차** | 09.28 ~ 10.05 | 애니메이션 · 몬스터 · 전 병종 스킬 24종 · 이펙트 |
| **4주차** | 10.08 ~ | 집중 공격 명령 · 카드 UI 애니메이션 |

<br>

## 📒 개발일지

### 🔹 1주차 — 기반 다지기

#### `09.10` 프로젝트 시작
> `세팅` `기획`

- GitHub 저장소 생성 및 Unity 프로젝트 초기 푸쉬 (URP, Input System)
- 게임 기획서 작성 및 씬 레퍼런스 문서 추가
- 메인 에셋 **Toony Tiny RTS Set** 임포트

#### `09.11` 몬스터 에셋 추가
> `에셋`

- 몬스터 에셋(Big / Blob / Flying 등) 추가

#### `09.13` 건물 시스템 구현
> `건물` `UI`

- 건물을 **ScriptableObject**(`BuildingDefinition`)로 정의
- 팔레트(`BuildingPalette`)에서 선택해 배치하는 `BuildingPlacementController` 구현
- 병력 모집(`RecruitmentController`), 유닛 정의(`UnitDefinition`), 마을 ↔ 전장 이동(`SceneTravel`) 추가

#### `09.14` 자원 · 건물 관리 · 런 초안
> `자원` `UI` `런`

- 벼 수확(`RiceHarvest`) 및 자원 HUD(`ResourceHUD`) 추가
- 건물 관리 UI(`BuildingManagementUI`) — 업그레이드 버튼 기능
- Game Start 버튼 및 전장 진행 `RunController` 초안 작성

<br>

### 🔹 2주차 — 전투와 시스템

#### `09.16` 전투 시스템 기초
> `전투` `AI`

- 공격 / 적 데이터를 `AttackDefinition`, `EnemyDefinition`으로 분리
- `CombatController` 추가 — 플레이어 공격 수정, 몬스터 추적 구현

#### `09.22` 저장 기능 · 시작 메뉴
> `저장` `UI`

- 저장 / 불러오기 구현 (`GameSave`, `BuildingSave`, `RecruitmentSave`)
- 시작 메뉴(`StartMenu`), 폰트 및 UI 추가

#### `09.23` 날짜 시스템
> `시스템` `전투`

- 100일 진행을 위한 `DayController`, `DaySettings` 추가
- 플레이어 공격 로직 수정

#### `09.24` 경험치 · 체력 · 카메라
> `성장` `UI` `카메라` `맵`

- **경험치 병**(`BattleExperience`) — 하단 병력 카드에서 병사 개별로 경험치 투자
  - 같은 클래스여도 레벨 · EXP를 개별 관리
- 전투 HUD(`BattleHUD`), 유닛 체력(`UnitHealth`) 추가
- 마을 카메라 드래그(`VillageCameraDrag`), 휠 줌(`CameraWheelZoom`)
- UI 배치 수정 및 맵 디자인 변경

<br>

### 🔹 3주차 — 스킬과 이펙트

#### `09.28` 애니메이션 · 몬스터 추가
> `애니메이션` `몬스터` `에셋`

- 전투 / 사망 애니메이션, 피격 혈흔 효과 적용
- 신규 몬스터 추가
- 궁수용 화살 3D 에셋 추가

#### `09.29` 궁수 스킬 작업 시작
> `스킬` `에셋`

- 각종 에셋 추가 및 궁수 스킬 구현 착수

#### `10.02` 궁수 스킬 완료
> `스킬` `이펙트`

- 궁수 스킬 5종 완성
- **Cartoon FX Remaster** 이펙트 에셋 도입, 병사 스킬 제작 시작

#### `10.04` 전 병종 스킬 및 이펙트 완성
> `스킬` `이펙트` `리팩터링`

- `CombatController`를 **partial 클래스**로 분리해 스킬별 파일로 구현
- 병사 · 마법사 · 방패병 · 기마병 스킬 전부 구현 → [병종별 스킬](#-병종별-스킬)
- LightningBolt 에셋 추가 (연쇄 번개용)

#### `10.05` 스킬 이미지
> `UI` `에셋`

- 스킬 카드에 사용할 스킬 아이콘 이미지 추가 (총 24개)

<br>

### 🔹 4주차 — 지휘와 연출

#### `10.08` 집중 공격 · 카드 UI 애니메이션
> `지휘` `UI` `연출`

- **집중 구역 명령**(`CombatController.FocusCommand`)
  - 클릭한 지점에 집결 구역을 지정해 병력이 집중 공격
  - 우클릭으로 명령 해제
- **DOTween** 도입 — 카드 연출(`CardMotion`), UI 기울기 효과(`UITiltController`)

<br>

## 🗡 병종별 스킬

> 레벨업 시 해당 병종의 스킬 카드 중 최대 3개가 무작위로 제시된다. 상세 수치는 [Skills.md](Docs/Skills.md) 참고.

| 병종 | 기본 공격 | 스킬 |
|:---:|---|---|
| 🗡️ **병사** | 전방 160° 베기 | 낙하 창 · 피의 갈증 · 회전검 · 회전 베기 · 전투 깃발 |
| 🏹 **궁수** | 직진 화살 | 관통 화살 · 멀티샷 · 집중 · 제압 사격 · 폭발 사격 |
| 🔮 **마법사** | 마력탄 | 마법 함정·메테오 · 마나 순환 · 광역 레이저 · 연쇄 번개 |
| 🛡️ **방패병** | 범위 베기 | 팀 실드 · 공격력 오라 · 위압의 함성 · 철벽 · 공격속도 오라 |
| 🐎 **기마병** | 돌진 찌르기 | 연쇄 돌격 · 화염 발굽 · 돌진 무적 · 진군의 나팔 · 추진 |

<br>

## 🛠 스크립트 구조

```
Assets/Scripts
├── Army/                          # 병력 · 전투 · 런 진행
│   ├── CombatController.cs        # 전투 처리 (partial)
│   ├── CombatController.*.cs      #   └ 스킬별 분리 파일
│   ├── RunController.cs           # 전장(런) 진행
│   ├── BattleExperience.cs        # 경험치 병 · 레벨업 카드
│   ├── CardMotion.cs              # 카드 애니메이션
│   ├── DayController.cs           # 날짜 진행
│   └── ...
├── Building/                      # 건물 배치 · 관리 · 자원 · 저장
├── GameSave.cs                    # 저장 / 불러오기
├── StartMenu.cs                   # 시작 메뉴
└── UITiltController.cs            # UI 기울기 효과
```

<br>

<div align="center">

<sub>Last updated · 2026.10.08</sub>

</div>
