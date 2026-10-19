# 도서관 교범 시스템

## 현재 구현

마을의 기존 도서관(`Assets/BuildingData/Library.asset`)을 재사용한다. 마을 → PlayScene 편성 → 게임 시작 → 교범 선택 → 전투 시작 순서다. 선택 전에는 RunStarted가 발생하지 않으며 전투 시간, 적 생성, 경험치 초기화는 선택/건너뛰기가 끝난 뒤 시작한다.

- 건설된 도서관이 없으면 선택 화면을 건너뛴다.
- 해금된 교범 중 출전 병사 수와 필요한 병종 조건을 만족하는 후보를 중복 없이 최대 3개 제시한다. 1개면 1개만 중앙에 표시하며 후보가 없으면 바로 시작한다.
- `선택 안 함`으로 교범 없이 시작할 수 있다.
- 등록된 교범은 현재 **7개**다. 편성 조건에 따라 최대 3개를 제시한다. 추가 교범의 수치와 설정은 [TacticalManuals.md](TacticalManuals.md)를 참조한다. 이전 유물 데이터는 보존하지만 출전 후보로 사용하지 않는다.
- 선택 상태는 `ManualManager.Selected`에 보관해 씬 전환 시에도 참조할 수 있다. 현재 흐름은 PlayScene 편성에서 선택하므로 추가 씬 로딩 없이 전투로 이어진다. 디스크 저장 대상은 아니며 전투 종료/비활성화 시 정리한다.
- 도서관 강화 레벨에 따른 교범 보너스는 현재 없다. 건설·강화·철거·저장 구조는 기존 시스템을 유지한다.

## 균등 교육

경험치 병을 **성공적으로 투자**하면 `BattleExperience.OnExpBottleInvested(Transform)`가 1회 발생한다. 병 획득, 투자 실패, 레벨업 선택 자체는 카운트하지 않는다. 대기 병력과 지휘관은 대상이 아니다.

기본값: 서로 다른 병사 3명 → 전투 병사 공격력 +30%, 공격 속도 +20%, 8초. 지휘관은 제외한다. 병종이 같아도 서로 다른 개체면 인정한다.

- A → B → C: 발동, 기록 초기화.
- A → B → A: 기존 기록을 버리고 마지막 A만 남음(1/3).
- 기록된 A가 사망/비활성/제거되어도 기록 유지. 살아 있는 B, C에게 투자하면 발동 가능.
- 기본 재발동 정책: 중첩 없이 남은 시간 8초로 갱신.
- Stack 옵션: 발동할 때마다 보너스 합산(+30/+20 → +60/+40), 모든 중첩의 남은 시간 갱신. 만료 시 함께 제거.
- 일시정지, 레벨업/즉시 카드 선택 중에는 지속 시간이 줄지 않는다.
- 종료/컴포넌트 비활성화 시 이벤트 해제, 진행 기록·버프·이펙트 정리.

기존 `CombatController.AttackDamage`에 공격력 배율, `AttackInterval`에 공속 보너스를 추가한다. 공격력은 다른 공격력 배율과 곱연산, 공속은 기존 공속 보너스와 합산한다. 따라서 단독 적용 시 피해 1.3배, 공격 간격 1/1.2배다. 메테오 등 원래부터 기본 공격력을 사용하지 않고 별도의 고정 피해 수치를 사용하는 스킬은 이 공격력 증가의 대상이 아니다.

## UI / 조작

- 기존 uGUI 교범 카드: 제목, 효과, 플레이 변화, 도서관 아이콘. 효과 수치는 Effect 에셋에서 읽어 설명을 생성한다.
- 전투 하단 중앙: 교범 이름, 기본 3칸 진행 표시, 지속 시간 게이지·숫자.
- 투자한 병사 초상화 오른쪽 위: 체크. 반복 투자 시 기존 체크 초기화, 발동 시 모두 초기화.
- 발동 시 `균등 교육!`, 살아 있는 병사에게 짧은 Healing 이펙트(회복 효과는 없음).
- 기존 Ctrl+1~5 투자, 진형 버튼, 포션 표시 유지.
- Editor/Development Build 전용: **F8**은 편성 화면에서 도서관 보유 조건을 무시하고 선택 화면을 연다. 편성/교범 병종 조건은 유지하므로 기본 교범 테스트에는 병사 3명 이상 필요하다. **F9**는 전투 중 병 30개 지급. ManualManager에서 키 기능과 지급량 조정.

## 에디터 세팅 (현재 씬에는 연결 완료)

1. `Create > Army Survivor > Manual > Equal Education Effect`로 효과 에셋 생성. 필요 병사 수, Damage Bonus(0.3), Attack Speed Bonus(0.2), Duration(8), Reactivation, 버프 프리팹·위치·크기·생존 시간 설정.
2. `Create > Army Survivor > Manual > Definition` 생성. 이름·설명·플레이 변화·아이콘 입력, Effect 연결. Unlocked 켜고 Minimum Soldiers와 Required Types 설정. Required Types 배열은 **모든 지정 병종 필요** 조건이며 예비 병사는 포함하지 않는다.
3. `VillageScene`의 기존 Building Palette/건물 관리 설명/GameSave 건물 카탈로그에 Library.asset 등록 유지. 이미 등록되어 있어 새 레이어/카메라/건물 프리팹은 필요 없다.
4. `PlayScene > Recruitment System > Relic Draft Controller`의 Manuals 배열에 교범 등록. Library, Run, Recruitment, Panel, Cards(3개), Skip Button 연결. 기존 컴포넌트 이름은 씬 참조 보존을 위해 유지했다.
5. 같은 오브젝트에 ManualManager, ManualHud. ManualHud의 Manager, Hud Parent(Run UI), Font 연결. HUD와 체크는 실행 시 생성한다. 위치는 ManualHud에서 조정한다.
6. 기본 데이터는 `Assets/ManualData/EqualEducation.asset`, `EqualEducationEffect.asset`. 필요 병사 수를 늘리면 후보 편성 조건도 자동으로 그 수 이상을 요구한다.

## 새 교범 추가

`ManualEffectDefinition`을 상속한 설정 SO와 `ManualEffect`를 상속한 전투별 효과를 만든다. `CreateEffect`에서 새 런타임 인스턴스를 반환하고 `OnBattleStart`에서 이벤트 구독, `OnBattleEnd`에서 해제/버프 제거한다. SO 자체에 전투 중 상태를 저장하지 않는다. 새 ManualDefinition의 Effect에 연결하고 Draft의 Manuals에 등록한다.

공격 버프는 `Combat.SetManualBuff(effectInstance, damageBonus, speedBonus, optionalTarget)`와 `RemoveManualBuff`로 관리한다. 다른 효과의 버프를 덮어쓰지 않는다. 전투 이벤트는 투자, 정상 명중, 진형 전환, 실제 피해/보호막 흡수로 확장했다. 교범의 추가 피해는 정상 명중 이벤트를 다시 발생시키지 않는다.

## 변경 파일과 이유

- 신규 ManualDefinition / ManualEffectDefinition: 교범 데이터·편성 조건과 공통 효과 계약.
- 신규 EqualEducationEffect: 투자 연쇄, 갱신/중첩, 타이머, 정리.
- 신규 ManualManager: 선택 상태, 전투 수명 관리, UI 상태·발동 이펙트·디버그 키.
- 신규 CombatController.Manuals: 효과 출처별 공격 보너스.
- 신규 ManualHud: 진행도·지속 시간·발동 문구.
- BattleExperience: 성공 투자 이벤트 선언/발생, 사망자 직접 투자 방지.
- CombatController: 공격력/공속에 교범 보너스 반영, Clear에서 안전 정리.
- RunController: 교범 선택이 끝나야 전투 초기화.
- RecruitmentController / DayController: 씬 이동 후 비활성 마을도 검색해 고용·결과 귀환 연결 유지(실제 출전 경로 테스트에서 발견).
- RelicDraftController: 기존 유물 카드 UI를 교범 후보 필터·선택/건너뛰기로 교체.
- OperatorHud: 이벤트로 초상화 체크 갱신.
- PlayScene: 데이터·UI·컴포넌트 연결, 카드 설명 영역 확장.
- VillageScene: 도서관 설명을 교범으로 수정.

## 추가 교범 (2026-10-17 구현)

| 교범 | 편성 조건 | 플레이 변화 |
|---|---|---|
| 화염 사격 | 궁수 + 마법사 | 불붙은 적을 사격해 불길 확산 |
| 성벽 돌파 | 방패병 + 기마병 | 보호막을 받은 돌진으로 적진 붕괴 |
| 교대 선봉 | 병사 + 방패병 | 서로 번갈아 공격한 적에게 추가 충격 |
| 추격 명령 | 궁수 + 기마병 | 제압 대상 처치로 다음 돌진 강화 |
| 진형 반격 | 방패병 | 밀집에서 버티고 산개로 반격 |
| 집중 교육 | 제한 없음 | 같은 병사에게 연속 투자해 선봉 강화 |
