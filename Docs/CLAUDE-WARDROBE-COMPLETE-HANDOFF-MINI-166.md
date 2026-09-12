# Complete Claude wardrobe handoff — MINI-166

Prepared 2026-09-12 at the user's request. Consolidates current implementation, tools, skills, workflow, commands, evidence, known mistakes and remaining work. Follow the user's latest instruction and repository rules. Historical notes describe superseded attempts; do not rerun them blindly.

**Live update at document completion:** Claude has now claimed MINI-166 in the top handoff block for a mirrored MIKE/LACOS wordmark repair in `Mini166RepairAccessories.cs`'s shared `Word()` function. Its generated shirt/cap assets are changing. That work is newer than the verified 9c2b91c baseline documented here; its outcome has not been verified by this documentation task. Do not overwrite it or treat this handoff as a request to start a competing implementation. MINI-167 vehicle work was committed as 940cbb4 and released. Read current ownership and logs for the latest state; fixed-name evidence images may now reflect Claude's newer generation.

## 1. Task and current truth

Continue the SAME task ID, MINI-166, when the user requests further wardrobe work. Do not start another implementation merely because you received this document. The wardrobe baseline is implemented and built; exact styling was not user-approved. The newer mirrored-wordmark correction is actively claimed by Claude as noted above.

The user requested selectable fitted 3D shirts, pants, hats and shoes; wave hair visible without a cap; removable headphones; individual clothing for each character; fuller shoulders/traps closer to the original fits; and a working Windows EXE. They rejected earlier incomplete wardrobe work and authorized Codex's repair.

Current wardrobe commits:

- `9881fa3` — MINI-166: complete fitted wardrobe for Franki and Sacat.
- `9c2b91c` — MINI-166: individual outfits and fuller shoulder contours.

Verify current head with Git. New unrelated commits may exist. These are reference checkpoints, not instructions to reset the repository.

## 2. Project identity and ownership

- Writable Mini project: `E:\Unity\Up Iz Up Mini`.
- Separate full-size project: `E:\Unity\Up iz up` — read-only reference, NOT the working directory.
- `E:\Assets` is read-only reference under project rules.
- Unity: `6000.3.10f1`.
- Unity executable: `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`.
- Canonical scene: `Assets/UpIzUpMini/Scenes/GrandBayProof.unity`.
- Observed branch: `codex/mini-085-baseline-20260820`; verify rather than switching automatically.

Read the Current claim in `PROJECT-HANDOFF.md` before editing. During the implementation it contained Claude/MINI-167 above concurrent Codex/MINI-166. At document completion, the top claim changed to Claude/MINI-166 for mirrored lettering; Codex's lower claim covered this document only and is being released. Continue the existing valid claim rather than creating conflicting ownership. Only one agent may own scene/prefab/settings/import integration at a time. Recheck reservations; historical non-overlap is not current authorization.

At document creation, these unrelated dirty files were present:

- `Assets/UpIzUpMini/Editor/Mini134CrashDamageRecoveryValidation.cs`
- `Assets/UpIzUpMini/Scripts/Vehicles/BikeCrashEjectionController.cs`
- `Packages/packages-lock.json`

Do not stage, revert or overwrite unrelated edits. The vehicle edits were subsequently committed as 940cbb4; new active wardrobe edits now include Mini166RepairAccessories.cs and generated shirt/cap assets. The package-lock edit also remains outside this documentation scope. Recheck Git on arrival. A later build may include other working-tree changes; report its actual scope honestly.

## 3. Required reading order

1. `AGENTS.md` — read completely.
2. `Docs/CURRENT.md` — latest current state, especially MINI-166.
3. Current claim and relevant MINI-166 entries in `PROJECT-HANDOFF.md` and `TASKS.md`.
4. `Docs/Systems/README.md` — system router.
5. Latest MINI-166 entries in `Docs/Systems/Characters.md` and `Docs/Systems/UI.md`.
6. `Docs/WorkPackets/MINI-166.md` — User revision and Latest implemented state first; historical sections as needed for root causes.
7. `Docs/AI-PRODUCTION-WORKFLOW.md`, `Docs/WORK-PACKET-TEMPLATE.md` and applicable `Docs/VISUAL-APPROVAL-REGISTER.md` entries.
8. `Docs/CLAUDE-HANDOFF-CURRENT.md` if required by the router; its old priorities do not override current user-authorized wardrobe work.

`Docs/CODEX-CONTINUE-MINI-166.md` is a historical pointer. Its missing-Sacat-slot descriptions are superseded. This document and the latest work-packet sections describe what ships now.

## 4. What the player has

Both characters have eight designs across four independent slots:

| Slot | Designs | Stable IDs |
|---|---|---|
| Shirt | Mike Crew Tee; Lacos Polo | `shirt_tee_mike`; `shirt_polo_lacos` |
| Pants | Straight Jeans; Tailored Trousers; Denim Shorts | `pants_jeans`; `pants_trousers`; `pants_shorts_denim` |
| Hat | Lacos Curved Cap; No Hat | `hat_lacos`; `hat_none` |
| Shoes | Mike 90; Mike 97 | `shoes_mike90`; `shoes_mike97` |

No Hat is an extra menu entry, not a ninth design. These are in-game labels; do not rename IDs to real-brand spellings or imply official branded assets.

Colour indices: 0 Navy, 1 White, 2 Black, 3 Red, 4 Green, 5 Denim, 6 Silver. Preserve order for save compatibility.

Defaults:

- Franki: navy Mike tee, denim jeans, no hat, black Mike 90.
- Sacat: green Lacos polo, black trousers, no hat, white Mike 97.

Both retain the full catalogue. Each is dressed separately; deliberately matching outfits remain possible. Existing saved clothing is preserved. Matching clothes in an old save do not prove shared selections and are not authorization to erase the save.

Controls: Tab changes character. At the owned safehouse, E opens interaction and 5 opens wardrobe. Apply retains that character's choices; F5 saves. Apply/close before switching to dress the other character. The UI identifies its wearer and says changes affect only him.

Both retain black waves beneath removable caps. Sacat's headphones are independently removable. Franki has no fitted headphone assignment. Watch/shades support Wear/Remove; unowned try-ons remain session-only. No purchases or ownership grants were added.

## 5. Tools and skills actually used

No separate `SKILL.md` skill was invoked for the completed implementation. Project instructions and production documents governed it. Do not invent a Blender, Unity, Hitem3D or modeling skill and claim Codex used it.

| Tool/capability | Actual use | Claude equivalent |
|---|---|---|
| `functions.exec` + `tools.exec_command` | Orchestrated PowerShell with explicit working directory | Your terminal tool |
| `Get-Content`, `rg`, `Get-Item`, `Get-Process` | Read code/docs, find logs, inspect files/processes | Same commands |
| `apply_patch` | Targeted C# and Markdown edits | Your patch/edit tool |
| Unity batch mode | Compile, generate assets, Play Mode tests, render, build | Installed Unity executable |
| Unity C# mesh/animation/render APIs | Actual modeling, skinning and captures | Existing Editor scripts |
| Local image display | Inspected actual rendered PNGs and rejected bad shapes | Image-read tool/viewer |
| FFmpeg | PNG comparisons and MP4 assembly | Same CLI |
| Git | Review, scoped staging and checkpoints | Same CLI |

Not used for the final repair: Blender modeling/export, AI image generation, web research, paid assets, Hitem3D credits, uploads, new plugins or subagents.

Codex tool names are not automatically callable in Claude. Match capabilities to your tools. If your environment provides relevant skills, read their real instructions before using them; they are optional assistance, not a missing project dependency. No browser/native UI automation is required for the batch workflow. DOCX-writing skills are unnecessary for Markdown handoffs.

Codex's default workspace was the WRONG project and Mini was outside writable roots, so calls used explicit Mini `workdir` and `sandbox_permissions: "require_escalated"`. This is environment-specific. Use your actual access controls; never bypass rejection. One refinement call hit an account usage-limit rejection and resumed after the user said continue; that was not a Unity failure.

## 6. Implementation map

Paths are relative to Mini.

| File | Responsibility |
|---|---|
| `Assets/UpIzUpMini/Scripts/Character/OutfitWardrobe.cs` | Pieces, bindings, selection, colour blocks, capture/restore |
| `Assets/UpIzUpMini/Scripts/Character/CharacterEquipment.cs` | Legacy compatibility, accessory equip/remove |
| `Assets/UpIzUpMini/Scripts/Character/WardrobePreviewMesh.cs` | Immediate preview skinning |
| `Assets/UpIzUpMini/Scripts/UI/VisualWardrobePanel.cs` | Wearer-only UI, portrait, ChoosePiece, RestoreOpeningOutfit, Apply/Cancel, EXE proof |
| `Assets/UpIzUpMini/Scripts/SaveLoadSystem.cs` | Separate character outfit/headphone save hooks |
| `Assets/UpIzUpMini/Editor/Mini166Repair.cs` | Constants, audit, visibility finalization |
| `Assets/UpIzUpMini/Editor/Mini166RepairMesh.cs` | Surface conversion, clipping, weights, retarget, shape builder, asset/material writing |
| `Assets/UpIzUpMini/Editor/Mini166RepairClothes.cs` | BuildCharacter, defaults, shirts/pants/legs, ShoulderForm |
| `Assets/UpIzUpMini/Editor/Mini166RepairAccessories.cs` | Cap, distinct shoes, wordmarks |
| `Assets/UpIzUpMini/Editor/Mini166RepairRender.cs` | Static captures and shoulder baseline |
| `Assets/UpIzUpMini/Editor/Mini166RepairMotion.cs` | Sampled motion/deformation |
| `Assets/UpIzUpMini/Editor/Mini166RepairValidation.cs` | Actual Play Mode regression harness |
| `Assets/UpIzUpMini/Art/Characters/Garments/Outfits166/` | Generated meshes/materials/weave textures |
| `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` | Integrated playable scene |

## 7. How the 3D garments were built

The important correction was reusing already rigged continuous topology in Unity instead of repeatedly exporting/reimporting Blender meshes and guessing bone remaps.

Franki corresponds to Strong/Ch28 with separate mesh objects. Sacat corresponds to Mainchar/Ch06, originally fused. Old notes once reversed those names; trust live renderer evidence.

Franki's retained repaired sources include `Franki_ArmsRestored.fbx` and `Franki_ReshapedGarments_Colored.fbx`. Old renderers stay disabled as repeatable generation sources. Do not delete them as cleanup.

`Surface` reads positions, normals, UVs, weights and bind poses, transforms them into character-root coordinates, and exposes rest-bone queries. `Retarget` matches bone suffixes, uses corresponding target rest matrices multiplied by source bind matrices, weights the resulting positions/normals per vertex, and remaps bone indices. It fits clothes to Sacat's skeleton; it does not transplant Franki's head/body/rig.

Visible body surfaces retain heads/hands/hair while covered old clothing and footwear are masked. Sacat's fused material was not a reason to leave slots missing: classify geometry and bind separate surfaces.

`Clip` cuts polygons at boundaries and interpolates position, normal, UV and weights. The strongest four blended influences are normalized. Shorts use interpolated hems and smoothly weighted lower legs, rather than merely colouring trouser-shaped legs as skin.

`Shape` accumulates vertices and material-submesh triangles and emits a skinned Mesh. Jeans and trousers differ in shaping. Mike 90 has angular panels/heel windows; Mike 97 has flowing bands/longer windows. The cap has a curved brim and panel crown rather than sphere/cube geometry.

`Asset` preserves paths/GUIDs and explicitly assigns mesh channels to existing Mesh assets. Do NOT substitute `EditorUtility.CopySerialized` for this Mesh path: it previously produced stale/corrupted native vertex buffers. CopySerialized remains appropriate for some non-Mesh objects.

Skin, fabric, buttons and soles have separate material slots. Cotton/denim use two 128x128 procedural weave textures. Shared materials hold base properties; per-renderer indexed property blocks provide independent colours.

## 8. Shoulder/trapezius refinement

The user reported dips between neck and shoulders and requested a muscular shape like the original fits. Previous garment shaping/hood flattening left an unconvincing transition.

`ShoulderForm` in `Mini166RepairClothes.cs` adds localized rest-space volume to tee and polo:

- Inner horizontal SmoothStep: absolute x .07 to .125.
- Outer fade: .82 to 1.45 times shoulder-bone half-width.
- Height blend: LeftArm rest height minus .095 to plus .055.
- Gaussian trap falloff: centre .55 times shoulder half-width, spread .38 times width.
- Vertical addition: `shoulder * (.005f + .016f * traps)`.
- Depth: `(p.z - neck.z) * shoulder * .12f`.
- Small outer volume: `sign(p.x) * shoulder * .006f * (1-traps)`.

Original topology and weights remain. Central differences with step .0001 form a deformation Jacobian; its inverse transpose transforms normals. Do not move vertices but leave misleading undeformed normals.

The first trial used inner blend .056 to .09 and vertical `.014 + .044 * traps`. Inspection showed steep neck spikes, so Codex REJECTED it. The committed smaller/wider blend is the final implementation candidate, still awaiting user styling approval. Do not restore the rejected coefficients.

No rig scaling, head movement, sleeve-end edits, accessory-transform normalization or body replacement was used.

## 9. Selection, persistence, UI and rendering

Each OutfitWardrobe has its own choices dictionary and separate Shirt/Pants/Hat/Shoes renderer bindings. `Current` and `Capture` copy choice data. `Restore` applies the character's defaults, then saved choices. SaveLoadSystem has separate `sacatOutfit`, `frankiOutfit` and unequipped-accessory lists. Preserve stable IDs and colour order.

Matching starting clothes came from identical `BuildCharacter` defaults, not a shared dictionary. The revision changes defaults and strengthens isolation checks. It does not rewrite old saves or forbid matching designs.

`Select` assigns a piece's mesh/materials to its slot renderer and uses indexed MaterialPropertyBlocks for permitted tint slots. Never mutate shared material colours to make a selection. Clear obsolete property blocks when swapping pieces.

Raw mesh bounds previously assigned to renderer.localBounds made garments disappear because coordinate spaces differed. Selection uses updateWhenOffscreen instead of trusting a wrong culling volume. This correctness fix is not a claim that offscreen updating is free on mobile.

Same-frame/paused mesh swaps previously produced stale portrait skinning. `WardrobePreviewMesh.Bake` performs immediate CPU skinning for readable meshes, with BakeMesh fallback for GPU-only imports. Gameplay remains normal SkinnedMeshRenderer rendering.

Portrait copies indexed colour blocks, not just renderer-wide blocks. Front orientation and width-aware framing were corrected. Playable Animators use AlwaysAnimate; active original skin renderers including hair/headphones update offscreen so portraits do not depend on the gameplay camera seeing them.

Buttons and tests share `ChoosePiece` and `RestoreOpeningOutfit`. Restore clears pending colours; Cancel restores opening clothes/accessories; Apply retains. Changing active character closes/cancels an open wardrobe. The duplicate primitive-cap accessory button was removed; use Hats.

## 10. Unity entry points and side effects

Prefix every method below with `UpIzUpMini.EditorTools.`.

| Method | Effect/caution |
|---|---|
| `Mini166Repair.Audit` | Loads scene, writes audit, no scene save; may overwrite baseline-audit.txt |
| `Mini166Repair.ShoulderBaseline` | Renders current scene without generation; overwrites standard evidence filenames |
| `Mini166Repair.Preview` | REGENERATES task assets then renders; does not save scene; NOT read-only |
| `Mini166Repair.Integrate` | Regenerates assets/wardrobe definitions/defaults, saves scene, renders |
| `Mini166Repair.FinalizeVisibility` | Saves Animator/offscreen-skin settings; already applied |
| `Mini166Repair.Motion` | Loads scene, checks sampled deformation, writes 432 frames; no scene save |
| `Mini166RepairValidation.Run` | Actual Play Mode tests; preserves original PlayerPrefs save and exits |
| `Mini001Build.BuildWindowsPlayer` | Builds Windows player to existing output path |

Methods exit the Editor themselves. Run one Editor at a time; do not launch a second against the same project. Do not use `-nographics` for captures: it produced blank images here.

Do not run `Mini011PhaseBSetup` or broad scene builders for a shirt fix. Historical MINI-166 integration scripts can overwrite the repaired configuration. Preview and Integrate both write task assets; run only when authorized generator changes require them.

## 11. PowerShell cookbook

Run each stage separately and inspect its result before dependent work. Sample names use `claude` to avoid overwriting original logs; choose fresh names for later iterations.

### Inspect and claim

```powershell
Set-Location 'E:\Unity\Up Iz Up Mini'
git status --short
git log -5 --oneline
Get-Content AGENTS.md
rg -n 'MINI-166|Current claim|current_owner|active_task' PROJECT-HANDOFF.md
Get-Process -Name Unity -ErrorAction SilentlyContinue
# After claiming a valid reservation:
& .\Tools\AIWorkflow\Invoke-Preflight.ps1 -Agent Claude -TaskId MINI-166
```

The current preflight parser only reads the first Current claim pair. It reported Claude/MINI-167 during Codex's explicitly reserved concurrent MINI-166 work. Do not overwrite another agent's claim to pass the parser. Inspect and document actual non-overlap; resolve genuinely ambiguous ownership before editing.

### Launch tests

```powershell
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
$wardrobeJob = Start-Process -FilePath $unityExe -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini166RepairValidation.Run -logFile "E:\Unity\Up Iz Up Mini\Logs\mini166-claude-tests.log"'
$wardrobeJob.Id
```

A returned PID means STARTED, not passed. After a reasonable interval, inspect the process/log. Do not repeatedly relaunch or terminate all Unity processes. Track only the process you started.

```powershell
Get-Process -Id $wardrobeJob.Id -ErrorAction SilentlyContinue
Get-Content Logs/mini166-claude-tests.log -Tail 30
rg -n 'REPAIR_TEST_PASS|error CS|Exception' Logs/mini166-claude-tests.log
```

For a different entry point, replace executeMethod and log path. If your shell does not preserve variables, retain the PID explicitly. Keep the user informed during long runs. Coordinate an already open interactive Editor instead of forcing another instance.

### Build after checks pass

```powershell
$wardrobeBuild = Start-Process -FilePath $unityExe -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini001Build.BuildWindowsPlayer -logFile "E:\Unity\Up Iz Up Mini\Logs\mini166-claude-build.log"'
```

After completion:

```powershell
rg -n 'BUILD SUCCEEDED|BUILD FAILED|error CS' Logs/mini166-claude-build.log
```

The launcher EXE timestamp can remain old. Check the build log and payload freshness, e.g. UpIzUpMini_Data/level0 and Managed/Assembly-CSharp.dll. Keep adjacent data/DLLs with the executable.

### Actual EXE proof

```powershell
$wardrobePlayer = Start-Process -FilePath 'E:\Unity\Up Iz Up Mini\Builds\GrandBayProof\UpIzUpMini.exe' -WindowStyle Hidden -PassThru -ArgumentList '-batchmode -wardrobe-proof "E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-166\Repair\Claude" -logFile "E:\Unity\Up Iz Up Mini\Logs\mini166-claude-player.log"'
```

After completion:

```powershell
rg -n 'WARDROBE_LIVE_PROOF_COMPLETE|Exception|Error' Logs/mini166-claude-player.log
Get-Item Logs/Tasks/MINI-166/Repair/Claude-*-player.png
Get-Item Logs/Tasks/MINI-166/Repair/Claude-*-portrait.png
```

The explicit proof flag never runs in normal play. It switches characters, temporarily selects their defaults, captures GPU-skinned players and actual portrait textures, restores opening outfits on close and exits. It does not save test selections. It does not prove every combination or simulate all button clicks.

## 12. Visual inspection and FFmpeg

Read actual PNGs with an image-capable tool. Codex used local image display; when direct sandbox access did not reach Mini, an approved PowerShell read provided PNG bytes for display. It was the same Unity render, not an edited concept. Claude should use its permitted image viewer rather than assume Codex-only tools exist.

`Mini166RepairRender` uses a fixed sampled idle pose, neutral lighting and 640x800 images. It bakes temporary static copies to avoid stale Editor GPU skinning between same-frame selections, copies colour blocks, renders, then destroys temporary objects and restores renderer visibility. This static evidence does not replace actual EXE proof.

Keep before/after images separate. `ShoulderBefore/` holds pre-revision views. Render methods write fixed names; archive earlier evidence to a new task-local directory before overwriting it when comparison is needed.

FFmpeg assembled existing evidence; it did not modify source textures or conceal defects. Actual-player comparison:

```powershell
ffmpeg -y -loglevel error -i 'Logs/Tasks/MINI-166/Repair/Individual-Franki-player.png' -i 'Logs/Tasks/MINI-166/Repair/Individual-Sacat-player.png' -filter_complex '[0:v][1:v]hstack=inputs=2' -frames:v 1 'Logs/Tasks/MINI-166/Repair/Individual-Outfits.png'
```

Motion assembly:

```powershell
ffmpeg -y -loglevel error -framerate 15 -i 'Logs/Tasks/MINI-166/Repair/Motion/Franki-0-Run-%03d.png' -framerate 15 -i 'Logs/Tasks/MINI-166/Repair/Motion/Sacat-0-Run-%03d.png' -filter_complex '[0:v][1:v]hstack=inputs=2' -c:v libx264 -pix_fmt yuv420p 'Logs/Tasks/MINI-166/Repair/Shoulders-Run-Comparison.mp4'
```

These examples overwrite their named output because of -y; choose fresh names to preserve existing proof. Combining screenshots does not make them simultaneous gameplay capture. Franki left and Sacat right were captured separately.

## 13. What verification actually proves

Actual Play Mode validator checks:

- Two wardrobes, nine menu entries including No Hat.
- Four distinct slot renderers per character and no cross-character renderer sharing.
- Selection visibility, material/submesh counts, bone palettes, finite deformed geometry and reasonable bounds.
- Distinct shoe mesh references and rejection of invalid selections.
- Indexed fabric tint with untinted skin/sole slots.
- Distinct default shirt/pants choices.
- Real two-character SaveLoadSystem.Save/Load and headphone persistence.
- Legacy absent outfit fields falling back to defaults.
- UI ChoosePiece, RestoreOpeningOutfit, Cancel, Apply and portrait tint.
- Other character's selections, actual meshes and indexed colours unchanged during UI operations.
- Accessory toggles and retained body visibility.

The test backs up/restores `UpIzUpMini.Save.v1` in PlayerPrefs, including original absence. Preserve that protection; never replace the user's save with a test fixture. Avoid another game session changing the same save during persistence testing.

Motion produces 432 frames: two characters, three outfit combinations, four sequences, eighteen frames. Tee coverage is explicitly selected so Sacat's new polo default does not eliminate tee testing. Sequences sample walk/run clips, motorcycle idle and a manual knee-flex sweep. KneeFlex is NOT an implemented gameplay crouch; motorcycle idle is NOT seated bike-fit certification. Old files labeled Seated came from a mislabeled earlier attempt and are superseded.

Codex inspected front/back shoulders and sampled moving poses. Automated bounds/screenshots cannot certify all clipping or human-perceived styling. State the limits rather than claiming perfection.

## 14. Evidence and delivered build

| Evidence | Observed result |
|---|---|
| `Logs/mini166-shoulders-before.log` | SHOULDER_BASELINE_PASS |
| `Logs/mini166-individual-tests.log` | REPAIR_TEST_PASS, actual Play Mode |
| `Logs/mini166-shoulders-refined.log` | INTEGRATE_PASS and PREVIEW_PASS, final assets |
| `Logs/mini166-shoulders-motion.log` | REPAIR_MOTION_PASS, 432 frames |
| `Logs/mini166-individual-build.log` | BUILD SUCCEEDED |
| `Logs/mini166-individual-player.log` | WARDROBE_LIVE_PROOF_COMPLETE, exit and no Exception/Error matches observed |

Order: independence tests passed before the final smaller shoulder-coefficient refinement. Subsequent final integration/compile, motion, build and EXE proof covered final geometry. Do not imply every test reran after every cosmetic coefficient edit.

Latest delivered output: `Builds/GrandBayProof/UpIzUpMini.exe`, packaged total 420,366,933 bytes, build about 15.41 seconds. This is the whole build size, not launcher size. Earlier 420,322,677-byte output belongs to the preceding repair checkpoint.

Current images/video under `Logs/Tasks/MINI-166/Repair/`:

- `Individual-Outfits.png` — actual player comparison, Franki left/Sacat right.
- `Individual-Franki-player.png`, `Individual-Sacat-player.png` — separate GPU captures.
- `Individual-Franki-portrait.png`, `Individual-Sacat-portrait.png` — portraits.
- `Franki-Shoulders-Front.png`, `Franki-Shoulders-Back.png`, Sacat equivalents.
- `ShoulderBefore/` — pre-revision views.
- `Shoulders-Run-Comparison.mp4` — revised tee running sample.
- `Motion/` — regenerated frame sequences.

Earlier Final-* captures show the previous wardrobe state; Individual-* show distinct defaults/revised shoulders. Earlier Built-* exposed T-pose/headphone culling problems and are not current success evidence.

## 15. Performance and known limits

Approximate triangles per character from existing budget files:

- Shirts: 8.7k including retained arms.
- Jeans: 8k; trousers: 7.9k.
- Shorts: 5.5k including lower legs.
- Cap: 2.4k.
- Mike 90 pair: 2.8k; Mike 97 pair: 6.1k.

Four to five material slots per piece, some empty; two 128x128 weave textures. Shoulder revision changes four shirt meshes without extra topology/textures/materials. No cloth physics, gameplay lights or colliders added. Original high-detail sources remain. No mobile performance certification or finished LOD solution is claimed. Offscreen updates and CPU portrait baking have costs; profile before promising performance.

Style is stylized game geometry, not photoreal reference replication. Original neck/face/hair/accessory details retain prior limitations; a shoulder correction does not certify every seam. Protected manual chain/watch placement must not be normalized as cleanup.

## 16. Failure patterns to avoid

1. Assuming both characters share mesh layouts, or reversing identities.
2. Deleting sleeve geometry because a full hidden arm is assumed. Earlier attempts left floating hands; retain repaired continuous arm coverage.
3. Treating Sacat's fused material as an unsolvable blocker.
4. Recolouring shared materials and tinting skin or the other character.
5. Assigning localBounds from an incompatible mesh coordinate space.
6. Trusting same-frame BakeMesh without checking stale skinning.
7. Copying only renderer-wide blocks when colours are indexed.
8. CopySerialized for Mesh regeneration.
9. Using -nographics then calling blank captures visual proof.
10. Fixed world camera axes when character roots are rotated; actual-player captures must respect root forward/right.
11. Calling Preview read-only or Integrate a harmless test; both write assets.
12. Broad/old scene builders overwriting repaired configuration.
13. Restoring rejected steep traps or moving bones to hide garment defects.
14. Overwriting saves to force new defaults.
15. Calling eight clothing designs eight complete outfits, or claiming Franki has fitted headphones.
16. Calling process launch, compile or static capture complete gameplay acceptance.
17. Staging unrelated vehicle/package edits or releasing another task's claim.

## 17. Workflow for the next requested change

1. Read the latest user feedback and state the specific defect/target. Do not invent new scope.
2. Read instructions/history and inspect Git, ownership and running Unity processes.
3. Claim MINI-166 with exact reservations. Extend its bounded work packet instead of duplicating the task.
4. Reproduce using the current scene/EXE. Preserve before evidence for visible changes.
5. Determine whether the defect is geometry, skinning, material blocks, culling, UI routing, defaults or saves. Not every visible defect is a mesh problem.
6. Make the smallest reversible patch. Honor approval already given; seek a new decision for new scope or consequential unresolved choices.
7. Inspect a cheap geometry preview and account for generator side effects. Stop when the requested defect is fixed rather than redesigning everything.
8. Run compile/relevant tests. Use actual Play Mode for persistence/menu claims and motion for deformation claims.
9. Inspect final static/moving outputs and fix visible failures. Do not ask the user to approve a result you know is broken.
10. Build when requested/needed for delivery, verify success and inspect actual player output.
11. Update work packet, Characters/UI ledgers, CURRENT/TASKS, CHANGELOG and DECISIONS as relevant. Record a visual lock only when the user actually approves it.
12. Review diff, stage exact owned files, commit and release only your claim. Report behavior, evidence and human checks concisely.

Documentation-only changes require Markdown/path/command and scope checks, not Unity regeneration or a fictional new build.

## 18. Completion checklist

- [ ] Latest user request addressed without scope drift.
- [ ] Correct project/reservation verified.
- [ ] Saves, IDs, source renderers and protected placement preserved.
- [ ] Both characters checked; one-character edits leave the other unchanged.
- [ ] Before/after evidence inspected for visible changes.
- [ ] Appropriate compile/Play Mode/motion logs show actual success.
- [ ] Requested EXE rebuilt and inspected.
- [ ] Claims distinguish automated evidence from user approval.
- [ ] Scoped documentation/checkpoint complete; unrelated work untouched.
- [ ] Own claim released.

## 19. Copy/paste startup instruction

> Continue the existing MINI-166 wardrobe task in E:\Unity\Up Iz Up Mini. Read AGENTS.md, Docs/CURRENT.md, current ownership reservations, then Docs/CLAUDE-WARDROBE-COMPLETE-HANDOFF-MINI-166.md and its latest work-packet/system references. Latest wardrobe checkpoint is 9c2b91c. Preserve unrelated dirty vehicle/package files and saved outfits. Claim MINI-166 before editing. Use existing Unity C# garment/preview/test tools; do not regenerate the whole scene or rerun historical failed integration scripts. Address my latest specific feedback, inspect actual renders, run appropriate tests, rebuild when required, document evidence, commit only your files and release your claim. Receiving this handoff alone is not a request to redesign or rebuild the already completed wardrobe.
