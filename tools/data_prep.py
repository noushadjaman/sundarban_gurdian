"""
data_prep.py — pulls real satellite readings and writes them into the
level JSON files' placeholder fields (rawDataValue / actualSalinityPSU /
actualTemperatureC / actualSalinityPSU).

Run this ONCE before the hackathon (or the morning of) to replace the
placeholder numbers in Assets/StreamingAssets/Levels/*.json with real data.

Requires: pip install earthaccess numpy xarray

You'll need a free NASA Earthdata login: https://urs.earthdata.nasa.gov/
"""

import json
import os

import earthaccess  # https://github.com/nsidc/earthaccess

LEVELS_DIR = os.path.join("Assets", "StreamingAssets", "Levels")

# Real coordinates used across the game's levels — edit/extend as needed.
COORDINATES = {
    "tiger_katka": (21.87, 89.75),
    "hilsha_mouth": (21.60, 91.05),      # Bay of Bengal mouth, near Meghna estuary
    "hilsha_chandpur": (23.23, 90.66),   # upstream Meghna-Padma confluence area
}


def authenticate():
    # Reads EARTHDATA_USERNAME / EARTHDATA_PASSWORD from environment,
    # or prompts interactively if not set.
    earthaccess.login(strategy="environment")


def fetch_ndvi(lat: float, lon: float) -> float:
    """
    Placeholder — replace with an actual MODIS NDVI granule search + read.
    Example approach:
        results = earthaccess.search_data(
            short_name="MOD13Q1",
            bounding_box=(lon - 0.05, lat - 0.05, lon + 0.05, lat + 0.05),
            temporal=("2025-01-01", "2025-12-31"),
        )
        files = earthaccess.download(results[:1], "downloads/")
        # then open with xarray / rasterio and sample the pixel at (lat, lon)
    """
    raise NotImplementedError("Wire this up to an actual MOD13Q1 granule read")


def fetch_smap_salinity(lat: float, lon: float) -> float:
    """Placeholder — SMAP L3 sea surface salinity granule read."""
    raise NotImplementedError("Wire this up to an actual SMAP salinity granule read")


def fetch_modis_sst(lat: float, lon: float) -> float:
    """Placeholder — MODIS/Aqua SST granule read."""
    raise NotImplementedError("Wire this up to an actual MODIS SST granule read")


def update_tiger_level():
    path = os.path.join(LEVELS_DIR, "level_tiger_01.json")
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)

    lat, lon = COORDINATES["tiger_katka"]
    ndvi = fetch_ndvi(lat, lon)  # NotImplementedError until wired up

    for zone in data["hazardZones"]:
        zone["rawDataValue"] = ndvi
        zone["riskTier"] = "high" if ndvi < 0.25 else "medium" if ndvi < 0.4 else "low"

    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)


def update_hilsha_level():
    path = os.path.join(LEVELS_DIR, "level_hilsha_bongoposagor_to_meghna.json")
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)

    for point in data["corridor"]:
        # NOTE: real implementation should interpolate salinity/SST along
        # the actual river transect at this distanceFromMouthKm, not reuse
        # a single coordinate — this is a simplification for the stub.
        lat, lon = COORDINATES["hilsha_mouth"]
        point["actualSalinityPSU"] = fetch_smap_salinity(lat, lon)
        point["actualTemperatureC"] = fetch_modis_sst(lat, lon)

    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    authenticate()
    update_tiger_level()
    update_hilsha_level()
    print("Levels updated with real satellite data.")
