# 사운드 · 환경설정 (2026-11-15)

## 사운드

- `Assets/Resources/SoundLibrary.asset`에 효과음/배경음 파일을 넣으면 바로 재생된다. 비어 있는 항목은 소리 없이 넘어간다.
- 효과음 항목: 파일 여러 개(무작위 재생), 음량, 음높이 범위, 최소 간격(같은 소리 연속 재생 제한).
- 배경음: 씬별 기본 곡(StartScene=Title, VillageScene=Village, PlayScene=Preparation). 전투 시작 시 Battle/Survival, 보스 등장 시 Boss, 보스 처치 후 전투 곡, 전투 종료 시 Preparation으로 바뀐다. 교체할 때 `bgmFadeSeconds` 동안 겹쳐서 넘어간다.
- `GameAudio`는 게임 시작 때 자동으로 생기는 소리 재생기(씬 배치 없음)다. UI 클릭음은 버튼마다 연결하지 않고, 활성 버튼·슬라이더 등을 누르면 공통으로 난다.
- 전투 소리는 PlayScene `Recruitment System`의 `BattleSoundBridge`가 기존 전투 이벤트를 듣고 낸다.

| 효과음 | 나는 때 |
|---|---|
| UiClick | UI 조작 요소 클릭 |
| Hit / EnemyDeath / EliteDeath / BossDeath | 적 피격·처치 |
| EliteAppear / BossAppear | 정예·보스 등장 |
| CommanderHurt / CommanderDeath | 지휘관 피격·사망 |
| ExpPickup / SealPickup | 경험치 병·인장 획득 |
| UnitLevelUp | 병사 레벨 업 |
| WaveStart / SpecialWave | 전투 시작·특수 웨이브 예고 |
| Victory / Defeat / NewRecord | 모험 승리·패배(중도 복귀 제외), 서바이벌 종료(신기록이면 NewRecord) |
| SupplyDraw / SupplyRare | 보급 시작·A등급 이상 획득 |
| BuildingPlace / Recruit | 건물 건설·병사 고용 |

## 환경설정

- 프리팹 `Assets/Prefabs/UI/Settings Canvas.prefab`을 StartScene, VillageScene, PlayScene에 배치했다(정렬 순서 500).
- 항목: 전체 음량/배경음/효과음, 피해 숫자, 튜토리얼 초기화, 해상도, 화면 모드, 수직 동기화, 그래픽 품질, 프레임 제한(VSync를 켜면 잠김).
- 열기: 시작 화면 `설정` 버튼, 마을·전투 준비 중 ESC, 전투 중 ESC → 일시정지 메뉴 → `환경설정`. 열린 창은 ESC로 닫는다. 보급소 화면이 열려 있으면 ESC는 보급소가 먼저 처리한다.
- 게임 중에 열면 시간을 멈추고, 닫으면 되돌린다(시작 화면은 멈추지 않음).
- 값은 PlayerPrefs(`Settings.*`)에 저장한다. 저장 슬롯·새 게임과 무관한 기기 설정이다.
- 기본값과 선택지: `Assets/Resources/GameSettingsProfile.asset`(기본 음량, 해상도 최소 높이, 프레임 선택지, 표시 문구, 그래픽 단계).
- 그래픽 품질: PC 빌드에는 Quality 단계가 하나뿐이어서, 실행 중 URP 에셋 복사본의 렌더 스케일·MSAA·그림자 거리/해상도/단계 수를 바꾼다. 원본 `PC_RPAsset`은 바뀌지 않고, 에디터에서는 플레이를 끝내면 원래 에셋으로 돌아간다. 기본 단계 `높음`은 기존 설정과 같다.
- 해상도·화면 모드는 빌드에서만 적용된다(에디터 Game 뷰는 그대로).
