# Codex continuation — MINI-166 (2026-09-11, handed off from Claude)

Claude worked MINI-166 through several rounds this session (Shirt, Pants,
Hat, Shoes, Denim Shorts, accessory-fit fixes, wardrobe UI wiring). Full
detail is already written up in `Docs/Systems/Characters.md` and
`Docs/Systems/UI.md`'s MINI-166 entries and `Docs/WorkPackets/MINI-166.md` -
read those, not this file, for the actual history and root causes. This
file is just the "where things stand right now, start here" pointer.

## Read first
1. `AGENTS.md`
2. `Docs/CURRENT.md` (MINI-166 entry at the top)
3. `Docs/Systems/README.md`, then `Characters.md` and `UI.md` (their MINI-166
   entries specifically - full root-cause writeups already there)
4. `Docs/WorkPackets/MINI-166.md` (the complete round-by-round log)
5. The `### Current claim` block in `PROJECT-HANDOFF.md`

## What's actually shipped and verified (all committed, latest commit on
`codex/mini-085-baseline-20260820` at handoff time)

| Slot | Franki | Sacat |
|---|---|---|
| Shirt (Tee/Polo) | done | done (collar overlay) |
| Pants (Jeans/Trousers) | done | **blocked** - confirmed real: his `Ch06` mesh has skin+shirt+pants all on ONE material index, no separate slot to tint |
| Denim Shorts | done | not attempted - needs the same texture-repaint technique as Pants, not a submesh split |
| Hat (No Hat/Lacos Cap) | done | done |
| Shoes (Mike 90/97) | done | not started - same fused-material blocker as Pants |

The wardrobe UI (`VisualWardrobePanel.cs`) lists real pieces per slot
through the E -> 5 safehouse menu - verified end to end in Play Mode
(`Mini166WardrobeUIValidation.cs`). Cap/watch accessory placement was also
fixed this session (cap was floating ~6cm above the head).

## Two lessons worth reading before writing more geometry code

1. **Prefer direct C# mesh operations over a Blender-export-then-bone-remap
   round trip** wherever new topology isn't actually required (colour
   variants, submesh splits by height/position). That round trip caused
   three separate hard-to-diagnose bugs this session; a pure C# submesh
   split (`SkinnedMeshRenderer.BakeMesh()` + `Mesh.SetTriangles()` on the
   ALREADY-WORKING live mesh) is what actually shipped Denim Shorts after
   9 failed rounds trying the Blender approach. See `Characters.md`'s
   MINI-166 entry for the full trace.
2. **A "looks broken" render can be the render tool's camera, not the
   geometry.** Sacat's Hat looked wrong (cap at ear height) until the
   camera's fixed world-space offset was replaced with
   `root.transform.forward`/`right` - his root carries a ~277.8-degree Y
   rotation that a naive fixed-axis camera doesn't account for. Always
   re-derive the math before trusting a bad-looking render is a real bug.

## Suggested next steps, roughly in priority order

1. **Sacat's Pants/Shoes/Shorts** - all three need the real MINI-157
   texture-repaint technique (classify polygons by bone position, rasterize
   a new colour/skin-tone onto the shared diffuse texture's UV region) since
   his `Ch06` mesh has no separable material slot. This is the single
   biggest remaining gap between the two characters.
2. **Motion proof** - everything shipped so far has only been verified in
   idle pose. Walk/run/crouch on both characters for every shipped piece
   before calling any slot fully done.
3. Real distinct Mike 90 vs Mike 97 panel geometry (currently colour-only,
   same honest scoping as Jeans/Trousers).
4. A second Hat/Shoes design each, matching the originally-approved 8-design
   scope (`Docs/WorkPackets/MINI-147.md`'s final approval).

## Ownership

Released to `None` in `PROJECT-HANDOFF.md`'s Current claim block. Claim
MINI-166 (it's still the active task ID - do not start a new one) before
editing.
