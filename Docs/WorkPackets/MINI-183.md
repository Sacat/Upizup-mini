# MINI-183 — Lalay black-market firearm vertical slice

```yaml
task_id: MINI-183
title: Buy a fictional sidearm and ammunition in Lalay; aim, fire, reload and react
request_owner: User
integrator: Codex
status: implemented; awaiting hands-on visual/gameplay confirmation
approval_class: B
external_credits: 0
stop_condition: one testable sidearm with saved ownership/ammo, NPC hit response, police heat, and visible aim/fire feedback
reserved_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Scripts/Combat/Firearm*.cs
  - Assets/UpIzUpMini/Scripts/Economy/EconomyManager.cs
  - Assets/UpIzUpMini/Scripts/Economy/ShopItemDefinition.cs
  - Assets/UpIzUpMini/Scripts/UI/ShopPanelController.cs
  - Assets/UpIzUpMini/Scripts/SaveLoadSystem.cs
  - Assets/UpIzUpMini/Scripts/Interaction/TownNPCInteractable.cs
  - Assets/UpIzUpMini/Editor/Mini183*.cs
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
  - Assets/UpIzUpMini/Art/Environment/Mini182/
```

## Intent

The existing Lalay black-market trader discreetly sells one fictional sidearm and repeatable ammunition. The active protagonist can aim, fire, and reload. Hits use the existing NPC health and ragdoll systems; hitting a police officer fills heat. Purchases and ammo survive save/load. The other character uses the same shared ownership and ammunition after switching.

## Approach

Use the existing ShopPanelController and EconomyManager. Keep clothing resale available as the trader's second tab. Begin with a low-poly in-project placeholder model and procedural upper-body aim/recoil; no Hitem3D points are spent before the user approves a concrete asset brief. Keep the new interaction out of vehicles and menus. This is one firearm slice, with additional weapons and improved animation clips deferred until this one is visibly played.

## Acceptance

- [x] Black-market stock and resale view are wired, with prices and controls.
- [x] Ownership, magazine, menu, vehicle and character guards are implemented.
- [x] Procedural aim/recoil, tracer, hit response and heat are implemented.
- [x] Police hit adds maximum heat in code.
- [x] Save/load includes ownership, reserve ammo and magazine state.
- [x] Compile, scene wiring checks, static scene regression and Windows build pass.
- [ ] Hands-on purchase, fire, NPC hit and aim-pose visual playtest in the built player.

## Baseline

Current owner was None, no Unity process was running, preflight passed for Codex/MINI-183. Existing working tree is dirty (25 files); preserve all unrelated changes. MINI-182 isolated scene is protected. No Hitem3D credits used.

## Result and verification (2026-09-29)

The live GrandBayProof scene now has a targeted sidearm setup for Sacat and Franki. The Lalay black-market trader sells a fictional $750 sidearm with 12 rounds and $70 boxes of 12 rounds; key 0 opens the existing clothing resale view. Right mouse aims, left mouse fires, R reloads. The sidearm model is a temporary three-part in-project mesh. The shop remains story-gated at M12. The existing shared economy, NPC health and heat systems handle the transaction and reaction. No Hitem3D points were used.

Unity 6000.3.10f1 batch commands used `-projectPath "E:\Unity\Up Iz Up Mini"` and `-logFile` at each path below:

- `-batchmode -nographics -quit`: `Logs/Tasks/MINI-183/compile.log`, exit 0, no C# errors.
- `-executeMethod UpIzUpMini.EditorTools.Mini183FirearmSetup.Apply`: `Logs/Tasks/MINI-183/apply.log`, pass.
- `-executeMethod UpIzUpMini.EditorTools.Mini183FirearmSetup.Verify`: `Logs/Tasks/MINI-183/verify.log`, pass.
- `-executeMethod UpIzUpMini.EditorTools.Mini001SceneValidation.Run`: `Logs/Tasks/MINI-183/scene-validation.log`, pass.
- `-executeMethod UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer`: `Logs/Tasks/MINI-183/build.log`, succeeded; `Builds/GrandBayProof/UpIzUpMini_Data/level0` updated 2026-09-29 14:58 local.

The protected `GrandBayProof_HouseEnhance.unity` SHA-256 remained `CF0E4E8A06140F3DA0B65989F9C791D77E80590E9A4F9CF748E5DC242ED07CF0`. No automated input/play test or visual confirmation was performed. The repo contained unrelated dirty files before this task, including the canonical scene, so no mixed-history Git checkpoint was made.
