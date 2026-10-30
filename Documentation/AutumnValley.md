# Autumn Valley / Thung lũng lá đỏ

Open Assets/Scenes/AutumnValley.unity and press Play, then NHẬN HỢP ĐỒNG.

- Editable Unity Terrain: 144 x 144 metres, heightmap 513 x 513.
- Four paint layers: meadow, dirt trail, rock and fallen leaves.
- Safe south camp; main west trail and east flank both cross the shallow stream on timber bridges.
- North outpost is marked by a stone gateway and red flags.
- Five Monster4 guards plus the existing Monster1 laser contract target.
- Contract: defeat the laser target and return to the south camp extraction disc. Reward: 300 credits.
- Current Female FPS player, weapons, reload animation and HUD are retained.
- Scene is enabled at build index 1, after Scene1; retry reloads this map.
- Original Scene1 and the user's existing Scene2 edits are preserved.
- Props use the existing Palmov low-poly pack. The pack must remain installed under Assets/DL.

Editing:
- Sculpt/Paint AutumnValley_Map/AutumnValley_Terrain.
- Move trees, stones, tents and bridge pieces as regular GameObjects.
- After collision/layout edits, bake AutumnValley_Map's NavMeshSurface again using Unity's Bake control.
- The Editor builder (GunQuest > Maps > Create Autumn Valley) refuses to overwrite an existing map.
- No runtime terrain generation. PaintTerrain is an explicit authoring helper and replaces the paint mask when invoked; do not call it after hand painting.

Verified in Unity 6000.3.7f1 on 2026-10-23:
- Scene save/reopen retains terrain paint and navigation. No missing scripts.
- Complete NavMesh paths from spawn to both bridges and north contract target.
- All six enemy NavMeshAgents start on the baked surface.
- CharacterController traversed 22.5m across each bridge with no fall-through.
- A guard physically traversed the east bridge (22m).
- Initial east walk test met the guard parked at the exit; isolated collision checks passed for both bridges.
- Contract starts in Hunting; target death activates extraction; ReturnToBoard reloads AutumnValley correctly.
- Test did not award credits or change saved progress. Test-only movement, AI and collider changes were discarded on exit from Play Mode.
