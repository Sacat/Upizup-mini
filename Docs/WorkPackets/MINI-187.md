# MINI-187 — Lalay Tool original colorways and character proofs

```yaml
task_id: MINI-187
title: Research official modern sidearm colorways and preview original Lalay Tool variants on the game characters
request_owner: User
integrator: Codex
status: evidence_ready; user aiming review pending
approval_class: B
budget:
  external_credits: 0
  stop_condition: at most three distinct researched colorways, full-character Unity renders, and material assets ready for selection
reserved_files:
  - Assets/UpIzUpMini/Art/Weapons/Mini187/
  - Assets/UpIzUpMini/Editor/Mini187*.cs
  - Assets/UpIzUpMini/Editor/Mini186ToolModelSetup.cs
  - Assets/UpIzUpMini/Scripts/Combat/FirearmController.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Docs/WorkPackets/MINI-187.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
  - Assets/UpIzUpMini/Art/Weapons/Mini186/
depends_on:
  - MINI-186
```

## Intent

Research factory color families from official manufacturer sources, adapt them into original unbranded Lalay Tool colorways, and show the model on a full game character. Keep the approved geometry and existing in-game default while the user reviews the variants.

## Acceptance

- [x] Primary-source color research recorded with links.
- [x] Three distinct material variants made without new Hi3D credits or mesh changes.
- [x] Full-character and close Unity renders for both characters.
- [x] Explicit grip and muzzle anchors aligned to the measured character rig.
- [x] Material import and anchor scene validation recorded.

## Color research and result

Official manufacturer sources supplied visual color families, not branding for the fictional Lalay Tool: [Glock G19X coyote](https://us.glock.com/en/press-release/news-page/glocks-compact-slide-and-full-size-frame-join-forces), [FN black and flat dark earth](https://fnamerica.com/press-releases/fn-releases-fn-509-compact-tactical/), [Walther green/gray/tan catalog examples](https://waltherarms.com/wp-content/uploads/2024/02/Walther-Product-Catalog-2024.pdf), and [SIG M18 coyote/black controls](https://www.sigsauer.com/blog/sig-sauer-introduces-the-commercial-variant-of-the-u-s-military-m18-with-the-p320-m18). The three original game variants are MatteBlack, CoyoteSand and TwoToneOlive. One small shader recolors the existing 1024 texture by mesh region; each option is one material on the unchanged 2,998-triangle mesh. No Hi3D credits were used. Assets are under `Assets/UpIzUpMini/Art/Weapons/Mini187/`. The default scene material remains the original graphite/teal.

## Character anchor measurements and placement

Unity's [HumanBodyBones.RightHand](https://docs.unity3d.com/6000.0/ScriptReference/HumanBodyBones.RightHand.html) is the wrist. [GetBoneTransform](https://docs.unity3d.com/6000.0/ScriptReference/Animator.GetBoneTransform.html) exposes the mapped finger joints, and [OnAnimatorIK](https://docs.unity3d.com/6000.0/ScriptReference/MonoBehaviour.OnAnimatorIK.html) changes the final hand pose before the weapon's LateUpdate placement. The prior wrist plus world-offset attachment placed the grip beside the fist in `Franki-CoyoteSand-Close.png` (first proof). In the sampled one-hand combat idle, Franki's middle finger intermediate joint was approximately 0.12 m from the wrist, and the old weapon center was on the wrist side. The finger joint lies inside the closed hand in both characters' renders.

The model now has named `LalayTool_Visual/GripAnchor` at local `(0,-0.035,-0.045)` m and `LalayTool_Visual/ShotOrigin` at `(0,0.012,0.095)` m. `FirearmController.PlaceAtPalm` uses the rig's right middle intermediate joint and the current aim direction to align the grip anchor there. The muzzle transform remains the tracer origin. If a rig lacks that finger bone, the former wrist offset is a fallback. `Mini186ToolModelSetup.Apply` is repeatable and reassigns both serialized anchors after replacing the visual. Scene verification sampled the combat clip and measured grip-to-joint error `0.0000 m` for Franki and Sacat (`Logs/Tasks/MINI-187/anchor-verify.log`).

Final static evidence: `Logs/Tasks/MINI-187/Franki-CoyoteSand-Close.png`, `Sacat-CoyoteSand-Close.png`, and full-body/colorway peers. The grip is visibly within the closed hand in both close-ups. These sampled editor images do not prove the live camera-driven IK through aiming, recoil, walking or character switches; that still requires an interactive player review. The aim pose itself was not redesigned.

## Verification

Unity 6000.3.10f1 batch runs: `Mini186ToolModelSetup.Apply` passed in `anchor-apply-final.log` (second application also confirmed repeatability); `Mini187ColorwayProof.RenderCharacterColorways` passed in `anchor-render2.log`; `Mini187ColorwayProof.VerifyGripAnchors` passed in `anchor-verify.log`. Windows build passed in `build-final.log`: `Builds/GrandBayProof/UpIzUpMini.exe`, 442,475,397 bytes. The protected HouseEnhance scene SHA-256 remained `CF0E4E8A06140F3DA0B65989F9C791D77E80590E9A4F9CF748E5DC242ED07CF0`.
