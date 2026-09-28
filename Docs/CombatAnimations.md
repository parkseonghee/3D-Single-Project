# 공격·사망 애니메이션 — 2026-09-26

기존 프로젝트 Animator를 확장했으며 외부 에셋 원본은 수정하지 않았다.

- 오크/보스: Orc FBX의 Weapon 클립을 OrcAttack.anim으로 복사했다. 공격 중 이동을 멈추고 0.35초 뒤 사거리 내 살아 있는 대상에게 한 번 피해를 준다. 회복 동작 포함 0.867초 후 추적을 재개한다. 준비 중 대상이 벗어나면 빗나가며, 몬스터가 죽으면 예약 타격은 취소된다.
- 지휘관·병사: infantry 사망 A, 궁수: archer 사망 A, 방패병: shield 사망 A, 마법사: staff 사망 A, 기마병: cavalry 사망 A. 각 클립은 1.667초이며 마지막 자세를 유지한다.
- UnitHealth.TakeDamage에서 체력 0이 되는 순간 Death 상태를 재생한다. 진행 중 공격 트리거를 제거하며 중복 피해로 사망 모션을 다시 시작하지 않는다.
- CombatController와 RunController는 죽은 병사의 공격·이동을 제외한다. 시체는 전장 종료까지 남는다. 지휘관은 사망 모션과 0.25초 여유 후 기존 준비 복귀 흐름으로 돌아간다. 별도 게임 오버 기능은 이번 변경에 포함하지 않았다.
- 준비 복귀 시 UnitHealth.Initialize가 사망 Animator를 Rebind하여 대기 자세로 복구한다. 보스는 사망 연출이 끝난 후 BossDefeated를 알리므로 결과 화면에 모션이 잘리지 않는다.

## 조정 위치

Assets/CombatData의 Orc, DayEnemy0~2, DayBoss0~2: Attack Windup(타격 시점), Attack Duration(공격 동작 유지), Attack Interval(공격 주기), Attack State(Animator 상태 경로). 현재 Base Layer.Attack으로 연결했다.
Assets/Animations/Army의 각 Controller: Death 상태. Assets/Animations/Combat의 병과별 Death.anim: 사망 모션. Death는 종료 전환 없이 마지막 자세를 유지한다.

## 확인

Unity Play 모드에서 6종 아군의 Death 진입과 모든 애니메이션 바인딩의 모델 뼈대 존재를 확인했다(누락 0). 최종 사망 자세를 화면으로 확인했다. MCP로 Tick/피해를 호출해 타격 준비 전 무피해, 준비 후 1회 피해, 사망 후 공격 중단, 지휘관 사망 중 이동 차단, 모션 대기 후 준비 복귀와 체력·Animator 복원을 확인했다. 콘솔 오류 0개. 시간 경계 검증은 에디터 일시정지 및 메서드 호출로 수행했으며 실제 입력 전체를 통한 종단 간 검증은 아니다. 테스트 병력은 실행 종료로 제거했고 저장 슬롯을 수정하지 않았다.
