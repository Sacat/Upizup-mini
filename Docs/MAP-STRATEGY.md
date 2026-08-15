# Up Iz Up Mini — Map Strategy

## Geographic model

Use verified latitude/longitude anchors as source data, then convert them to a local Unity coordinate system. Preserve landmark relationships while compressing long travel distances.

The first connected gameplay area is:

```text
Lalay road → market/institutions → Montine turnoff → dirty farm trail → farm clearing/safehouse
```

## Scene plan

- `GrandBayProof`: temporary vertical-slice scene.
- `LalaySouth`: homes, starting safehouse, and first contacts.
- `LalayMarket`: vendors, buyer, police presence, and traffic.
- `MontineRoute`: rough track, vegetation, slopes, and isolation.
- `MontineFarms`: farming land, later land purchases, and farm safehouse.
- `DominicaTravelMap`: island overview used for future district selection.

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

