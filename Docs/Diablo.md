# 즉시 카드: 디아블로

현재 테스트용으로 PlayScene의 MiniBossController > Guarantee Diablo를 켰다. 즉시 카드 선택창 첫 장에 디아블로가 반드시 포함되고 나머지 두 장은 기존 무작위 추첨을 따른다. 원래 확률로 되돌리려면 이 체크를 해제한다.

## 구성과 재사용 (2026-10-12)

영웅 등급 즉시 카드. MiniBossController의 기존 등급 추첨에 추가했다. 기존 7종과 합쳐 8종 중 3장 택1이며, 필수 프리팹을 연결한 DiabloController가 있는 경우에만 등장한다.

RunController가 지휘관 이동/병사 진형을, RecruitmentController.Units가 병력 목록을 관리한다. UnitHealth는 아군·적 공통 체력 컴포넌트다. 적은 현재 Default 레이어/Untagged이며 기존 전투 목록과 선분 거리로 피해를 판정한다. 별도 투사체 풀은 없고 기존 Shot 생성/이동/명중/제거 방식을 재사용한다. 메테오는 MagicTrapState와 공통 BeginMeteorFall, 낙하·도트·착탄 함수 그대로 사용한다.

## 기본 수치

| 항목 | 기본값 |
|---|---|
| 지속 시간 | 10초, 재획득 시 남은 시간만 10초 갱신 |
| 흡수 | 0.4초 |
| 히트스톱 | 0.15초, 실제 시간 기준 |
| 지휘관 크기 / 이동 배율 | 1.5 / 1.5 |
| 피해 감소 | 80%, 기존 방어 효과와 곱연산 |
| 이동 화살 | 2m 이동마다 원형 12발, 발당 기본 피해 35 |
| 화살 속도/판정 | 연결한 ArcherArrow AttackDefinition 값 |
| 화살 관통/거리 | 적 관통, 맵 경계까지 비행. 같은 적은 화살당 한 번 피해 |
| 좌클릭 화염 | 3발, 총 40도, 홀드 간격 0.3초 |
| 화염 피해 / 속도 / 거리 / 판정 반경 | 50 / 12 / 15 / 0.7 |
| 화염 높이 / 시각 크기 | 0.8 / 1.0 (기존 대비 2배) |
| 자동 메테오 | 1초마다 반경 15 내 무작위 생존 적 1명 |
| 메테오 피해 | 기존 메테오의 도트·착탄 피해 2배 |
| HUD 전환 / 중앙 타이틀 | 0.25초 페이드 / 1초 타이틀 소멸 |

수치는 DiabloController와 DiabloHud Inspector에서 변경한다. 메테오의 낙하 높이·속도·범위·도트·착탄 기본값은 기존 CombatController의 마법 함정 설정을 공유한다. 맵 가장자리 화살은 기존 바닥 경계로 거리 제한한다. 적이 없으면 해당 주기 메테오는 생성하지 않는다.

흡수 즉시 원본 병력을 비활성화하고, AI/충돌/체력바를 끈 외형 복사본으로 흡수 연출을 한다. 따라서 AI가 연출 위치를 덮어쓰거나 흡수 중 병사가 공격받지 않는다. 기존 예약 공격과 병사 화살은 취소하며, 이미 떨어지던 메테오는 기존 처리대로 진행된다. 종료 시 동일한 원본을 진형 원형 위치에 돌려놓는다. 체력·레벨·스킬은 유지하고 사망자를 부활시키지 않는다.

상태 지속·쿨다운은 게임 시간 기준으로 일시정지/카드 선택 중 정지한다. 히트스톱만 실제 시간으로 해제한다. 전투 종료·지휘관 사망·컴포넌트 비활성화 시 병력, 크기, 이동 배율, 피해 감소와 시간 정지를 복구한다.

## 변경 파일과 이유

- 추가 `Assets/Scripts/Army/DiabloController.cs`: 흡수/복귀, 시간, 오라, 히트스톱, 입력과 발사 주기. 원본 능력치 에셋 변경 없음.
- 추가 `Assets/Scripts/Army/CombatController.Diablo.cs`: 기존 Shot/적 목록/피해 경로에 연결하는 부분 클래스. 새로운 피해 시스템이나 풀을 만들지 않음.
- 추가 `Assets/Scripts/Army/DiabloHud.cs`: uGUI 체력 구슬, 두 슬롯, 남은 시간, DIABLO 타이틀. HP는 OnHpChanged 구독.
- `MiniBossController.cs`: 영웅 카드 등록·문구·발동 연결.
- `RunController.cs`: 디아블로 전용 이동 배율/상태를 추가해 기존 이동 버프를 덮어쓰지 않음.
- `UnitHealth.cs`: 독립 피해 감소 항목을 추가해 기존 방패병 방어력과 충돌 방지.
- `CombatController.cs`: 병력 흡수 동안 병사 스킬 AI 갱신 정지. 적·투사체·메테오 갱신 유지.
- `CombatController.FocusCommand.cs`: 강화 중 좌클릭이 집중 구역 명령과 동시에 실행되는 것 방지.
- `CombatController.MagicTrap.cs`: 기존 메테오 낙하 시작을 BeginMeteorFall로 분리. 함정과 디아블로가 같은 함수 사용. 개별 메테오 피해 배율 추가.
- `Assets/Scenes/PlayScene.unity`: 컴포넌트·프리팹·폰트·기존 HUD CanvasGroup 참조 연결.

## 에디터 연결 순서

현재 PlayScene은 아래 연결을 완료했다. 사용자가 추가로 연결할 항목은 없다. 다른 씬으로 옮길 때 아래 순서를 따른다.

1. Recruitment System(RunController/CombatController가 있는 오브젝트)에 DiabloController를 추가한다. DiabloHud는 RequireComponent로 함께 추가된다.
2. DiabloController > Arrow에 `Assets/CombatData/ArcherArrow.asset`을 연결한다. 이 정의가 기존 궁수 화살 프리팹을 참조한다.
3. Aura Prefab에 `Assets/Hovl Studio/Magic effects pack/Prefabs/Magic circles/Magic circle.prefab`을 연결한다.
4. Fire Prefab에 `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR2 Firewall A.prefab`을 연결한다.
5. CombatController의 기존 마법 함정 Fireball/Portal/Impact 프리팹 연결을 유지한다. 별도의 디아블로 메테오 프리팹은 필요 없다.
6. DiabloHud > Font에 기존 HUD 한글 TMP 폰트를 연결한다. Original Hud에는 Run UI의 기존 표시 요소들과 Experience Panel의 CanvasGroup을 등록한다. 중복 부모·자식 그룹을 등록하지 않는다. Pause Button 및 카드 선택/결과 패널은 제외한다.
7. 새 HUD는 실행 시 자동 생성된다. 체력 구슬은 왼쪽 아래, 두 스킬 슬롯과 지속 바는 하단 중앙이다. 기존 HUD는 알파/입력만 조절하며 GameObject를 끄지 않는다. 별도 레이어·태그·카메라는 필요 없다.

## 실행 검증

좌클릭 화염 발사 시 CommanderIdle의 Attack 상태를 재생한다. 같은 캐릭터 세트의 infantry_04_attack_A에서 복사한 CommanderAttack.anim을 사용하며, FireAttackSpeed로 발사 쿨다운에 맞춰 한 번의 검 공격을 완료한다. 모션 종료 후 Moving 값에 따라 Run/Idle로 복귀한다. 이펙트 크기만 2배이며 화염 피해와 판정 반경은 유지한다.

Unity에서 병력 3명 흡수, Magic circle 오라, 1.5배 크기, HUD 전환, 자연 종료 후 원본 병력/크기 복귀를 확인했다. 이동 화살 12발·정지 추가 발사 0발, 좌클릭 화염 3발·쿨다운 전 재발사 방지·홀드 재발사, 화염 명중 및 메테오 낙하/처치, 피해 100→20, 재획득 시 시간 10초/크기 중첩 없음, 준비 복귀 시 이동·크기·timeScale 복구를 확인했다. 콘솔 오류/경고 0건. 장기적인 난이도와 대규모 투사체 성능은 추가 플레이 조정 대상이다.
