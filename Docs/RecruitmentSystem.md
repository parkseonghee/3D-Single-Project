# 출전 준비와 병력 고용

## 사용 순서

1. VillageScene에서 병영 등 필요한 건물을 건설한다.
2. 오른쪽 아래 `준비`를 누르면 PlayScene이 열린다.
3. 오른쪽 목록에서 `고용`을 누르면 해당 병력이 지휘관 주변의 빈 자리에 등장한다.
4. `마을로 돌아가기`로 복귀한다. 같은 실행 중에는 건물·식량·병력이 유지된다.

시작 식량 320에서 병영 110을 건설하면 병사 60을 3명 고용할 수 있고 식량 30이 남는다.
현재 마을은 기존의 빈 건설 데모를 유지한다. 기획서의 시작 건물·병사 자동 지급은 아직 적용하지 않았다.

## 구성

| 파일 | 책임 |
|---|---|
| Assets/Scripts/Army/UnitDefinition.cs | 프리팹, 필요한 건물, 실제 식량 비용, 모델 보정 값 |
| Assets/Scripts/Army/SceneTravel.cs | 준비/마을 복귀, 두 씬의 표시 전환 |
| Assets/Scripts/Army/RecruitmentController.cs | 고용 조건 검사, 비용 차감 요청, 병력 생성, 버튼 상태 |
| Assets/Scripts/Building/BuildingPlacementController.cs | 기존 건설 상태·식량 관리. HasBuilding과 TrySpendRice를 추가 |

UnitDefinition은 Inspector에서 편집하는 공유 설정 에셋이다. 실행 중 병력 수나 식량을 이 에셋에 기록하지 않는다.
병종 이름으로 분기하지 않고 각 유닛 데이터의 requiredBuilding 참조로 해금을 판단한다.

준비 버튼 → SceneTravel.Prepare → PlayScene 추가 로드 → 마을 루트 비활성화 → 고용 화면 순서다.
마을은 메모리에 남고 PlayScene도 복귀할 때 비활성화만 하므로, 별도 저장 시스템 없이 왕복 중 상태를 보존한다.
Scene Travel 오브젝트는 전환 중에도 활성 상태여야 한다. Village Roots 목록에 자기 자신을 넣지 않는다.

고용 버튼 → TryHire → 건물·빈자리·식량 검사 → TrySpendRice → Spawn Points의 다음 자리 생성 → Appear 순서다.
등장 효과는 짧은 크기 변화다. 등장 도중 마을로 돌아가도 원래 크기로 복구한다.
동일 병종을 여러 번 고용할 수 있으며, 최초 5개의 Spawn Points가 초기 편성 상한을 결정한다. 지휘관은 이 수에 포함하지 않는다.

## 연결한 데이터

| 데이터 | 원본 에셋 | 필요한 건물 | 식량 |
|---|---|---|---:|
| Soldier | TT_HeavySwordman | 병영 / Workshop | 60 |
| Archer | TT_Archer | 사격장 / Archery | 110 |
| Shield | TT_Heavy_Infantry | 방패 훈련소 / TownHall | 150 |
| Mage | TT_Mage | 마법탑 / MageTower | 210 |
| Cavalry | TT_Mounted_Knight | 마구간 / Stables | 260 |

지휘관은 TT_King을 사용하고 PlayScene의 원점에 놓았다. 이동 로직은 없다.
고용 비용은 최신 기획서 및 참고 HTML의 임시값이다.
원본 프리팹은 수정하지 않았다. Assets/Prefabs/Army의 프로젝트용 프리팹에서 크기·바닥 높이·방향·Idle Animator를 설정했다.

## Inspector에서 바꾸는 위치

- 실제 비용과 해금 건물: Assets/ArmyData의 5개 UnitDefinition.
- 배치 자리와 최대 인원: PlayScene > Spawn Points, Recruitment System의 Spawn Points 배열.
- 등장 시간: Recruitment System > Appear Duration.
- 문구와 표시 가격: Recruitment UI > Hire Panel > Viewport > Cards 아래 TextMeshPro의 Text Input.
- 건물 미건설/식량 부족: 각 카드의 Building Required / Rice Required 문구.

고정 UI 문구는 Inspector 입력값을 유지한다. 준비 화면의 Rice Display > Amount만 실제 보유 벼를 표시한다.
실제 비용을 변경하면 카드의 표시 가격도 직접 수정해야 한다. 마을의 기존 정적 표시는 그대로이며, 준비 화면의 숫자는 진입·고용·재진입 시 갱신된다. PlayScene 단독 실행처럼 마을 자원 정보가 없으면 —로 표시한다.
버튼은 건물 미건설, 식량 부족, 편성 가득 참 상태에 따라 비활성화한다. 안내 문구는 오브젝트 활성 여부만 변경한다.

## 검증과 범위

2026-09-13 Unity Play 모드에서 버튼 콜백 및 실제 건설/고용 메서드를 호출해 확인했다.

- 준비/복귀 버튼, 건물 없는 5종 고용 거부.
- 시작 자원 320으로 병영 건설 후 병사 3명 고용, 잔액 30, 추가 고용 거부.
- 임시 테스트 자원을 사용해 5종 건물 해금 및 5종 고용, 총 비용 790 차감.
- 여섯 번째 고용 거부 및 실패 시 식량 유지.
- 씬 왕복 후 건물·병력 수·잔액 유지, 등장 중 복귀 후 크기 정상화.
- 지휘관 원점과 서로 다른 5개 배치 자리, 모델 바닥 높이 0.
- 목록 스크롤, 실제 화면 캡처, 콘솔 오류·경고 없음.

물리 마우스의 종단 간 조작 검증은 아니다. 테스트용 자원과 배치는 실행 종료로 제거했다.
PlayScene을 직접 실행하면 지휘관과 잠긴 목록이 나오며, 뒤로가기로 VillageScene에 진입할 수 있다.
앱 종료 후 디스크 저장, 전투/이동, 강화, 해고, 막사에 따른 편성 상한 확장은 아직 구현하지 않았다.

## 준비 화면 카메라와 보유 벼 (2026-09-13)
Play Camera는 Perspective, Field of View 40으로 변경했다. 시점을 낮추고 지휘관 근처로 당겨 얼굴과 장비가 크게 보이도록 했다. 유닛 크기와 고용 위치는 유지했다.
RecruitmentController의 Rice Amount에 숫자용 TMP를 연결한다. ‘벼’ 라벨은 별도 TMP이므로 직접 편집할 수 있다.
현재 Inspector 시작 자원 999999를 유지한 상태에서 병영 건설 후 999889, 병사 5명 고용 후 999589 표시 및 왕복 후 유지, 전체 6명 화면 표시와 콘솔 오류 없음 확인.

## 2026-09-14 갱신: 시작 건물
VillageScene > Buildings에 Starting Farm, Starting Workshop(병영), Starting Castle(막사)을 각 1개씩 미리 배치했다. Building System > Starting Buildings에서 정의와 씬 오브젝트를 연결한다. 위치는 각 오브젝트 Transform에서 직접 변경 가능하다. 비용 차감 없이 시작하며 병사 고용이 즉시 해금된다. 시작 건물은 추가 구매 횟수에 포함하지 않아 첫 추가 Farm 비용 120을 유지한다. 겹침 방지 콜라이더도 적용했다. 씬 왕복 시 중복 생성 없음과 고용 해금을 검증했다. 이전 문서의 '빈 마을/시작 건물 미구현' 설명은 이 내용으로 대체한다. 시작 병사 자동 지급은 여전히 미구현이다.

## 2026-09-14: 자원 UI와 환전
VillageScene과 PlayScene의 Resource Bar에 벼/골드/특수 자원과 환전 버튼을 추가했다. ResourceHUD는 숫자만 갱신하며 문구는 TMP Inspector에서 편집한다. 기존 마을 정적 Rice 표시와 준비 화면 전용 숫자 갱신은 공통 HUD로 대체했다. BuildingPlacementController가 현재 세 자원을 관리하며 초기 골드/특수 자원은 0, 기존 시작 벼 설정은 유지한다. TryExchangeRice는 벼100을 골드40으로 원자적으로 환전한다. 환전 비율은 Inspector Exchange Rice Cost/Exchange Gold Gain으로 조절하며 버튼 문구도 수동 변경해야 한다. 부족한 벼나 정수 초과 시 거래하지 않는다. 환전 후 고용/건설 버튼도 잔액에 따라 갱신한다. 양쪽 씬 환전·잔액 부족·골드 유지·고용 비활성화를 검증했다.
특수 자원은 표시와 초기값 연결까지 구현했다. 보스 보상/영구 강화/새 회차 초기화/디스크 영구 저장은 아직 미구현이며, 현 상태는 동일 실행 중 씬 왕복에서만 보존된다.
