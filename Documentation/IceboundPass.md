# Icebound Pass / Đèo Băng Giá

Open Assets/Scenes/IceboundPass.unity, press Play, then NHẬN HỢP ĐỒNG.

- Editable snow Terrain: 160 x 160 metres; heightmap 513 x 513.
- Four paint layers: powder snow, packed trail, frost rock and glacier.
- Frozen lake has a continuous static MeshCollider, with Terrain beneath it as support.
- Frost veins are decorative lines, not holes. Normal movement is retained on ice.
- Two approaches: a direct crossing of the frozen lake and an east shoreline/mountain trail.
- Start in the south winter village. The laser target is at the north blue-flag outpost.
- Six guards plus one laser target; return to the starting village extraction disc after the target dies.
- Contract reward: 350 credits. Existing Female FPS player, weapons, reload animations and HUD retained.
- New scene enabled at build index 2; Scene1 and AutumnValley remain available.
- Original Palmov winter prefabs/materials retained; Assets/DL pack must remain installed.
- Winter houses are closed exterior scenery with solid colliders.
- Trees use narrow trunk colliders instead of foliage-sized boxes.

Editing:
Sculpt/Paint IceboundPass_Map/IceboundPass_Terrain. Props and ice structures remain ordinary GameObjects.
Bake the map root NavMeshSurface after collision/layout changes.
GunQuest > Maps > Create Icebound Pass is a one-time Editor builder and refuses to overwrite an existing scene/map folder.
PaintTerrain is an explicit authoring helper that replaces the paint mask; avoid it after manual paint edits.
No runtime map generation.

Verified in Unity 6000.3.7f1 on 2026-10-23:
- Scene save/reopen retains terrain paint and NavMesh; no missing scripts.
- Complete navigation paths to frozen lake, east shore and target.
- All 7 enemy agents start on the baked NavMesh.
- CharacterController traverses 60m across both lake rims in both directions without falling.
- Full east shoreline route physically traversed from spawn to target using the CharacterController.
- Enemy agent physically crosses the frozen lake (58m).
- Gameplay contract enters Hunting; target death activates extraction without changing saved credits.
- Temporary test movement/AI/collider changes are discarded with the test scene reload and exit from Play Mode.
