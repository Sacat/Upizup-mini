# MINI-163 — hair: Franki fixed and shipped, Sacat attempted and rejected

```yaml
task_id: MINI-163
title: Fix Franki's broken hair texture; attempt real hair geometry for Sacat
request_owner: User
integrator: Claude
status: evidence_ready
approval_class: C
budget:
  claude_time: one session, Franki fix succeeded (2 iterations), Sacat attempted and rejected (3 iterations, stopped before shipping)
  external_credits: 0
  stop_condition: do not integrate a visibly broken result
reserved_files:
  - Tools/CharacterPipeline/mini163_franki_hair.py
  - Tools/CharacterPipeline/mini163_sacat_hair.py
  - Assets/UpIzUpMini/Editor/Mini163IsolateFrankiHair.cs
  - Assets/UpIzUpMini/Editor/Mini163IntegrateFrankiHair.cs
  - Assets/UpIzUpMini/Art/Characters/Garments/Franki_Hair.fbx
  - Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Hair.fbx (produced but NOT integrated)
  - Assets/UpIzUpMini/Scenes/GrandBayProof.unity
  - Docs/WorkPackets/MINI-163.md
  - Docs/Systems/Characters.md
  - Docs/CURRENT.md
  - PROJECT-HANDOFF.md
  - TASKS.md
  - CHANGELOG.md
  - Builds/GrandBayProof/
  - Logs/Tasks/MINI-163/
depends_on:
  - MINI-162 (identified the two hair problems: Sacat has no real hair, Franki's hair texture is broken)
```

## Franki: fixed and shipped

Root cause (from MINI-162's isolated render): `Ch28_Hair`'s material
("Ch28_hair") points at the shared `Ch28_1001_Diffuse` BODY atlas - the
hair mesh's own UVs sample nonsense pixels there, producing a glitchy
stripe pattern. `Ch28_Eyelashes` shares that exact material instance, so
it could not be edited in place.

The mesh SHAPE itself was already a reasonable short/faded haircut
(confirmed by an isolated render before touching anything) - this was a
material fix, not a rebuild:

1. Gave `Ch28_Hair` its own cloned material (flat dark hair colour, Base
   Color set directly on the Principled BSDF node - `material.diffuse_color`
   alone does not drive the actual render, the same bug hit and fixed on
   MINI-157's Franki shirt colour).
2. Added a small multi-frequency sine displacement along each vertex's
   normal for a wavy surface read, fading out near the bottom rim so the
   hairline edge stays clean.
3. Split off a second flat material for the lowest ~32% of the mesh's own
   height range, blended toward a natural fade tone - approximates a
   shape-up fade transition.
4. Integrated via the same bone-remap-by-name technique as MINI-159/161/162
   (reused, not reinvented) - only `Ch28_Hair`'s mesh/material/bones
   changed, every other renderer on Franki confirmed untouched by name in
   the integration log.

Verified in the live scene by rendering and looking, twice (once before
saving as a preview-only pass, once after saving) - genuinely reads as a
short wave/fade haircut now, not the glitchy stripe pattern.

## Sacat: attempted, rejected, NOT shipped

Sacat has no separate hair mesh at all - what read as a "cap" in every
prior screenshot is a smooth dome baked directly into the `Ch06` head
geometry/texture, with a hard seam at ear level (MINI-162 finding).
Attempted a real fix in two parts:

1. **Dome retexture** - first attempt repainted the shared
   `Ch06_1001_Diffuse` atlas directly (same rasterize technique as
   MINI-157). This **broke the headphones' colour** - they reuse the same
   pixel region via UV tiling even though their faces weren't part of the
   reclassified dome region, so overwriting those pixels corrupted them as
   a side effect. Caught by rendering and looking, not assumed safe.
   Fixed by switching to a separate flat-colour material index instead
   (zero shared pixels with anything else, matching the technique already
   proven safe for Franki's shirt/pants/hair colours) - re-rendered,
   confirmed the headphones were no longer affected.
2. **Hair geometry** - reused Franki's now-fixed hair shape (not invented
   fresh) and attempted to fit it onto Sacat's head via a bounding-box
   remap (measure the dome region's bounds, scale/translate Franki's hair
   mesh to match). **This produced a visibly broken result** - a flat,
   distorted band overlapping the face, not a hair shape sitting properly
   on the head. A real bug in the bounding-box-only remap approach (it
   doesn't account for the two characters' different head proportions/
   orientations correctly).

**Stopped here rather than shipping it.** `Sacat_Hair.fbx` was exported to
disk for reference but was never wired into the live scene - the dome
retexture and hair attachment were never integrated together, and Sacat's
live `Ch06` renderer is unchanged from before this packet (confirmed:
only `Ch28_Hair`, Franki's renderer, appears in this session's scene
diff).

## Acceptance scorecard

- [x] Franki: broken hair texture root-caused (not guessed)
- [x] Franki: fixed with own dedicated material, verified by rendering before AND after saving
- [x] Franki: integrated into live scene, other renderers confirmed untouched by name
- [x] Franki: Windows build succeeded, 15s smoke clean
- [x] Sacat: attempted, caught a real regression (headphones corruption) before it shipped, fixed the technique
- [ ] Sacat: hair geometry - attempted, produced a broken result, correctly NOT shipped
- [ ] Sacat: real hair - still not done, needs a properly Sacat-fitted approach (not a raw bounding-box transplant from a different character's head shape)

## Evidence

| Claim | Method | Result/path | Human check owed? |
|---|---|---|---|
| Franki hair mesh shape already reasonable | Isolated render (hide all but Ch28_Hair) | `Logs/Tasks/MINI-163/FrankiHair-Front/Side.png` | No |
| Franki hair fix works | Standalone Blender render, then live-scene preview + post-save render | `Logs/Tasks/MINI-163/HairFixed-Side.png`, `Live-Franki-Head-Side.png` | Yes - final look |
| Franki other renderers untouched | Integration log (named renderer list) | `Logs/MINI-163-integrate.log` (Ch28_Hair only line) | No |
| Build/smoke | Unity batch build + 15s headless run | `Logs/MINI-163-build.log` (409,318,629 bytes), `Logs/MINI-163-smoke.log` (0 exception/error/fatal) | No |
| Sacat dome-texture-repaint broke headphones | Standalone Blender render, looked at it | `Logs/Tasks/MINI-163/SacatHair-Side.png` (first version, not kept) | No - self-caught |
| Sacat hair-geometry transplant is broken | Standalone Blender render, looked at it | `Logs/Tasks/MINI-163/SacatHair-Side.png` (final version) | No - self-caught, not shipped |

## Handoff

- Files changed: new `Ch28_Hair`-only material/mesh wired into the live `GrandBayProof.unity` (Franki); new Blender/Editor tools; `Sacat_Hair.fbx` exists on disk but is NOT referenced by anything live.
- Decisions made: Franki's hair fix technique (own material, flat colour + fade band, small wave displacement) is the pattern to reuse. Sacat needs a different technique than "remap another character's hair mesh via bounding box" - likely building geometry that respects Sacat's own head shape directly, or a more careful fit (matching head-bone orientation axes, not just a linear per-axis scale).
- Visual locks added/changed: none.
- Known limitations: Sacat still has no real hair (same as before this packet - not worse, not better). Franki's neckline (MINI-162) and hair are both real, shipped improvements.
- Next action: Sacat's hair needs its own scoped attempt with a better fitting method; user should playtest Franki's fix in the meantime.
- Ownership released: yes, at the end of this session's pass.
