# 왕국 보급소 UI / DoorPivot 연출

## 적용 내용
- 기존 uGUI/TMP와 EquipmentInventory, ItemInspectView, 보급 테스트 환경을 유지했습니다.
- Figma의 무기고 배경, 장비 목록, 우측 상세, 보장 안내, 1회/10회 버튼, 확률 팝업을 반영했습니다.
- 무기 모델과 회전/휠 관찰, InfernoSealIcon은 기존 에셋을 사용합니다. 목록 썸네일은 실제 무기 모델을 최초 한 번 렌더링해 캐싱합니다.
- 보유 장비 버튼은 목록을 보유 장비로 필터링하며 다시 누르면 전체 목록으로 돌아옵니다.
- 선택 장비는 관찰 대상일 뿐 뽑기 결과/확률에 영향을 주지 않습니다.
- 인장 부족 시 버튼을 비활성화하고 부족 수량을 표시합니다.
- 1920x1080 기준 UI를 화면에 맞춰 축소해 다른 화면비에서도 잘리지 않게 했습니다.

## 문 설정
- VillageScene의 Wall_A_gate에 연결된 SupplySlidingGate가 DoorPivot과 DoorPivot (1)을 회전시킵니다.
- 문 메시의 로컬 위치/회전과 피벗 위치는 그대로 유지합니다.
- Open Angle A / Open Angle B: 로컬 Y 회전 각도 (기본 +90 / -90).
- EquipmentCatalog의 Supply Door Start, Supply Door Duration으로 열리는 시점과 속도를 조정합니다.
- 시작 직전 피벗 자세를 닫힘 상태로 저장하고, 종료/ESC/컴포넌트 비활성화 시 복원합니다.
- 같은 이름의 성문이 중복되어도 두 피벗 참조가 연결된 컨트롤러만 사용합니다.
- 피벗을 다시 연결하거나 회전 클립을 갱신하려면 Edit Mode에서 `Army Survivor > Equipment > Bind Door Pivots` 실행.
- `Validate Supply Cinematic`은 위치 불변 및 로컬 Y 회전을 검사합니다.

## 주요 파일
- Assets/Scripts/Equipment/EquipmentUI.cs: 기존 동작 및 UI 진입 연결
- Assets/Scripts/Equipment/EquipmentUI.Design.cs: Figma 레이아웃, 상태/팝업, 썸네일
- Assets/Scripts/Equipment/SupplyUIFit.cs: 화면비 대응
- Assets/Scripts/Equipment/SupplyUIGradient.cs: 가독성 음영
- Assets/Resources/SupplyDesign/: Figma 원본 배경, SVG와 Unity용 PNG
- Assets/Scripts/Equipment/SupplyDrawSequence.cs: 피벗 회전 및 복원
- Assets/Scripts/Equipment/SupplySlidingGate.cs: 피벗 참조/열림 각도
- Assets/Scripts/Equipment/Editor/SupplyCinematicValidation.cs: 연결/검사 도구
- Assets/EquipmentData/Wall_A_door_A-RotateOpen.anim, Wall_A_door_B-RotateOpen.anim
- Assets/Scenes/VillageScene.unity

## 에디터 테스트
1. Army Survivor > Equipment > Open Draw Test Window
2. VillageScene에서 테스트 시작
3. 1회/10회 보급, 0개 인장, 확률 안내, 보유 장비 필터, 3D 관찰 확인
4. 테스트 종료 시 실제 장비/인장 상태로 복원됩니다. 테스트 상태는 저장에서 제외됩니다.

## 디자인 출처
Figma 파일 5y3wmvZRoqnpHUDZNfixww: 11:5467, 11:5850, 11:6085의 디자인 컨텍스트/화면을 사용했습니다.
11:5657 전체 컨텍스트는 서버 시간 초과 후 Starter MCP 한도에 도달했습니다. 인장 부족 상태는 동일하게 제공된 11:6085 상태 보드의 규격과 실제 재화 값으로 구현했습니다.

## 검증 결과
- Unity 컴파일 오류 없음.
- 피벗 회전 클립 검사 통과: 위치 불변 / Y 회전.
- 실제 10회 보급: 문 회전 확인, 원래 피벗 자세 복원, 100 → 90개 인장, 결과 UI 표시.
- 0개 인장: 뽑기 차단 및 부족 수량 표시.
- 보유 장비 필터: 활성 행 수와 실제 보유 종류 수 일치.
- 실제 무기 썸네일/3D 관찰, 기존 인장, 확률 팝업 화면 확인.
- 테스트 종료 후 원래 인벤토리 상태 복원.
