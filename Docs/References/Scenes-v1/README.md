# 씬 AI 레퍼런스 v1

생성일: 2026-09-10. 생성 도구: 내장 image_gen.

코드 구현 전 시각 방향 탐색용 이미지 5장이다. 실제 Unity 렌더나 보유 에셋의 정확한 재현이 아니다. 제목·영문 UI·수치·병력 수는 임시 표현이다. 시작/결과 화면은 연출용 구도이며 실제 플레이 카메라는 GameScene의 높은 사선 시점을 참고한다. BossScene의 목재·돌 자원, 추가 문구·미니맵은 AI가 추가한 표현으로 기획에 채택한 기능이 아니다. 실제 재화는 기획서를 따른다. 보스전과 결과 화면은 별도 Unity Scene이 아니라 전투 변형·UI로 구현할 수도 있다.

| 파일 | 목적 |
|---|---|
| 01-StartScene.png | 타이틀·메뉴 |
| 02-VillageScene.png | 채취·건설·편성·출전 |
| 03-GameScene.png | 일반 전투·진형·클래스 성장 HUD |
| 04-BossScene.png | 보스 크기·공격 예고·전투 분위기 |
| 05-ResultScene.png | 클리어 보상·마을 귀환 |

## 생성 프롬프트

### start

Use case: stylized-concept. Create one polished 16:9 landscape game screen reference for a Unity 3D isometric army survivor game, 100 day village-to-battle campaign. Cohesive style: cute chunky low-poly medieval RTS miniature soldiers, large heads, simple faceted meshes, matte flat colors, blue allied tabards, orange tile roofs, green grass, orthographic isometric camera. Feasible indie game screenshot aesthetic, not painterly splash art or photorealism. Crisp restrained game UI with navy translucent panels, warm ivory lettering, gold accents. No mobile controls, no watermarks. START SCENE: inviting small fortified village and a commander with sword soldiers, archers and a mounted knight standing on a winding path in the right two thirds. Rice paddies, barracks, mage tower in background. Morning light, readable silhouettes. Left third has elegant uncluttered title menu, title text '100 DAYS', subtitle 'ARMY SURVIVOR', buttons 'NEW CAMPAIGN', 'CONTINUE', 'SETTINGS', 'QUIT'. Strong calm composition, finished PC game main menu. Single screen, no collage.

### village

Use case: stylized-concept. Create one polished 16:9 landscape game screen reference for a Unity 3D isometric army survivor game, 100 day village-to-battle campaign. Cohesive style: cute chunky low-poly medieval RTS miniature soldiers, large heads, simple faceted meshes, matte flat colors, blue allied tabards, orange tile roofs, green grass, orthographic isometric camera. Feasible indie game screenshot aesthetic, not painterly splash art or photorealism. Crisp restrained game UI with navy translucent panels, warm ivory lettering, gold accents. No mobile controls, no watermarks. VILLAGE SCENE: strictly high orthographic isometric view, no horizon. Compact playable village with orange-roof barracks, archery range, small purple mage tower, stable, smithy, four rice paddy plots, paths and tiny blue soldiers. Clear buildable ground spaces. Top slim resource HUD 'DAY 3', rice icon '320', gold '180'. Left compact panel 'HARVEST' with '25 RICE' and '4 LEFT'. Bottom troop roster portraits sword soldier, archer, shield soldier with counts, 'FORMATION 5 / 5'. Bottom right large 'DEPLOY' button. Village takes 75 percent of screen. Warm daytime. Single cohesive screen, no collage.

### game

Use case: stylized-concept. Create one polished 16:9 landscape game screen reference for a Unity 3D isometric army survivor game, 100 day village-to-battle campaign. Cohesive style: cute chunky low-poly medieval RTS miniature soldiers, large heads, simple faceted meshes, matte flat colors, blue allied tabards, orange tile roofs, green grass, orthographic isometric camera. Feasible indie game screenshot aesthetic, not painterly splash art or photorealism. Crisp restrained game UI with navy translucent panels, warm ivory lettering, gold accents. No mobile controls, no watermarks. GAME SCENE: strictly high orthographic isometric gameplay view, no horizon. Broad simple grassy battlefield, a few edge rocks and trees, central commander with small gold selection ring, five sword soldiers and two archers in a clear protective formation. Dozens of small green goblins approaching from outer edges, open navigable space, arrows and two spinning blades, readable controlled combat effects. Tiny glowing cyan experience bottles on ground. Top slim HUD 'DAY 4', '01:12', commander health. Bottom compact CLASS-SHARED growth panels labeled 'SOLDIER Lv.3', 'ARCHER Lv.2', 'SHIELD Lv.1', bottle bank '12'. No manual skill hotbar. Visible gameplay area over 80 percent. Feasible simple low-poly Unity scene. Single screen no collage.

### boss

Use case: stylized-concept. Create one polished 16:9 landscape game screen reference for a Unity 3D isometric army survivor game, 100 day village-to-battle campaign. Cohesive style: cute chunky low-poly medieval RTS miniature soldiers, large heads, simple faceted meshes, matte flat colors, blue allied tabards, orange tile roofs, green grass, orthographic isometric camera. Feasible indie game screenshot aesthetic, not painterly splash art or photorealism. Crisp restrained game UI with navy translucent panels, warm ivory lettering, gold accents. No mobile controls, no watermarks. BOSS SCENE: strictly high orthographic isometric gameplay view, no horizon. DAY 10 battle at ruined stone village gate on earthy grassy ground. Oversized chunky green orc chief with stone hammer at upper center, clear red circular ground attack telegraph, commander with gold ring and blue squad maneuvering OUTSIDE telegraph below, shield soldier front, archers and mage rear. Scattered green goblin minions. Short readable purple magic bursts, low-poly scenery, no gore. Top boss health bar text 'ORC CHIEF', small 'DAY 10'. Bottom class growth roster and experience bottle counter. Dusk warm light with readable terrain, game screenshot not dramatic closeup. Single screen no collage.

### result

Use case: stylized-concept. Create one polished 16:9 landscape game screen reference for a Unity 3D isometric army survivor game, 100 day village-to-battle campaign. Cohesive style: cute chunky low-poly medieval RTS miniature soldiers, large heads, simple faceted meshes, matte flat colors, blue allied tabards, orange tile roofs, green grass, orthographic isometric camera. Feasible indie game screenshot aesthetic, not painterly splash art or photorealism. Crisp restrained game UI with navy translucent panels, warm ivory lettering, gold accents. No mobile controls, no watermarks. RESULT SCENE: campaign daily stage victory screen, no ending yet. High orthographic isometric view of blue commander and surviving sword soldiers and archers returning to orange-roof village gate at warm sunset. Scene visible behind restrained central navy-and-gold result panel. Text 'DAY 10 CLEAR', 'BOSS DEFEATED'. Three clear reward icons and labels 'GOLD +290', 'RICE +150', 'RELIC +1'. Smaller line 'NEXT: DAY 11'. Main button 'RETURN TO VILLAGE'. Victory hopeful but modest, no loot boxes, no stars, no mobile UI. Use same chunky miniature low-poly medieval aesthetic. Single screen no collage.

