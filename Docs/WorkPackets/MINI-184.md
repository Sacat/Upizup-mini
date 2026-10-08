# MINI-184 — Sidearm live validation and repair

```yaml
task_id: MINI-184
title: Verify and repair the first Lalay sidearm slice in the actual game
request_owner: User
integrator: Codex
status: evidence_ready; awaiting player-controlled aim review
approval_class: B
external_credits: 0
stop_condition: repeatable runtime proof of purchase, ammo, aim/fire, NPC impact, plus a visually checked pose and new Windows build
reserved_files:
  - Assets/UpIzUpMini/Scripts/Combat/Firearm*.cs
  - Assets/UpIzUpMini/Scripts/UI/ShopPanelController.cs
  - Assets/UpIzUpMini/Editor/Mini184*.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
  - Assets/UpIzUpMini/Art/Environment/Mini182/
```

## Intent

Continue MINI-183 through runtime and visual verification, fixing concrete faults. Keep the Hitem3D credit balance untouched.

## Acceptance

- [x] Built-player runtime purchase, ammo, reload, firing and NPC impact assertions.
- [x] Static in-game render of the corrected gun attachment.
- [ ] Player-controlled aim motion and feel review; the headless player will not lock its cursor.
- [x] Clean Unity compile, scene check and Windows build.
- [x] Human-only feel check is identified below.

## Evidence and repair

Built-player diagnostic in `-batchmode` returned `TOTAL_FAIL=0` (`Logs/Tasks/MINI-184/runtime-probe.txt`): sidearm and 12-round purchase, owned state, one-round consumption, NPC health 100 to 62, and reloading 12 rounds from reserve. The initial NPC miss was the test dummy being behind the safehouse floor on the default camera ray; moving it in front of that obstruction proved the actual hit path. Headless Unity reports `Cursor.lockState=None`, so it cannot exercise ordinary right-mouse aiming. The temporary diagnostic component was removed before the final build.

The first render showed the model hanging behind the hand. `Mini183FirearmSetup` now parents it to the player root, while `FirearmController.LateUpdate` positions it at the animated right hand and points it along the camera ray. The corrected close render is `Logs/Tasks/MINI-184/sidearm-after-attachment.png`. `forced-ik-diagnostic.png` is a static diagnostic only and does not prove player-controlled motion. The placeholder is still visibly blocky; a polished model is a separate asset decision.

Final Unity 6000.3.10f1 evidence: `Logs/Tasks/MINI-184/final-compile.log` (exit 0, no C# errors), `final-verify.log` (two controllers, scene stock and model root pass), `scene-validation.log` (static GrandBayProof pass), and `final-build.log` (Windows build succeeded). The built scene data timestamp is 2026-09-29 18:08 local. Protected HouseEnhance SHA-256 remained `CF0E4E8A06140F3DA0B65989F9C791D77E80590E9A4F9CF748E5DC242ED07CF0`.

The shared repository had pre-existing dirty scene and project files before MINI-183 and MINI-184. A selective Git commit would omit the scene or absorb unrelated work, so no mixed checkpoint was made.

## Optional Hitem3D asset card — proposed, no credits spent

Purpose: replace only the temporary `Mini183_Sidearm` meshes after aim-pose review. Fictional compact sidearm; no real brand, logo or markings. Overall length about 0.19 m; grip origin at the hand, barrel along local +Z. One rigid mesh with distinct slide, grip and muzzle shape, no rig. LOD0 target under 1,500 triangles; one material and at most one 1024px texture set; simple box collider if needed. Max two generation attempts, with the actual point cap to be approved before generation. Import through Blender cleanup and a neutral render before replacing the scene model.
