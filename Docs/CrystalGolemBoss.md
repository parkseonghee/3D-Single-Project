# 크리스탈 골렘 보스 (2026-10-19)

`Meshy_AI_Crystal_Golem_biped/Meshy_AI_Character_output.fbx`로 Day 보스 3종 및 생존 모드 최종 보스를 교체했다. 중간 보스/정예 오크는 기존 설정을 유지한다. 기존 HP, 기본 공격력, 출현 시점, 보상은 그대로 사용한다.

## 행동

- 생성 후 5초에 첫 포효. 이후 포효 시작 기준 5초 주기이며, 진행 중인 포효/돌진을 중간에 끊지는 않는다.
- Walk로 지휘관을 추적하고, roar(약 2.97초) 동안 멈춘다.
- 포효 종료 시 지휘관 위치를 향해 Run으로 직선 돌진한다. 이후 지휘관이 움직여 피할 수 있다.
- 속도 18, 최대 1.5초, 피격 반경 1.4. 지휘관에게 한 돌진당 기존 공격력의 2배 피해를 한 번 적용한다. 진형 피해 배율, 보호막, 무적은 기존 UnitHealth 처리를 따른다.
- 사망하면 Die 클립을 재생하고 마지막 자세를 유지한다. 기존 공통 사망 처리와 호환하도록 Animator 상태명만 Death로 연결했다.
- 일반 적 AI와 돌진 AI가 동시에 이동/공격하지 않는다. 전투 컨트롤러가 Tick을 호출하므로 일시정지/카드 선택 시 함께 멈춘다. 풀 재생성 시 공격 타이머가 초기화된다.

## 조정

`Assets/Prefabs/Combat/CrystalGolemBoss.prefab`의 **BossCharge**에서 Roar Interval, Roar Duration, Charge Speed, Max Charge Duration, Hit Radius, Damage Multiplier를 조절한다. Visual 자식의 Scale은 1.6이다. 프리팹/애니메이터/DayBoss0~2/SurvivalSettings 연결은 완료되어 별도 수동 설정이 필요 없다.

원본 애니메이터는 수정하지 않았다. 전용 CrystalGolemBoss.controller 및 반복 재생하는 BossWalk.anim/BossRun.anim을 생성했다. roar와 Die는 원본 클립을 참조한다.

## 검증

Unity 에디터의 임시 PreviewScene에서 5초 이전 피해 없음, 빠른 돌진의 구간 충돌 판정, 기본 공격력 2배 피해, 돌진 중 중복 피해 방지, Death 상태/3초 사망 클립, 풀 초기화 코드를 검증했다. 확인 시 콘솔 오류/경고 0건. 실제 전투 전체 플레이와 시각적 연출/난이도는 추가 플레이 확인 대상이다.
