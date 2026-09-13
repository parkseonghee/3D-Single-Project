# 건설 시스템 — 세 파일로 읽기

## 먼저 실행하기

`Assets/Scenes/VillageScene.unity`를 열고 Play를 누른다.
왼쪽 아래 건설하기 → 가로 목록에서 건물 선택 → 땅 위로 마우스 이동 → 좌클릭으로 건설한다.
목록은 휠, 드래그, 하단 스크롤바로 좌우 이동한다. 선택하면 목록이 닫히고 배치가 시작된다.
닫기 버튼 또는 건설하기 버튼을 다시 누르면 목록만 닫는다. 건설하기를 누르면 기존 배치 선택은 취소된다.
초록은 가능, 빨강은 불가다. 우클릭이나 Esc로 취소한다.
한 채를 지으면 선택이 끝나며, 다음 건설은 버튼을 다시 누른다.

## 읽는 순서

1. `BuildingDefinition.cs`: 어떤 건물인지 정하는 데이터.
2. `BuildingPlacementController.cs`: 선택, 배치 검사, 건설을 처리하는 코드.
3. `BuildingPalette.cs`: 화면의 버튼과 데이터를 연결하고 비용을 표시하는 코드.

이 세 파일만 사용한다. 카드 전용 컴포넌트, 완성 건물의 단순 표식 컴포넌트,
별도 배치 설정 ScriptableObject, 씬 생성 도구, 이벤트 구독 구조는 제거했다.
기존 파일명을 유지해 씬과 스크립트의 GUID 연결이 불필요하게 바뀌지 않도록 했다.

## BuildingDefinition: 건물 데이터

`Farm.asset`에 이름, 모델, 아이콘, 비용, 비용 증가 배율, 점유 크기와 모델 보정값을 둔다.
ScriptableObject는 Inspector에서 편집하는 공유 데이터 파일이다.
현재 벼나 건설 횟수는 여기에 저장하지 않는다.
`CostAt(count)`는 최초 비용 × 배율의 건설 횟수 승을 반올림한다.
Farm은 120 → 180 → 270이며 수치는 Inspector에서 바꿀 수 있다.

## BuildingPlacementController: 실제 건설

위에서부터 다음 순서로 읽으면 된다.

- `Awake`: 필수 참조 확인과 시작 벼 설정.
- `Select`: 선택한 건물의 미리보기 생성. 충돌·물리 기능을 꺼 자기 자신을 장애물로 보지 않게 한다.
- `Update` / `UpdatePlacement`: Input Actions를 읽고, 화면 좌표를 지면 위치로 변환한 뒤 격자 정렬, 검사, 표시, 클릭 확정을 처리한다.
- `CanPlace`: 유효한 건물 → 자원 → 지면 → 장애물 순서로 검사한다.
- `HasGround`: 중심과 경계의 9곳이 땅 위에 있는지 검사한다. 평평한 지면 기준이다.
- `HasObstacle`: 건물의 점유 상자와 다른 Collider가 겹치는지 검사한다.
- `TryBuild`: 다시 검사하고 모델·점유 Collider를 생성한 뒤 벼와 건설 횟수를 갱신한다.
- `Cancel`: 미리보기와 선택을 정리한다. 비용은 들지 않는다.

씬의 Building System에서 카메라, 땅 Collider, Buildings 부모, Input Action 참조를 연결한다.
격자 간격, 시작 벼, 지면 오차, 미리보기 재질, 장애물 마스크도 같은 Inspector에서 조정한다.
격자 간격 0은 자유 배치다. 공통 수치는 코드의 고정 상수로 분기하지 않는다.
`buildCounts`는 현재 실행 중 건설한 횟수를 건물 데이터별로 보관하는 Dictionary다.
완성 건물은 Buildings 아래의 모델과 BoxCollider로 구성된다.

## BuildingPalette: 화면

Inspector의 Options 항목 하나가 건물 데이터·Button·Image·Text를 연결한다.
`Start`에서 버튼 클릭을 컨트롤러의 `Select`에 연결한다.
`TogglePanel`은 건설 창을 열고 닫고, `SelectBuilding`은 창을 닫은 뒤 `Select`를 호출한다.
Inspector의 Building Panel, Open Button, Close Button으로 연결한다. 가로 이동은 Unity의 ScrollRect를 사용한다.
한글 표시는 현재 Windows의 맑은 고딕을 실행 시 불러온다. UI Font Name은 Inspector에서 변경한다.
`Update`는 잔액이 바뀌었을 때 `RefreshButtons`로 버튼의 건설 가능 여부만 갱신한다.
별도 카드 생성이나 이벤트 구독을 따라갈 필요가 없다.

## 다른 건물 추가하기

1. Create > Army Survivor > Building Definition으로 새 데이터를 만든다.
2. 모델·아이콘·비용·Footprint·모델 보정을 설정한다.
3. 씬의 Farm Button을 복제해 UI에 배치한다.
4. Building UI의 Options에 항목을 추가하고 새 데이터, 복제한 버튼, 그 안의 Icon과 Text를 연결한다.

코드를 추가하지 않아도 같은 건설 동작을 사용한다. 자동 카드 생성 대신 직접 UI를 배치하는 구조다.
Farm의 Model Rotation X=90은 원본 FBX 축 보정이다. 다른 모델은 실제 방향·크기에 맞춘다.
Prefab은 게임 동작 스크립트가 없는 시각 모델을 사용한다. 외부 원본 에셋은 수정하지 않는다.

## 검증과 현재 범위

2026-09-13 리팩터링 후 Unity Play 모드에서 입력 처리기에 상태를 전달해 재검증했다.
선택·미리보기·선택 당일 프레임 클릭 방지·확정·UI 클릭 제외·취소·경계·겹침·비용·잔액 부족·UI 갱신을 통과했다.
두 번째 건물 정의를 전달했을 때도 같은 로직으로 배치되고 Farm의 비용 상태와 분리되는 것을 확인했다.
물리 마우스를 직접 조작한 종단 간 테스트는 아니다.

현재는 독립 건설 데모다. 플레이를 종료하면 자원과 배치가 초기화된다.
저장/불러오기, 미리 배치된 건물의 횟수 복원, 생산, 철거, 회전 조작, StartScene 연결은 구현하지 않았다.
새로 확정된 건물은 별도 데이터 컴포넌트를 가지지 않으므로 저장이나 철거가 필요해질 때 그 기능에 맞춰 추가한다.

## 추가한 건물 목록 (2026-09-13)

| 카드 | 모델 | 최초 벼 비용 |
|---|---|---:|
| Farm | Farm | 120 |
| 막사 | Castle | 170 |
| 병영 | Workshop | 110 |
| 방패 훈련소 | TownHall | 250 |
| 사격장 | Archery | 290 |
| 마법탑 | MageTower | 520 |
| 마구간 | Stables | 640 |
| 대장간 | Blacksmith | 400 |

새 7종 비용과 반복 건설 배율 1.4는 첨부 HTML의 임시값이다. Farm의 기존 비용과 배율 1.5는 유지했다.
정의 파일은 Assets/BuildingData에 있으며, 실제 모델을 렌더한 썸네일과 측정한 점유 크기·모델 축 보정을 연결했다.
시작 벼는 320이므로 더 비싼 건물은 선택할 수 없다. 전체 건물을 직접 시험하려면 Building System의 Starting Rice를 조절한다.
병력 해금·생산·강화 효과는 이번 목록 추가에 포함하지 않았다.
Play 모드에서 메뉴 열기/닫기, 선택 후 닫힘, 휠 이벤트의 가로 이동, 8종 각각의 건설·바닥 높이·비용 차감·겹침 거부를 검증했다.
테스트는 UI 콜백과 스크롤 이벤트, 건설 메서드를 호출했으며 물리 마우스의 종단 간 조작은 아니다.

## UI 문구 편집
건설 UI는 TextMeshProUGUI를 사용한다. 각 텍스트의 Inspector > Text Input에서 문구를 직접 입력한다.
BuildingPalette는 텍스트나 폰트를 변경하지 않는다. 식량 숫자와 카드 비용 표시도 수동 문구이므로 실제 자원 및 비용과 자동 동기화되지 않는다.
한글 폰트는 Assets/UI/Fonts의 Noto Sans KR과 Village Korean SDF를 사용한다. 폰트 라이선스는 같은 폴더의 OFL.txt에 있다.
