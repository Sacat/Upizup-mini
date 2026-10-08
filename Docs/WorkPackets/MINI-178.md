# MINI-178 — Franki gets Sacat's double-loop chain, scaled to his body (Claude, 2026-09-20)

User: rejected Franki's single-loop MINI-176 fit; wants Sacat's chain (same style, same double loop and links) on Franki, referenced to Sacat's fit: same closeness to the nape and chest, same angle, only adjusted for the height/size difference.

## What changed
- `ChainGarmentFit.cs` restored to committed HEAD (removed the MINI-176 Franki-only branch). Copy of the rejected branch: `Logs/Tasks/MINI-178/ChainGarmentFit.MINI176-franki-branch.cs.txt`. Sacat's fit algorithm is the committed one, unchanged.
- New `Assets/UpIzUpMini/Data/Equipment/FrankiChainPlacement.asset`: same `fittedChildren` as `SacatChainPlacement.asset` (second loop = duplicate `geometry_0 (1)`, same link transforms), root pose derived from Sacat's.
- Franki's `CharacterEquipment.chainPlacement` assigned to it in `GrandBayProof_ExpansionImport.unity` ONLY. `GrandBayProof.unity` untouched. Scene backup before: `Logs/Tasks/MINI-178/GrandBayProof_ExpansionImport.before.unity`.
- `SacatChainPlacement.asset`, `CharacterEquipment.cs` and `ChainGarmentFit.cs`: no diff vs HEAD.
- Tool: `Assets/UpIzUpMini/Editor/Mini178FrankiChain.cs` (Measure / Sweep / Save / Verify / Preview, Play Mode via InitializeOnLoad + SessionState).

## Method
1. Measured both rigs (`Logs/Tasks/MINI-178/Measure.txt`): nape-to-chest vertical Sacat .3116 m, Franki .2732 m, so k = .8768. Both chest bones are uniform scale (.94 / .96).
2. Sacat's chain root pose expressed in his character frame relative to the neck bone, scaled by k, rebuilt in Franki's frame, converted to Franki's chest-bone local space. Root scale = Sacat lossy scale x k.
3. Only Sacat's PITCH (~-22 deg) is kept. His ~5.7 deg yaw / ~1.2 deg roll compensate for his own torso twist and made Franki lopsided.
4. Body surface depth profile (baked SkinnedMeshRenderer colliders, same technique as ChainGarmentFit) shows Franki's chest front is ~5 cm further back relative to the neck bone than Sacat's, nape ~2 cm further back. Five offset variants were rendered on a contact sheet (`Renders/Sweep-sheet.png`, rows: Sacat, V0..V4). V4 (back .015 m, up .03 m, no extra pitch) had the closest neck and nape look and was saved.
5. Sacat's fit code then fits Franki's chain to his shirt surface at equip time.

## Verification (Claude)
- Compile clean; `MINI178_SAVE_PASS`, `MINI178_ASSIGN_PASS`, `MINI178_VERIFY_PASS` (logs `Logs/mini178-*.log`).
- Fresh Play Mode from the SAVED scene: Franki chainPlacement = FrankiChainPlacement.asset, 2 loops, 12,254 vertices. Sacat identical to baseline (12,254 vertices, same extents).
- Franki chain extents from neck: size (.202, .324, .220); highest rear vertex y .116 (Sacat scaled would be .086); lowest y -.207 (Sacat scaled -.237). So V4 hangs ~3 cm shorter and sits ~3 cm higher at the nape than a pure proportional copy.
- Renders: `Logs/Tasks/MINI-178/Renders/Verify-{Franki,Sacat}-{front,side,back}.png`.

## Back fix (second pass, same day)
Root cause of the bad back: `CharacterEquipment` moves EVERY chain 3.5 cm toward the neck (`ChainNeckwardCorrection`) AFTER the placement profile is applied. I copied Sacat's chain position as measured AFTER that correction, so Franki got it twice (3.5 cm too far back), which wrapped his lower loop around the back of the neck. The earlier V4 offset (+.03 up, -.015 back) was a hack that partly hid this.
Fix: compensate that correction in the profile (+.035 forward) and refine: saved variant M5 = +.025 forward, no extra lift, pitch-only rotation, no per-loop shifts (lower-loop lift was tried and rejected: it merged the two loops in front). Sweep sheet: `Renders/Sweep-sheet.png`.
Numbers vs Sacat scaled by .877 (from neck bone): nape-side top .086 vs .086; lowest -.238 vs -.237; size (.203,.324,.200) vs (.201,.323,.182); lower loop rear reach z +.082 vs +.059.
Verified again from the SAVED scene (`MINI178_SAVE_PASS`, `MINI178_ASSIGN_PASS`, `MINI178_VERIFY_PASS`, `Logs/mini178-*3.log`); Sacat chain metrics identical to baseline; `SacatChainPlacement.asset`, `ChainGarmentFit.cs`, `CharacterEquipment.cs` no diff vs HEAD.
Result: back shows one clasp at the nape, chain hugging the neck, links going over the shoulders, no dangling ends. Remaining difference: Franki's lower loop crosses the base of the neck at the front with its clasp visible (his neck surface is closer to the chain there); in Sacat's render that part is hidden inside his thicker neck.

## Visual result / known differences (not approved) — FIRST PASS, superseded by the back fix above
- Front and side: double loop, symmetric, upper links rise beside the neck and lie on the shirt like Sacat's.
- Back: Franki's two loops show two separate clasps, the lower one resting on the upper back and a few small spiky link fragments near the shoulder-side link ends. Sacat's back shows one tight clasp at the nape. Not equal to Sacat's yet.
- Not done: idle/walk/run/turn/crouch/bike motion (AccessorySwing up to .045 m, not collision-aware), other shirts (wardrobe refresh), live `GrandBayProof.unity`, EXE. User approval NOT recorded in `Docs/VISUAL-APPROVAL-REGISTER.md`.

## Next if the back needs work
Give the lower loop its own rear offset (per-loop child transform in the Franki profile) so its rear sits at the nape like Sacat's; re-run `Mini178FrankiChain.Sweep`/`Save`/`Verify`.

## Third pass — front top hidden in the neck (user request, 2026-09-20)
User: raise the chain slightly so the front top of the chain does not show; it can go into the neck to give the illusion it is around the neck.
Swept lifts of 0 / .015 / .025 / .035 m (+ one variant pulled back 1.5 cm) on the contact sheet, saved N2 = +.025 forward compensation, **+.025 m up**, pitch-only rotation (`FrankiChainPlacement.asset`, assigned in the expansion scene only).
Result (Franki, from neck bone): nape-side top y .111 (was .086), lowest y -.214 (was -.238), size (.203,.325,.192), 2 loops. Front: the arc across the throat and its clasp no longer show; the chain rises on both sides into the neck like Sacat's. Back: single clasp at the nape, no dangling ends. Side: chain runs neck -> shoulder -> chest.
Verified from the saved scene (`MINI178_SAVE_PASS`, `MINI178_ASSIGN_PASS`, `MINI178_VERIFY_PASS`, `Logs/mini178-*4.log`); `SacatChainPlacement.asset`, `ChainGarmentFit.cs`, `CharacterEquipment.cs` no diff vs HEAD. Renders: `Logs/Tasks/MINI-178/Renders/Verify-Franki-*.png`.
Still NOT done: motion (idle/walk/run/turn/crouch/bike), other shirts, live `GrandBayProof.unity`, EXE, and no user approval recorded in the visual register.
