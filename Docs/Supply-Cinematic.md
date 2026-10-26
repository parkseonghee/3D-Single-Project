# 왕국 보급소 뽑기 연출

1회/10회 보급 성공 시 기존 `EquipmentInventory.TryDraw`에서 결과와 비용을 한 번 확정한 뒤 `SupplyDrawSequence`를 재생한다. 10회도 연출은 한 번이다. 확률/보장 규칙은 기존 그대로다.

## 기본 연출
- 마을 `Wall_A_gate` 앞 정면에서 시작하여 4.5초 동안 카메라 접근.
- 좌우에 `CFXR4 Firework HDR Shoot Single (Random Color)`를 0.15초마다 두 개씩 생성. 크기 1.5배.
- 전체 시간의 50%부터 1.4초간 기존 `wall_A_open` 클립으로 문 열기.
- 종료 후 원래 마을 카메라와 UI, 성문 자세 복원 후 획득 결과 표시.
- ESC로 건너뛰기. 티켓 추가 차감/재추첨 없이 이미 확정한 결과 표시.
- 연출 중 건설/카메라 드래그/줌 입력 차단. UI는 CanvasGroup으로 숨기고 기존 스크립트는 유지.
- 비활성화/씬 종료 시 임시 카메라와 폭죽 제거, 원래 상태 복원.

## Inspector 조정
`Assets/Resources/EquipmentCatalog.asset`의 **보급 연출**:
- Supply Firework / Supply Gate Open: 연결 완료. 다른 이펙트/클립으로 교체 가능.
- Supply Sequence Duration: 전체 길이.
- Supply Firework Interval / Scale: 폭죽 빈도/크기.
- Supply Camera Start / End / Look At: 성문 위치 기준 월드 방향 오프셋. 현재 마을 정면은 -Z.
- Supply Camera Fov: 시야각.
- Supply Door Start: 전체 시간 대비 문이 열리기 시작하는 비율.
- Supply Door Duration: 문 열기 시간. 전체 길이보다 늦게 끝나지 않도록 조정.

씬/프리팹 수동 연결은 필요 없다. VillageScene 내 Bone_Base 자식이 있는 Wall_A_gate를 찾아 사용한다. 카메라/성문/리소스가 없다면 경고 후 기존 결과 UI를 즉시 표시한다.

## 검증
Unity 컴파일 성공. `Army Survivor > Equipment > Validate Supply Cinematic`에서 프리팹 참조, 애니메이션 바인딩 3개 및 문 회전 96도를 확인했다.
실제 플레이에서 1회/10회, ESC 건너뛰기, 연속 뽑기, 종료 후 카메라 드래그/줌과 UI 복귀를 확인할 것. 화면 구도와 폭죽 밀도는 아직 플레이 화면으로 검수하지 않았다.
