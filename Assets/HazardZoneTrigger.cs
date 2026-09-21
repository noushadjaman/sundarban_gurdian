using System.Collections.Generic;
using UnityEngine;
using SundarbanGuardian.Environment;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Core
{
    /// <summary>
    /// Environmental Hazard Zone representing satellite NDVI canopy degradation,
    /// high temperature, or pollution.
    /// Can be restored and made safe using Seed (বীজ) for deforested zones or
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

        [Header("Restoration Visuals")]
        public GameObject restoredVegetationPrefab;

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
            if (col is SphereCollider sc)
            {
                interactionRadius = Mathf.Max(sc.radius * transform.lossyScale.x, 6.0f);
            }
            else
            {
                interactionRadius = 7.0f;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (isCleared) return;

            var health = other.GetComponent<SpeciesHealth>();
            if (health == null) return;

            float dps = riskTier switch
            {
                "high" => highDamagePerSec,
                "medium" => mediumDamagePerSec,
                "safe" => 0f,
                _ => lowDamagePerSec
            };

            health.TakeDamage(dps * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCleared) return;
            if (other.GetComponent<SpeciesHealth>() == null) return;

            HudEvents.RaiseEnteredHazardZone(riskTier, rawDataValue, rawDataUnit);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SpeciesHealth>() == null) return;
            HudEvents.RaiseExitedHazardZone();
        }

        protected override void OnClearedVisuals()
        {
            // Zero out damage
            lowDamagePerSec = 0f;
            mediumDamagePerSec = 0f;
            highDamagePerSec = 0f;
            riskTier = "safe";

            HudEvents.RaiseExitedHazardZone();

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
                if (colSapling != null) Destroy(colSapling);

                var mr = sapling.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.color = new Color(0.18f, 0.82f, 0.32f, 1.0f); // vibrant emerald regrowth
                    m.SetFloat("_Smoothness", 0.4f);
                    mr.sharedMaterial = m;
                }
            }
        }
    }
}
