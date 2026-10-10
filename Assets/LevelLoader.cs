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
            string json = null;

            // 1. Check Resources first (instant, 100% reliable on Android APK, iOS, WebGL, Editor)
            var textAsset = Resources.Load<TextAsset>($"Levels/{levelFileName}");
            if (textAsset != null)
            {
                json = textAsset.text;
            }

            // 2. Fallback to StreamingAssets
            if (string.IsNullOrEmpty(json))
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Levels", levelFileName + ".json");
                if (Application.platform == RuntimePlatform.Android)
                {
                    using (var uwr = UnityEngine.Networking.UnityWebRequest.Get(path))
                    {
                        uwr.SendWebRequest();
                        while (!uwr.isDone) { }
                        if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        {
                            json = uwr.downloadHandler.text;
                        }
                    }
                }
                else if (File.Exists(path))
                {
                    json = File.ReadAllText(path);
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[LevelLoader] Level file not found: {levelFileName}");
                return null;
            }

            LevelData data = JsonUtility.FromJson<LevelData>(json);
            CurrentLevel = data;

            SpawnZones(data);
            return data;
        }

        private List<GameObject> spawnedRuntimeObjects = new List<GameObject>();

        public void ClearSpawnedZones()
        {
            foreach (var go in spawnedRuntimeObjects)
            {
                if (go != null)
                {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }
            spawnedRuntimeObjects.Clear();

            // Clear any lingering cloned markers under LevelLoader transform
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private void SpawnZones(LevelData data)
        {
            ClearSpawnedZones();

            var existingDangerRoot = GameObject.Find("[Danger_Zones]");
            if (existingDangerRoot != null)
            {
                // Only activate handcrafted danger zones for Tiger round
                bool isTiger = (data.speciesId != null && data.speciesId.ToLower().Contains("tiger"));
                existingDangerRoot.SetActive(isTiger);
                if (isTiger && existingDangerRoot.transform.childCount > 0)
                {
                    Debug.Log("[LevelLoader] Using existing handcrafted [Danger_Zones] for Tiger.");
                    if (startMarkerPrefab != null && data.startZone != null)
                    {
                        var sm = Instantiate(startMarkerPrefab, new Vector3(data.startZone.centerX, 0f, data.startZone.centerZ), Quaternion.identity, transform);
                        spawnedRuntimeObjects.Add(sm);
                    }
                    if (goalMarkerPrefab != null && data.goalZone != null)
                    {
                        var gm = Instantiate(goalMarkerPrefab, new Vector3(data.goalZone.centerX, 0f, data.goalZone.centerZ), Quaternion.identity, transform);
                        spawnedRuntimeObjects.Add(gm);
                    }
                    return;
                }
            }

            if (data.hazardZones != null && hazardZonePrefab != null)
            {
                foreach (HazardZone zone in data.hazardZones)
                {
                    Vector3 pos = new Vector3(zone.centerX, 0.05f, zone.centerZ);
                    GameObject go = Instantiate(hazardZonePrefab, pos, Quaternion.identity, transform);
                    go.transform.localScale = new Vector3(zone.radius * 2f, 0.1f, zone.radius * 2f);
                    spawnedRuntimeObjects.Add(go);

                    var trigger = go.GetComponent<HazardZoneTrigger>();
                    if (trigger != null)
                    {
                        trigger.riskTier = zone.riskTier;
                        trigger.rawDataValue = zone.rawDataValue;
                        trigger.rawDataUnit = zone.rawDataUnit;
                    }

                    // Hide flat red ground discs — mud ground remains 100% natural, danger zones shown on minimap radar
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        r.enabled = false;
                    }
                }
            }

            if (startMarkerPrefab != null && data.startZone != null)
            {
                var sm = Instantiate(startMarkerPrefab, new Vector3(data.startZone.centerX, 0f, data.startZone.centerZ), Quaternion.identity, transform);
                spawnedRuntimeObjects.Add(sm);
            }

            if (goalMarkerPrefab != null && data.goalZone != null)
            {
                var gm = Instantiate(goalMarkerPrefab, new Vector3(data.goalZone.centerX, 0f, data.goalZone.centerZ), Quaternion.identity, transform);
                spawnedRuntimeObjects.Add(gm);
            }
        }
    }
}
