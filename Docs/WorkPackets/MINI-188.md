# MINI-188 — Online-referenced Lalay Tool hand fit

```yaml
task_id: MINI-188
title: Compare the in-game hand fit with online visual and rigging references, then correct the anchors
request_owner: User
integrator: Codex
status: evidence_ready; live aiming review pending
approval_class: B
budget:
  external_credits: 0
  stop_condition: one bounded anchor correction, both-character close renders, validation and Windows build
reserved_files:
  - Assets/UpIzUpMini/Editor/Mini186ToolModelSetup.cs
  - Assets/UpIzUpMini/Editor/Mini188*.cs
  - Assets/UpIzUpMini/Scripts/Combat/FirearmController.cs
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Docs/WorkPackets/MINI-188.md
protected_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
  - Assets/UpIzUpMini/Art/Weapons/Mini186/
depends_on:
  - MINI-187
```

## Intent

Use online visual and rigging references to place the fictional tool convincingly inside Franki's and Sacat's right hands. Preserve the model, colors, gameplay ray and missions.

## References and finding

- [SIG SAUER backstrap reference](https://www.sigsauer.com/glossary/backstrap/) identifies the grip's backstrap as the surface contacting the palm between thumb and index finger. This is a better anatomical target than the middle finger's intermediate joint used in MINI-187.
- [Unity humanoid right-hand bone](https://docs.unity3d.com/6000.0/ScriptReference/HumanBodyBones.RightHand.html) is the wrist, so the wrist alone is not a grip placement point.
- [Unity humanoid rig mapping](https://docs.unity3d.com/2020.2/Documentation/Manual/ConfiguringtheAvatar.html) supports explicitly checking the optional finger bone mapping and previewing the clip.
- [Unity Animation Rigging Multi-Aim Constraint](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.2/manual/constraints/MultiAimConstraint.html) documents local aim axes and world-up alignment; the muzzle axis must be checked separately from the grip point.
- [Side-view held-handgun photograph](https://zooroomtechnologies.com/zr_Blog/_firearms/_articles/_article007.html) and [side-profile photo](https://www.thefirearmblog.com/blog/2023/03/30/xtech-mtx-365-extensions/) are visual comparisons only. They show the handle inside the fingers and the slide above the hand. No photo assets will be copied into the game.

## Acceptance

- [x] Compare current Franki and Sacat close renders to the online references.
- [x] Add an explicit upper backstrap point and move it toward the thumb-index web proxy while preserving finger overlap.
- [x] Verify muzzle point independently of the two grip points.
- [x] Compile, inspect both-character renders, run saved-scene anchor checks and build Windows player.
- [x] State what live aiming motion still needs human review.

## Comparison and decision

The source photo and SIG definition both place the handle inside the fingers and the upper rear of the grip against the thumb-index web. The MINI-187 finger-joint alignment kept the handle inside the closed hand, but the rear grip remained 35.6 mm from Sacat's web proxy and 38.7 mm from Franki's. A full mathematical backstrap-to-web alignment was rendered for each character in `*-WebAnchor.png`. It visibly pulled the handle upward and out of the closed fingers, because the available one-hand combat clip was not authored around this model.

The selected quarter-distance correction (`*-ReferenceFit.png`) moves the upper backstrap toward the web while retaining the finger wrap in both inspected close-ups. The grip center is 8.9 mm from Sacat's middle-finger joint and 9.7 mm from Franki's; web gaps fall to 26.7 mm and 29.0 mm respectively. This is a visual compromise, not a claim that bone proxies reproduce actual palm contact. The saved model has named `GripAnchor` `(0,-0.035,-0.045)` m, `BackstrapAnchor` `(0,-0.005,-0.075)` m and `ShotOrigin` `(0,0.012,0.095)` m relative to its visual origin. The muzzle point is independent of the hand anchors. Gameplay hit rays still come from the camera.

`FirearmController.PlaceAtHand` blends the rig's curled middle-finger and thumb-index web solutions at 75:25 and follows the current camera direction. If the optional finger bones are not mapped, it falls back to the earlier wrist method. No model, texture, color materials, mission, ammo or damage rules changed. A dedicated pistol grip animation with conforming finger joints would be needed to close the remaining 27-29 mm web gap without pulling the handle outside the fist. The static clip render cannot verify moving/aiming/recoil behavior or whether the character's hand looks natural in player view.

## Evidence

Unity 6000.3.10f1 commands used `Unity.exe -batchmode -projectPath 'E:\Unity\Up Iz Up Mini' -executeMethod <method> -logFile <path>`; `-nographics` was omitted for renders and used for Apply/Verify/build. `Mini188HandFitProof.RenderComparison` passed (`Logs/Tasks/MINI-188/final-render.log`); `Mini186ToolModelSetup.Apply` passed (`anchor-apply.log`); `Mini188HandFitProof.VerifySavedWebGrip` passed (`verify.log`) for both characters. Render comparisons: `Franki-FingerAnchor.png`, `Franki-WebAnchor.png`, `Franki-ReferenceFit.png` and Sacat equivalents in the same log folder. Windows build passed (`build.log`), `Builds/GrandBayProof/UpIzUpMini.exe`, total 442,476,325 bytes. The protected HouseEnhance scene SHA-256 remained `CF0E4E8A06140F3DA0B65989F9C791D77E80590E9A4F9CF748E5DC242ED07CF0`.
