# 전투 HUD

- 상단 중앙: DAY, 남은 생존 시간, 지휘관 체력바와 현재/최대 HP.
- 하단: 실제 출전 병력 수만큼 생성하는 초상화 슬롯. Lv.N, HP 바, 그 아래 금색 EXP 바, 1~5 키캡을 표시한다. +100 버튼은 표시하지 않는다.
- 하단 오른쪽: 보유 경험치 병.
- 전술 3개는 작은 버튼으로 카드 위에 유지. Z/X/C 단축키 유지.
- 우측 상단 II 버튼: 일시정지. 계속하기 또는 준비로 돌아가기 선택.
- 자원 HUD는 준비 중 표시하고 전투 중 숨김. 카메라와 전투 지형은 변경하지 않음.
- 고정 문구와 위치는 PlayScene에서 수정 가능. BattleHUD는 지휘관 체력 숫자/게이지와 일시정지만 갱신.

## 초상화 슬롯 (2026-10-11)

- 기존 uGUI 유지. 슬롯 폭 120으로 이전 카드 폭 274의 절반 이하이며 공간이 부족하면 가로 스크롤한다.
- 슬롯 클릭 또는 1~5 키로 선택한다. 선택 슬롯은 1.15배 확대되고 원형 테두리가 표시된다. 6번째 이후는 클릭으로 선택한다.
- Ctrl+1~5는 해당 번호 병사에게 경험치 병 1개(100 EXP)를 직접 투자한다. 왼쪽/오른쪽 Ctrl을 모두 지원하며 누를 때마다 1개만 사용한다. 사망자·없는 슬롯·병 부족·일시정지·강화 선택 중에는 사용하지 않는다.
- 선택 후 오른쪽 경험치 병 아이콘을 클릭하면 해당 병력에게 병 1개(100 EXP)를 먹인다. 이 선택은 투자 대상 선택이며 직접 조작 유닛을 바꾸지 않는다.
- HP 30% 이하 빨강, 사망 시 회색 초상화와 알파 0.4를 적용한다. 선택 대상 사망 시 다음 생존자를 선택한다.
- HP/레벨/EXP는 OnHpChanged/UnitLeveledUp/UnitExperienceChanged 이벤트로 갱신한다. 매 프레임 상태 폴링하지 않는다. EXP는 현재 경험치/다음 레벨 요구량 비율이며 레벨업 후 잔여 경험치를 표시한다.
- 병종별 256×256 RenderTexture로 1회 촬영해 Texture2D를 공유한다. 촬영 카메라/모델은 즉시 제거하고, 캐시는 HUD 종료 시 해제한다.

## 구현 파일과 연결

- Assets/Scripts/Army/OperatorHud.cs: 동적 슬롯, 선택/투자, 이벤트 구독.
- Assets/Scripts/Army/UnitPortraitCache.cs: 전용 레이어/카메라, 초상화 캐시, 회색 텍스처.
- Assets/Scripts/Army/PortraitSelectionRing.cs: 원형 테두리.
- Assets/Scripts/Army/UnitHealth.cs: HP 변경 이벤트.
- Assets/Scripts/Army/BattleExperience.cs: 편성/레벨 바인딩과 기존 카드 코드 제거.
- Assets/Scenes/PlayScene.unity: 기존 텍스트 카드 제거 및 Operator Slots 연결.
- ProjectSettings/TagManager.asset: UnitPortrait 레이어.

현재 PlayScene은 참조와 레이어가 연결되어 있어 추가 수동 연결은 없다. 카메라는 자동 생성되므로 별도 배치하지 않는다.

새 병종은 기존 모집 시스템의 UnitDefinition.prefab을 등록하면 자동 촬영한다. HEAD_CONTAINER/Head를 우선 사용하고 없는 경우 Renderer bounds를 사용한다. 크기/전방 축이 다른 외부 모델은 촬영 구도 조정이 필요할 수 있다.

슬롯 폭은 OperatorHud의 Slot Width, 공간은 Experience Panel / Operator Slots의 RectTransform에서 조절한다. 촬영 구도는 UnitPortraitCache의 halfHeight, focus, capture.transform.position에서 조절한다. 다른 씬에 복사할 때는 Experience, Run, Slot Area, Font, Bottle Button 참조 및 UnitPortrait 레이어를 유지한다.

## 실행 검증

6명/5개 병종으로 슬롯 6개, 촬영 5회, 동일 병종 Texture 공유를 확인했다. HP 25% 빨간 바, 사망 알파 0.4 및 회색 텍스처, 선택 배율 1.15, 병 3개 투자 후 Lv.2와 강화 카드, 선택 대상 사망 시 생존자 자동 선택을 확인했다. 추가 촬영 없이 캐시가 유지됐고 Unity 오류는 없었다. 테스트 병력은 Play Mode 종료로 제거했다.
