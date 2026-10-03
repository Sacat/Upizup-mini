# MINI-190: Pistol handling animation for every humanoid (player characters and NPCs)

```yaml
task_id: MINI-190
title: Researched, procedural pistol handling system (draw, two-hand aim, recoil, reload, low ready, holster) for all humanoid rigs
request_owner: User ("work on the firearm animation perfectly for all characters and npcs. research first")
integrator: Claude
status: evidence_ready; awaiting player-controlled feel review
approval_class: B
external_credits: 0
reserved_files:
  - Assets/UpIzUpMini/Scripts/Combat/FirearmPose.cs
  - Assets/UpIzUpMini/Scripts/Combat/FirearmController.cs
  - Assets/UpIzUpMini/Scripts/Combat/IPistolUser.cs
  - Assets/UpIzUpMini/Scripts/Combat/NpcPistolUser.cs
  - Assets/UpIzUpMini/Editor/Mini190FirearmProof.cs
  - Assets/UpIzUpMini/Prefabs/Weapons/Mini190_SidearmVisual.prefab
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity   # NOT edited: FirearmPose was already on both players; new fields use defaults
  - Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
```

## Research (what the references say)
- A complete third-person pistol set (Mocap Online's shooter guide and pistol packs; the PRO pack has 372 clips) covers idle, low-ready and aimed poses, draw and holster, armed locomotion in 8 directions, **aim offsets** (a 2D pitch/yaw additive blend layered over locomotion from the spine upward), single fire with recoil recovery, tactical reload, hit reactions and deaths. Sources: [Shooter animation pack guide](https://mocaponline.com/blogs/mocap-news/shooter-animation-pack), [Pistol mocap packs](https://blendermarket.com/products/pistol-packs-mocap-animations), [Pistol Animset Pro description](https://kubold.com/s/PistolAnimsetPro_AnimationsDescriptions.html).
- Unity's Animation Rigging approach (Multi-Aim spine/head distribution, Two-Bone IK off-hand to the weapon, Override Transform) spreads aim over the spine with rising weights (about spine .35, chest .5, upper chest .75, head 1.0) and locks the support hand to a weapon socket: [Unity Animation Rigging manual](https://docs.unity3d.com/Packages/com.unity.animation.rigging@0.2/manual), [Mocap Online rigging guide](https://mocaponline.com/blogs/mocap-news/unity-animation-rigging-guide).
- Enemy design notes: telegraphed and less accurate while moving, accurate when still or in cover; light/heavy/directional hit reactions that keep the weapon hold.
- Project facts: the Humanoid IK pass already works (`HumanoidAnimationLayerBuilder` enables IK on the action layers); the Animation Rigging package is installed but needs per-character rig GameObjects; free Mixamo pistol clips would need per-clip downloads under the user's account.

## Decision
A **procedural, data-driven system on the Humanoid IK pass** (no per-character rig objects, no downloads, works on every humanoid, including the 30-finger-bone player rigs and the fingerless low-poly NPCs): one `FirearmPose` component per Animator, fed by an `IPistolUser` (the real `FirearmController` for players, `NpcPistolUser` for NPCs). Mocap clips can still be layered later; the IK layer is the part clips cannot do (fit any arm length to the actual gun).

## What was built
- `IPistolUser`: aiming, low ready, reloading, reload progress, aim direction, weapon root.
- `FirearmPose` (rewritten in place so the scene's existing component and GUID are kept): two-hand grip with IK targets derived from each rig's own arm length, elbow hints, body and head look along the aim, pistol finger curls on both hands (axis from the knuckle line, sign chosen so tips close on the grip, trigger finger left straight), spring-damper recoil (muzzle flip, push-back, body kick), draw/holster blend (weapon appears once the hand is up), lowered two-hand low ready, and a procedural reload (gun tilts to the chest, support hand to the belt, magazine prop up to the grip, seat, rack, return). Reload timing follows the controller's existing 1.35 s so gameplay is unchanged. Fingerless rigs get a hand-pitch correction and a fist-shaped squash of the hand mesh. Solved once per frame because several layers have an IK pass.
- `FirearmController`: implements `IPistolUser`, exposes reload progress, calls `NotifyShot()` on every shot, follows the animated gun direction (recoil flip, reload tilt) instead of raw camera forward, and shows/hides the gun from the pose's draw state. Damage, ammo, tracer, range and timings are untouched. The `PlaceAtPalm` fallback now seats the grip at the palm centre for fingerless hands.
- `NpcPistolUser`: lets any humanoid NPC hold and use the same gun (stance Holstered / LowReady / Aiming, `AimTarget`, `TriggerShot()`, `BeginReload()`); builds the weapon from `Mini190_SidearmVisual.prefab`; shot flash. Pure animation/visual layer: no AI, damage or scene wiring changed. NO live NPC was given a gun.
- `Mini190FirearmProof` (Play Mode, scene never saved): renders holstered, draw, aim, recoil peak, low ready and six reload phases from three body views and three hand close-ups for Sacat, Franki and a town NPC.

## Evidence
`Logs/Tasks/MINI-190/Renders/final_sacat.png`, `final_franki.png`, `final_npc.png` (contact sheets) and the full set `Renders/*_*_*.png`; `ProofLog.txt` (right middle finger to grip anchor 11-29 mm on the player rigs). Run: `Unity.exe -batchmode -projectPath ... -executeMethod UpIzUpMini.EditorTools.Mini190FirearmProof.Run` (NO `-quit`, no `-nographics`).

## Lessons
- The proof camera must carry the `MainCamera` tag, otherwise `FirearmController.LateUpdate` (which uses `Camera.main`) never places the gun.
- Mecanim hand goal rotation: the character's right is +X when facing +Z. Right-hand T-pose fingers point +X, left-hand -X. Right hand goal (local) = Rz(-90) * Ry(-90), left = Rz(90) * Ry(90), verified in renders. A positive rotation about the character's right axis pitches DOWN in Unity.
- `OnAnimatorIK` runs once per layer with an IK pass; guard with `Time.frameCount` or finger rotations stack.
- A Unity already open on another project can starve memory and crash batch runs.

## Open
- Not yet felt with the real mouse and camera (the cursor cannot lock in batch mode): check aim feel, recoil size, reload pacing and draw speed in a build.
- Not done: hit reactions that keep the weapon hold, crouch/cover, armed locomotion (walking while aimed still uses the normal walk with the upper body overridden), aim-offset clips. NPC stances need a gameplay owner (which NPCs carry guns, when police or brawlers draw, accuracy and telegraph rules).
- The magazine prop is a plain box; the slide does not move (single rigid mesh).
