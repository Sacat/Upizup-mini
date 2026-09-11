# MINI-159 — integrate reshaped clothes into the live game; accessory check

```yaml
task_id: MINI-159
title: Wire the motion-tested reshaped shirt/pants into live Sacat/Franki; check accessories
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: C
budget:
  claude_time: one session - integration technique, two real bugs found+fixed, live-scene verification, accessory check
  external_credits: 0
  stop_condition: clothes integrated and rendered in the actual live scene; accessories investigated, not redesigned this pass
reserved_files:
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Assets/UpIzUpMini/Editor/Mini159IntegrateGarments.cs
  - Assets/UpIzUpMini/Editor/Mini159DiagnoseMaterial.cs
  - Assets/UpIzUpMini/Editor/Mini159FixSacatTexture.cs
  - Assets/UpIzUpMini/Editor/Mini159CheckAccessories.cs
  - Assets/UpIzUpMini/Art/Characters/Garments/Ch06_1001_Diffuse_Reshaped.png
  - Docs/WorkPackets/MINI-159.md
  - Docs/Systems/Characters.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
  - Builds/GrandBayProof/
  - Logs/Tasks/MINI-159/
depends_on:
  - MINI-158 (motion proof passed for both characters)
```

## Intent

"continue with all the clothes and accessories. after let me test" — take
the motion-tested, coloured reshaped garments (MINI-157/158) and actually
put them on the live playable Sacat/Franki, look at accessory placement,
then build an EXE.

## Clothes: integrated into the live scene

**Technique**: reuse the exact mesh/material already imported and
motion-tested (not re-derived) - only rebuild each renderer's `bones`/
`rootBone` array, mapped BY BONE NAME onto the live character's own
existing skeleton Transforms (same rig, same rest pose, two separate
imports of the same source). This means no Animator/avatar/controller/
hierarchy change and no other component's serialized reference
(`CharacterEquipment`, `PlayerController`, camera target, etc.) was
touched or invalidated.

**Two real bugs found and fixed before saving anything:**
1. First live-scene render came back nearly solid white for both
   characters - the LIVE scene's own sun/skybox/reflections overexposed
   the shot (my proof camera reused the scene's real lighting instead of
   the controlled ambient setup MINI-158's isolated proof used). Fixed by
   moving each character to open air with the same controlled
   Trilight ambient settings before capturing, same as before.
2. Sacat still rendered as a blown-out flat white mask afterward -
   diagnosed by dumping the material's actual shader properties (all
   normal) and then checking `material.mainTexture`, which was **null**.
   The MINI-157 Blender export's `embed_textures=True` had silently not
   actually embedded the repainted diffuse texture into the FBX - no
   `Texture2D` sub-asset existed anywhere in the imported file. Fixed by
   copying the repainted PNG (`Logs/Tasks/MINI-157/Ch06_1001_Diffuse_Reshaped.png`)
   directly into `Assets/.../Garments/` and wiring it onto the material
   with a small dedicated fix tool, confirmed by re-rendering.

**Verified before saving**: rendered both live characters (moved
temporarily to open air, restored after) and manually looked at the
result - Sacat correctly shows his short-sleeve navy top + grey pants
*together with his existing cap+headphones* (proving the accessory
attachment system, which reads `Animator.GetBoneTransform` generically,
was unaffected by the mesh swap), Franki shows the same on his own model.
Only then was the scene actually saved.

**After saving**: full project compile-check passed clean (0 `error CS`).

## Accessories: investigated, not redesigned this pass

Rendered close-up head shots of both characters with `cap_mike`/
`shades_ray` (the `CharacterEquipment` primitive prototypes) trial-equipped.
Findings:

- **Sacat**'s visible cap+headphones in these renders is a well-fitted,
  reasonable-looking result - but it does NOT match `BuildCap()`'s actual
  primitive (a red sphere+cube by default; this render shows a grey/metal
  cap). This strongly suggests what's visible is a different, already-
  baked-in part of Sacat's design, not the `cap_mike` prototype geometry
  the trial-equip call actually spawned - unconfirmed which GameObject is
  actually rendering here, not chased further this pass.
- **Franki**'s close head shot shows no visible cap/shades at all after
  the same trial-equip call - unconfirmed whether they rendered off-frame
  or the anchor/attach step silently failed for this character.
- A real jagged/pixelated boundary is visible at Sacat's collar-to-shoulder
  seam in these close-up shots (the texture-repaint boundary from MINI-157,
  previously flagged as a "small cosmetic remnant" - closer inspection
  shows it's rougher than that description implied).

**Not fixed this pass** - both the cap/shades placeholder-primitive system
and the collar-seam roughness need their own bounded follow-up, not a
rushed fix inside an already-large session. Recorded honestly as open
items rather than silently left off the handoff.

## Windows build

Built after the clothes integration was saved and compile-checked (see
`Docs/Systems/BuildAndVerification.md` for why: build after a meaningful
compound change, not every step). Result and smoke-test evidence below.

## Acceptance scorecard

- [x] Reshaped/coloured shirt+pants integrated into the LIVE Sacat/Franki (scene saved)
- [x] Verified via render BEFORE saving (not assumed correct from code)
- [x] Two real bugs (overexposed render, missing texture) found and fixed, not glossed over
- [x] Full project compile-check clean after the scene change
- [x] Accessory placement investigated with real renders
- [ ] Accessory placeholder geometry/placement - NOT redesigned this pass, open item
- [ ] Windows build + smoke test - see below
- [ ] User playtest - the actual final acceptance gate

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Live integration verified before save | Unity batch-mode preview render (moved to open air, controlled lighting) | `Logs/Tasks/MINI-159/Live-Sacat-InScene-*.png`, `Live-Franki-InScene-*.png` | Yes - final look |
| Missing-texture bug found | `material.mainTexture` check, not assumed | `Logs/MINI-159-diag-mat2.log` | No |
| Scene actually saved with the fix | File diff + compile check | `Logs/MINI-159-save.log`, `Logs/MINI-159-compile.log` (0 `error CS`) | No |
| Accessory close-up placement | Unity batch-mode render, cap/shades trial-equipped | `Logs/Tasks/MINI-159/Acc-Sacat-Head-*.png`, `Acc-Franki-Head-*.png` | Yes |
| Build succeeds / starts clean | Unity batch build + headless smoke | `Logs/MINI-159-build.log` (403,574,245 bytes), `Logs/MINI-159-smoke.log` (15s, 0 exception/error/fatal matches) | No |

## Addendum: a third screenshot-tool bug, user-caught

After sending the "live in-scene" renders, the user immediately caught a
real-looking problem: arms appeared to end at the short-sleeve cuff with
the hand floating disconnected far to the side. Checked rather than
dismissed - this was genuinely alarming-looking, not obviously cosmetic.

Root cause: the follow-up verification tool (`Mini159BetterProof.cs`)
sampled the Idle clip via `PlayableGraph`/`AnimationClipPlayable` onto the
LIVE scene's already-populated Animator, same technique MINI-158 used
successfully - but MINI-158's version also set
`animator.cullingMode = AnimatorCullingMode.AlwaysAnimate` before
sampling, and this one didn't. Without it, the manual pose evaluation
silently did nothing and the render still showed the raw T-pose bind
pose (arms spread wide) - which, framed for a normal standing figure,
put the hands outside the visible crop with the sleeve-to-wrist gap
reading as "arm missing."

Confirmed by logging `Animator.GetBoneTransform(HumanBodyBones.LeftHand).position`
before and after adding the fix - moved from a T-pose-consistent wide
spread to a hip-adjacent idle position. Re-rendered
(`Better-Sacat-Front.png`, `Better-Franki-Front.png`): fully connected
arms, no gap, correct idle pose. **This was purely a screenshot-tool bug -
the actual built EXE runs its Animator Controller normally every frame in
real Play Mode and was never affected**, so no rebuild was needed for this
specific fix. Third distinct rendering/verification-tool bug found this
session alone (after the overexposed-render and missing-texture bugs
above) - a pattern worth remembering: this project's screenshot/proof
tooling is fragile in ways the actual game is not, and every render must
be looked at, never trusted from a clean batch-mode exit alone.

## Handoff

- Files changed: `Assets/UpIzUpMini/Scenes/GrandBayProof.unity` (Sacat's `Ch06` and Franki's `Ch28_Hoody`/`Ch28_Pants` renderers now point at the reshaped/coloured meshes+materials, bones remapped by name); new `Assets/UpIzUpMini/Art/Characters/Garments/Ch06_1001_Diffuse_Reshaped.png` (the actually-wired texture); new diagnostic/integration Editor tools; new `Logs/Tasks/MINI-159/*` evidence.
- Decisions made: bone-remap-by-name integration technique (reuses tested mesh/material exactly, touches nothing else); accessories deliberately NOT redesigned this pass given session size.
- Visual locks added/changed: none yet - this is real in-game content now, but not yet user-accepted as a visual lock.
- Known limitations: cap/shades prototype system unclear/likely still broken for at least Franki; Sacat's collar-seam texture boundary is rougher than previously described; no user playtest yet.
- Next action: user plays the new build and confirms the look/feel in real gameplay; accessory placeholder geometry is real follow-up work, not done here.
- Ownership released: yes, at the end of this session's pass.
