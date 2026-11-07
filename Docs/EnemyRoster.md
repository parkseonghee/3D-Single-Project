# 추가 몬스터 — 2026-09-26

Assets/Monster/Big/FBX의 Ninja, Orc_Skull, Yeti와 Assets/Monster/Blob/FBX의 Wizard를 프로젝트용 프리팹으로 추가했다. 외부 원본은 변경하지 않았다. Wizard는 일정 거리에서 마법탄을 발사한다.

| 몬스터 | 역할 | Day 1~10 HP / 공격력 | Day 11~50 HP / 공격력 | Day 51~100 HP / 공격력 | 이동 속도 | 공격 간격 | 타격 준비 | 정지 거리 |
|---|---|---|---|---|---|---|---|---|
| Ninja | 빠른 추격 | 40 / 8 | 60 / 10 | 93 / 12 | 3.6 | 1초 | 0.35초 | 1.1 |
| Orc_Skull | 공격형 | 90 / 16 | 135 / 20 | 210 / 24 | 2.0 | 1.4초 | 0.35초 | 1.1 |
| Yeti | 고체력형 | 180 / 24 | 270 / 30 | 420 / 36 | 1.4 | 2.2초 | 0.5초 | 1.3 |
| Wizard | 원거리형 | 55 / 7 | 82 / 10 | 128 / 14 | 2.5 | 2.4초 | 0.45초 | 8 |

공격력은 전술의 받는 피해 배율 적용 전 수치다. 초기 임시 밸런스이며 실제 생존 난이도는 추가 조정이 필요하다. 이동 속도 단위는 Unity 거리/초다.

## 애니메이션

각 모델 자체 FBX에서 Idle, 이동, Attack, Death를 별도 anim으로 복사했다. 닌자는 Run과 Weapon, 해골 오크는 Walk와 Weapon, 예티는 Walk와 Punch를 사용한다. 예티 공격 재생 속도는 0.75이며 동작 유지 시간은 약 1.156초다. 다른 둘은 0.867초다. 사망 클립은 0.767초이며 기존 1.3초 제거 대기 시간 안에서 재생된다. 공격·사망은 반복하지 않고, 사망 마지막 자세를 유지한 뒤 제거한다. 모델별 URP 머티리얼은 기존 오크와 같은 몬스터 아틀라스를 사용한다.

Wizard는 원본 FBX의 Idle, Walk, Bite_Front, HitRecieve, Death 클립을 Animator Controller에서 사용한다. Idle/Walk는 반복, 공격/피격/사망은 한 번만 재생한다. 피격 중 0.3초 멈추며, 연속 피격 시 반응은 1초 간격으로 제한한다. 공격 시작 0.2초 후 기존 MageBolt 프리팹을 발사한다. 탄속 11, 최대 거리 13, 명중 반경 0.55이며 발사 뒤에는 목표를 유도하지 않는다. 원거리 설정은 Wizard EnemyDefinition Inspector에서 변경할 수 있다.

## 출현 구조와 수정 위치

- DaySettings.Stage.enemies: EnemyDefinition 참조, Weight, First Day 목록. 기존 오크 50, 닌자 25, 해골 오크 15, 예티 10은 Day 1부터, Wizard 8은 Day 5부터 출현한다. 서바이벌에서는 Wizard가 3분부터 출현한다.
- DayController.BeginStage: 구간별 목록을 CombatController.Configure에 전달한다.
- CombatController.ChooseEnemy: 유효한 항목의 가중치 합에서 무작위 선택한다. 빈 목록이나 양수 가중치가 없으면 기존 Stage.enemy로 대체한다. 이름으로 병종을 분기하지 않는다.
- 기존 전체 생성 간격, 최대 적 수, 보스, 보상은 유지한다. 몬스터 종류를 추가했다고 전체 적 수를 늘리지는 않았다.
- Assets/CombatData/DaySettings.asset: 구간과 출현 비율.
- Assets/CombatData/Ninja.asset, Orc_Skull.asset, Yeti.asset 및 Tier2/Tier3: 능력치·공격 타이밍.
- Assets/Prefabs/Combat: 모델 프리팹.
- Assets/Animations/Combat: 몬스터별 Animator Controller와 클립.

## 확인

Unity Play 모드에서 실제 프리팹을 생성하고 공격 상태 진입, 준비 전 무피해, 타격 시 전술 배율을 반영한 피해, 사망 상태·클립 바인딩, 사망 후 목록 제거를 세 종류 모두 확인했다. 고정 시드로 1,000회 추첨하여 기존 오크 503, 닌자 236, 해골 오크 155, 예티 106을 확인했다. 렌더 캡처에서 모델과 머티리얼을 확인했고 콘솔 오류 0개였다. 일시정지 상태에서 메서드를 직접 호출한 기능 검증이며 장시간 전투 밸런스 검증은 아니다. 테스트 객체는 Play 종료로 제거했다.
