# Final evidence — MINI-166 footwear previews

Both final build logs contain AIRMAX_PREVIEW_COMPLETE, with no Python traceback. Six views per model were rendered: hero, side, top, front, back and clay. These are Blender renders of the supplied geometry.

| Check | AM90 | AM97 |
|---|---:|---:|
| Evaluated triangles (one shoe) | 92964 | 111348 |
| Exported mesh objects | 535 | 468 |
| Minimum checked lace clearance, mm | 0.7333 | 0.9041 |
| Checked lace vertices | 920 | 920 |
| GLB/source bounds discrepancy, mm | 0.0 | 0.0 |
| Near-zero-area triangles (cleanup remains) | 2 | 5 |

Air cavity centre rays pass through the foam on both models. Collar centre rays reach the inset footbed at 49mm elevation rather than a cap at collar height. Evaluated vertex coordinates are finite. Reimported GLB bounds match source bounds. See validation.json for exact values and tested scope.

The validator PASS covers these named invariants only. It does not certify all intersections, all topology, likeness, manufacturability, UVs, performance or Unity runtime behavior. Near-zero-area triangles are reported, not hidden; production mesh cleanup remains.

Observed visual improvements: roughly halved heel sole height, shaped forefoot/toe plan, physical Air openings and internal bladders, distinguishable 90 panels versus 97 ripples, open collar, real tongue and crossed laces. All final elevations were inspected.

Remaining visual limitations: these are stylized procedural interpretations, not exact replicas. Panel terminations and the rear centre seam are still simplified; tongue/eyestay shapes and material microdetail are less faithful than the product photos. Reflection/refraction in the bladder is a Cycles preview. The 90 preview palette differs from the white/turquoise structural reference. No likeness score or user approval is claimed.

Runtime limitations: authoring meshes are intentionally detailed and have hundreds of separate objects. They need retopology/material consolidation, baked textures and LODs before mobile use. Both sides are symmetric. No character fitting, rigging, animation, Unity tests, performance profiling or EXE build was performed.

Skill validation: quick_validate.py passed after PyYAML was installed in this artifact folder only. Root and skill-copy generators are byte-identical at packaging time.

Evidence: AirMax-Review.jpg; AirMax-Elevations-Clay.jpg; AirMax-Measurements.jpg; AirMax-Small-View.jpg; AM90 and AM97 per-view PNGs; editable .blend files; .glb exports; measurement JSON and effective 1mm profile CSVs; logs; rejected-pass images and correction scripts.