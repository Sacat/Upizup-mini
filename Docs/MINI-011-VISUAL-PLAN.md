# MINI-011 — Grand Bay Production Vertical Slice: Phase A Visual Plan

Status: **Phase A only.** Production scene/script work (Phase B onward) is
not claimed or started yet. This document is the gate the corrective-rebuild
brief itself requires before that work begins — see "Phase A gate" at the
bottom.

## Tool limitation, stated up front

I have no way to capture a screenshot of the Unity Editor's Game view or a
running desktop window — my available tools only see a sandboxed in-app
browser pane and command-line/log output, not your actual screen. I cannot
produce the "before" Game-view screenshots the brief asks for, or any of the
Gate B/C/D screenshots, myself. What I can do instead, as already
established on `MINI-001`: build you real Windows `.exe` builds you run and
look at yourself, and describe scene content precisely from the source data
(scene file contents, component values) rather than from a rendered image.
Visual sign-off stays a human step throughout this task.

## Before (MINI-001, current state — described, not screenshotted)

`GrandBayProof.unity` is exactly what the corrective brief objects to:
- Player and NPC are Unity capsule primitives with flat-colour materials.
- Buildings are pairs of scaled cubes (wall block + roof block), 8 total.
- The road is a single scaled cube.
- The Montine path is a rotated scaled cube; the farm clearing/plot are
  flattened scaled cubes.
- Ground is a scaled default Plane.
- Interaction prompts render via `OnGUI` (debug-style immediate mode), not
  a Canvas/TextMeshPro HUD.
- One NPC, one farm plot, one boy (no Smart/Strong distinction, no
  switching).

This confirms the brief's diagnosis is accurate — MINI-001 proved the
camera/control/interaction *logic*, not the visual target, and was
documented as such at the time.

## Asset direction (updated after Unity Asset Store acquisition pass)

Superseded the local-library-only plan below once the user granted browser
access and the five requested free Asset Store packages were added to
their account, plus three already-owned packages discovered in the same
pass (`Demo City By Versatile Studio (Mobile Friendly)`, `Human Basic
Motions FREE`, `Human Melee Animations FREE` — added Aug 14, apparently
staged ahead of this task). Full detail in `Docs/ASSET-REGISTER.md`.

Current plan: **Demo City (Mobile Friendly)** as the primary building
source (test against the corrugated-roof shanty look before committing),
**Low Poly Character Pack** for Smart/Strong and NPCs, **Human Basic
Motions FREE** for locomotion (modern, actively-maintained, Mecanim-ready —
this removes the biggest risk from the original plan, since it replaces
Shanty Town 2's unconfirmed legacy character rigs), and **Low Poly
Environment - Nature Free** + **Low Poly Tropical Beach** for vegetation.
`Cartoon Farm Crops` and `POLYGON Starter Pack` get tested per the user's
own caution before committing (old materials / style-match risk
respectively).

Acquisition (adding to the account) is done. Actually pulling these into
`Assets/` still requires the Unity Editor's Package Manager "My Assets"
download+import — a native GUI action with no CLI/scripting path I could
find, and no tool available to me that can drive or observe the Unity
Editor or Unity Hub windows. This is the next concrete blocker.

### Original local-library plan (superseded, kept as fallback)

Single coherent style: **Arteria3d Shanty Town + Shanty Town 2** as the
primary building/prop/character kit (corrugated roofs, barrels, crates,
clotheslines, gravel yards — a direct match for Caribbean village
architecture), with **Arteria3d Tropical Island Foliage** (and
`Tropical Nature Pack` as a secondary test) for vegetation dressing. Kept
as a fallback if the Asset Store plan above doesn't pan out visually.

Palette: warm sun (soft yellow-white directional light), corrugated roofs
in weathered white/silver, rust-red, and faded blue/green (matching the
Shanty Town material set rather than inventing new colours), packed-earth
road brown, and saturated tropical greens for vegetation. No pastel/toy
rainbow palette.

### Known technical risk (flagged, not yet resolved)

Shanty Town 2's `Character1`-`Character9` and `ShantySoldier` are exported
in a legacy multi-format bundle (ASE/B3D/DAE/MS3D/Torque/U3D/X alongside
FBX) with animations documented as frame-range regions inside longer takes
rather than separate clips — a pre-Mecanim-era authoring style. Whether
these rig as Unity Humanoid avatars (needed for switching, retargeting, and
reusing any Human Basic Motions-style locomotion later) is **unconfirmed**
and must be tested at Phase B's start before Smart/Strong are built on top
of them. If they don't retarget cleanly, the fallback is the `Human
Characters` (Male/Female) pack noted as MINI-AST-005 in the asset register,
reskinned for two visually distinct boys.

## Scene blockout (text description — no diagram tool used)

```
                       [ NPCs/vendor/police cluster ]
Lalay safehouse -- Lalay road (bends, not straight) -- Lalay shop/market --
        |                                                    |
   yards/houses                                        police presence
        |                                                    |
        +---- Montine turnoff (rough dirt trail, sloped) ----+
                              |
                     Farm clearing + safehouse
                     (6+ tomato plots / free-place zone)
```

Compressed per D-003 (existing project decision): landmark order and road
character preserved, literal distances compressed ~3:1-4:1 for
playability, consistent with the existing `Docs/MAP-STRATEGY.md` scene
plan.

## Terrain

Per your decision: hand-authored Unity Terrain sculpted to roughly match
`Grandbay entire.jpg`'s silhouette and slope character for this vertical
slice's playable area only (not the whole island), labelled **approximate**
throughout, consistent with `GrandBayReference`'s own confidence-label
system. The `heightmapper-*` files are not used directly — see
`Terrain_File_Assessment.md`'s own finding that they're not a valid Unity
heightmap format/provenance.

## What gets replaced from MINI-001

Everything in the "Before" list above: capsules → rigged Shanty Town
characters, cube buildings → Shanty Town building kit, plane ground →
sculpted Terrain, `OnGUI` prompts → Canvas + TextMeshPro (requires adding
`com.unity.ugui` and `com.unity.textmeshpro` — standard Unity registry
packages, not Asset Store, so no account/licence question there).

## Performance budget (target, to verify once built)

Development PC: 60 FPS. Mobile-forward budget for later Android pass:
≤100 batches/SetPass calls in the default Lalay-road view, ≤150k visible
triangles, pooled ambient NPCs (max ~10 concurrent), baked/mixed lighting
with one real-time directional light, no more than 2-3 additional
real-time point lights active at once. Exact draw-call/triangle numbers
will be recorded from the Unity Frame Debugger/Stats window once Phase B
geometry exists — not estimated further here.

## Risks / open items

1. Shanty Town 2 character rig compatibility (above) — first thing to test
   in Phase B, before building Smart/Strong on top of it.
2. No landmark-quality building match for school/church/credit union —
   plan is custom modular wall/roof/door/window pieces built from the
   Shanty Town material set, not a new import.
3. No crop-growth-stage asset exists locally — tomato plant will be
   hand-built (simple stage-scaled meshes/materials), which is well within
   reach but is original modeling work, not asset import.
4. Licence entitlement for every pack in the asset register is unverified
   by me — please confirm you hold valid Asset Store licences for the
   Arteria3d packs specifically (they weren't the exact packs you linked).
5. Full MINI-011 scope (this document is Phase A of four) is large;
   Phase B/C/D will be claimed and executed as separate follow-up passes
   with their own gates, not silently folded into one giant change.

## Phase A gate

Per the corrective-rebuild brief's own instruction: production scene work
does not start until this plan is reviewed. Stopping here for your
go/no-go before claiming Phase B (environment/camera rebuild) — including
the character-rig risk test, which is the first real technical unknown.
