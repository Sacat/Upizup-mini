# MINI-144 - Modular wardrobe: base-body audit and test capsule

User approved resuming character production on 2026-09-07 after accepting MINI-143 gameplay.
Status: implementing an isolated diagnostic first. Integrator: Codex. External credits: 0.
Approval class C: no playable replacement or paid generation before visual approval.

## Scope and stop condition

Inspect original AccuRIG Sacat and failed mobile derivatives; measure the failure before attempting repair.
Preserve the master, gameplay scene, playable bodies, Animator, bike fittings and approved two-piece chain.
First outfit target: one polo shirt, long jeans, trainers, cap and gold watch, plus existing chain.
Use project fictional labels (Mike/Lacos/Adibas style), not new paid branded assets.
Stop this first pass at measured rig diagnosis and a reusable next-step record, not an unverified body swap.
User follow-up: match how the original clothed characters work and their material quality. Reuse compatible original garment parts where possible; audit their separability before promising shirt/shorts swaps. Add an isolated Blender gold-watch product preview in this pass; no paid credits, no playable integration. Watch fit still requires an on-character review.

## Wardrobe contract

- Reuse EconomyManager per-character ownership and existing save-compatible item IDs.
- Ownership is not equipment selection: each character needs one selected item per slot and colour ID.
- Slots: top, bottom, footwear, headwear, glasses, necklace, wristwatch.
- Safehouse wardrobe opens a preview transaction: equip applies; cancel restores; no purchase occurs here.
- Skinned clothes share a verified skeleton/bind pose and explicitly hide covered body regions.
- Accessories reference authored fitting profiles; never auto-normalize the approved chain.
- New item definitions specify model, slot, compatible body family, colour variants, fitting/body mask and existing shop ID.
- No runtime mesh generation for final clothes. No coloured primitives presented as finished watch/clothing.
- Save migration must retain old purchases; resale must unequip only the sold character's item.
- Small phone-friendly catalogue, pooled/reused preview, no separate live physics per accessory.

## Acceptance sequence

1. Rig/LOD diagnosis and isolated repair.
2. Original-versus-repaired idle, walk, run, kick, seated/grip evidence.
3. User approves moving base body.
4. One fitted outfit plus gold-watch close-up and phone-distance screenshots.
5. User approves appearance; then safehouse UI, per-character saves and integration.

No wardrobe or replacement body is claimed shipped by this diagnostic packet.

## Evidence-ready checkpoint

- Blender 5.0.1 executed both scripts successfully. No Assets, scenes, runtime C#, imports or EXE changed; Unity compile/build not applicable to this preview-only pass.
- `Logs/Tasks/MINI-144/Gold-Watch-Preview.png` and `.blend`: original unbranded linked gold watch; polished/brushed gold, markers, hands, crown and clasp. Visually inspected as a product concept, NOT an in-game fitting screenshot. User approval pending.
- Preview contains 29,800 evaluated triangles / 144 parts. Do NOT ship directly. Bake/merge/LOD to a proposed 1-2k-triangle accessory and shared small material set after design approval; recheck close and phone distance.
- `Logs/Tasks/MINI-144/rig-audit.json`: master 100k triangles; mobile 25k/12k/4.5k. All have zero unweighted/non-normalized vertices; derivatives obey four influences. Imported Blender single-bone tests have similar dimensions/displacement across master and derivatives. This does NOT reproduce or solve the Unity tearing; investigate Unity LOD bone remapping/bind poses with original-versus-derived PlayableGraph proof before rebuilding topology blindly.
- Diagnostic correction: compare evaluated rest and posed meshes in the same space and preserve imported bone basis. Raw mesh-versus-evaluated comparison gives a misleading axis displacement.
- Existing CharacterEquipment only recolours garment slots and generates primitive watch/cap; gold chain uses approved prefab/profiles. Reuse purchase IDs and profiles but do not treat primitive watch or a recolour as an approved new garment.
- Next: user accepts watch design, then optimize/fix wrist fit in isolated character proof. Inspect original separate garments and Unity mobile rig mapping; keep gameplay models unchanged until motion and outfit acceptance.
- Ownership released. External spend: zero.
