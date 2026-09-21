using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SundarbanGuardian.Data;

namespace SundarbanGuardian.Core
{
    /// Reads a level_<species>_<id>.json from StreamingAssets/Levels/
    /// and instantiates a HazardZoneTrigger prefab for each zone, colored
    /// by risk tier. Works for Tiger, Deer, and Gangetic Dolphin levels.
    /// (Hilsha uses HilshaMigrationController instead — see that file.)
    public class LevelLoader : MonoBehaviour
    {
        [Header("Assign in Inspector")]
        public GameObject hazardZonePrefab;   // a trigger collider + colored decal
        public GameObject startMarkerPrefab;
        public GameObject goalMarkerPrefab;

        [Header("Risk tier colors")]
        public Color lowRiskColor = new Color(1f, 0.92f, 0.23f, 0.5f);   // yellow, translucent
        public Color mediumRiskColor = new Color(1f, 0.6f, 0f, 0.55f);  // orange
        public Color highRiskColor = new Color(0.9f, 0.1f, 0.1f, 0.6f); // red

        public LevelData CurrentLevel { get; private set; }

        /// Call this from GameManager with the file name, e.g. "level_tiger_01"
        public LevelData LoadLevel(string levelFileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Levels", levelFileName + ".json");
            if (!File.Exists(path))
            {
                Debug.LogError($"[LevelLoader] Level file not found: {path}");
                return null;
            }

            string json = File.ReadAllText(path);
            LevelData data = JsonUtility.FromJson<LevelData>(json);
            CurrentLevel = data;

            SpawnZones(data);
            return data;
        }

        private void SpawnZones(LevelData data)
        {
            if (data.hazardZones == null) return;

            foreach (HazardZone zone in data.hazardZones)
            {
                Vector3 pos = new Vector3(zone.centerX, 0.05f, zone.centerZ);
                GameObject go = Instantiate(hazardZonePrefab, pos, Quaternion.identity, transform);
                go.transform.localScale = new Vector3(zone.radius * 2f, 0.1f, zone.radius * 2f);

                var trigger = go.GetComponent<HazardZoneTrigger>();
                if (trigger != null)
                {
                    trigger.riskTier = zone.riskTier;
                    trigger.rawDataValue = zone.rawDataValue;
                    trigger.rawDataUnit = zone.rawDataUnit;
                }

                var renderer = go.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = zone.riskTier switch
                    {
                        "high" => highRiskColor,
                        "medium" => mediumRiskColor,
                        _ => lowRiskColor
                    };
                }
            }

            if (startMarkerPrefab != null && data.startZone != null)
                Instantiate(startMarkerPrefab, new Vector3(data.startZone.centerX, 0f, data.startZone.centerZ), Quaternion.identity);

            if (goalMarkerPrefab != null && data.goalZone != null)
                Instantiate(goalMarkerPrefab, new Vector3(data.goalZone.centerX, 0f, data.goalZone.centerZ), Quaternion.identity);
        }
    }
}
