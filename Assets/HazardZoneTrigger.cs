using System.Collections.Generic;
using UnityEngine;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Core
{
    /// <summary>
    /// Environmental Hazard Zone representing satellite NDVI canopy degradation,
    /// high temperature, or pollution.
    /// Can be restored and finished using Seed (বীজ) for deforested canopy zones or
    /// Cleaning Liquid (পরিষ্কারের তরল) for pollution/oil spill zones.
    /// </summary>
    public class HazardZoneTrigger : UsableObstacle
    {
        [Header("Hazard Data")]
        public string riskTier = "high";       // "low" | "medium" | "high" | "safe"
        public float rawDataValue;
        public string rawDataUnit = "NDVI";

        [Tooltip("Damage per second while the player stays inside this zone")]
        public float lowDamagePerSec = 2f;
        public float mediumDamagePerSec = 6f;
        public float highDamagePerSec = 12f;

        [Header("Version 2 Seeding Progress")]
        public int seedsPlantedInZone = 0; // Target: 5 seeds per zone

        [Header("Restoration Visuals")]
        public GameObject restoredVegetationPrefab;

        private GameObject hazardRingGo;
        private Light hazardLight;
        private bool wasPlayerInside = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            ConfigureToolRequirement();
        }

        private void Awake()
        {
            var rend = GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.enabled = false;
            }

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            ConfigureToolRequirement();
        }

        public void ConfigureToolRequirement()
        {
            string unit = (rawDataUnit ?? "").ToLowerInvariant();
            string n = (gameObject != null ? gameObject.name.ToLowerInvariant() : "");
            if (unit.Contains("ppm") || unit.Contains("oil") || unit.Contains("pollution") || unit.Contains("salinity") || unit.Contains("psu") || n.Contains("hypersaline") || n.Contains("pollution"))
            {
                requiredTool = ToolType.CleaningLiquid;
                obstacleNameEn = "Toxic Pollution / Saline Mudflat";
                obstacleDescriptionEn = "Use Cleanser to neutralize toxic pollution.";
                obstacleNameBn = "দূষিত জলভাগ / লবণাক্ত ভূমি (Pollution Zone)";
                obstacleDescription = "পরিষ্কারের তরল দিয়ে বিষাক্ত দূষণ দূর করুন।";
            }
            else
            {
                requiredTool = ToolType.Seed;
                obstacleNameEn = "Deforested Canopy Zone";
                obstacleDescriptionEn = "Plant seeds to restore mangrove vegetation.";
                obstacleNameBn = "উজাড়কৃত বনভূমি (Deforested High-Risk Zone)";
                obstacleDescription = "বীজ রোপণ করে ম্যানগ্রোভ বন পুনরুজ্জীবিত করুন।";
            }

            var col = GetComponent<Collider>();
            if (col is CapsuleCollider cc)
            {
                interactionRadius = Mathf.Max(cc.radius * transform.lossyScale.x, 22.0f);
            }
            else if (col is SphereCollider sc)
            {
                interactionRadius = Mathf.Max(sc.radius * transform.lossyScale.x, 22.0f);
            }
            else
            {
                interactionRadius = 22.0f;
            }

            EnsureHazardVisuals();
        }

        public void EnsureHazardVisuals()
        {
            // The mud must look natural — DO NOT spawn red discs, cylinders, or lights on the ground.
            // Danger zones are displayed exclusively on the NASA Landsat map radar.
            if (hazardRingGo != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(hazardRingGo);
                else Destroy(hazardRingGo);
#else
                Destroy(hazardRingGo);
#endif
                hazardRingGo = null;
            }

            // Remove ANY legacy Hazard_Ground_Ring, Hazard_Beacon_Light, or Scarred_Deforestation_Soil children
            var allChildTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildTransforms)
            {
                if (t != null && t != transform)
                {
                    string n = t.name;
                    if (n.Contains("Hazard_Ground_Ring") || n.Contains("Scarred_Deforestation_Soil") || 
                        n.Contains("Hazard_Beacon_Light") || n.Contains("Sensor_Beacon"))
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying) DestroyImmediate(t.gameObject);
                        else Destroy(t.gameObject);
#else
                        Destroy(t.gameObject);
#endif
                    }
                }
            }

            hazardLight = null;
        }

        private void Update()
        {
            if (isCleared)
            {
                if (wasPlayerInside)
                {
                    wasPlayerInside = false;
                    HudEvents.RaiseExitedHazardZone();
                }
                return;
            }

            var backpack = Backpack.LocalPlayerBackpack;
            if (backpack == null)
            {
                var playerGo = GameObject.FindWithTag("Player") ?? GameObject.Find("Tiger");
                if (playerGo != null) backpack = playerGo.GetComponent<Backpack>();
            }

            if (backpack == null) return;

            float dist = Vector3.Distance(transform.position, backpack.transform.position);
            bool isInside = (dist <= interactionRadius);

            if (isInside && !wasPlayerInside)
            {
                wasPlayerInside = true;
                HudEvents.RaiseEnteredHazardZone(riskTier, rawDataValue, rawDataUnit);
            }
            else if (!isInside && wasPlayerInside)
            {
                wasPlayerInside = false;
                HudEvents.RaiseExitedHazardZone();
            }

            if (isInside)
            {
                var health = backpack.GetComponent<SpeciesHealth>();
                if (health != null)
                {
                    float dps = riskTier switch
                    {
                        "high" => highDamagePerSec,
                        "medium" => mediumDamagePerSec,
                        "safe" => 0f,
                        _ => lowDamagePerSec
                    };
                    health.TakeDamage(dps * Time.deltaTime);
                }

                // Direct check to finish this danger zone with Seeds on F, Enter, Space, or Click
                bool actionPressed = false;
#if ENABLE_INPUT_SYSTEM
                try
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    var mouse = UnityEngine.InputSystem.Mouse.current;
                    if (kb != null && (kb.fKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) actionPressed = true;
                    if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    {
                        var es = UnityEngine.EventSystems.EventSystem.current;
                        if (es == null || !es.IsPointerOverGameObject()) actionPressed = true;
                    }
                }
                catch (System.Exception) { }
#else
                try
                {
                    if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) actionPressed = true;
                    if (Input.GetMouseButtonDown(0))
                    {
                        var es = UnityEngine.EventSystems.EventSystem.current;
                        if (es == null || !es.IsPointerOverGameObject()) actionPressed = true;
                    }
                }
                catch (System.Exception) { }
#endif

                if (actionPressed)
                {
                    TryInteractWithSeeds(backpack);
                }
            }
        }

        public bool TryInteractWithSeeds(Backpack backpack)
        {
            if (isCleared) return false;
            if (backpack == null) return false;

            if (backpack.GetCount(ToolType.Seed) > 0)
            {
                backpack.ConsumeTool(ToolType.Seed, 1);
                seedsPlantedInZone++;

                if (seedsPlantedInZone < 5)
                {
                    HudEvents.RaiseEnvironmentalRestoration($"🌱 Planted Mangrove Seed ({seedsPlantedInZone}/5 in this zone)");
                    return true;
                }
                else
                {
                    Clear(backpack);
                    return true;
                }
            }
            else
            {
                HudEvents.RaiseEnvironmentalRestoration("⚠️ Need Mangrove Seeds (বীজ) to restore this danger zone! Collect seeds from the ground.");
                return false;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (isCleared) return;

            var health = other.GetComponent<SpeciesHealth>();
            if (health == null) return;

            float dps = 0f;
            if (riskTier != null && riskTier.ToLowerInvariant().Contains("pollut"))
            {
                // V2 exact requirement: polluted water loses 5 HP/sec
                dps = 5.0f;
            }
            else
            {
                dps = riskTier switch
                {
                    "high" => highDamagePerSec,
                    "medium" => mediumDamagePerSec,
                    "safe" => 0f,
                    _ => lowDamagePerSec
                };
            }

            health.TakeDamage(dps * Time.deltaTime);
        }

        protected override void OnClearedVisuals()
        {
            // Zero out damage
            lowDamagePerSec = 0f;
            mediumDamagePerSec = 0f;
            highDamagePerSec = 0f;
            riskTier = "safe";

            HudEvents.RaiseExitedHazardZone();
            HudEvents.RaiseHazardZoneCleared(transform.position.x, transform.position.z);

            // Transform scarred mud patch to fertile moss/greenery
            var scarredSoil = transform.Find("Deforestation_Visuals/Scarred_Deforestation_Soil");
            if (scarredSoil != null)
            {
                var smr = scarredSoil.GetComponent<MeshRenderer>();
                if (smr != null)
                {
                    var fertMat = new Material(smr.sharedMaterial);
                    fertMat.color = new Color(0.18f, 0.58f, 0.24f, 0.85f);
                    smr.sharedMaterial = fertMat;
                }
            }

            var sensorLight = transform.Find("Deforestation_Visuals/Deforestation_Warning_Post/Sensor_Beacon")?.GetComponent<Light>();
            if (sensorLight != null)
            {
                sensorLight.color = new Color(0.2f, 0.95f, 0.4f);
            }

            // Reward player with health and celebratory toast
            var player = Backpack.LocalPlayerBackpack;
            if (player != null)
            {
                var health = player.GetComponent<SpeciesHealth>();
                if (health != null) health.Heal(25f);
            }

            if (GameManager.Instance != null && requiredTool == ToolType.Seed)
            {
                GameManager.Instance.zonesRestoredCount++;
                int restored = GameManager.Instance.zonesRestoredCount;
                if (restored < 3)
                {
                    HudEvents.RaiseEnvironmentalRestoration($"🌲 Deforestation Zone Reforested! ({restored}/3 Zones Restored)");
                }
                else
                {
                    HudEvents.RaiseEnvironmentalRestoration($"🎉 3/3 Deforestation Zones Restored! Reach the Ocean Sanctuary to complete Level 2!");
                }
            }
            else
            {
                HudEvents.RaiseEnvironmentalRestoration("🎉 +25 HP — DEFORESTATION ZONE RESTORED! Mangroves Reforested with Seeds!");
            }

            // Spawn restored green vegetation if Seed was used
            if (requiredTool == ToolType.Seed)
            {
                SpawnRestoredVegetation();
            }
        }

        private void SpawnRestoredVegetation()
        {
            var parent = new GameObject("Restored_Green_Vegetation").transform;
            parent.SetParent(transform, true);
            parent.position = transform.position;

            // Spawn green grass and mangrove saplings across the restored clearing
            int count = 14;
            float radius = 8f;
            var col = GetComponent<SphereCollider>();
            if (col != null) radius = Mathf.Max(6f, col.radius * transform.lossyScale.x * 0.7f);

            for (int i = 0; i < count; i++)
            {
                float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float d = Mathf.Sqrt(UnityEngine.Random.Range(0.05f, 0.95f)) * radius;
                Vector3 pos = transform.position + new Vector3(Mathf.Cos(a) * d, 0.05f, Mathf.Sin(a) * d);

                var sapling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sapling.name = $"Restored_Sapling_{i + 1}";
                sapling.transform.SetParent(parent, true);
                sapling.transform.position = pos;
                sapling.transform.localScale = new Vector3(0.6f, UnityEngine.Random.Range(0.8f, 1.6f), 0.6f);

                var colSapling = sapling.GetComponent<Collider>();
                if (colSapling != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(colSapling);
                    else Destroy(colSapling);
#else
                    Destroy(colSapling);
#endif
                }

                var mr = sapling.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    m.color = new Color(0.18f, 0.82f, 0.32f, 1.0f); // vibrant emerald regrowth
                    m.SetFloat("_Smoothness", 0.4f);
                    mr.sharedMaterial = m;
                }
            }
        }
    }
}
