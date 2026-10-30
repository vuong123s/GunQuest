# Female Player animation

World-body controller: Assets/Animations/Female/FemalePlayer.controller
FPS controller: Assets/Resources/FemaleFirstPerson.controller
Playable prefab: Assets/Prefabs/Player/PlayerFemale.prefab
Visual source: Assets/Prefabs/Player/DefaultFemalePBR.prefab (Body02/Head02)

The conversion keeps the Player gameplay root and mission references, disables the old bird visuals/Animator, installs the Female Humanoid, and moves weapon models to a right-hand socket. The previous Player prefab remains unchanged. A dated scene backup is saved before replacement.

## World-body controller

- Base Layer: HG / AR directional locomotion, jump start-air-land, hits, dizzy, knockdown-recovery, get up, dash and roll in four directions, power-up, victory, death.
- Upper Body: masked handgun/rifle shots and reload while the legs can continue moving.
- Clip Library - Preview Only: all source clips, including root-motion versions and aim offsets, available for manual preview. This sub-state machine is deliberately not entered by gameplay.
- IsRifle selects rifle versus handgun stance, updated on weapon equip. IsDead blocks action transitions and disables the upper-body override.
- MoveX/MoveY preserve forward/back/strafe input; MoveRate scales locomotion for crouch/sprint.
- Source FBX assets are unchanged. Copies have explicit looping/root baking and no imported demo animation events.
- Existing gameplay automatically drives locomotion, jump, hit, dizzy, death, weapon switching, shooting and reload.
- FemaleAnimationDriver.PlayAction(trigger) exposes the remaining optional animations: Dash, DashBack, DashLeft, DashRight, Roll, RollBack, RollLeft, RollRight, Hit2, Knockdown, GetUp, PowerUp, Victory. These are animation previews/hooks, not new movement or combat abilities.
- The pack contains no crouch or melee attack clip. Existing crouch collider and melee damage remain available. The FPS rig adds a procedural melee swing; this does not provide a matching world-body melee clip.

## Editor tools

GunQuest > Female > 1 - Build Animator rebuilds the generated world-body controller/clips.
GunQuest > Female > 2 - Replace Scene Player installs the generated Female into the current scene.
Replacement refuses if Female is already installed, preventing duplicate rigs.

GunQuest > Female > 3 - Build First Person Meshes regenerates the skinned FPS arm meshes and independent FPS Animator in Assets/Resources. Generation runs only in Editor. The source meshes and animation clips remain unchanged. Adjust Animated Arms Offset on PlayerCameraFollow for framing; saved offsets are treated as a trim relative to the previous default.

## First-person presentation (current)

The camera hides the world body while retaining its shadow. AnimatedFirstPersonRig creates a separate camera-local Female render rig. FemaleFirstPersonBody01/Body02 retain the original skinned elbows, forearms, hands and fingers, with the original bind poses, materials and elbow/wrist geometry. Vertices substantially weighted to chest/clavicle are excluded; tiny residual non-arm weights are removed and normalized within the retained arm. The upper-arm crop excludes the oversized shoulder armor; its open boundary stays below the camera frame. The old generated sleeve-tube assets are retained for recovery and are no longer used.

The FPS rig has its own event-free Humanoid Animator using Assets/Resources/FemaleFirstPerson.controller. ReadyHG/ReadyAR, ShootHG/ShootAR and ReloadHG/ReloadAR use embedded copies of the existing BattleRoyaleDuoPAPBR clips. These clips are adapted through camera-space positioning and two-bone arm IK; they are not newly authored FPS keyframe animations. Automatic Animator updates are disabled. The rig evaluates Animator.Update once per playing LateUpdate before solving the shoulders, elbows and wrists; zero-time evaluations initialize a stance on weapon selection. It no longer copies bone poses from the world-body Animator each frame.

Native PBR weapon grip offsets from the same pack are reused with the retained Polyart gun art. Each stance has a support-hand target, elbow hints and a gradual release of the support grip during reload. Additional camera-local motion provides recoil, mouse sway, breathing/movement bob, sprint lowering, landing response, equip lowering and a procedural melee swing. Holding the right mouse button blends toward a visual aiming pose; it does not introduce a separate accuracy, zoom or damage rule.

PlayerWeapons remains the only authority for ammo, damage and reload completion. The existing reload durations remain handgun 1.4s and rifle 2s; FPS reload state speeds are configured to match, with ReloadRate defaulting to 1. Weapon changes cancel the reload and restore the corresponding Ready state. The render rig contains no independent ammo timer or gameplay animation events; its cloned gameplay scripts, physics and effects are disabled or removed.

The source character keeps its chibi proportions, so the skinned arms retain that stylized anatomy. Gun art does not contain separate moving magazine/slide parts; detachable-magazine or slide mechanics require suitable model parts and additional animation. Neither the independent controller nor IK creates those missing parts.

Select FemaleVisual in Scene view / Animator to inspect world-body animations. The runtime FirstPersonAnimatedArms object under the camera uses the separate FPS controller.

## Historical verification — 2026-10-17

The following checks describe the earlier conversion and pose-copy FPS rig. They are a historical record and do not validate the current independent Animator/IK presentation.

Generated 92 distinct Humanoid clip copies and sampled each on the Female avatar without invalid head poses. Reviewed rifle full-body preview. Disabled bundled PistolPBR/AssaultRiflePBR demo weapons to avoid duplicates. Calibrated grip rotation separately for handgun/rifle; reload clip speed matched 1.4s/2s gameplay.

Play Mode checks passed for the earlier setup: two-layer Female avatar active, old Animator disabled, handgun/rifle switching, shot/reload state transitions, all 42 base states, jump start-air-land, hit trigger and death with upper-body layer disabled. Existing first-person camera/gameplay root retained; no test rewards or progress changes.

FPS reload verification (Unity 6000.3.7f1, previous pose-copy rig):

- Hand local position/rotation matched the source Animator exactly on both sides during reload; the clone had zero enabled Animators and zero colliders.
- Full magazine rejected reload; attacking during reload was blocked.
- HG Reload and AR Reload were entered through the gameplay reload trigger; effective clip durations were 1.4s and 2s.
- Pistol: shot 12/60 -> 11/60, completed reload 12/59.
- Rifle: shot 30/120 -> 29/120, completed reload 30/119.
- Switching during a second rifle reload restored Ready and left rifle at 29/119 after the cancelled timer would have elapsed.
- Reviewed idle pistol and reload poses for pistol/rifle through MainCamera. No static FirstPersonWeapon duplicate.
- Final scripts compiled; the existing TutorialMapBuilder NavigationStatic deprecation warning was unrelated.
- Test AI disabling, ammo consumption and pose sampling were confined to Play Mode and discarded on stop.

## Current FPS overhaul — verified 2026-10-23

Run GunQuest > Female > 4 - Validate First Person in Play Mode to repeat the Editor-only checks. The suite writes Documentation/FemaleFPSValidation.txt and PNG captures, restores input and exits Play Mode; it does not grant rewards or save progress.

Latest run: PASS, zero failed checks. Unity script compilation completed and the Console had no errors after the final run.

- One selected Female skin uses the generated FPS geometry, with no fake sleeve tubes or colliders. The controller contains six event-free clips and uses manual evaluation once per playing frame.
- Pistol shot/reload: exactly one round consumed; the magazine refilled and reserve decreased by one. Reload completed in 1.402 game seconds (configured 1.4).
- Rifle shot/reload: magazine/reserve counts remained correct. Reload completed in 2.023 game seconds (configured 2).
- Pause froze the game clock, reload animation time and ammo; resume completed the same reload.
- Switching weapons during reload restored ReadyHG and did not transfer ammo after the cancelled rifle timer's old deadline.
- Bat/Shovel selections and melee attacks passed; high/low camera pitch views were captured at +/-75 degrees.
- Progress credits, completed contracts and saved PlayerPrefs remained unchanged. Play Mode test modifications were discarded, Mouse input restored and background-running setting returned to its original disabled state.

Reviewed the final Pistol/Rifle idle and rifle reload captures; shoulder armor is excluded and the support elbow/receiver grip fits the camera framing. Procedural sway, sprint lowering, landing and visual aim are implemented separately from gameplay animation state; they can be tuned further without modifying source clips or world-body assets.