# Grand Bay Map Source Notes

## Phase-one source

- Dataset: OpenStreetMap raw geodata through the public Overpass API.
- Extract date: 2026-08-20.
- Bounding box: south `15.236`, west `-61.326`, north `15.252`, east `-61.306`.
- Intended use: road, waterway, coastline, and existing named-feature truth for the Lalay-to-beach planning preview.
- Licence: Open Database License (ODbL). The game must visibly credit OpenStreetMap and provide detailed source/licence information in a suitable credits or legal location.
- Attribution target: `Map data from OpenStreetMap`, linked to `https://www.openstreetmap.org/copyright` where links are available.

## Coordinate convention

- WGS84 source coordinates.
- Preview origin: `15.2450638, -61.3181049` at Unity `(X=0, Z=0)`.
- Positive Unity X is east; positive Unity Z is north.
- One Unity unit is one metre.
- The preview uses the documented short-distance approximation: longitude delta × `107,500` metres; latitude delta × `110,650` metres.
- A proper projected GIS master remains required before final terrain or large-distance placement.

## Trust levels

- `osm_mapped_local_confirmation_pending`: directly present in the dated OSM extract, but current entrance, local meaning, or visual importance still needs resident confirmation.
- `official_institution_candidate_coordinate`: the institution is supported by an official source, while the coordinate came from prior place research and needs a local pin check.
- `candidate_coordinate`: useful for planning only; do not lock or model it as exact before local confirmation.

## Explicit gaps

- Resident-defined start and end of Lalay.
- Highland road turnoff and first planting clearing.
- Exact jetty/boat landing used by the story.
- Current Grand Bay Village Council office entrance.
- The user-supplied whole-island heightmap has no confirmed bounds, vertical scale, CRS, or source licence and is not used as production elevation truth.

The Google map screenshot remains an internal visual reference only. It must not be traced, imported, or shipped as terrain imagery.
