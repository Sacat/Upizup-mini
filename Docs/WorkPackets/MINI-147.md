# MINI-147 — first clothing capsule reference lock

Integrator: Codex. Status: evidence_ready; awaiting user design approval. Approval class C for later character integration. Hitem3D/asset-store spend: zero. One built-in image-generation concept sheet only; stop for design approval before modeling/integration. Ownership released.

## User intent

MINI-146 watch/home wardrobe accepted as good for now. Next explicitly shirt, pants, hat, shoes, with independently selectable colours. User references Nike shoes/shirt, Lacoste hat/shirt; earlier fictional labels remain Mike/Lacos pending clarification, preserve current shop IDs. Bottoms include long jeans, denim shorts and long pants. Colour choice belongs to each character and each slot, not shared ownership or a whole-character tint.

## First test capsule

One collared pique polo (Lacos style), one crew-neck sports tee (Mike style), straight/slim long jeans plus denim shorts and plain long trousers, a curved-brim six-panel cap, and leather low-top trainers. Neutral black/white/navy plus red/green/blue choices; denim retains seams/wash; shoe soles and trim need independent masks rather than a uniform tint. Final colours remain subject to approval. No real logo copied for the game in this proposal.

## Technical contract / later implementation

- Existing CharacterEquipment garment path only recolours materials by name; not real removable shirts/pants. Do not mislabel recolours as new garment meshes.
- MINI-144 audit found no missing weights; MINI-107 derived-body Unity motion tearing is unresolved. Before a body swap, diagnose Unity binding and demonstrate idle/walk/run/kick/seated motion on isolated proof stage. Do not regenerate body or spend credits blindly.
- Separate skinned shirt/bottom/shoes share verified body skeleton/bind poses with explicit covered-region masks. Hat uses per-character fitting. Preserve head identity, approved watch and chains.
- Data definitions: stable shop ID, slot, body family, garment asset, mask, allowed colour IDs, material-region masks and fitting profile. Each character has one chosen item and colour per slot, existing purchases preserved. Wardrobe opens only at owned home; do not add another economy.
- Proposed added outfit budget (not measured yet): shirt <=2500 triangles, bottoms <=2500, shoes pair <=2000, cap <=800; LOD1 ~50%. Shared 1024 atlas maximum and minimal materials, no cloth physics or accessory colliders. Real phone performance requires profiling.

## Scope and protections

Reference sheet and production card only. No gameplay/scene/mesh/material/Animator/save/EXE changes. No watch, chain, body or map modifications. Reference sheet is AI concept art, NOT a Unity screenshot or a finished 3D asset.

## Next gate

Show clothing-only image for user approval. Then produce and motion-test first shirt on verified base; pants/hat/shoes follow same proven pipeline. In-game home colour UI follows compatible assets, not premature unverified integration.

## Evidence and reproducible prompt

Built-in imagegen created Logs/Tasks/MINI-147/Wardrobe-Concept-v1.png (1536x1024), shown inline and visually inspected: seven requested garment types present, original Mike/Lacos labels, fabric/denim/leather detail, selectable-colour swatches. This is a style target only; no Unity or Blender model produced this pass. No compile/build needed for documentation/concept-only changes; prior MINI-146 EXE untouched.

Prompt specification used: clothing-only studio catalog concept, warm gray backdrop, header UP IZ UP MINI — WARDROBE CONCEPT and STYLE PREVIEW — NOT IN GAME YET; navy pique short-sleeve Lacos polo, white Mike crew-neck tee, green six-panel Lacos cap, indigo long jeans, denim shorts, charcoal trousers, white leather low-top Mike trainers. Realistic game-art materials with seams, pockets, laces, economical silhouettes, separate colour swatches. No people, bodies, jewellery or watches; no real Nike swoosh/Lacoste crocodile. Soft studio light, complete isolated garments, clear labels; not a game screenshot. Future production must use this approved reference only after user decision, not assume generated appearance proves fit or mobile performance.
