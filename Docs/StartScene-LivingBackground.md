# StartScene 움직이는 성 배경

StartScene의 배경 그림 및 그림용 그라데이션 오브젝트를 제거하고 ToonyTinyPeople TT_RTS 모델로 성, 성벽, 성문, 망루, 주택, 시장을 배치했습니다. 기존 메뉴 버튼과 저장/설정 UI는 유지합니다. 원본 이미지 파일 자체는 다른 용도에 사용할 수 있도록 보존합니다.

## 에디터 조정

- `Living Castle Background` 아래 건물 Transform으로 배치를 변경합니다. FBX의 원본 X 회전(90도)을 유지해야 건물이 똑바로 섭니다.
- `Citizen 1` ~ `Citizen 15`의 `MenuCitizen`에서 Walk Speed, Pause Seconds, Turn Speed를 조정합니다.
- `West/East promenade`, `West/East market lane`, `Castle visitors` 아래 Waypoint를 옮기면 동선이 바뀝니다. 주민을 선택하면 경로가 Gizmo로 보입니다. NavMesh가 아닌 지정 경로이므로 경로를 건물 밖으로 배치해야 합니다.
- 주민은 TT_Peasant 프리팹과 원본 humanoid 걷기/대기 클립을 사용합니다. 물리 충돌 및 전투/병력/저장 시스템에 등록하지 않습니다.
- `Main Camera`의 위치와 Field of View, `Afternoon Sun`의 강도와 색상으로 구도를 조절합니다.
- 생성된 공용 재질과 주민 AnimatorController는 `Assets/Art/StartScene`에 있습니다.

## 재생성

StartScene을 Edit Mode로 열고 `Army Survivor > Start Scene > Build Living Castle Background`를 실행합니다. **기존 Living Castle Background 아래 수동 편집을 초기화하고 씬을 저장하므로**, 수동 배치를 유지할 때는 재실행하지 않습니다. 재생성 스크립트는 Editor 전용이며 빌드 실행 시에는 저장된 씬과 MenuCitizen만 사용합니다.

## 주변 경관

- `Living Castle Background/Surrounding Landscape`에 Kenney hexagon-kit의 unit-tree, stone-rocks, grass-hill, stone-mountain을 배치합니다.
- 산맥은 성 뒤 원경, 숲은 뒤쪽과 마을 양옆, 바위는 외곽에 배치하며 주민 동선을 비웁니다.
- `Assets/Art/StartScene`의 Forest, Hills, Mountain Stone 재질은 이 배경 전용입니다. 원본 에셋 재질은 변경하지 않습니다.
- `Army Survivor > Start Scene > Decorate Castle Surroundings`로 주변 경관만 재생성할 수 있습니다. 해당 그룹의 수동 편집은 초기화되며, 건물·주민·카메라·UI는 유지합니다.
- 전체 배경 재생성 시에도 주변 경관이 포함됩니다.

## 간결한 시작 메뉴

- 기존 금색 테두리 버튼 그림을 제거하고 왼쪽 아래 텍스트 메뉴로 교체했습니다. 버튼 이벤트/StartMenu 참조를 유지해 서바이벌, 모험, 이어하기, 설정, 종료 동작을 보존합니다.
- `Menu Dark Gradient`는 menu-dark-gradient 스프라이트를 사용하고 Raycast Target을 끕니다.
- 각 버튼의 `MenuButtonFeedback`에서 일반/강조 색, 이동 거리, 전환 속도를 변경할 수 있습니다. 마우스 호버와 키보드 선택 시 금색 표시 및 텍스트 이동이 나타납니다.
- `Army Survivor > Start Scene > Apply Minimal Menu Layout`은 메뉴 배치와 오른쪽 마을 구도를 다시 적용합니다. 카메라 구도를 변경해 건물과 주민의 상대 위치 및 동선을 보존합니다.

## 동작 검증

- Play Mode에서 주민 이동, 휴식, 걷기 Animator 재생 확인.
- 시간 차를 두고 동일 주민이 실제로 이동하는 것 확인.
- 설정 창 열기/닫기 및 기존 메뉴 버튼 활성 상태 확인.
- 컴파일 및 Play Mode 콘솔 오류 없음.


