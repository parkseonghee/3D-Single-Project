# 3D-Single-Project
3D게임프로젝트 개발

> 군인을 키우고 편성해 뱀서라이크 전장에 투입하는 3D 군대 서바이버.
> 마을과 전장을 오가며 100일을 버텨라 — 어떤 부대를 데려가느냐가 곧 빌드다.

- **장르:** 3D 뱀서라이크 + 부대 편성 · 성장
- **엔진:** Unity (URP)
- **개발:** 1인 개발 · 포트폴리오용
- **문서:** [게임 기획서](Assets/Scripts/게임_기획서_최종.md) · [전체 스킬 정리](Docs/Skills.md)

---

## 📒 개발일지

### 2026-09-10 — 프로젝트 시작
- GitHub 저장소 생성 및 Unity 프로젝트 초기 푸쉬 (URP, Input System 설정)
- 게임 기획서(`게임_기획서_최종.md`) 작성 및 씬 레퍼런스 문서 추가
- 메인 에셋 **Toony Tiny RTS Set** 임포트

### 2026-09-11 — 몬스터 에셋 추가
- 몬스터 에셋(Big / Blob / Flying 등) 추가

### 2026-09-13 — 건물 시스템 구현
- 건물을 **ScriptableObject**(`BuildingDefinition`)로 정의하고 팔레트(`BuildingPalette`)에서 선택해 배치하는 `BuildingPlacementController` 구현
- 병력 모집(`RecruitmentController`), 유닛 정의(`UnitDefinition`), 마을 ↔ 전장 씬 이동(`SceneTravel`) 추가
- 기본 UI 추가

### 2026-09-14 — 자원 · 건물 관리 · 런 초안
- 벼 수확(`RiceHarvest`)으로 자원 획득, 자원 HUD(`ResourceHUD`) 추가
- 건물 관리 UI(`BuildingManagementUI`) — 업그레이드 버튼 기능
- Game Start 버튼 및 전장(런) 진행을 담당하는 `RunController` 초안 작성

### 2026-09-16 — 전투 시스템 기초
- 공격 / 적 데이터를 `AttackDefinition`, `EnemyDefinition`으로 분리
- 전투 처리 `CombatController` 추가 — 플레이어 공격 수정, 몬스터가 아군을 추적하도록 구현

### 2026-09-22 — 저장 기능 · 시작 메뉴
- 게임 저장/불러오기(`GameSave`, `BuildingSave`, `RecruitmentSave`) 구현
- 시작 메뉴(`StartMenu`), 폰트 및 UI 추가

### 2026-09-23 — 날짜 시스템
- 100일 진행을 위한 `DayController`, `DaySettings` 추가
- 플레이어 공격 로직 수정 진행

### 2026-09-24 — 경험치 · 체력 · 카메라
- **경험치 병**(`BattleExperience`) 추가 — 하단 병력 카드에서 병사 개별로 경험치 투자 (같은 클래스여도 레벨 · EXP 개별 관리)
- 전투 HUD(`BattleHUD`), 유닛 체력(`UnitHealth`) 추가
- 마을 카메라 드래그(`VillageCameraDrag`), 휠 줌(`CameraWheelZoom`) 구현
- UI 배치 수정 및 맵 디자인 변경

### 2026-09-28 — 애니메이션 · 몬스터 추가
- 전투 / 사망 애니메이션, 피격 혈흔 효과 적용
- 신규 몬스터 추가
- 궁수용 화살 3D 에셋 추가

### 2026-09-29 — 궁수 스킬 작업 시작
- 각종 에셋 추가 및 궁수 스킬 구현 착수

### 2026-10-02 — 궁수 스킬 완료
- 궁수 스킬 5종 완성: 관통 화살 · 멀티샷 · 집중 · 제압 사격 · 폭발 사격
- **Cartoon FX Remaster** 이펙트 에셋 도입, 병사 스킬 제작 시작

### 2026-10-04 — 전 병종 스킬 및 이펙트 완성
- `CombatController`를 partial 클래스로 분리해 스킬별 파일로 구현
- **병사:** 낙하 창, 피의 갈증, 회전검, 회전 베기, 전투 깃발
- **마법사:** 마법 함정·메테오(`MagicTrap`), 마나 순환, 광역 레이저(`Laser`), 연쇄 번개(`ChainLightning`)
- **방패병:** 팀 실드(`TeamShield`), 공격력 오라(`TeamAttack`), 위압의 함성(`Shout`), 철벽, 공격속도 오라(`TeamAttackSpeed`)
- **기마병:** 연쇄 돌격(`ChainCharge`), 화염 발굽(`FlameHooves`), 돌진 무적, 진군의 나팔(`MarchingHorn`), 추진
- LightningBolt 에셋 추가 (연쇄 번개용)

### 2026-10-05 — 스킬 이미지
- 스킬 카드에 사용할 스킬 아이콘 이미지 추가 (총 24개 스킬)

### 2026-10-08 — 집중 공격 · 카드 UI 애니메이션
- **집중 구역 명령**(`CombatController.FocusCommand`) — 클릭한 지점에 집결 구역을 지정해 병력을 집중 공격시키고, 우클릭으로 해제
- **DOTween** 도입, 카드 연출(`CardMotion`) 및 UI 기울기 효과(`UITiltController`) 구현

---

## 🛠 주요 스크립트 구조

```
Assets/Scripts
├── Army/          # 병력 · 전투 · 런 진행
│   ├── CombatController(.스킬명).cs   # 전투 및 병종별 스킬 (partial)
│   ├── RunController.cs              # 전장(런) 진행
│   ├── BattleExperience.cs           # 경험치 병 · 레벨업 카드
│   ├── DayController.cs              # 날짜 진행
│   └── ...
├── Building/      # 건물 배치 · 관리 · 자원 · 저장
├── GameSave.cs    # 저장 / 불러오기
└── StartMenu.cs   # 시작 메뉴
```
