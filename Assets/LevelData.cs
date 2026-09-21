using System;
using System.Collections.Generic;

namespace SundarbanGuardian.Data
{
    // ── Root JSON structure for one level ──────────────────────────────
    // Each level file (e.g. level_tiger_01.json) describes ONE real coordinate
    // and the satellite-derived hazard zones inside it.
    [Serializable]
    public class LevelData
    {
        public string levelId;
        public string speciesId;          // "tiger" | "deer" | "gangetic_dolphin" | "hilsha"
        public string realCoordinateName; // e.g. "Katka Range, Sundarbans"
        public float latitude;
        public float longitude;
        public string dataSource;         // e.g. "MODIS NDVI, Oct 2025 composite"
        public string factTextBn;         // fallback Bengali fact text if LLM call fails
        public List<HazardZone> hazardZones;
        public SafeZone startZone;
        public SafeZone goalZone;
    }

    [Serializable]
    public class HazardZone
    {
        public string zoneId;
        public float centerX;
        public float centerZ;
        public float radius;
        public string riskTier;     // "low" | "medium" | "high"
        public float rawDataValue;  // the actual NDVI/salinity/SST reading behind this zone
        public string rawDataUnit;  // e.g. "PSU" (salinity), "°C" (SST), "NDVI index"
    }

    [Serializable]
    public class SafeZone
    {
        public float centerX;
        public float centerZ;
        public float radius;
    }

    // ── Salinity/temperature corridor for the Hilsha migration level ───
    // Distinct from HazardZone because the Hilsha level isn't "avoid a
    // circle", it's "stay inside a moving safe band as you swim upstream".
    [Serializable]
    public class MigrationCorridorPoint
    {
        public float distanceFromMouthKm; // 0 = Bay of Bengal mouth, increasing upstream
        public string location;
        public float minSafeSalinityPSU;
        public float maxSafeSalinityPSU;
        public float actualSalinityPSU;   // real SMAP / BFRI reading at this point
        public string salinityDataSource;
        public float actualTemperatureC;  // real MODIS LST reading at this point
        public string temperatureDataSource;
    }

    [Serializable]
    public class HilshaLevelData
    {
        public string levelId;
        public string dataSource; // e.g. "SMAP salinity + MODIS SST, Bay of Bengal to Chandpur transect"
        public List<MigrationCorridorPoint> corridor;
        public string factTextBn;
    }
}
