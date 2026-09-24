# PlayScene 맵

- Kenney Hexagon Kit 타일 577개를 씬에 직접 배치했습니다.
- `Battle Hex Map/Battlefield Tiles`: 평평한 전투 초원.
- `Battle Hex Map/Forest Mountains and Water`: 외곽 숲, 산, 해안과 물.
- 이동 영역은 64×64이며 기존 Ground 콜라이더 참조를 유지합니다. 장식 영역은 이동 영역 밖입니다.
- 준비 화면 카메라와 전투 카메라 배율, 지휘관 추적 방식은 유지합니다.
- 맵 생성용 런타임 코드는 추가하지 않았습니다. Hierarchy에서 타일을 직접 편집할 수 있습니다.
- Play 모드에서 Run 시작, 확장된 이동 경계, 카메라 추적 및 준비 복귀를 확인했습니다.
