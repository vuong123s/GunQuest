# Camping Terrain

Scene1 uses a 513 x 513 Unity Terrain sampled from the original `land camping forest` mesh. Trees, rocks, tents, river water and bridge remain original objects.

- Select `PalmovMap_CampingOutpost/CampingTerrain` to sculpt or paint in the Terrain Inspector.
- `OriginalGround` preserves the original mesh UV colors as a map-sized texture. Grass, Dirt and Rock layers are available for subsequent painting.
- `CampingCliffs` under the original ground retains vertical faces and undersides. Editing terrain near these faces may require adjusting the cliff mesh as well.
- The original ground renderer and colliders are disabled as prefab overrides. Imported vendor assets are unchanged.
- `Scene1_BeforeTerrain_20261006.unity` is the pre-conversion backup, including the existing monster/spawner changes.
- After sculpting, rebuild the NavMesh on `PalmovMap_CampingOutpost` before testing enemies.
- The converter menu is `GunQuest > Terrain > Convert Camping Ground`. It refuses to overwrite an existing terrain; it is not used at runtime.

BridgeTerrainApproaches provides invisible collision ramps at both bridge entries to prevent catching on the first/last plank.


## Verification

- Height comparison with the original mesh: 7,569 samples; mean error 0.00037 m, maximum 0.0678 m; no sample above 0.2 m.
- CharacterController probe crossed the bridge in both directions after adding approach ramps.
- In Play Mode, a NavMeshAgent traversed from z=2 to z=21 across the bridge, ending with PathComplete and zero remaining distance.
- Player was grounded; Monster1 and the three spawned Monster4 agents were on the NavMesh.
- Scene was saved, reopened and checked for Terrain/TerrainCollider persistence.
- Existing unrelated runtime issue: Bullet.Awake uses null-coalescing on a Unity Rigidbody reference and can throw MissingComponentException for EnemyBullet. Terrain conversion does not modify combat code.
