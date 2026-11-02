# 전장 확장 및 경험치 병 최적화

## 원인
- Battle Hex Map의 이동 바닥은 64×64, 지휘관은 가장 넓은 진형 반경(7.8)+1을 제외한 ±23.2 범위로 제한됐습니다.
- 경험치 병은 적 사망 위치에 그대로 생성되어 지휘관이 갈 수 없는 바깥 영역에도 남았습니다.
- SnakeExperienceBottle 모델은 381,579 정점이며, 매번 Instantiate/Destroy 및 모든 병의 회전 처리를 수행했습니다.
- 여러 병을 동시에 획득하면 지휘관 경험치/HUD 갱신 이벤트도 병 개수만큼 호출됐습니다.

## 변경
- PlayScene의 Battle Hex Map X/Z 스케일 1 → 1.5: 바닥 96×96, 경관도 함께 확장. 지휘관 중심 이동 범위 ±39.2.
- RunController.ClampCommanderPosition을 이동과 경험치 병 드롭에서 함께 사용. 병은 추가로 0.3 안쪽에 배치.
- 원본 FBX는 유지하고 병 프리팹에 전용 경량 메시 적용: 6,367 정점 / 7,387 삼각형. 원본 대비 정점 약 98.3% 감소.
- 병 그림자 비활성화, 풀 재사용, 프레임당 생성 제한, 같은 셀의 병 수량 합산.
- 표시 한도에 도달하면 가장 가까운 병에 보상을 합산하며, 경험치 병 수량은 버리지 않음.
- 주변 병만 약 30Hz로 회전. 수거 검색 대상은 최대 표시 개수로 제한.
- 지휘관 경험치 이벤트를 OnBottlesCollected(int count)로 변경해 묶음 지급 및 HUD 한 번 갱신.

## Inspector 조정
BattleExperience > 경험치 병 성능 / 경계:
- Maximum Visible Bottles: 128
- Prewarm Bottles: 32 (여러 프레임에 나눠 준비)
- Creations Per Frame: 4
- Bottle Merge Cell Size: 2
- Bottle Boundary Inset: 0.3
- Bottle Animation Interval: 0.0333
- Bottle Animation Distance: 30

모델 재생성 도구: Army Survivor > Optimize Experience Bottle.
이미 경량 메시가 연결되어 있으면 중복 처리하지 않습니다.

## 검증
- Unity 컴파일/콘솔 오류 없음.
- (1000,0,1000)에 1,000개 드롭 → 도달 가능한 경계 안으로 배치 → 1,000개 모두 수거, 남은 수량 0.
- 분산 위치 5,000개 생성/수거 후 총 지급량 보존 확인.
- 표시 한도 8개로 제한한 1,000개 분산 테스트: 표시 8개 / 총 지급 1,000개 / 묶음 콜백 8회 / 남은 수량 0.
- 경량 병의 게임 화면 확인.
- FPS 자체의 전후 비교 측정은 하지 않았습니다. 위 결과는 정점 수, 생성 예산, 보상 보존 및 기능 검사입니다.

## 변경 파일
- Assets/Scenes/PlayScene.unity
- Assets/Prefabs/Combat/SnakeExperienceBottle.prefab
- Assets/EquipmentData/ExperienceBottle-LowPoly.asset
- Assets/Scripts/Army/RunController.cs
- Assets/Scripts/Army/BattleExperience.cs
- Assets/Scripts/Army/BattleExperience.Drops.cs
- Assets/Scripts/Army/CommanderProgression.cs
- Assets/Scripts/Army/Editor/ExperienceBottleOptimization.cs
