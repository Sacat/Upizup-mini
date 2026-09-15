# Grand Bay — Full Settlement Expansion — Sources

Record every dataset/image with source ID, publisher, retrieval date, bounds, licence, attribution, redistribution limits and exact use. Commercial satellite imagery is reference-only unless its licence explicitly permits production use.

This review reuses the existing dated source package; no new geographical download or satellite image import was performed.

- OpenStreetMap via Overpass, extracted 2026-08-20. Bounds south 15.236, west -61.326, north 15.252, east -61.306. ODbL; attribution: Map data from OpenStreetMap, https://www.openstreetmap.org/copyright. Source file: Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json.
- Copernicus DEM GLO-30 2021, same crop, 65 by 65 sample grid. Source file: Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json. Attribution and limitations are retained in Docs/MAP-SOURCE-NOTES.md. Grading is an artistic gameplay adaptation of the terrain.
- MINI142 approved local house meshes and Palette.mat are reused unchanged. Their source history is in Docs/CLAUDE-HOUSES-MAP-COMPLETE-HANDOFF-MINI-142.md.
- School centroid derives from the existing OSM anchor. Campus wings, footprint, windows, courtyard, entrance lane and new house lots are artistic approximations. Do not present them as surveyed buildings or a precise school replica.

## Geneva revision source addendum — 2026-09-15

GENEVA-SOURCE.json is an extraction from the retained MINI-094 OpenStreetMap snapshot (ODbL), not a new survey. Features:22917921 bay road,440104499 roundabout,440101884 coastal Berekua road,548578022 school/Grand Bay road,142337516 Geneva Playing Field sports-centre boundary. The school-facing direction follows the user's correction. Existing GLO30-derived terrain is retained; the added eastern strip extrapolates its boundary and is artistically graded rather than newly DEM-sampled. See GENEVA-WORKFLOW.md for measured versus artistic dimensions. No aerial/satellite pixels or external 3D data imported.
