# MINI-176 — Franki nape fit and complete Claude workflow handoff
User explicitly approves Sacat chain as perfect. Preserve Sacat algorithm, source asset, placement and silhouette. Correct Franki upper chain at chin/jaw: upper loop belongs at nape, rest flatter against shirt. Inspect front, side and back in actual Play Mode, compare unchanged Sacat. Compile and focused fit checks, record limits of motion/outfit coverage. Provide linked complete workflow guide covering map system, Grand Bay buildings, measurements/elevations, tools, validation and per-character chain fitting. No EXE requested.

## Final reconciliation with Claude MINI-178/179

During this work, Claude completed a newer Franki double-loop placement and restored ChainGarmentFit.cs to HEAD. User stated Claude had set everything up; repository inspection found MINI-178 profile, MINI-179 skin work and released ownership. The visual register records Franki accepted "not perfect but good for now". Preserve that newer state. MINI-176's mesh-remapping experiments are superseded, not shipped fixes. No runtime script diff remains from MINI-176.

Rejected experiments: lowering the crown buried the sides and placed the back too low; radial anatomical projection stretched toward shoulders; parameter remapping distorted link spacing or left excessive rear clearance. Front, side, back and settled screenshots caught these problems. Do not restore the archived branch under Logs/Tasks/MINI-178. Sacat source/profile/shared algorithm remain unchanged against HEAD.

Current result is Claude's FrankiChainPlacement.asset assigned in the expansion scene. Independent Codex Unity batch/Play Mode rerun: `UpIzUpMini.EditorTools.Mini178FrankiChain.Verify`, log `Logs/mini176-verify-current.log`, marker MINI178_VERIFY_PASS. Saved-scene profile path confirmed; two loops/12254 vertices. Bounds relative neck: (.202,.322,.200), highest rear+.111, lowest-.211. Fresh front/side/rear renders inspected in Logs/Tasks/MINI-178/Renders. Side view still shows some rear stand-off; do not claim exact skin contact or physically collision-free anatomy. Current user acceptance is preserved. No movement, alternate-shirt, EXE or live-scene claim.

Deliverables: Docs/CLAUDE-GRANDBAY-BUILDINGS-CHAIN-WORKFLOW.md (latest procedural map, references, dimensions, elevations, building/road/coast validation, chain profile transfer and transform pitfalls, future-character recipe); links to the earlier complete Blender MINI-142 house guide and Geneva district guides. Mini176ChainProof.cs remains a reproducible capture harness, not a fitter or comprehensive assertion test. Its earlier output images depict rejected experiments; current appearance evidence is MINI-178.

Exact command pattern: Unity6000.3.10f1 -batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini178FrankiChain.Verify -logFile "E:\Unity\Up Iz Up Mini\Logs\mini176-verify-current.log". Rendering requires graphics; omit -nographics and let the Play Mode harness exit itself. Relevant existing wardrobe regression is Mini166RepairValidation.Run (canonical scene; not proof of expansion profile placement).

Claude scene/profile/material/editor files, ObjectiveMarker.mat and packages-lock.json were left intact. Shared handoff/visual-register edits include Claude work and are not swept into the MINI-176 documentation checkpoint. No map regeneration or EXE rebuild performed in MINI-176.

## Status — Claude continuation (2026-09-20)
Claim taken from Codex on the user's instruction; Codex's uncommitted edits were kept, not reverted.

**Current Franki method** (`ChainGarmentFit.cs`, `franki` branch only; Sacat path is gated out and unchanged in the diff): each of the 48 oval bins is remapped to anatomical targets around the neck bone (height -.22..+.075 m, half-width .105 m, depth .15 -> -.055 m), the front is ray-snapped to the baked outer garment/body surface with .011 clearance, and the rear crown (t > .80) is ray-snapped to the actual nape with .008 offset. Bin shifts are interpolated over the untouched source loop as before.

**Evidence:** `Logs/mini176-chain.log` (no `error CS`, `MINI176_CHAIN_CAPTURE_PASS`, `MINI176_CHAIN_SETTLED_CAPTURE_PASS`), run 07:24:55 after the last code edit 07:24:39. `Logs/Tasks/MINI-176/ChainProof.txt`; renders in `Logs/Tasks/MINI-176/Renders/` (front/side/back/settled for both characters).

**Visual inspection (Claude):** Franki front — upper loop sits at the nape/shoulder line, no chin/jaw contact, drape lies flat on the shirt. Side — chain runs nape -> shoulder -> chest with no obvious buried run. Back — clasp cluster centred at the nape. Sacat settled render inspected: unchanged double chain.

**Not done / limits:** rear-view outer links at the shoulders look crumpled/stretched — close-up not inspected. No idle/walk/run/turn/crouch/bike motion test; AccessorySwing (up to .045 m, not collision-aware) may pull links through the shirt. No pixel diff against the approved MINI-173 Sacat image (renders use a different harness camera), so "Sacat unchanged" rests on the code diff plus visual inspection. No wardrobe-swap refresh test with other shirts. No EXE built. Franki approval is NOT recorded; the register still says Franki rejected until the user reviews these images.

**User decision (2026-09-20):** after these renders the user said they will revert the Franki chain work because they don't like it, and prefer Sacat's chain. The `franki` branch in `ChainGarmentFit.cs` is therefore rejected; the user is doing the revert. Nothing further was changed by Claude.

Existing Mini166RepairValidation.Run passed: all designs/bones/bounds, independent tint masks, save/load and legacy defaults, UI Restore/Cancel/Apply, removable accessories and character isolation. Log Logs/mini176-wardrobe-regression.log. Unity's incidental PlayerSettings default serialization from that test was reverted (file was clean immediately before the test).
