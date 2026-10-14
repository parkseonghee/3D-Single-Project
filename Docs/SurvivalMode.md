# 서바이벌 / 모험 모드

## 구조

- `GameModeManager`는 StartScene에서 고른 모드를 씬 전환 동안 유지한다. 새 게임 기본값은 서바이벌이며, 기존 저장 파일의 모드 정보가 없으면 모험으로 읽는다.
- `DayController`는 모험 모드의 기존 Day 보상·진행을 맡는다. 서바이벌에서는 `SurvivalController`를 붙이고 Day UI를 숨긴다.
- `CombatController`는 두 모드가 공유하는 생성·이동·피해·풀링을 맡는다. `IWaveRule`의 `AdventureDayWaveRule`과 `SurvivalWaveRule`이 각 모드의 시간·종류·수량을 결정한다.
- `SurvivalSettings`는 두 모드가 공유하는 몬스터 체력표, 서바이벌 스폰/보스/점수 설정이다. 모험 모드도 이 에셋에서 체력을 읽어 런타임 정의를 만들며 원본 `EnemyDefinition`을 수정하지 않는다.
- `RecordManager`는 최고 생존 시간 기록 한 건과 최근 다섯 건을 `Application.persistentDataPath/survival_records.json`에 저장한다. 테스트 기록은 지웠다.
- `BossUnit`이 등장 후 체력과 처치 이벤트를 제공한다. `IBossBehavior`는 이후 보스 패턴을 연결할 지점이다.

## 에디터 설정

1. `Assets/Resources/SurvivalSettings.asset`을 선택한다. 몬스터 네 종, 정예, 보스의 `definition`과 체력표가 연결되어 있다. 새로운 데이터가 필요하면 **Create > Army Survivor > Survival Settings**로 에셋을 만들고 같은 이름으로 Resources에 둔다.
2. `monsters`의 `firstAppearanceSeconds`, `spawnBands`, 구간 경계와 성장률을 조정한다. 기본값은 오크 0초, 닌자 120초, 해골 오크 300초, 예티 480초이다. 모험 Day 구간 경계는 11일/51일이다.
3. `bossPrefab`이 비어 있으면 정예 오크 모델을 확대·붉은색으로 표시한다. 실제 보스 프리팹을 넣으면 코드 변경 없이 그 프리팹을 사용한다. `enableBoss`로 등장 자체를 끌 수 있다.
4. StartScene의 기존 `NewGameButton` 디자인을 복제하여 서바이벌/모험 버튼으로 표시한다. 기존 이어하기·설정·종료 버튼은 아래로 이동하고, 최고 생존 시간은 서바이벌 버튼 아래에 표시한다.
5. PlayScene의 `DayController`가 모드에 따라 기존 Day UI 또는 서바이벌 HUD/보스 바/사망 결과 UI를 준비한다. 결과 UI 버튼은 `다시 하기`와 `시작 화면`이다. 별도 씬 참조를 수동 연결할 필요는 없다.

## 테스트 키

에디터/개발 빌드의 서바이벌 전투 중 사용한다. 카드 선택 등 일시 정지 중에는 동작하지 않는다.

| 키 | 기능 |
|---|---|
| F1 | 1배속 / 2배속 전환 |
| F2 | 다음 시간 구간으로 점프 |
| F3 | 보스 즉시 소환 (`enableBoss` 필요) |
| F4 | 생존 기록 초기화 |

## 실제 최종 보스 추가

`SurvivalSettings.bossPrefab`에 최종 보스 프리팹을 연결한다. 스폰 때 `UnitHealth`와 `BossUnit`은 자동으로 붙는다. 보스 전용 패턴이 필요하면 해당 프리팹에 `IBossBehavior`를 구현한 컴포넌트를 붙이고 `OnSpawn`, `OnPhaseChange`, `OnDeath`를 구현한다. 현재 전투 이동/공격은 기존 정예 AI를 사용하므로 실제 보스의 애니메이션 상태명·공격 타이밍은 별도로 맞춰야 한다.

## 확인 범위

- Unity 6000.3.18f1에서 스크립트 컴파일 및 설정 에셋 참조 확인.
- StartScene의 두 모드 버튼, 서바이벌 HUD 한글 폰트, 씬 전환 확인.
- 서바이벌 전투 시작, 15분 시간 점프 후 구간 3·첫 보스 체력 1,000 확인.
- 모험 모드 Day 1 전투 시작과 기존 웨이브 진행 확인.
- 지휘관 사망 시 결과/기록 저장을 확인한 뒤 테스트 기록을 초기화.
