# Up Iz Up Mini — Asset Register

Every non-generated art asset imported into the Mini project must have a row
here before (or in the same change as) import, per `AGENTS.md`. This is a
register of what's *proposed*/imported for `MINI-011`; nothing below is
imported yet as of this writing — see `Docs/MINI-011-VISUAL-PLAN.md` for the
Phase A gate this is waiting on.

## Licence status caveat — read before trusting any row below as cleared

All candidate packs below live in `E:\Assets`, a personal local library
treated as read-only reference per `AGENTS.md`. That library's own
`GrandBayReference\Assets\Asset_Licence_Register.csv` (prepared for the
larger `Up iz up` project) records **no verified licence for anything in
it** — every row is `Unknown`/`Research` status. I have not independently
verified Unity Asset Store entitlement, seat count, or commercial-use terms
for any pack named here; I can only confirm the files exist on disk. The
user selected "use what's already owned locally" when asked, which I'm
treating as their statement of ownership — but if any pack below turns out
to not actually be licensed to this account, pull it before a real release.

## Proposed for MINI-011

| asset_id | asset_name | publisher | source path (local) | licence | status | purpose |
|---|---|---|---|---|---|---|
| MINI-AST-001 | Arteria3d - Shanty Town | Arteria3d | `E:\Assets\Pack\Arteria Pack\Arteria3d - Shanty Town\shanty town\` | Unverified — user to confirm Asset Store entitlement | Proposed | Primary building/prop kit: corrugated-roof shanty structures, barrels, clotheslines, crates, fences, gravel/ground materials. Best visual match found locally for "corrugated-roof village architecture." |
| MINI-AST-002 | Arteria3d - Shanty Town 2 | Arteria3d | `E:\Assets\Pack\Arteria Pack\Arteria3d - Shanty Town 2\Arteria3d - ShantyTown 2\` | Unverified — user to confirm Asset Store entitlement | Proposed | Additional buildings (CityBlockA/B/C, FarmArea), 9 numbered rigged characters (Character1-9) for ambient NPCs/vendors, and one "ShantySoldier" character usable as the police officer. **Technical risk:** old multi-format export (ASE/B3D/DAE/MS3D/Torque/U3D/X alongside FBX) suggests a pre-Mecanim-era pack; animations are documented as single-track frame ranges inside longer takes rather than separate clips, and Humanoid-avatar compatibility is unconfirmed. Needs an import test before it's trusted for Smart/Strong specifically. |
| MINI-AST-003 | Arteria3d - Tropical Island Foliage Pack | Arteria3d | `E:\Assets\Pack\Arteria Pack\Arteria3d - Tropical Island Foliage Pack.unitypackage` | Unverified — user to confirm Asset Store entitlement | Proposed | Palms, tropical undergrowth for road/yard/farm dressing and the Montine trail. |
| MINI-AST-004 | Tropical Nature Pack | Unlisted publisher (folder only) | `E:\Assets\3D Models\Environments\Nature\Tropical Nature Pack\Tropical Nature Pack.unitypackage` | Unverified — user to confirm Asset Store entitlement | Proposed | Secondary vegetation variety alongside MINI-AST-003; test for style clash before combining both. |
| MINI-AST-005 (candidate, not selected) | Human Characters (Male/Female) | Unlisted publisher | `E:\Assets\3D Models\Characters\Humanoids\Humans\Humans Characters\Human Characters.unitypackage` | Unverified | Rejected for Smart/Strong pending Phase A decision — kept as a fallback if Shanty Town 2's Character1-9 turn out not to be Humanoid-avatar compatible. |

## Explicitly rejected for this pass

- Adventurer Character, Fighter Character, Warriors And Commoner — fantasy/medieval sword-fighter aesthetic and equipment, wrong genre for a Caribbean farming/crime game; would clash with the shanty-town style per the "one coherent style" rule in the corrective-rebuild brief.
- `Grandbay entire.jpg` and `heightmapper-*.png/.raw` — explicitly marked "do not ship" / "not ready for direct Unity import" in `GrandBayReference`'s own assessment docs. Used as internal visual reference only for hand-authoring terrain, never imported as a texture or heightmap asset.

## Not yet sourced (blocking gaps)

- Nothing in the local library matches "modular Caribbean village buildings" beyond the shanty-town packs above closely enough for landmark buildings (school, church, credit union) — those will need custom-built modular pieces (walls/roof/door/window kit) rather than a direct asset import, per `Docs/MINI-011-VISUAL-PLAN.md`.
- No local crop-growth-stage asset pack (tomato/banana/carrot/weed tiers) was found; Mission 1's tomato plant will need to be hand-built (simple stem+leaf+fruit meshes with 3 material/scale stages) rather than imported.
