# MINI-145 - Round gold watch, mobile geometry and both wrist previews

User approves MINI-144 watch design except top/case must be entirely round. Approved scope: optimize and show on Sacat and Franki wrists.
Integrator Codex. External credits 0. Status implementing. Visual approval still needed for exact wrist fit.
Protected: canonical GrandBayProof scene, playable character prefabs, chains and materials, gameplay code, EXE.
Reserved: Tools/CharacterPipeline/mini145_watch_mobile.py; Assets/UpIzUpMini/Art/Accessories/GoldWatchMobile/; Assets/UpIzUpMini/Editor/Mini145WatchProof.cs; Assets/UpIzUpMini/Scenes/WatchWardrobeProof.unity; this packet and current/Characters/handoff/changelog/task docs.

## Acceptance

- Circular case and bezel (no square top plate), same linked gold style.
- LOD0 <= 2500 triangles; LOD1 <= 1200; one shared atlas material, small textures.
- Separate measured left-wrist fitting profiles for actual current Sacat/Franki models.
- Isolated Unity wrist close-ups and full-character views; no chain changes.
- Validate frame sampling attachment stability. Static poses do not approve live motion/fit.
- No integration into purchases or wardrobe UI until visual acceptance.

## Evidence ready (2026-09-07)

- Round-case meshes created locally in Blender, no external assets or credits. LOD0 1,644 tris, LOD1 788. One renderer/material per active LOD, two shared 32x8 palette/metallic textures; asset folder about 86KB including metadata.
- Unity compile/import/isolated proof passes in Logs/MINI-145-watch-final.log. Logs/Tasks/MINI-145/unity-watch-proof.txt records exact profiles, 36 idle/walk/run samples per protagonist with unchanged local attachment, and canonical scene SHA256 unchanged.
- Isolated scene: Assets/UpIzUpMini/Scenes/WatchWardrobeProof.unity. Both actual current protagonists cloned; all gameplay behaviours disabled in proof only. No chain, character, outfit, gameplay scene, shop or EXE changed.
- Screenshots: Logs/Tasks/MINI-145/Sacat-Watch-Close.png, Franki-Watch-Close.png, Sacat-Watch-Full.png, Franki-Watch-Full.png. Close-ups and Sacat full view inspected. Final fit is OVER EXISTING CUFFS, not a bare-wrist fit. The clothes cover the wrists; no garment geometry was removed.
- First fit clipped Franki's sleeve; moving toward the hand clipped the dial into the bent hand. Final preview returns up the forearm and uses separate uniform fit sizes (Sacat1.14, Franki1.24 world scale) preserving circular case. Automated skin cross-section maxima were inflated by nearby geometry and rejected; do not treat these preview offsets as universal automatic clothing fit.
- Fitting profiles SacatWatchFit.asset / FrankiWatchFit.asset are saved independently. Repeat proof runs preserve existing profile values rather than recalculate them. No approved chain profile is readjusted.
- Mobile content budgets passed; no Android device profiling or live wrist movement approval claimed. Sampled attachment stability does not prove no clipping throughout combat/riding.
- Next: user approves/revises exact cuff fit. Then integrate via watch_rollie purchase + CharacterEquipment prefab/profile override; keep ownership separate. Safehouse wardrobe and outfit production remain separate unimplemented steps.
- Status evidence_ready; ownership released. No EXE build needed for isolated preview.
