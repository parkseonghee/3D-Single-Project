# 튜토리얼 시스템

## 구조

- `TutorialSequence`는 튜토리얼 ID, 시작/중단/완료 이벤트, 단계와 시작 거리 조건을 담는다. `interruptCurrent`를 켜면 해당 시작 이벤트가 이미 진행 중인 튜토리얼을 중단하고 새 시퀀스를 시작한다.
- `TutorialStep`은 대상 ID, 구멍 형태·여백, 설명, 위치, 진행 조건, 일시정지·입력 허용·적 이동 속도를 담는다. 단계 내용과 수치는 `Assets/TutorialData/*.asset`에서 수정한다.
- `TutorialTarget`은 UI 또는 월드 오브젝트에 붙여 문자열 ID를 등록한다. `TutorialManager`는 활성 대상을 찾아 스포트라이트를 매 프레임 따라 움직인다. 대상이 비활성/미생성이면 해당 단계는 기다린다.
- `TutorialEvents.Raise(key, target)`가 기존 게임 로직의 성공 시점에 발생한다. 튜토리얼이 이벤트를 소비하므로 게임 코드에는 개별 튜토리얼 분기가 없다.
- 완료 기록은 기존 슬롯 저장과 분리한 `Application.persistentDataPath/tutorial_progress.json`에 튜토리얼 ID만 저장한다. StartScene 설정 창의 **튜토리얼 초기화** 버튼으로 삭제할 수 있다.

## 표시 및 입력

`TutorialSpotlight.shader`를 쓴 화면 전체 Image가 어두운 배경, 둥근 사각형/원형 구멍, 깜빡이는 테두리를 함께 그린다. 셰이더는 `TutorialStyle.asset`의 `spotlightShader`에 직접 연결하여 플레이어 빌드에 포함한다. 셰이더가 누락되거나 지원되지 않을 때는 Image 렌더링을 꺼서 흰색 기본 이미지가 화면을 덮지 않게 한다. 네 패널 방식은 원형 구멍과 부드러운 모서리를 구현할 때 패널이 더 필요하므로 단일 셰이더를 택했다. 구멍은 화면 좌표를 Canvas 로컬 좌표로 변환해 계산하므로 Canvas Scaler와 해상도 변경을 따른다. UI 대상은 `RectTransform`의 네 모서리를, 월드 대상은 렌더러 바운딩 박스를 카메라에 투영한다.

`blockOutsideClicks`가 켜지면 구멍 밖 UI/월드 클릭을 막는다. 구멍 안은 `ICanvasRaycastFilter`가 레이캐스트를 통과시켜 기존 버튼 및 우클릭 집중 공격 입력이 실행된다. 말풍선과 건너뛰기 버튼은 그 위에서 표시된다. `allowKeyboard`를 끄면 지휘관 이동·전술키와 병사 경험치 투자 단축키를 막는다. 일시정지는 `Time.timeScale`로 처리하며 튜토리얼 UI의 애니메이션은 `unscaledDeltaTime`을 사용한다.

설명 단계(`NextButton`)는 별도 다음 버튼 없이 화면을 좌클릭하면 진행한다. 이때 스포트라이트가 클릭을 받아 뒤의 게임 버튼이 함께 눌리지 않게 한다. 시작 프레임의 클릭은 무시해 같은 클릭으로 두 단계를 넘기지 않는다. 건너뛰기 버튼과 확인창 클릭은 진행 클릭으로 처리하지 않는다. `requiredEventCount`를 지정한 이벤트 단계는 성공 이벤트가 그 횟수만큼 발생해야 넘어간다. `hideWhenTargetMissing` 단계는 강조 UI가 닫힌 동안 말풍선을 숨기고 이벤트는 계속 기다린다. 카드 선택 등 `RunController.IsChoosingUpgrade` 상태에서는 튜토리얼 표시를 잠시 숨긴다.

## 새 튜토리얼 만들기

1. `Assets > Create > Army Survivor > Tutorial > Sequence`로 새 asset을 만들고 고유 `tutorialId`, `startEventKey`, 단계 목록을 입력한다.
2. VillageScene의 `Building UI` 또는 PlayScene의 `Recruitment UI`에 붙은 `TutorialManager.sequences`에 asset을 추가한다. 두 씬을 건너는 시퀀스는 두 매니저 모두에 추가한다.
3. 강조할 UI/오브젝트에 `TutorialTarget`을 붙이고 `targetId`를 입력한다. 단계의 `targetId`와 정확히 일치시킨다. 런타임 생성 오브젝트에는 생성 직후 `SetId`를 호출한다.
4. 이벤트 진행 단계는 성공한 게임 동작 직후 `TutorialEvents.Raise("이벤트키", 대상Transform)` 한 줄을 추가한다. 이 키를 단계의 `eventKey`에 입력한다.
5. 말풍선 스타일은 `TutorialStyle.asset`에서 조정한다. 씬의 Tutorial Canvas는 런타임에 Sorting Order 1000으로 생성된다.

## 현재 예시 데이터

| Asset | 시작 | 주요 대상/이벤트 |
|---|---|---|
| `FirstVillage` | `VillageEntered` | 마을 자원, 건설 메뉴, 병영 건설, 전투 준비 화면, 병사 5회 고용, 편성 변경, 출전 |
| `FirstBattle` | `BattleStarted` | 일시정지 설명, 지휘관 이동, 자동 전투 설명, 경험치 병 획득/투자 |
| `FirstLibrary` | `BuildingBuilt:Library` | 도서관, 교범 선택 (`ManualSelected`) |
| `FirstEliteFocus` | 정예 몬스터 등장(`EliteSpawned`), 지휘관 12m 이내 | 강조된 정예 몬스터 우클릭, 집중 공격 확인, 빈 땅 좌클릭 해제 |

정예 몬스터가 가까이 오면 원형 스포트라이트와 말풍선으로 우클릭 집중 공격을 안내한다. 첫 단계는 `requireEventTargetMatch`를 사용하므로 일반 몬스터를 우클릭해도 진행되지 않는다. 집중 마커를 보여준 뒤 6초가 지나거나 집중 대상을 처치하면 해제 설명으로 넘어간다. 정예 몬스터가 우클릭 전에 죽으면 이번 튜토리얼을 완료 처리하지 않아 다음 정예 몬스터에서 다시 안내한다. 다른 정예 몬스터의 처치 이벤트가 진행 중인 단계를 건너뛰지 않도록 `requireJumpTargetMatch`를 사용한다. 단계별 `enemyMovementMultiplier=0.3`, `pauseGame=false`라서 적은 천천히 움직이고 우클릭 입력은 살아 있다. `BattleEnded` 또는 지휘관 사망 시 완료 없이 중단하고, 튜토리얼 종료 시 적 이동 배율은 1로 복원한다.

전투에서 경험치 병을 얻은 뒤 **Ctrl+1~5**를 누르면 해당 번호의 병사에게 병을 투자한다. 예를 들어 **Ctrl+1**은 1번 병사다. 경험치가 가득 차면 병사가 레벨업하고 강화 카드를 선택한다.

첫 마을의 병영은 미리 배치하지 않는다. 플레이어가 건설 메뉴에서 병영을 선택하면 메뉴가 닫힌 동안 건설 설명 말풍선도 숨겨 배치 공간을 확보한다. 전투 준비 화면은 진입할 때 고용 목록을 맨 위로 올리며, 고용 성공 이벤트 5회를 받은 뒤 편성 단계로 진행한다. 첫 전투에서는 지휘관 조작과 병사 자동 전투를 설명할 때만 잠시 멈춘다. 실제 이동·경험치 수집·투자 단계에서는 전투가 흐르며, 강화 카드 튜토리얼 단계는 없다. 전투 중 지휘관/정예 몬스터/병사 카드 선택창이 열리면 튜토리얼 오버레이를 숨겼다가 선택 후 다시 표시한다. 적 우클릭 집중 공격은 콜라이더가 없는 프리팹도 화면에 보이는 렌더러 영역으로 선택한다.

## 디버그

- **F5**: 현재 씬의 `debugSequence`를 완료 기록과 관계없이 처음부터 강제 시작 (마을은 첫 마을, 전투는 첫 전투)
- **F6**: 현재 단계 넘기기
- **F7**: 튜토리얼 완료 기록 초기화. 이미 시작된 씬에서 즉시 다시 보려면 이어서 F5를 누른다.
- **F8**: Unity 에디터 또는 **Development Build**에서 `Tutorial Test` 창 열기/닫기. 창에서 각 시퀀스 완료 여부 확인, 개별 다시 보기, 모든 완료 기록 초기화, 현재 씬 기본 튜토리얼 초기화 후 다시 보기를 할 수 있다.

반복 테스트는 전투 시작 후 **F8 → `Clear and replay default`**가 가장 빠르다. 일반 빌드에서는 테스트 창이 표시되지 않지만 **F5/F6/F7**은 사용할 수 있다. StartScene의 **설정 → 튜토리얼 초기화**를 누르면 다음에 마을/전투에 들어갈 때 첫 튜토리얼이 자동으로 다시 시작한다. 초기화는 기록만 지우며 현재 화면에 즉시 튜토리얼을 띄우지는 않는다. 대상 오브젝트가 없는 단계를 재생하면 대상이 생성될 때까지 기다리므로, 전투 튜토리얼은 병사를 고용하고 전투를 시작한 뒤 테스트한다.

`Army Survivor > Tutorial > Install Example Sequences` 메뉴는 예시 asset과 씬 연결을 처음 설치할 때 사용한다. 기존 예시 asset의 단계 내용은 덮어쓰지 않는다.
