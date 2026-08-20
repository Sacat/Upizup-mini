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

## Phase-one elevation

- Dataset: Copernicus DEM GLO-30, 2021 release, public COG tile covering Grand Bay.
- Crop: south `15.236`, west `-61.326`, north `15.252`, east `-61.306`.
- Production sample: smoothed `65 × 65` grid in `Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json`.
- Use: the surrounding terrain follows the DEM. The approved Lalay road/sidewalk/yard corridor is gameplay-graded to a continuous maximum `1.5%` slope with no DEM bumps and a wide yard transition; Highland retains the stronger local relief.
- Limitation: GLO-30 is a surface model and cannot resolve individual drains, retaining walls, steps, or exact road crowns.
- Required attribution: `produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and © Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved`.

## User-confirmed map truth (2026-08-20)

- The MINI-094 OSM preview is the approved phase-one road-network reference.
- The original Lalay street screenshot supplied back by the user is the approved road-scale target: narrow two-lane asphalt, grey sidewalks, close houses, and a bay-facing view.
- Highland is the first planting district, reached by an inroad from Lalay; one plot starts active and three parcels are reserved for later progression.

## Lower bay and church correction (MINI-097)

- The user confirmed that the lower beach/jetty strip contains no houses and that the church is the only building to retain there in this graybox.
- The church uses the existing `grand_bay_catholic_church` anchor derived from OSM way `392195638`, cross-checked against public Church of St. Patrick place records.
- User-provided satellite imagery is used only to judge land use and relative density. Pixels and individual building footprints are not traced, imported, or shipped.
