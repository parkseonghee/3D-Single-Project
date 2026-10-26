# Meshy 에셋 위치

2026-10-26: Assets 최상위에 흩어져 있던 Meshy 폴더 6개를 `Assets/Art/Meshy`로 이동했다.

| 분류 / 폴더 | 내용 |
|---|---|
| Characters/CrystalGolem | 크리스털 골렘 보스 모델, 애니메이션, 컨트롤러, 머티리얼 |
| Projectiles/BlazingComet | 마법사 혜성 투사체 모델 |
| Projectiles/SimpleArrow | 궁수 화살 모델 |
| Effects/RunicCircle | 소환 마법진 모델 |
| Props/ExperiencePotion | 경험치 포션 모델 |
| Props/InfernoSeal | 몬스터 드롭 뽑기권 인장 모델 |
| Weapons/OrbitingSword | 회전검 모델 |
| Weapons/AetherstormBlade | A급 에테르스톰 블레이드 |
| Weapons/OrnateMagicWand | A급 화려한 마법 지팡이 |
| Weapons/VoidShield | A급 공허의 방패 |
| Weapons/FrostbiteDagger | S급 프로스트바이트 단검 |

Unity AssetDatabase.MoveAsset으로 폴더와 .meta를 함께 이동했다. FBX/텍스처/머티리얼의 내부 이름과 하위 구조는 유지한다. FBX에서 추출한 .fbm 폴더도 유지하여 재임포트 시 원본 텍스처 경로가 달라지지 않게 한다. 실제 게임용 프리팹은 기존 `Assets/Prefabs` 위치를 유지한다.

검증: 기존 65개 에셋/폴더의 GUID 동일, 파일 49개의 SHA-256 동일. 게임 프리팹 30개와 PlayScene의 재귀 의존성 GUID 집합도 이동 전후 동일. Unity 콘솔 오류 없음. 기존 경로를 직접 사용하는 런타임/에디터 코드는 검색되지 않았으며 문서의 경로를 갱신했다.
