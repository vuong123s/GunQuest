# Female Player animation

Controller: Assets/Animations/Female/FemalePlayer.controller
Playable prefab: Assets/Prefabs/Player/PlayerFemale.prefab
Visual source: Assets/Prefabs/Player/DefaultFemalePBR.prefab (Body02/Head02)

The conversion keeps the Player gameplay root and mission references, disables the old bird visuals/Animator, installs the Female Humanoid, and moves weapon models to a right-hand socket. The previous Player prefab remains unchanged. A dated scene backup is saved before replacement.

## Controller

- Base Layer: HG / AR directional locomotion, jump start-air-land, hits, dizzy, knockdown-recovery, get up, dash and roll in four directions, power-up, victory, death.
- Upper Body: masked handgun/rifle shots and reload while the legs can continue moving.
- Clip Library - Preview Only: all source clips, including root-motion versions and aim offsets, available for manual preview. This sub-state machine is deliberately not entered by gameplay.
- IsRifle selects rifle versus handgun stance, updated on weapon equip. IsDead blocks action transitions and disables the upper-body override.
- MoveX/MoveY preserve forward/back/strafe input; MoveRate scales locomotion for crouch/sprint.
- Source FBX assets are unchanged. Copies have explicit looping/root baking and no imported demo animation events.
- Existing gameplay automatically drives locomotion, jump, hit, dizzy, death, weapon switching, shooting and reload.
- FemaleAnimationDriver.PlayAction(trigger) exposes the remaining optional animations: Dash, DashBack, DashLeft, DashRight, Roll, RollBack, RollLeft, RollRight, Hit2, Knockdown, GetUp, PowerUp, Victory. These are animation previews/hooks, not new movement or combat abilities.
- The pack contains no crouch or melee attack clip. Existing crouch collider and melee damage remain available, but those actions require matching animation assets for complete visual coverage. No unrelated animation is substituted.

## Editor tools

GunQuest > Female > 1 - Build Animator rebuilds the generated controller/clips.
GunQuest > Female > 2 - Replace Scene Player installs the generated Female into the current scene.
Replacement refuses if Female is already installed, preventing duplicate rigs.

The first-person camera hides the world body while retaining its shadow. AnimatedFirstPersonRig copies the Female skeleton poses in LateUpdate into a camera-local render rig; its Animator, scripts, physics and effects are disabled. The original skinned hands and held weapon follow real shot/reload clips. Capped low-poly sleeves connect the wrists to the bottom of the frame, avoiding exposed third-person elbow cuts. This does not add duplicate animation events or ammo timers.

GunQuest > Female > 3 - Build First Person Meshes regenerates the hand meshes and sleeve assets in Assets/Resources. Generation runs only in Editor. The camera offset is saved on PlayerFemale and Scene1; adjust Animated Arms Offset on PlayerCameraFollow for framing.

Weapon changes cancel the upper-body action and restore Ready. The existing reload timer remains authoritative (handgun 1.4s, rifle 2s); clip playback speeds match it. Gun mesh art is retained, so detachable magazines/slide mechanics would need separate model parts. The first-person rig currently shares the third-person clips rather than using a dedicated FPS animation pack.

Select FemaleVisual in Scene view / Animator to inspect full-body animations.

Verification: generated 92 distinct Humanoid clip copies and sampled each on the Female avatar without invalid head poses. Reviewed rifle full-body preview. Disabled bundled PistolPBR/AssaultRiflePBR demo weapons to avoid duplicates. Calibrated grip rotation separately for handgun/rifle; reload clip speed matches 1.4s/2s gameplay.

Play Mode checks passed: two-layer Female avatar active, old Animator disabled, handgun/rifle switching, shot/reload state transitions, all 42 base states, jump start-air-land, hit trigger and death with upper-body layer disabled. Existing first-person camera/gameplay root retained; no test rewards or progress changes.


FPS reload verification (2026-10-17, Unity 6000.3.7f1):
- Hand local position/rotation matches source Animator exactly on both sides during reload; clone has zero enabled Animators and zero colliders.
- Full magazine rejects reload; attacking during reload is blocked.
- HG Reload and AR Reload are entered through the gameplay reload trigger; effective clip durations are 1.4s and 2s.
- Pistol: shot 12/60 -> 11/60, completed reload 12/59.
- Rifle: shot 30/120 -> 29/120, completed reload 30/119.
- Switching during a second rifle reload restores Ready and leaves rifle at 29/119 after the cancelled timer would have elapsed.
- Reviewed idle pistol and reload poses for pistol/rifle through MainCamera. No static FirstPersonWeapon duplicate.
- Final scripts compiled; existing TutorialMapBuilder NavigationStatic deprecation warning is unrelated.
- Test AI disabling, ammo consumption and pose sampling were confined to Play Mode and discarded on stop.
