# Claude continuation: Grand Bay, buildings and character chains

This is a reproducible record of the actual MINI-171–176 work plus Claude's subsequent MINI-178 chain setup, not a claim that every map feature is surveyed or that necklaces automatically fit every future character. Read the latest work packet before running any historical generator. User-approved Sacat chain is locked. **The MINI-176 Franki mesh-remapping experiments were superseded by Claude's MINI-178 placement profile; do not restore them.**

For the earlier **Blender 5.0.1 house modeling**, source scripts, export/import steps and original district repair, also read [the complete MINI-142 house/map handoff](CLAUDE-HOUSES-MAP-COMPLETE-HANDOFF-MINI-142.md). It covers the earlier Blender asset pipeline; the later procedural replacements described here used Unity. Treat historical ownership and scene promotion instructions in that guide as history, not current authorization. Together these two guides cover the earlier house production and the latest Grand Bay expansion fixes.

## Start here and preserve ownership

Only write to `E:\Unity\Up Iz Up Mini`. `E:\Unity\Up iz up` and `E:\Assets` are read-only references. Read AGENTS.md, Docs/CURRENT.md, current claim in PROJECT-HANDOFF.md, relevant TASKS entries, Docs/AI-PRODUCTION-WORKFLOW.md and Docs/VISUAL-APPROVAL-REGISTER.md. Claim one task and its exact files before edits. Run `Tools/AIWorkflow/Invoke-Preflight.ps1 -Agent Claude -TaskId MINI-###`. Keep only one Unity Editor on the project. Inspect `git status --short`; never absorb unrelated changes in a checkpoint.

The working expansion scene is `Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity`. Protect `GrandBayProof.unity` and `MapLab_GrandBayExpansionCopy.unity`. Do not replace the canonical scene or Build Settings merely because a review scene looks good. The live world root is `GrandBayPhase1_ApprovedWorld_VA005`, with expansion `MINI168_Expansion`. Scene serialization is binary: use Unity Editor APIs, not text replacement in scene files.

## Tools actually used

PowerShell and `rg` locate code, inspect logs and launch Unity. Unity C# Editor scripts create real meshes, shared materials, colliders and saved scene objects. Play Mode validates runtime character equipment. Dedicated Unity cameras render PNG evidence. Git checkpoints scoped changes. Web text research supports addresses and architectural types, with limitations recorded below. These map/building/chain passes did not use Blender, QGIS or a paid model generator; do not invent a Blender workflow as their provenance.

Use Unity `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`. Typical PowerShell launch:

```powershell
Start-Process 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' -WindowStyle Hidden -ArgumentList '-batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini176ChainProof.Run -logFile "E:\Unity\Up Iz Up Mini\Logs\mini176-chain.log"'
```

Do not add `-nographics` when rendering. Do not add `-quit` to a Play Mode harness that exits itself after captures. Start-Process returning is not success: wait for completion and inspect explicit pass markers, exceptions and compiler errors. Methods called Apply can mutate/save a scene. Methods called Render may reopen the saved scene, discarding unsaved edits: inspect implementation and save first.

## Map coordinates, measurements and truth

The district conversion uses longitude origin -61.3181049 and latitude origin 15.2450638:

```
x = (longitude - originLongitude) * 107500 / 3
z = (latitude  - originLatitude)  * 110650 / 3
```

+X east, +Z north, +Y up. Travel distances are compressed by three. Building dimensions are separately authored game metres; do not divide every object dimension again. Terrain is a mesh, not a Unity Terrain component. Record each anchor's geographic source, converted X/Z, sampled terrain/road Y, and any deliberate frontage offset separately. A directory pin does not establish a surveyed building footprint or exact entrance.

Maintain the existing road graph and district order before decoration. Inspect full-map, street-level, junction and landmark cameras. At every join inspect both edges, collision surface, height and tangent continuity. Shared endpoint geometry is more reliable than overlapping slabs or a disc hiding a gap. Preserve the asymmetric bay seam: historical endpoints (250,3.11,-100.25) and (250,2.98,-90.03), centre (250,3.042918,-95.14). Later grading can change elevations; the saved scene is authoritative. Never replace this with a symmetric seam by assumption.

Historical roundabout: X279.14/Z-66.62, centreline radius8.5, width7, 64 segments, island9.7. Main/coast/bay road width6.2, campus4.8. Source preview maximum grade7.96% was a measurement of that revision, not certification of every current road. Repeated smoothing can accumulate changes: read Mini172 tooling before rerunning it. Ground adjacent green areas slightly below the road surface, avoiding a curb-like step or z-fighting. MINI171 repaired 3604 terrain vertices using22 road colliders, about3cm below hits. Both renderer and collision mesh must use the resulting mesh. Drive and walk corners after geometric checks; a mesh report cannot establish ride comfort.

PCSS historical centroid X106.5267/Z49.2967, entrance north/+Z. MINI172 moved wings3m inward and used a34m courtyard with right wing3m setback. Verify the right-hand side while facing from the entrance, and road clearance including roof/porch bounds, not only root position.

## Geneva cricket ground, coast and seawall

The latest state supersedes older football previews. MINI173 removed141 football goals, nets, pitch markings and stripe objects. Cricket field centre X273.2/Z57, Y12.87–12.89; right axis normalized(.93,0,.36), forward(-.36,0,.93). Pad42x64, ellipse boundary radii18.5/29.5, cricket strip20.12x3.05. Two small three-tier stands rise toward the back. Wickets and surrounding layout are stylized; do not describe these as a surveyed sporting facility.

`Mini173BayRevision.cs` starts from immutable `Art/Environment/Mini173/TerrainBeforeCoast.asset`. Artistic shoreline X by Z:250 below Z-160;250+(Z+160)*.55 below Z-60;305+(Z+60)*.25 otherwise. Triangle clipping, an18m inland SmoothStep grade and preservation of road height minus.025 formed CoastalTerrain.asset. Vertex welding tolerance.0001 produced36857 vertices; renderer and collider use the same mesh. Water Y.25, shore.32, sand strip6m. Measured minimum roundabout shore clearance8.524689. Older broad southern terrain extrusion created unwanted land; do not rerun it.

`Mini174CoastAndSpacing.cs` adds65 joined wall segments of roughly3m along Z-159..36,1.1m inland of the shoreline. Wall body width.55,height1.15; cap width.68,depth.13; embed.2. There are130 box colliders and shared materials. User clarified the bay continues by the roundabout with a seawall. Google Maps did not provide usable current imagery during this pass; historical sea-defense text is not a measurement of today's wall. Preserve this distinction in future reports.

## Lalay houses: construction and spacing

`Mini173LalayRevision.cs` disables31 Lalay_Shanty roots for rollback and replaces their narrow lots with31 procedural homes, preserving root position/orientation and using local+Z as front. Footprint2.65x3.6, height2.7 or5.1, porch depth.7, columns.13, roof slope17 degrees, floor spacing2.55. Five plaster colours, window frames/glass/sills with actual depth, pitched roof planes and porches establish local architectural character. Combine by material per house. Use body/slab/step colliders; tiny window trim does not need independent collision. Older6–12m-wide houses were too wide for approximately3.6m lots.

Research examples establish building types, not exact replicas at every address:

- https://www.safehavenrealestate.com/property/historical-stone-structure-in-lalay-grand-bay/
- https://www.milleniarealtydominica.com/properties/grand-bay-two-storey-home-with-sea-views-business-potential/
- https://www.largeup.com/2016/10/28/pic-week-elfs-place-grand-bay/

Some source photos were not accessible for inspection. Never claim photogrammetry or photo matching from those pages alone.

MINI174 checks169 active generic house roots with prefixes Lalay_House_, Lalay_Home_, Highland_House_, ExpansionHouse_. Compute oriented XZ rectangles from all mesh bounds, including roofs and porches; conservative historical disabled child geometry can enlarge these envelopes. Separating-axis checks use.18 clearance. Bounded lateral/back searches and horizontal scales1,.94,.88,.82 preserve height. Preserve each root's original grounding offset; sample road clearance at a5x5 footprint grid. Initial72 envelope conflicts,62 roots adjusted, zero remaining in that set. This is not an audit of every mission prop. Exact transforms: `Logs/Tasks/MINI-174/Spacing.txt`. Inspect street views after numerical checks; nonintersecting buildings can still block doors or look poorly arranged.

## Grand Bay Credit Union

`Mini175CreditUnion.cs`: Apply, RepairShop, Validate, Inspect, Render. Official https://gbccu.com/contact/ supports Lalay Main Road. Directory https://dm.near-place.com/grand-bay-cooperative-credit-union-lalay-main-road supplies15.2407793,-61.3166228. Another directory's town-centre pin15.245064,-61.318104 was rejected. Matching an old candidate anchor is recovered provenance, not independent survey confirmation.

Converted directory pin(53.11,0,-158.03). Actual road `ImportTrim_Road_way_22917921`, sampled centre(53.11,7.12,-160.98). Building centre(54.11,6.55,-152.94), frontage setback5.187216 game metres from directory pin for road clearance. Body6.8x4.8,height5.3, parapet7.05x5.08, canopy6.9. Columns, glass, signage and decorative ATM; no banking gameplay added. Facade is stylized, not photo matched. User requested white walls and black windows: WarmConcrete/WhiteTrim white; WindowGlass/legacy DeepGreen black, white lettering on black fascia, black columns/ATM. Both saved materials and builder defaults updated; legacy names retained for GUID stability.

Disabled parcel house `Lalay_House_SideB_054_OneStorey`. First render exposed CLOTHES sign inside bank: move `Market_CLOTHES` and `NPC_ApparelShop` together by(15.88,-.39,-1.98). Fresh validation returned roadFootprintHits0 and houseMarketEnvelopeOverlaps0. Shop interaction was not tested. Match road names containing Road: StartsWith Road_ misses imported roads. Inspect actual mesh cross-sections when source topology has changed. Evidence `Logs/Tasks/MINI-175/Renders/CreditUnion.png`.

## Chain attachment pipeline and lessons

Read `Scripts/Character/CharacterEquipment.cs`, `ChainGarmentFit.cs`, `AccessorySwing.cs`. RefreshEquipment creates Equip_chain_gold on Humanoid Chest from GoldChain18k.prefab. Manual placement profiles override fallback placement. Franki fallback uses forward.044,up.383,side-.032,tilt-25.6 degrees,width.27, then3.5cm neckward correction. Sacat has a separate approved placement. Fit occurs before AccessorySwing.Initialize captures its rest pose. Bone local axes are not necessarily character axes: use character right/up/forward for anatomical directions and transform world vertices back into mesh-local space.

Source GoldChain18k_clean.glb contains welded loops,6127 vertices per loop; Sacat uses two, Franki originally one and now two under MINI-178. Disconnected-component rigid-link fitting failed because each welded loop was one component. Pure radial torso projection stretched the chain toward shoulders. The MINI173 Sacat path instead bins vertices around a projected oval into48 samples and interpolates depth displacements continuously. Bake active visible SkinnedMeshRenderers into temporary MeshColliders with correct world transform/scale, raycast the body/garment, bound depth shifts to±.10 and add.011 centre clearance. Clone each source mesh per instance, recompute bounds/normals, destroy temporary colliders/baked meshes and owned copies on teardown. Never mutate the imported shared mesh.

Sacat's settled MINI173 result is user approved. Preserve his branch, source, placement and silhouette. Franki's old upper arc reached the jaw. MINI176's first attempt simply lowered and pulled back the upper arc: front/side/rear renders revealed buried side segments and a back loop too low. Subsequent anatomical oval remaps caused stretched links or excessive rear stand-off. These are rejected experiments, not recipes to reuse. Claude restored ChainGarmentFit.cs to committed HEAD and saved the rejected branch under Logs/Tasks/MINI-178.

### Current chain setup: MINI-178 takes precedence

Read `Docs/WorkPackets/MINI-178.md` and `Editor/Mini178FrankiChain.cs`. Claude transferred Sacat's double-loop fittedChildren to a separate `Data/Equipment/FrankiChainPlacement.asset`, assigned ONLY in GrandBayProof_ExpansionImport.unity. The shared fitter, CharacterEquipment and Sacat profile remain unchanged against HEAD. Keep Claude's subsequent MINI-179 skin-material changes intact as well.

Measured neck-to-chest separation: Sacat.3116m, Franki.2732m; ratio.8768. Express Sacat's chain pose relative to his neck in character axes, scale by that ratio and reconstruct in Franki's frame, then convert into Franki chest-bone local space. Retain pitch, not Sacat's compensatory yaw/roll. The critical transform trap: a measured equipped pose ALREADY includes ChainNeckwardCorrection=.035m. Account for this when building a profile or the equip pipeline applies it a second time, moving the chain too far back.

Claude's final N2 variant uses+.025 forward compensation and+.025 up, pitch-only transfer and no individual-loop offsets. Saved profile localPosition=(-.03520415,.44585127,.06502418), localEulerAngles=(336.9502,359.12976,1.2698834), localScale=(.7841031,.9940071,.7841029). Those local numbers belong to THIS chest rig; do not paste them onto another skeleton. The shared parent/child transforms also matter: copy the profile structure, not just its root values.

The updated visual register records user acceptance of MINI-178: "ok not perfect but its good for now". Preserve this accepted current profile. This supersedes the rejected MINI-176 experiment. Do not present it as final perfection or silently regenerate it. Codex's fresh verification measured size(.202,.322,.200), top+.111, bottom-.211; minor differences from Claude's earlier bounds reflect the sampled animated pose.

Reported final bounds relative to neck: nape-side top+.111, lowest-.214, size(.203,.325,.192),12254 vertices in two loops. The user in Claude's task requested that the front top be hidden into the neck; intentional hidden rear/upper geometry is therefore part of that variant, not proof of physically collision-free fit. Current user emphasizes as close to nape as possible. Inspect the actual side view for stand-off; do not certify contact from a rear image alone.

Tools: Measure records rigs/surfaces; Sweep creates variant contact sheets; Save writes the chosen profile; Verify reopens the saved expansion scene and captures both characters. Verify logs measurements and images but is not a comprehensive assertion suite. Proof: `Logs/Tasks/MINI-178/Renders/Verify-Franki-{front,side,back}.png`; original Claude logs mini178-*4.log; independent Codex rerun log `Logs/mini176-verify-current.log`. Motion, alternate shirts, live scene and EXE remain outside this verification. A static image does not establish zero clipping or exact skin clearance.

## Future-character chain fitting recipe

1. Record rig scale, chest/neck/head positions, local axes and garment renderers. Capture front, side, rear and three-quarter views before changes. Do not derive neck width from an entire renderer containing arms.
2. Establish actual anatomical targets: rear crown at nape, sides around neck above shoulders, front drape over clavicle/chest. A joint named Neck is a rig reference, not automatically the visible collar surface. Confirm offsets visually.
3. Inspect source topology. Use rigid transforms for genuinely separate links; use a smooth displacement field for a welded loop. Always start from the untouched source to avoid cumulative fitting errors.
4. Fit the whole continuous loop, not just the visible front. Guard against rays hitting face/chin, arms or shirt shoulders. Sample the current outer garment/body surface, use clearance related to link thickness and check transition continuity. Do not rotate decorations toward a camera to conceal bad geometry.
5. Preserve link thickness and proportions as much as possible. Large deformations can distort links; inspect close views, not only a distant silhouette. A fitted front with hidden side segments is a failure.
6. Initialize accessory motion only after the final rest fit. Inspect after settling, then idle/walk/run/turn/crouch/bike poses. The current spring can displace up to.045m and is not collision-aware. Static PNGs cannot certify those movements.
7. Refit from source after clothing changes when the visible outer surface changes. Current ChainGarmentFit is equip-time fitting, not continuous cloth simulation. Explicitly test whether the wardrobe path refreshes equipment; do not assume it.
8. Store future measurements in a per-character profile with named anatomical targets rather than adding unexplained global offsets. Current implementation explicitly supports names Sacat and Franki; it is not universal automatic fitting for any new character.
9. Preserve approved characters with regression screenshots. Test source mesh immutability, repeated equip/unequip cleanup and character-independent equipment. Record remaining clipping or coverage honestly.

## Proof, documentation and completion

`Editor/Mini176ChainProof.cs` uses InitializeOnLoad and SessionState to survive domain reload, opens the actual expansion scene and enters Play Mode. It finds both real CharacterEquipment actors (including inactive), enables them, injects chain_gold into private alwaysEquipped for the proof and calls RefreshEquipment. SetTrialItem(chain_gold,true) is unsupported: that earlier approach failed. Use a separate proof camera so the runtime follow camera cannot overwrite framing. Front/side/rear and a delayed settled image go to `Logs/Tasks/MINI-176/Renders`,1200x1000. Capture markers prove harness completion only; manually inspect the PNGs.

For every continuation keep a work packet listing exact input references, measurements, reserved files, editor methods, commands, logs, rejected approaches, final scene paths and screenshots. Separate compile success, geometric validation, visual inspection, motion tests and user approval. Run available tests appropriate to the change; do not substitute a screenshot for gameplay testing. Update CURRENT, system ledger, TASKS, CHANGELOG and PROJECT-HANDOFF. Commit only scoped files, release ownership. No EXE was built for MINI173–176; do not imply these changes are already in the last executable.

Detailed historical sources: `Docs/Maps/dm-dom-grand-bay-expansion-v1/GENEVA-WORKFLOW.md`, its `CLAUDE-CONTINUE.md`, and `Docs/WorkPackets/MINI-171.md` through MINI-176.md. Latest packet overrides old football/source-only instructions. Treat this document as the project memory to read before similar work, not as proof that future work is already tested.
