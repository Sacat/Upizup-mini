# Up Iz Up Mini — Map Strategy

## Geographic model

Use verified latitude/longitude anchors as source data, then convert them to a local Unity coordinate system. Preserve landmark relationships while compressing long travel distances.

The first connected gameplay area is:

```text
Lalay road → market/institutions → Highland inroad → dirty farm trail → farm clearing/safehouse
```

## Scene plan

- `GrandBayProof`: temporary vertical-slice scene.
- `LalaySouth`: homes, starting safehouse, and first contacts.
- `LalayMarket`: vendors, buyer, police presence, and traffic.
- `HighlandRoute`: rough track, vegetation, gentle drivable grades, and isolation.
- `HighlandFarms`: farming land, later land purchases, and farm safehouse.
- `DominicaTravelMap`: island overview used for future district selection.

## Phase-one road-system rule

- Preserve the approved route coordinates and compressed scale; spline tooling changes road construction, not map truth.
- Use one line-free 6.2m two-vehicle paved class across retained phase-one roads, with the Highland farm spur as a 4.8m dirt class.
- Limit generated sidewalks to Lalay's main street. Highland, Backstreet, farm and supporting secondary routes use shoulders/terrain instead.
- Every spline road requires matching mesh collision and an inactive copy of its approved predecessor until live driving is accepted.
- Bridge approaches overlap the deck at both ends and grade from authoritative road/terrain colliders with a maximum 8-degree crossfall.

## Data requirements

Create `Docs/MAP-ANCHORS.json` only from verified coordinates. Each record should contain:

```json
{
  "id": "stable_name",
  "displayName": "Place Name",
  "latitude": 0.0,
  "longitude": 0.0,
  "category": "road|school|market|institution|district|farm|coast",
  "source": "reference description or URL",
  "verified": false
}
```

Do not invent exact coordinates. Approximate artistic placement must be marked as approximate.
