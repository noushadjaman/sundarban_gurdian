using System;
using System.Collections.Generic;
using UnityEngine;
using SundarbanGuardian.Core;
using SundarbanGuardian.Inventory;

namespace SundarbanGuardian.Environment
{
    /// <summary>
    /// Interactive Environmental Hazard Zone (NDVI Deforested Canopy Loss).
    /// Drains character HP over time while inside. The player must survive and finish the zone
    /// by planting Seeds (বীজ). Finishing the zone neutralizes damage, sprouts green mangrove trees,
    /// heals the player +25 HP, and displays a celebration banner.
    /// </summary>
    public class WorldDangerZone : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        public string zoneName = "Deforested Canopy Zone";
        public string riskTier = "high"; // "low" | "medium" | "high" | "safe"
        public float rawDataValue = 0.18f;
        public string rawDataUnit = "NDVI";
        public float damagePerSecond = 8.0f;
        public float interactionRadius = 18.0f;

        [Header("Restoration Status")]
        public bool isFinished = false;
        public int seedsPlantedInZone = 0;

        private GameObject ringVisual;
        private Light beaconLight;
        private bool playerInside = false;

        private void Awake()
        {
            EnsureVisuals();
            EnsureTriggerCollider();
        }

        private void Start()
        {
            EnsureVisuals();
        }

        private GameObject FindActivePlayer()
        {
            var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if (gm != null && gm.playerObject != null && gm.playerObject.activeInHierarchy)
            {
                return gm.playerObject;
            }

            string[] names = { "Deer", "Tiger", "Dolphin", "Hilsha" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null && go.activeInHierarchy) return go;
            }
            return GameObject.FindWithTag("Player");
        }

        private void Update()
        {
            if (isFinished) return;

            var player = FindActivePlayer();
            if (player == null || !player.activeInHierarchy) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);

            // 1. Damage check (proximity or trigger)
            if (dist <= interactionRadius)
            {
                if (!playerInside)
                {
                    playerInside = true;
                    HudEvents.RaiseEnteredHazardZone(riskTier, rawDataValue, rawDataUnit);
                }

                var health = player.GetComponent<SpeciesHealth>();
                if (health != null && health.currentHealth > 0f)
                {
                    health.TakeDamage(damagePerSecond * Time.deltaTime);
                }

                // 2. Check Seed planting input
                CheckSeedFinishInput(player);
            }
            else if (playerInside)
            {
                playerInside = false;
                HudEvents.RaiseExitedHazardZone();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (isFinished) return;
            var health = other.GetComponent<SpeciesHealth>();
            if (health != null && health.currentHealth > 0f)
            {
                health.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isFinished) return;
            if (other.name.Contains("Tiger") || other.name.Contains("Deer") || other.CompareTag("Player"))
            {
                playerInside = true;
                HudEvents.RaiseEnteredHazardZone(riskTier, rawDataValue, rawDataUnit);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.name.Contains("Tiger") || other.name.Contains("Deer") || other.CompareTag("Player"))
            {
                playerInside = false;
                HudEvents.RaiseExitedHazardZone();
            }
        }

        private void CheckSeedFinishInput(GameObject player)
        {
            // Plant seeds with F, Enter, Left-Click, or Space
            bool plantPressed = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (kb != null && (kb.fKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) plantPressed = true;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (es == null || !es.IsPointerOverGameObject()) plantPressed = true;
                }
            }
            catch (Exception) { }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) plantPressed = true;
                if (Input.GetMouseButtonDown(0))
                {
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (es == null || !es.IsPointerOverGameObject()) plantPressed = true;
                }
            }
            catch (Exception) { }
#endif

            if (plantPressed)
            {
                TryFinishWithSeeds(player);
            }
        }

        public bool TryFinishWithSeeds(GameObject player)
        {
            if (isFinished) return false;

            var backpack = player.GetComponent<Backpack>() ?? player.GetComponentInParent<Backpack>();
            if (backpack != null)
            {
                int seeds = backpack.GetCount(ToolType.Seed);
                if (seeds > 0)
                {
                    backpack.ConsumeTool(ToolType.Seed, 1);
                    seedsPlantedInZone++;

                    if (seedsPlantedInZone < 5)
                    {
                        HudEvents.RaiseEnvironmentalRestoration($"🌱 Planted Mangrove Seed ({seedsPlantedInZone}/5) in Deforestation Zone");
                        return true;
                    }
                    else
                    {
                        FinishZone(player);
                        return true;
                    }
                }
                else
                {
                    HudEvents.RaiseEnvironmentalRestoration("⚠️ Need Mangrove Seeds (বীজ) to restore this danger zone! Find seeds in the forest.");
                    return false;
                }
            }
            else
            {
                // Fallback if backpack not found
                FinishZone(player);
                return true;
            }
        }

        public void FinishZone(GameObject player)
        {
            if (isFinished) return;
            isFinished = true;
            damagePerSecond = 0f;
            riskTier = "safe";

            HudEvents.RaiseExitedHazardZone();
            HudEvents.RaiseHazardZoneCleared(transform.position.x, transform.position.z);

            // 1. Turn ground ring to vibrant emerald green
            if (ringVisual != null)
            {
                var mr = ringVisual.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    var gMat = new Material(litShader);
                    gMat.color = new Color(0.18f, 0.88f, 0.38f, 0.45f);
                    mr.sharedMaterial = gMat;
                }
            }

            // 2. Turn beacon light emerald green
            if (beaconLight != null)
            {
                beaconLight.color = new Color(0.2f, 0.95f, 0.4f);
                beaconLight.intensity = 2.2f;
            }

            // 3. Heal player +25 HP
            if (player != null)
            {
                var health = player.GetComponent<SpeciesHealth>();
                if (health != null) health.Heal(25f);
            }

            // 4. Celebration Toast & Progress
            if (GameManager.Instance != null)
            {
                GameManager.Instance.zonesRestoredCount++;
                int total = GameManager.Instance.zonesRestoredCount;
                if (total < 3)
                {
                    HudEvents.RaiseEnvironmentalRestoration($"🌲 Deforestation Zone Reforested! ({total}/3 Zones Restored)");
                }
                else
                {
                    HudEvents.RaiseEnvironmentalRestoration($"🎉 3/3 Deforestation Zones Restored! Reach the Ocean Sanctuary to complete Level 2!");
                }
            }
            else
            {
                HudEvents.RaiseEnvironmentalRestoration("🎉 +25 HP — DANGER ZONE FINISHED! Reforested with Seeds!");
            }
            Debug.Log($"[WorldDangerZone] Successfully finished danger zone: {zoneName} with 5 Seeds!");

            // 5. Spawn 14 lush green mangrove saplings
            SpawnRestoredMangroveGrove();
        }

        private void SpawnRestoredMangroveGrove()
        {
            var groveRoot = new GameObject("Restored_Mangrove_Grove");
            groveRoot.transform.SetParent(transform, true);
            groveRoot.transform.position = transform.position;

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var treeMat = new Material(litShader);
            treeMat.color = new Color(0.15f, 0.78f, 0.30f);

            int treeCount = 14;
            float spawnRadius = Mathf.Max(8f, interactionRadius * 0.65f);

            for (int i = 0; i < treeCount; i++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float dist = Mathf.Sqrt(UnityEngine.Random.Range(0.08f, 0.95f)) * spawnRadius;
                Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                RaycastHit hit;
                if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                {
                    pos.y = hit.point.y;
                }

                var tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tree.name = $"Mangrove_Sapling_{i + 1}";
                tree.transform.SetParent(groveRoot.transform, true);
                tree.transform.position = pos + Vector3.up * 0.8f;
                tree.transform.localScale = new Vector3(0.55f, UnityEngine.Random.Range(1.0f, 1.8f), 0.55f);
                Destroy(tree.GetComponent<Collider>());
                tree.GetComponent<MeshRenderer>().sharedMaterial = treeMat;
            }
        }

        public void EnsureVisuals()
        {
            // Terrain mud must look natural — DO NOT spawn red discs or beacon lights.
            // Danger zones are shown exclusively on the minimap radar.
            var allChildTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildTransforms)
            {
                if (t != null && t != transform)
                {
                    string n = t.name;
                    if (n.Contains("Hazard_Ground_Ring") || n.Contains("Hazard_Beacon_Light"))
                    {
                        if (Application.isPlaying) Destroy(t.gameObject);
                        else DestroyImmediate(t.gameObject);
                    }
                }
            }
            ringVisual = null;
            beaconLight = null;
        }

        public void EnsureTriggerCollider()
        {
            var col = GetComponent<CapsuleCollider>();
            if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = interactionRadius;
            col.height = 14f;
            col.center = new Vector3(0f, 6f, 0f);
        }
    }
}
