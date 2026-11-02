# 경험치 병 / 보급 인장 획득 이펙트

- 경험치 병: Hovl Studio / Magic effects pack의 `Sparks explode green`.
- 보급 인장: 같은 팩의 `Sparks explode yellow`.
- 원본은 유지하고 `Assets/Resources/PickupEffects`에 수거 전용 프리팹 복사본을 추가했다.
- 지휘관 위치 + Y 0.8에서 초록/금색 반짝임을 재생하고 지휘관 이동을 따라간다.
- 실제 병/인장 지급 성공 경로에서만 호출한다. 디버그 지급에는 재생하지 않는다.

## 성능과 수명

`PickupFeedback.Prepare(run)`이 RunController에 컴포넌트를 한 번 추가하고 두 이펙트를 미리 생성한다. 수거 시 Instantiate/Destroy 대신 Emit을 사용한다.

- 병은 한 번의 묶음 수거당 12개 입자, 인장은 20개 입자.
- 종류별 재생 간격 0.08초, 최대 64개 입자, 수명 0.45초.
- 연속 획득 시 연출만 합친다. 실제 지급량은 바꾸지 않는다.
- 전투 시작/종료와 컴포넌트 비활성화 시 파티클을 비운다.
- 일시정지/강화 선택 중 파티클 진행을 멈춘다.
- 반복 방출, 충돌, 그림자를 사용하지 않는다.

## 설정

별도 연결 없이 자동 적용된다. 영구적으로 수치를 바꾸려면 Play 모드를 끈 상태에서 PlayScene의 RunController 오브젝트에 `PickupFeedback` 컴포넌트를 추가하고 Inspector 값을 조정한 뒤 씬을 저장한다. 기존 컴포넌트가 있으면 런타임에 중복 추가하지 않는다.

Prefab 필드를 비워두면 Resources의 기본 프리팹을 사용한다. 크기(size), 높이(offset), 수명(lifetime), 입자 수, 재생 간격을 조절할 수 있다. 프리팹은 루트에 ParticleSystem이 있는 단일 파티클 프리팹을 사용한다. 초기 생성에 사용하는 값은 다음 플레이 시작부터 적용된다.

## 변경 파일

- `Assets/Scripts/Army/PickupFeedback.cs`: 공용 재사용 수거 이펙트.
- `Assets/Scripts/Army/BattleExperience.Drops.cs`: 병 지급 후 연출 연결.
- `Assets/Scripts/Equipment/EquipmentRuntime.cs`: 인장 지급 후 연출 연결.
- `Assets/Resources/PickupEffects/ExperiencePickup.prefab`
- `Assets/Resources/PickupEffects/SealPickup.prefab`

## 검증

프로젝트의 Unity 참조 DLL과 소스를 사용한 C# 컴파일 검사 통과. Unity MCP가 현재 연결되어 있지 않아 에디터 내 재생 및 시각 검증은 수행하지 못했다. 다음 실행에서 병/인장 각각 수거, 대량 동시 수거, 이동 중 수거, 일시정지 및 전투 재시작 시 잔여 이펙트를 확인하면 된다.
