"""Create a compact Grand Bay height grid from the public Copernicus GLO-30 COG tile."""

from __future__ import annotations

import json
import math
import urllib.request
from pathlib import Path

import numpy as np
from PIL import Image


PROJECT = Path(__file__).resolve().parents[2]
TASK_DIR = PROJECT / "Logs" / "Tasks" / "MINI-095"
SOURCE_NAME = "Copernicus_DSM_COG_10_N15_00_W062_00_DEM.tif"
SOURCE_URL = (
    "https://copernicus-dem-30m.s3.amazonaws.com/"
    "Copernicus_DSM_COG_10_N15_00_W062_00_DEM/"
    + SOURCE_NAME
)
SOURCE_PATH = TASK_DIR / SOURCE_NAME
OUTPUT_PATH = PROJECT / "Assets" / "UpIzUpMini" / "Maps" / "GrandBayPhase1Height.json"
PREVIEW_PATH = TASK_DIR / "GrandBayPhase1-DEM-Hillshade.png"

SOUTH, WEST, NORTH, EAST = 15.236, -61.326, 15.252, -61.306
ORIGIN_LAT, ORIGIN_LON = 15.2450638, -61.3181049
GRID_SIZE = 65
HORIZONTAL_COMPRESSION = 1.0 / 3.0
VERTICAL_COMPRESSION = 1.0 / 3.0


def bilinear(array: np.ndarray, x: float, y: float) -> float:
    height, width = array.shape
    x = min(max(x, 0.0), width - 1.001)
    y = min(max(y, 0.0), height - 1.001)
    x0, y0 = int(math.floor(x)), int(math.floor(y))
    x1, y1 = min(x0 + 1, width - 1), min(y0 + 1, height - 1)
    tx, ty = x - x0, y - y0
    top = array[y0, x0] * (1.0 - tx) + array[y0, x1] * tx
    bottom = array[y1, x0] * (1.0 - tx) + array[y1, x1] * tx
    return float(top * (1.0 - ty) + bottom * ty)


def main() -> None:
    TASK_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    if not SOURCE_PATH.exists():
        request = urllib.request.Request(SOURCE_URL, headers={"User-Agent": "UpIzUpMiniTerrain/1.0"})
        with urllib.request.urlopen(request) as response, SOURCE_PATH.open("wb") as output:
            output.write(response.read())

    with Image.open(SOURCE_PATH) as image:
        source = np.asarray(image, dtype=np.float32)
    if source.ndim != 2:
        source = source[..., 0]
    source_height, source_width = source.shape

    grid = np.zeros((GRID_SIZE, GRID_SIZE), dtype=np.float32)
    for z in range(GRID_SIZE):
        latitude = SOUTH + (NORTH - SOUTH) * z / (GRID_SIZE - 1)
        pixel_y = (16.0 - latitude) * (source_height - 1)
        for x in range(GRID_SIZE):
            longitude = WEST + (EAST - WEST) * x / (GRID_SIZE - 1)
            pixel_x = (longitude + 62.0) * (source_width - 1)
            grid[z, x] = bilinear(source, pixel_x, pixel_y)

    smoothed = grid.copy()
    smoothed[1:-1, 1:-1] = (
        grid[1:-1, 1:-1] * 4.0
        + grid[:-2, 1:-1]
        + grid[2:, 1:-1]
        + grid[1:-1, :-2]
        + grid[1:-1, 2:]
    ) / 8.0
    grid = smoothed

    min_elevation = float(np.nanmin(grid))
    max_elevation = float(np.nanmax(grid))
    document = {
        "schemaVersion": 1,
        "source": "Copernicus DEM GLO-30 Public, 2021 release",
        "sourceUrl": SOURCE_URL,
        "sourceTile": SOURCE_NAME,
        "sourceResolutionMetres": 30,
        "sourceAttribution": (
            "produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and "
            "© Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS "
            "by the European Union and ESA; all rights reserved"
        ),
        "south": SOUTH,
        "west": WEST,
        "north": NORTH,
        "east": EAST,
        "originLatitude": ORIGIN_LAT,
        "originLongitude": ORIGIN_LON,
        "width": GRID_SIZE,
        "height": GRID_SIZE,
        "minElevationMetres": round(min_elevation, 3),
        "maxElevationMetres": round(max_elevation, 3),
        "horizontalCompression": HORIZONTAL_COMPRESSION,
        "verticalCompression": VERTICAL_COMPRESSION,
        "smoothing": "one 4-neighbour weighted pass; road-local grading remains a later layer",
        "samplesSouthToNorth": [round(float(value), 3) for value in grid.reshape(-1)],
    }
    OUTPUT_PATH.write_text(json.dumps(document, indent=2), encoding="utf-8")

    dz, dx = np.gradient(grid)
    slope = np.hypot(dx, dz)
    shade = np.clip(190.0 - dx * 5.0 + dz * 4.0 - slope * 1.5, 25.0, 245.0).astype(np.uint8)
    Image.fromarray(shade, mode="L").resize((780, 780), Image.Resampling.BICUBIC).save(PREVIEW_PATH)

    print(json.dumps({
        "sourceSize": [source_width, source_height],
        "gridSize": [GRID_SIZE, GRID_SIZE],
        "minElevationMetres": min_elevation,
        "maxElevationMetres": max_elevation,
        "output": str(OUTPUT_PATH),
        "preview": str(PREVIEW_PATH),
    }, indent=2))


if __name__ == "__main__":
    main()
