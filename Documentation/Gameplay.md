# GunQuest - First playable contract

Open Scene1 and enter Play Mode. The contract board appears before combat starts.

## Play loop

1. Accept "Don sach khu trai".
2. Hunt Monster1, the existing laser enemy. Other spawned enemies remain threats.
3. Return to the green extraction marker at PlayerSpawn and press E within 2.5 metres.
4. Receive 200 credits, saved once on successful extraction. Death pays nothing.
5. Use the contract board to buy damage (+10% per level) or magazine upgrades (+2 pistol / +5 rifle rounds per level). Five levels each; each purchase costs 100 times the next level.

## Controls

WASD move; mouse look; left mouse attack; F/right mouse quick melee; Space jump; Shift sprint; Ctrl crouch; 1-4 equip; R reload; E extract; Esc pause/resume. Pause also offers restart and return to the contract board. Quit exits the build or stops Play Mode in the Editor.

## Ammunition

Pistol: 12 + 60 reserve, 1.4 second reload. Rifle: 30 + 120 reserve, 2 second reload. Empty magazines cannot shoot and initiate reload if reserves remain. Switching weapons cancels reload without transferring rounds. Ammo stays separate per slot. Pause freezes reload. Melee weapons use no ammunition.

## Persistence and compatibility

Credits, damage level, magazine level and completion count are stored as JSON in PlayerPrefs under GunQuest.ContractProgress.v1. Current combat state/ammo is not saved. Retry starts a fresh contract and retains purchased upgrades.

Scene1_BeforeContracts_20261006.unity is the scene backup. Terrain and the existing Monster4 spawner are preserved. The legacy HUD is disabled at runtime; ContractUI supplies the objective, health, ammo and weapon slots. PlayerCameraFollow now places the camera at eye height and creates view meshes from the existing weapon art. Imported art/prefabs are not edited.

## Verification performed in Unity

- Progress purchase: insufficient funds rejected, correct credit deduction, level cap, JSON roundtrip.
- Contract board freezes combat and blocks attacks.
- Shot consumes one round; same-frame second shot is rejected by cooldown.
- Reload is blocked while reloading and freezes during pause; after resume transfers only the needed rounds from reserves.
- Weapon switch cancels reload and retains per-slot ammo.
- Enemy bullet Rigidbody is created without MissingComponentException.
- Target death activates extraction; early/out-of-zone extraction is rejected.
- Successful extraction pays once and persists completion/credits across scene reload.
- Upgrade purchases persist and produce 14-round pistol / 35-round rifle at magazine level 1.
- Dead player cannot shoot or melee; Game Over freezes combat and awards nothing.
- Visual review: Vietnamese contract board, first-person weapon and gameplay HUD.

Test-created credits/upgrades were restored to the original save after verification.

- Final Retry verification passed: contract resumed, target restored, player alive and magazine reset. Pause displayed all four actions (resume, retry, board, quit). Final Console error count was zero.

UI input regression: saved EventSystem now uses ContractUI.inputactions with Point/Click references; runtime repairs missing bindings. Verified synthetic mouse press/release through InputSystemUIInputModule started the contract without invoking Button.onClick directly. Contract UI now uses a 2048px SDF TextMesh Pro font with Vietnamese glyphs; visual review passed.
